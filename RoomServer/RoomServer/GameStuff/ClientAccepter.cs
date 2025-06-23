using Newtonsoft.Json;
using RoomServer.DTOs;
using System.Net;
using System.Net.Sockets;

namespace RoomServer.GameStuff
{
    /// <summary>
    /// Class that listens for TCP connections from clients and verifies them. Also creates
    /// Puppet objects for the clients who join.
    /// </summary>
    public class ClientAccepter
    {
        private int roomPort;
        private bool listening;
        public ClientAccepter(int roomPort)
        {
            this.roomPort = roomPort;
        }
        public void StartListening()
        {
            listening = true;
            TcpListener listener = new TcpListener(IPAddress.Any, roomPort); //Might need to be a certain IP
            listener.Start();
            Console.WriteLine("Listener started! Listening on port: " + roomPort);
            Thread acceptClientThread = new Thread(() => AcceptClient());
            acceptClientThread.Start();

            void AcceptClient()
            {
                while (listening)
                {
                    //Holds here
                    TcpClient client = listener.AcceptTcpClient();
                    Thread authThread = new Thread(() => Authenticate(client));
                    authThread.IsBackground = true;
                    authThread.Start();
                }
            }
        }
        private void Authenticate(TcpClient client)
        {
            Console.WriteLine("Beginning auth...");
            bool authenticating = true;
            StreamReader reader = new StreamReader(client.GetStream());
            while(client.Connected && authenticating)
            {
                //Setup for handling if client shuts down
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
                    ClientAuthSend clientAuth = JsonConvert.DeserializeObject<ClientAuthSend>(message);
                    Console.WriteLine("Client " + clientAuth.Username + " is trying to connect...");
                    //Console.WriteLine("Client had JWT: " + clientAuth.JWT);

                    string response = "";
                    //This waits it seems
                    bool validToken = TokenValidator.ValidateJwt(clientAuth.JWT).Result;
                    if(validToken)
                    {
                        PuppetManager.Instance.CreatePuppet(clientAuth.Username, client);
                        response = "Ok";
                    }
                    else
                    {
                        response = "Invalid token or Session";
                    }
                    

                    //Repond with Ok to say that the authentication was cleared
                    StreamWriter responseWrite = new StreamWriter(client.GetStream());
                    responseWrite.WriteLine(response);
                    responseWrite.Flush();

                    authenticating = false;

                }
            }
        }
    }
}
