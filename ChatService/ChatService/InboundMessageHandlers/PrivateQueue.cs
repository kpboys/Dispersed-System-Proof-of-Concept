using ChatService.DTOs;
using ChatService.OutboundMessageHandlers;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService.InboundMessageHandlers
{
    public class PrivateQueue
    {
        private readonly string privateMessageType = "Private";
        public PrivateQueue()
        {

        }
        public void Setup(ConnectionFactory factory)
        {
            //var factory = new ConnectionFactory { HostName = "localhost" };
            var connection = factory.CreateConnection();
            Console.WriteLine("Connected for PrivateMessageHandler");
            var channel = connection.CreateModel();

            channel.QueueDeclare(queue: Program.privateQueueName,
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);
            Console.WriteLine("Private Queue set up. Waiting for messages...");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("Received private message: " + message);
                InterpretMessage(message);
            };

            channel.BasicConsume(queue: Program.privateQueueName,
                                autoAck:true,
                                consumer: consumer);
        }
        /// <summary>
        /// Logic for how to understand a private message
        /// </summary>
        /// <param name="message"></param>
        private void InterpretMessage(string message)
        {
            //Deserialize so we can use its data to check for stuff
            PrivateMessageDTO? pmDTO = JsonConvert.DeserializeObject<PrivateMessageDTO>(message);

            //Check with SessionService if user is online and all that
            bool userIsOnline = true;

            if (userIsOnline)
            {
                WrappedChatMessageDTO wrappedDTO = new WrappedChatMessageDTO
                {
                    MessageType = privateMessageType,
                    JsonContent = message
                };
                string wrappedJson = JsonConvert.SerializeObject(wrappedDTO);
                //Sending the same json string, as the details needed for the Relay are the same
                DirectChatHandler.Instance.SendDirectMessage(pmDTO.ReceivingUser, wrappedJson);
            }

        }
    }
}
