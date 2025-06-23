using ChatService.DTOs;
using ChatService.OutboundMessageHandlers;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService.InboundMessageHandlers
{
    public class GroupQueue
    {
        private readonly string groupMessageType = "Group";
        public GroupQueue() { }
        public void Setup(ConnectionFactory factory)
        {
            //var factory = new ConnectionFactory { HostName = "localhost" };
            var connection = factory.CreateConnection();
            Console.WriteLine("Connected for GroupMessageHandler");
            var channel = connection.CreateModel();

            channel.QueueDeclare(queue: Program.groupQueueName,
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);
            Console.WriteLine("Group Queue set up. Waiting for messages...");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("Received group message: " + message);
                InterpretMessage(message);
            };

            channel.BasicConsume(queue: Program.groupQueueName,
                                autoAck: true,
                                consumer: consumer);
        }
        private void InterpretMessage(string message)
        {
            //Deserialize to maybe check stuff in its data
            GroupMessageDTO? groupMessage = JsonConvert.DeserializeObject<GroupMessageDTO>(message);
            WrappedChatMessageDTO wrappedDTO = new WrappedChatMessageDTO
            {
                MessageType = groupMessageType,
                JsonContent = message
            };
            string wrappedJson = JsonConvert.SerializeObject(wrappedDTO);

            List<UsernameDto> group = GetUsersInGroup(groupMessage.SendingUser);
            Console.WriteLine("Got group with count: " + group.Count);
            for (int i = 0; i < group.Count; i++)
            {
                DirectChatHandler.Instance.SendDirectMessage(group[i].Username, wrappedJson);
            }
        }
        private List<UsernameDto> GetUsersInGroup(string username)
        {
            string constructedAddress = Program.sessionServiceAddress + Program.sessionGetUserInRoomWithUser;
            using(HttpClient client = new HttpClient())
            {
                var response = client.GetAsync(constructedAddress + username).Result;
                if (response.IsSuccessStatusCode)
                {
                    string obj = response.Content.ReadAsStringAsync().Result;
                    List<UsernameDto> usernames = JsonConvert.DeserializeObject<List<UsernameDto>>(obj);
                    return usernames;
                }
                else
                {
                    Console.WriteLine("Could not get users in room with: " + username);
                    Console.WriteLine("Reason: " + response.Content);
                }
            }
            return new List<UsernameDto>();
        }
    }
}
