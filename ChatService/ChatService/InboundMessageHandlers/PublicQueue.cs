using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatService.DTOs;
using Newtonsoft.Json;
using ChatService.OutboundMessageHandlers;

namespace ChatService.InboundMessageHandlers
{
    public class PublicQueue
    {
        public PublicQueue() { }

        public void Setup(ConnectionFactory factory)
        {
            //var factory = new ConnectionFactory { HostName = "localhost" };
            var connection = factory.CreateConnection();
            Console.WriteLine("Connected for PublicMessageHandler");
            var channel = connection.CreateModel();

            channel.QueueDeclare(queue: Program.publicQueueName,
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);
            Console.WriteLine("Public Queue set up. Waiting for messages...");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("Received public message: " + message);
                InterpretMessage(message);
            };

            channel.BasicConsume(queue: Program.publicQueueName,
                                autoAck: true,
                                consumer: consumer);
        }
        private void InterpretMessage(string message)
        {
            //Deserialize to maybe check stuff in its data
            PublicMessageDTO? publicMessage = JsonConvert.DeserializeObject<PublicMessageDTO>(message);

            BroadcastChatHandler.Instance.SendBroadcast(message);
        }
    }
}
