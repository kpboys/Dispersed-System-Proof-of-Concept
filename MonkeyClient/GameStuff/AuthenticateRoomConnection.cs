using MonkeyClient.DTOs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyClient.GameStuff
{
    public class AuthenticateRoomConnection
    {
        public bool AuthSuccess { get; private set; }

        private TcpClient client;
        private string roomAddress;
        private int roomPort;
        public TcpClient Client { get => client; }
        public AuthenticateRoomConnection(string roomAddress, int roomPort)
        {
            this.roomAddress = roomAddress;
            this.roomPort = roomPort;
            AuthSuccess = false;
        }
        public void ConnectAndAuth()
        {
            Program.WriteInDebug("Starting auth client and connecting...");
            Thread.Sleep(1000);
            client = new TcpClient();
            if (client.ConnectAsync(IPAddress.Parse(roomAddress), roomPort).Wait(5000))
            {
                Program.WriteInDebug("Connected, trying to send auth");
                StreamWriter authWriter = new StreamWriter(client.GetStream());
                RoomTCPAuth roomAuth = new RoomTCPAuth()
                {
                    Username = ClientInfo.username,
                    JWT = ClientInfo.jwt
                };
                string message = JsonConvert.SerializeObject(roomAuth);
                authWriter.WriteLine(message);
                authWriter.Flush();

                //Read response
                ReadResponse();
            }
            else
            {
                Program.WriteInDebug("Did not connect...");
            }

            void ReadResponse()
            {
                StreamReader reader = new StreamReader(client.GetStream());
                string? message = null;
                try
                {
                    message = reader.ReadLine();
                }
                catch (Exception e)
                {

                }

                if (message != null)
                {
                    if (message == "Ok")
                    {
                        AuthSuccess = true;
                    }
                    else
                    {
                        Program.WriteInDebug("Didn't connect, got response: " + message);
                    }
                }
            }
        }
    }
}
