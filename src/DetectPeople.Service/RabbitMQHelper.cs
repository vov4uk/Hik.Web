using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Linq;
using System.Text;

namespace DetectPeople.Service
{
    public class RabbitMQHelper : IDisposable
    {
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly EventingBasicConsumer consumer;
        private readonly string queueName;
        private readonly string routingKey;

        public event EventHandler<BasicDeliverEventArgs> Received
        {
            add { consumer.Received += value; }
            remove { consumer.Received -= value; }
        }

        public RabbitMQHelper(string hostName, string queueName, string routingKey)
        {
            this.queueName = queueName;
            this.routingKey = routingKey;
            ConnectionFactory factory = new ConnectionFactory() { HostName = hostName };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            _ = channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            consumer = new EventingBasicConsumer(channel);
        }

        public void Consume()
        {
            channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
        }

        public void Sent(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: string.Empty, routingKey: this.routingKey, basicProperties: null, body: body);
        }

        public void Close()
        {
            channel.BasicCancel(consumer.ConsumerTags.FirstOrDefault());
            channel.Dispose(); // close, etc.
        }

        public void Dispose()
        {
            connection?.Dispose();
            channel?.Dispose();
        }
    }
}
