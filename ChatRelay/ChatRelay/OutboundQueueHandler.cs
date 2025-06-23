using ChatRelay.DTOs;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatRelay
{
    public class OutboundQueueHandler
    {
        //Singleton
        private static OutboundQueueHandler _instance;
        public static OutboundQueueHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    Console.WriteLine("Error in OutboundQueueHandler singleton");
                }
                return _instance;
            }
        }
        private readonly string privateQueueName = "private";
        private readonly string publicQueueName = "public";
        private readonly string groupQueueName = "group";
        private IModel channel;
        public OutboundQueueHandler()
        {
            _instance = this;
        }
        public void Setup(ConnectionFactory factory)
        {
            //var factory = new ConnectionFactory() { HostName="localhost" };
            var connection = factory.CreateConnection();
            channel = connection.CreateModel();
        }
        public void SendPrivateMessage(PrivateMessageDTO pmDTO)
        {
            string json = JsonConvert.SerializeObject(pmDTO);
            var body = Encoding.UTF8.GetBytes(json);
            channel.BasicPublish(exchange: "",
                                routingKey: privateQueueName,
                                basicProperties: null,
                                body: body);
        }
        public void SendPublicMessage(PublicMessageDTO pubMesDTO)
        {
            string json = JsonConvert.SerializeObject(pubMesDTO);
            var body = Encoding.UTF8.GetBytes(json);
            channel.BasicPublish(exchange: "",
                                routingKey: publicQueueName,
                                basicProperties: null,
                                body: body);
        }
        public void SendGroupMessage(GroupMessageDTO groupMesDTO)
        {
            string json = JsonConvert.SerializeObject(groupMesDTO);
            var body = Encoding.UTF8.GetBytes(json);
            channel.BasicPublish(exchange: "",
                                routingKey: groupQueueName,
                                basicProperties: null,
                                body: body);
        }
    }
}
