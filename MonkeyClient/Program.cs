using MonkeyClient.GameStuff;

namespace MonkeyClient
{
    internal class Program
    {
        public static SubWindow debugWindow;

        //Values (use environment variable later)
        public static int heartbeatInterval = 5000;
        public static int chatRelayPort = 6800;

        //Addresses (use environment variable later)
        public static string heartbeatAddress = "https://localhost:7036/";
        public static string roomRegistryAddress = "https://localhost:7005/";
        public static string chatRelayAddress = "127.0.0.1";
        public static string loginAddress = "https://localhost:8081/api/login";
        public static string accountRegisterAddress = "https://localhost:8081/api/register";
        static void Main(string[] args)
        {
            ClientConnectionTest(args);
        }
        public static void WriteInDebug(string text)
        {
            lock (debugWindow)
            {
                debugWindow.WriteLine(text);
            }
        }
        private static void ClientConnectionTest(string[] args)
        {
            //ClientInfo.username = "Kirke";
            //ClientInfo.username = "Peter";
            //ClientInfo.username = args[0];
            //ClientInfo.jwt = "nonsense";
            debugWindow = new SubWindow(new System.Drawing.Rectangle(70, 5, 20, 20), '=', 1);
            ClientStateHandler handler = new ClientStateHandler();

            Console.WriteLine("Exited out of handler");
            Console.ReadKey();
        }
        private static void SubWindowTestingGround()
        {
            //SubWindow window = new SubWindow(new System.Drawing.Rectangle(30, 5, 20, 10), '=', 1);
            //InputArea inparr = new InputArea(default, '#', 0);
            //SubWindowWithInput window = new SubWindowWithInput(new System.Drawing.Rectangle(30, 5, 20, 10), '=', 1, inparr, true);
            SubWindowWithInput window = new SubWindowWithInput(new System.Drawing.Rectangle(30, 5, 20, 10), '=', 1);

            window.WriteLine("This is a test");
            window.WriteLine("This is a very looooooooooooooooooooooooooooooooooooooooooong test");
            int counter = 0;
            while (true)
            {
                //Console.ReadLine();
                //Console.ReadKey();
                //counter += 3;
                //window.MoveWindow(30 + counter, 5 +  counter);
                window.ReadLineInInput();

            }
        }
    }
}

