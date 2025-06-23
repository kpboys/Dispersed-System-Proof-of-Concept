using MonkeyClient.DTOs;
using MonkeyClient.DTOs.UpdateDTOs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyClient.GameStuff
{
    public class ReceiveFromServer
    {
        public const string movementUpdateKey = "Movement";
        public const string disconnectUpdateKey = "Disconnect";
        public const string initialSetupKey = "Setup";

        private TcpClient client;
        private StreamReader reader;

        private InitialSetupDTO initialSetup;
        private bool gotSetup;
        public ReceiveFromServer(TcpClient client)
        {
            this.client = client;
            reader = new StreamReader(client.GetStream());
            gotSetup = false;
        }
        public void TestHearEcho()
        {
            Thread testThread = new Thread(() => HearEcho());
            testThread.IsBackground = true;
            testThread.Start();

            void HearEcho()
            {
                while (client.Connected)
                {
                    string? message = null;
                    try
                    {
                        message = reader.ReadLine();
                    }
                    catch (Exception e)
                    {

                    }
                    if(message != null)
                    {
                        Console.WriteLine(message);
                    }
                }
            }
        }
        public void ReceiveUpdateLoop()
        {
            Thread receiveThread = new Thread(() => ReceiveLoop());
            receiveThread.IsBackground = true;
            receiveThread.Start();

            void ReceiveLoop()
            {
                while (client.Connected)
                {
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
                        ProcessUpdate(message);
                    }
                }
                if(client.Connected == false)
                {
                    ClientStateHandler.Instance.LostServerConnection();
                }
            }
        }
        public void ProcessUpdate(string updateJson)
        {
            WrappedPuppetUpdateDTO wrappedUpdate = 
                JsonConvert.DeserializeObject<WrappedPuppetUpdateDTO>(updateJson);

            switch (wrappedUpdate.UpdateType)
            {
                case movementUpdateKey:
                    PuppetMovementUpdateDTO moveUpdate = 
                        JsonConvert.DeserializeObject<PuppetMovementUpdateDTO>(wrappedUpdate.JsonUpdateData);
                    PuppetManager.Instance.MovePuppet(moveUpdate.Username, moveUpdate.Position);
                    break;
                case disconnectUpdateKey:
                    PuppetDisconnectUpdateDTO disconnectUpdate =
                        JsonConvert.DeserializeObject<PuppetDisconnectUpdateDTO>(wrappedUpdate.JsonUpdateData);
                    PuppetManager.Instance.RemovePuppet(disconnectUpdate.Username);
                    break;
                case initialSetupKey:
                    initialSetup = JsonConvert.DeserializeObject<InitialSetupDTO>(wrappedUpdate.JsonUpdateData);
                    gotSetup = true;
                    break;
                default:
                    break;
            }
        }
        public InitialSetupDTO GetSetup()
        {
            Program.debugWindow.WriteLine("Waiting for initial setup...");
            while (gotSetup == false)
            {
                Thread.Sleep(100);
            }
            return initialSetup;
        }
        /// <summary>
        /// Special receiving function to get the game world setup
        /// </summary>
        /// <returns></returns>
        public InitialSetupDTO ReceiveInitialSetup()
        {
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
                return JsonConvert.DeserializeObject<InitialSetupDTO>(message);
            }
            else
            {
                return null;
            }
        }
        public void Shutdown()
        {
            reader.Close();
        }
    }
}
