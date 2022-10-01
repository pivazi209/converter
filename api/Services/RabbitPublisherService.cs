using RabbitMQ.Client;
using System.Text;

namespace api.Services
{
    public class RabbitPublisherService: IMessageSender, IDisposable
    {
        private IConnection connection;
        private string queueName;

        public RabbitPublisherService(IConfiguration config)
        {
            var rabbitHost = config.GetValue<string>("rabbitHost");
            var queueName = config.GetValue<string>("queueName");

            var factory = new ConnectionFactory() { HostName = rabbitHost };
            this.connection = factory.CreateConnection();
            this.queueName = queueName;
        }

        public void SendMessage(string message)
        {
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queueName,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "",
                                     routingKey: queueName,
                                     basicProperties: null,
                                     body: body);
                Console.WriteLine(" [x] Sent {0}", message);
            }
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }

    public interface IMessageSender
    {
        void SendMessage(string message);
    }
}
