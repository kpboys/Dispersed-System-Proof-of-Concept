using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ChatService.OutboundMessageHandlers
{
    public class BroadcastChatHandler
    {
        //Singleton
        private static BroadcastChatHandler _instance;
        public static BroadcastChatHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    Console.WriteLine("Error in BroadcastChatHandler singleton");
                }
                return _instance;
            }
        }
        private readonly string exchangeName = "broadcast_message";
        private IModel channel;
        public BroadcastChatHandler()
        {
            _instance = this;
        }
        public void Setup(ConnectionFactory factory)
        {
            //var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = factory.CreateConnection();
            channel = connection.CreateModel();
            Console.WriteLine("Connected for BroadcastMessageHandler");
            channel.ExchangeDeclare(exchange: exchangeName, ExchangeType.Fanout);
        }
        public void SendBroadcast(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: exchangeName,
                                routingKey: "Doesn't matter",
                                basicProperties: null,
                                body: body);
            Console.WriteLine("Broadcasted message: " + message);
        }
    }
}
