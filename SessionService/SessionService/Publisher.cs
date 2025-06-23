using RabbitMQ.Client;
using SessionService.Models;
using System.Text;
using System.Text.Json;

namespace SessionService
{
    public class Publisher
    {

        private readonly IConnection connection;
        private readonly IModel channel;

        private readonly string exchangeName;

        public Publisher(string hostname, int hostPort, string exchangeName)
        {
            this.exchangeName = exchangeName;

            var factory = new ConnectionFactory { HostName = hostname };
            //var factory = new ConnectionFactory { HostName = hostname, UserName = "rabbitmq", Password = "rabbitmq" };
            factory.Uri = new Uri($"amqp://rabbitmq:rabbitmq@{hostname}:{hostPort}");

            factory.UserName = Program.rabbitUsername;
            factory.Password = Program.rabbitPassword;

            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout);
        }

        public void Publish(StatusDto status)
        {
            byte[] body = GetJson(status);
            channel.BasicPublish(exchange: exchangeName, string.Empty, basicProperties: null, body: body);
        }

        private byte[] GetJson(object input)
        {
            string jsonString = JsonSerializer.Serialize(input);
            byte[] body = Encoding.UTF8.GetBytes(jsonString);
            return body;
        }

    }
}
