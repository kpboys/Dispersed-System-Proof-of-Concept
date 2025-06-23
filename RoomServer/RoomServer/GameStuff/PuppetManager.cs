using MonkeyClient.DTOs.UpdateDTOs;
using Newtonsoft.Json;
using System.Net.Sockets;

namespace RoomServer.GameStuff
{
    /// <summary>
    /// Class keeping all the Puppets and allowing other classes to access them
    /// </summary>
    public class PuppetManager
    {
        private static PuppetManager _instance;
        public static PuppetManager Instance
        {
            get 
            {
                if(_instance == null)
                {
                    _instance = new PuppetManager();
                }
                return _instance; 
            }
        }
        public const string initialSetupKey = "Setup";
        public const string movementUpdateKey = "Movement";
        public const string disconnectUpdateKey = "Disconnect";

        public Dictionary<string, Puppet> activePuppets;
        public PuppetManager()
        {
            //Setup can be done here, which then overrides the singleton 
            activePuppets = new Dictionary<string, Puppet>();
            _instance = this;
        }
        public void CreatePuppet(string userName, TcpClient clientConnection)
        {
            //Check for repeat names here
            Puppet nPup = new Puppet(userName, clientConnection);
            activePuppets.Add(userName, nPup);
            GridWorld.Instance.AddPuppet(nPup);
        }
        public void RemovePuppet(Puppet pup)
        {
            pup.Shutdown();
            activePuppets.Remove(pup.UserName);
            GridWorld.Instance.RemovePuppet(pup);

            var json = JsonConvert.SerializeObject(new PuppetDisconnectUpdateDTO() { Username = pup.UserName});
            var wrappedUpdate = new WrappedPuppetUpdateDTO()
            {
                UpdateType = disconnectUpdateKey,
                JsonUpdateData = json
            };
            string content = JsonConvert.SerializeObject(wrappedUpdate);

            SendUpdateToAll(content);
        }
        public void SendUpdateToAll(string message)
        {
            foreach (var puppetPair in activePuppets)
            {
                puppetPair.Value.SendUpdate(message);
            }
        }
        public void SendUpdateToAllButOne(string username, string message)
        {
            foreach (var puppetPair in activePuppets)
            {
                if(puppetPair.Key != username)
                {
                    puppetPair.Value.SendUpdate(message);
                }
            }
        }
    }
}
