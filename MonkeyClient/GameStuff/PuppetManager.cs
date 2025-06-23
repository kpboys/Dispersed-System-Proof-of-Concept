using MonkeyClient.DTOs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyClient.GameStuff
{
    public class PuppetManager
    {
        private static PuppetManager _instance;
        public static PuppetManager Instance
        {
            get 
            { 
                if(_instance == null)
                {
                    Program.WriteInDebug("Error in PuppetManager singleton");
                }
                return _instance; 
            }
        }
        public const string setupCommand = "Setup";
        public const string newPuppetCommand = "NewPuppet";

        private SendToServer sender;
        private ReceiveFromServer receiver;
        private GridDisplay gridDisp;
        private Dictionary<string, Point> puppets;
        public Dictionary<string, Point> Puppets { get => puppets; }
        public GridDisplay GridDisp { get => gridDisp; }

        public PuppetManager(SendToServer sender, ReceiveFromServer receiver)
        {
            _instance = this;
            this.sender = sender;
            this.receiver = receiver;
            puppets = new Dictionary<string, Point>();
        }


        public void GetGameWorld()
        {
            sender.SendCommand(new GameInputDTO() { Command = setupCommand });
            InitialSetupDTO setupDTO = receiver.GetSetup();
            for (int i = 0; i < setupDTO.Puppets.Count; i++)
            {
                puppets.Add(setupDTO.Puppets[i].Item1, setupDTO.Puppets[i].Item2);
            }
            gridDisp = new GridDisplay(setupDTO.Width, setupDTO.Height, this);
            gridDisp.UpdateGrid();
            //Test ending here
            //Program.debugWindow.WriteLine("Width: " + setupDTO.Width + " Height: " +  setupDTO.Height);
        }
        public void MovePuppet(string username, Point newPos)
        {
            if(puppets.ContainsKey(username) == false)
            {
                puppets.Add(username, newPos);
            }
            else
            {
                puppets[username] = newPos;
            }
            gridDisp.UpdateGrid();
        }
        public void RemovePuppet(string username)
        {
            if(puppets.ContainsKey(username))
            {
                puppets.Remove(username);
                gridDisp.UpdateGrid();
                //Probably write a message somewhere saying that this happened
            }
        }
        public void Shutdown()
        {
            gridDisp.RemoveGrid();
        }
    }
}
