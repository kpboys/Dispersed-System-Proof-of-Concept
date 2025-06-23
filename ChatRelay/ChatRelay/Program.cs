using RabbitMQ.Client;

namespace ChatRelay
{
    internal class Program
    {
        public static int roomPort = 6800;
        private static string hostName = "localhost";
        //private static string hostName = "rabbit";
        private static string rabbitUsername = "myuser";
        private static string rabbitPassword = "mypassword";
        private static int rabbitPort = 60500;
        static void Main(string[] args)
        {
            Console.WriteLine("Started");
#if DEBUG
            DevelopEnvLoader.Load("develop.env");
#endif
            var factory = new ConnectionFactory()
            {
                HostName = hostName,
                UserName = rabbitUsername,
                Password = rabbitPassword,
                Port = rabbitPort
            };
            ClientManager manager = new ClientManager(factory);
            RelayQueueReceiver relayQueue = new RelayQueueReceiver();
            relayQueue.SetupQueue(factory);
            OutboundQueueHandler outbound = new OutboundQueueHandler();
            outbound.Setup(factory);
            ClientAccepter accepter = new ClientAccepter(roomPort);
            accepter.StartListening();
            while (true)
            {

            }
        }
    }
}
