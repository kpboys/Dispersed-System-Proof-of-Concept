using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatRelay.DTOs;
using System.Text.Json.Nodes;
using Newtonsoft.Json;

namespace ChatRelay
{
    public class RelayQueueReceiver
    {
        private readonly string exchangeName = "broadcast_message";
        public RelayQueueReceiver() { }

        public void SetupQueue(ConnectionFactory factory)
        {
            //var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout);
            var queueName = channel.QueueDeclare().QueueName;

            channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: "Nothing");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("Received: " + message);
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                ProcessMessage(message);
            };
            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }
        private void ProcessMessage(string message)
        {
            //Can handle other types of message here, but for now just assume it's a public message
            PublicMessageDTO? messageDTO = JsonConvert.DeserializeObject<PublicMessageDTO>(message);
            string constructedMessage = $"{messageDTO.SendingUser} shouts: {messageDTO.Message}";
            ClientManager.Instance.SendToAllClients(constructedMessage);
        }
    }
}
