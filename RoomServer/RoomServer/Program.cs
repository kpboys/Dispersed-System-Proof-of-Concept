
using Newtonsoft.Json;
using RoomServer.DTOs;
using RoomServer.GameStuff;
using System.Text;

namespace RoomServer
{
    public class Program
    {
        //public static string connectIP;
        //public static int connectPort;
        public const string healthCheckUrl = "ROOM_HEALTH_CHECK_URL";
        public const string tcpIpAddress = "TCP_IP_ADDRESS";
        public const string tcpPort = "TCP_PORT";
        public const string registryUrl = "REGISTRY_URL";
        private static bool restSetupDone;
        public static void Main(string[] args)
        {
            //DebugSetEnvironmentVariables();
#if DEBUG
            DevelopEnvLoader.Load("develop.env");
#endif
            ThreadedRestSetup(args);
            SelfRegister(args);
            TestRoomFlow();
            //RestSetup(args);

        }
        /// <summary>
        /// Sets various Environment variables to something manually, for testing purposes
        /// </summary>
        //private static void DebugSetEnvironmentVariables()
        //{
        //    Environment.SetEnvironmentVariable(tcpIpAddress, "127.0.0.1");
        //    Environment.SetEnvironmentVariable(tcpPort, "7777");
        //    Environment.SetEnvironmentVariable(registryUrl, "https://localhost:7005/");
        //}
        private static void TestRoomFlow()
        {
            GameLogic gameLogic = new GameLogic();
            GridWorld gridWorld = new GridWorld(8, 8);

            int port = int.Parse(Environment.GetEnvironmentVariable(tcpPort));
            ClientAccepter accept = new ClientAccepter(port);
            accept.StartListening();
            while (true)
            {
                //Just to keep this open
            }
        }
        private static async void SelfRegister(string[] args)
        {
            HttpClient client = new HttpClient();
            var roomDetails = new RoomRegistryDetailsDTO()
            {
                HealthCheckUrl = Environment.GetEnvironmentVariable(healthCheckUrl),
                RoomTcpAddress = Environment.GetEnvironmentVariable(tcpIpAddress),
                RoomTcpPort = int.Parse(Environment.GetEnvironmentVariable(tcpPort))
            };
            var content = new StringContent(JsonConvert.SerializeObject(roomDetails), encoding: Encoding.UTF8, "application/json");
            Console.WriteLine("Trying to register");
            try
            {
                string address = Environment.GetEnvironmentVariable(registryUrl) + "api/register";
                Console.WriteLine("Posting to address: " + address);
                var response = await client.PostAsync(address, content);
                response.EnsureSuccessStatusCode();
                Console.WriteLine("Registered with: " + await response.Content.ReadAsStringAsync());
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to register with exception: " + e.Message);
            }
        }
        private static void ThreadedRestSetup(string[] args)
        {
            restSetupDone = false;
            Thread restThread = new Thread(() => RestSetup(args));
            restThread.Start();

            while (restSetupDone == false)
            {
                Thread.Sleep(50);
            }
        }
        private static void RestSetup(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.


            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            restSetupDone = true;

            app.Run();
        }
    }
}
