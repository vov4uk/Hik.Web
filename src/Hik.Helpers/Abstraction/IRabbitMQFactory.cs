using System;

namespace Hik.Client.Abstraction
{
    public interface IRabbitMQFactory
    {
        IRabbitMQSender Create(string hostName, string queueName, string routingKey);
    }

    public interface IRabbitMQSender : IDisposable
    {
        void Sent(object message);
    }
}
