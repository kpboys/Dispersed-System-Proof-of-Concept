using MonkeyClient.DTOs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyClient.GameStuff
{
    public class SendToServer
    {
        private TcpClient client;
        private StreamWriter writer;
        public SendToServer(TcpClient client)
        {
            this.client = client;
            writer = new StreamWriter(this.client.GetStream());
        }
        public void TestEcho()
        {
            while (client.Connected)
            {
                string message = Console.ReadLine();
                if(message != null)
                {
                    writer.WriteLine(message);
                    writer.Flush();
                }
            }
        }
        public void SendCommand(GameInputDTO input)
        {
            if(client.Connected)
            {
                var json = JsonConvert.SerializeObject(input);
                if (json == null || json.Length == 0 || json == "") return;

                //Program.WriteInDebug("Sending command " +  json);
                writer.WriteLine(json);
                writer.Flush();
            }
        }
        public void Shutdown()
        {
            writer.Close();
        }
    }
}
