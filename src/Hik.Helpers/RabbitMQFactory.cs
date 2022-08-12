using Hik.Client.Abstraction;

namespace Hik.Helpers
{
    public class RabbitMQFactory : IRabbitMQFactory
    {
        public IRabbitMQSender Create(string hostName, string queueName, string routingKey)
        {
            return new RabbitMQSender(hostName, queueName, routingKey);
        }
    }
}
