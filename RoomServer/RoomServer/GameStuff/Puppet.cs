using System.Net.Sockets;

namespace RoomServer.GameStuff
{
    /// <summary>
    /// Object representing a client on the server.
    /// </summary>
    public class Puppet
    {
        private string userName;
        private TcpClient clientConnection;

        private StreamWriter writer;

        public string UserName { get => userName; }

        public Puppet(string userName, TcpClient clientConnection)
        {
            this.userName = userName;
            this.clientConnection = clientConnection;

            writer = new StreamWriter(clientConnection.GetStream());

            Thread receiveThread = new Thread(() => ReceiveAction());
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        private void ReceiveAction()
        {
            StreamReader reader = new StreamReader(clientConnection.GetStream());
            while (clientConnection.Connected)
            {
                string? message = null;
                try
                {
                    message = reader.ReadLine();
                }
                catch (Exception e)
                {

                }
                if(message == "" || message == null)
                {
                    clientConnection.Close();
                }
                Console.WriteLine("Client " + userName + " performed action " + message);

                GameLogic.Instance.ReceiveCommand(this, message);
            }
            if(clientConnection.Connected == false)
            {
                PuppetManager.Instance.RemovePuppet(this);
            }
        }
        public void Shutdown()
        {
            writer.Close();
            clientConnection.Close();
        }
        public void SendUpdate(string message)
        {
            if (clientConnection.Connected == false) return;
            //Console.WriteLine("Sending update: " + message + " to client " + userName);
            writer.WriteLine(message);
            writer.Flush();
        }
    }
}
