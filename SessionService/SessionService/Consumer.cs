using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SessionService.Models;
using System.Text.Json;
using System.Text;
using SessionService.Interfaces;

namespace SessionService
{
    public class Consumer
    {

        private ISessionDatabase database;

        private readonly IConnection connection;
        private readonly IModel channel;

        //Other constructor that doesn't take a Database immediately
        public Consumer(string hostName, int hostPort, string queueName)
        {
            var factory = new ConnectionFactory { HostName = hostName };
            factory.Uri = new Uri($"amqp://rabbitmq:rabbitmq@{hostName}:{hostPort}");

            factory.UserName = Program.rabbitUsername;
            factory.Password = Program.rabbitPassword;

            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += UpdateSession;
            channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
        }
        public Consumer(string hostName, int hostPort, string queueName, ISessionDatabase database)
        {
            this.database = database;

            var factory = new ConnectionFactory { HostName = hostName };
            factory.Uri = new Uri($"amqp://rabbitmq:rabbitmq@{hostName}:{hostPort}");

            factory.UserName = Program.rabbitUsername;
            factory.Password = Program.rabbitPassword;

            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += UpdateSession;
            channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
        }
        public void SetDatabase(ISessionDatabase db)
        {
            database = db;
        }

        private void UpdateSession(object? model, BasicDeliverEventArgs ea)
        {
            byte[] body = ea.Body.ToArray();
            string json = Encoding.UTF8.GetString(body);
            UserRoomDto? newState = JsonSerializer.Deserialize<UserRoomDto>(json);
            if (newState != null)
                database.UpdateRoom(newState.Username, newState.RoomName);
        }

    }
}
