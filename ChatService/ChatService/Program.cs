using ChatService.InboundMessageHandlers;
using ChatService.OutboundMessageHandlers;
using RabbitMQ.Client;
using System.Threading.Channels;

namespace ChatService
{
    internal class Program
    {
        public static string sessionServiceAddress = "https://localhost:0000/"; //<- Fix port
        public static string sessionGetUserInRoomWithUser = "api/getusersinroomwithuser/";

        public static string privateQueueName = "private";
        public static string publicQueueName = "public";
        public static string groupQueueName = "group";

        private static string hostName = "localhost";
        //private static string hostName = "rabbit";
        private static string rabbitUsername = "myuser";
        private static string rabbitPassword = "mypassword";
        private static int rabbitPort = 60500;
        static void Main(string[] args)
        {
            Console.WriteLine("Started");
            var factory = new ConnectionFactory()
            {
                HostName = hostName,
                UserName = rabbitUsername,
                Password = rabbitPassword,
                Port = rabbitPort
            };
            int retryCount = 0;
            int maxRetries = 10; // Maks antal forsøg
            int delay = 2000; // Forsinkelse mellem forsøg i millisekunder
            while (retryCount < maxRetries)
            {
                try
                {
                    var connection = factory.CreateConnection();
                    var channel = connection.CreateModel();
                    Console.WriteLine("Connected to RabbitMQ successfully.");
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    Console.WriteLine($"Failed to connect to RabbitMQ. Attempt {retryCount} of {maxRetries}. Retrying in {delay / 1000} seconds...");
                    Thread.Sleep(delay);
                }
            }

            DirectChatHandler directMessageHandler = new DirectChatHandler();
            directMessageHandler.Setup(factory);
            BroadcastChatHandler broadcastMessageHandler = new BroadcastChatHandler();
            broadcastMessageHandler.Setup(factory);

            PrivateQueue privateQueue = new PrivateQueue();
            privateQueue.Setup(factory);
            PublicQueue publicQueue = new PublicQueue();
            publicQueue.Setup(factory);
            GroupQueue groupQueue = new GroupQueue();
            groupQueue.Setup(factory);

            while (true)
            {

            }
        }
    }
}
