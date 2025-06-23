using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatRelay
{
    public class ClientManager
    {
        //Singleton
        private static ClientManager _instance;
        public static ClientManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Console.WriteLine("Error in ClientManager singleton");
                }
                return _instance;
            }
        }
        public Dictionary<string, ClientConnection> connectedClients;
        private ConnectionFactory factory;
        public ClientManager(ConnectionFactory factory)
        {
            _instance = this;
            connectedClients = new Dictionary<string, ClientConnection>();
            this.factory = factory;
        }
        public void AddClient(string username, TcpClient client)
        {
            Console.WriteLine("Adding client: " +  username);
            ClientConnection nCliCon = new ClientConnection(username, client, factory);
            nCliCon.SetupQueue();
            nCliCon.StartReceiveLoop();
            connectedClients.Add(username, nCliCon);
        }
        public void RemoveClient(string username)
        {
            connectedClients.Remove(username);
        }
        public void SendToAllClients(string message)
        {
            //Also sending to the client itself
            foreach (var client in connectedClients.Values)
            {
                client.SendToClient(message);
            }
        }
        public bool IsUserOnline(string username)
        {
            //For now, this is fake and just checks the clients in on this Relay
            //Have it check with SessionService later instead
            return connectedClients.ContainsKey(username);
        }
    }
}
