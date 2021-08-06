using Hik.DTO.Message;
using Newtonsoft.Json;
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
        protected readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly EventingBasicConsumer consumer;
        private readonly string queueName;

        public event EventHandler<HikMessageEventArgs> Received;

        public RabbitMQHelper(string hostName, string queueName)
        {
            this.queueName = queueName;
            ConnectionFactory factory = new ConnectionFactory() { HostName = hostName };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            QueueDeclareOk queueDeclareOk = channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                DetectPeopleMessage msg = JsonConvert.DeserializeObject<DetectPeopleMessage>(body);
                logger.Debug("[x] Received {0}", body);
                Received?.Invoke(model, new HikMessageEventArgs(msg));
            };
        }

        public void Consume()
        {
            channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
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
