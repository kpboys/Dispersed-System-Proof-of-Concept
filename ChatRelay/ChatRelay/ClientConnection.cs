using ChatRelay.DTOs;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatRelay
{
    public class ClientConnection
    {
        //Rabbit strings
        private readonly string exchangeName = "direct_message";

        //Message types:
        private const string privateMessageType = "Private";
        private const string publicMessageType = "Public";
        private const string groupMessageType = "Group";

        private string username;
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;
        private ConnectionFactory factory;
        public ClientConnection(string username, TcpClient client, ConnectionFactory factory)
        {
            this.username = username;
            this.client = client;
            reader = new StreamReader(this.client.GetStream());
            writer = new StreamWriter(this.client.GetStream());
            this.factory = factory;
        }
        
        public void SetupQueue()
        {
            Console.WriteLine("Setting up queues for: " +  this.username);
            SetupPrivateQueue();
        }
        private void SetupPrivateQueue()
        {
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Direct);
            var queueName = channel.QueueDeclare().QueueName;

            channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: username);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("Received: " + message);
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                ProcessFromDirectQueue(message);
            };
            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }
        private void ProcessFromDirectQueue(string message)
        {
            WrappedChatMessageDTO? wrappedDTO = JsonConvert.DeserializeObject<WrappedChatMessageDTO>(message);
            string mesType = wrappedDTO.MessageType;
            switch (mesType)
            {
                case privateMessageType:
                    PrivateSendToClient(wrappedDTO.JsonContent);
                    break;
                case groupMessageType:
                    GroupSendToClient(wrappedDTO.JsonContent);
                    break;
                default:
                    break;
            }

        }
        private void PrivateSendToClient(string message)
        {
            if (client.Connected)
            {
                PrivateMessageDTO pmDTO = JsonConvert.DeserializeObject<PrivateMessageDTO>(message);
                string constructedMessage = $"{pmDTO.SendingUser} whispers: {pmDTO.Message}";
                //var data = Encoding.UTF8.GetBytes(constructedMessage);
                writer.WriteLine(constructedMessage);
                writer.Flush();
            }
        }
        private void PrivateSendToClient(PrivateMessageDTO message)
        {
            if (client.Connected)
            {
                string constructedMessage = $"{message.SendingUser} whispers: {message.Message}";
                //var data = Encoding.UTF8.GetBytes(constructedMessage);
                writer.WriteLine(constructedMessage);
                writer.Flush();
            }
        }
        private void GroupSendToClient(string message)
        {
            if (client.Connected)
            {
                GroupMessageDTO groupDTO = JsonConvert.DeserializeObject<GroupMessageDTO>(message);
                string constructedMessage = $"{groupDTO.SendingUser} says: {groupDTO.Message}";
                //var data = Encoding.UTF8.GetBytes(constructedMessage);
                writer.WriteLine(constructedMessage);
                writer.Flush();
            }
        }
        /// <summary>
        /// Directly send to client
        /// </summary>
        /// <param name="message"></param>
        public void SendToClient(string message)
        {
            if(client.Connected)
            {
                writer.WriteLine(message);
                writer.Flush();
            }
        }
        public void StartReceiveLoop()
        {
            Thread receiveLoop = new Thread(ReceiveLoop);
            receiveLoop.IsBackground = true;
            receiveLoop.Start();

            void ReceiveLoop()
            {
                while (client.Connected)
                {
                    string? message = null;
                    try
                    {
                        message = reader.ReadLine();
                    }
                    catch
                    {

                    }
                    if (message != null)
                    {
                        Console.WriteLine("Received message from " + this.username + ": " + message);
                        ProcessMessage(message);
                    }
                }
                if (client.Connected == false)
                {
                    Console.WriteLine("Client disconnect: " + username);
                    ClientManager.Instance.RemoveClient(username);
                }
            }
        }
        private void ProcessMessage(string message)
        {
            WrappedChatMessageDTO? wrappedDTO = JsonConvert.DeserializeObject<WrappedChatMessageDTO>(message);
            string mesType = wrappedDTO.MessageType;

            switch (mesType)
            {
                case privateMessageType:
                    PrivateMessageDTO? privateDTO = 
                        JsonConvert.DeserializeObject<PrivateMessageDTO>(wrappedDTO.JsonContent);
                    if (ClientManager.Instance.IsUserOnline(privateDTO.ReceivingUser))
                    {
                        PrivateSendToClient(privateDTO); //Send version back to client
                        OutboundQueueHandler.Instance.SendPrivateMessage(privateDTO);
                    }
                    else
                    {
                        SendToClient("User is not online");
                    }
                    break;
                case publicMessageType:
                    //Check if is admin here, using JWT. (Probably method on ClientManager)
                    PublicMessageDTO? publicDTO =
                        JsonConvert.DeserializeObject<PublicMessageDTO>(wrappedDTO.JsonContent);
                    OutboundQueueHandler.Instance.SendPublicMessage(publicDTO);
                    break;
                case groupMessageType:
                    GroupMessageDTO? groupDTO =
                        JsonConvert.DeserializeObject<GroupMessageDTO>(wrappedDTO.JsonContent);
                    OutboundQueueHandler.Instance.SendGroupMessage(groupDTO);
                    break;
                default: 
                    break;
            }
        }
    }
}
