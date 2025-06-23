using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatService.OutboundMessageHandlers
{
    public class DirectChatHandler
    {
        //Singleton
        private static DirectChatHandler _instance;
        public static DirectChatHandler Instance
        {
            get
            {
                if( _instance == null)
                {
                    Console.WriteLine("Error in DirectMessageHandler singleton");
                }
                return _instance;
            }
        }

        private readonly string exchangeName = "direct_message";
        private IModel channel;

        public DirectChatHandler()
        {
            _instance = this;
        }
        public void Setup(ConnectionFactory factory)
        {
            //var factory = new ConnectionFactory() { HostName = "rabbit" };
            //factory.UserName = "myuser";
            //factory.Password = "mypassword";
            var connection = factory.CreateConnection();
            channel = connection.CreateModel();
            Console.WriteLine("Connected for DirectMessageHandler");
            channel.ExchangeDeclare(exchange: exchangeName, ExchangeType.Direct);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="username">Username of the user to send to. Will be used as Routing Key for the Broker</param>
        /// <param name="message"></param>
        public void SendDirectMessage(string username, string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: exchangeName,
                                routingKey: username,
                                basicProperties: null,
                                body: body);
            Console.WriteLine("Sent message: " + message + " on Route: " + username);
        }
    }
}
