using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyClient.ChatStuff
{
    public class FakeRelayConnection
    {
        private int minTBM;
        private int maxTBM;
        private Thread chatterThread;
        public bool running;
        public FakeRelayConnection(int minTBM, int maxTBM)
        {
            this.minTBM = minTBM;
            this.maxTBM = maxTBM;
        }
        public void SimulateChatter()
        {
            running = true;
            chatterThread = new Thread(() =>
            {
                while (running)
                {
                    Thread.Sleep(new Random().Next(minTBM, maxTBM));
                    if (running == false)
                        break;
                    ReceiveMessage("Hi!");
                }
            });
            chatterThread.IsBackground = true;
            chatterThread.Start();
        }
        public void SendMessage(string message)
        {
            //Send to relay and stuff
            ReceiveMessage(message);
        }
        private void ReceiveMessage(string message)
        {
            ChatHandler.Instance.ChatWindow.WriteLine(message);
        }
    }
}
