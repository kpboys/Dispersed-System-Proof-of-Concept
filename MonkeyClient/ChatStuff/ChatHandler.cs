using MonkeyClient.GameStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyClient.ChatStuff
{
    public class ChatHandler
    {
        private static ChatHandler _instance;
        public static ChatHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    Program.WriteInDebug("Error in ChatHandler singleton");
                }
                return _instance;
            }
        }


        private SubWindowWithInput chatWindow;
        private Thread inputThread;
        private AdvancedInputArea inputArea;
        private bool running;
        private ChatMessageIntepreter interpreter; //Change to a real one later
        public SubWindowWithInput ChatWindow { get => chatWindow; }
        public ChatHandler(ChatMessageIntepreter interpreter)
        {
            _instance = this;
            //InputArea input = new InputArea(default, '#', 1);
            inputArea = new AdvancedInputArea(default, '#', 1);
            chatWindow = new SubWindowWithInput(new System.Drawing.Rectangle(40, 5, 20, 10), '#', 1, inputArea, true);
            
            this.interpreter = interpreter;
            this.interpreter.StartListening();
        }
        public void ChatFocused()
        {
            running = true;
            //MakeExternalTestKiller();
            while (running)
            {
                //If it ever comes back with nothing, user probably wants to go back to playing
                //Pressing TAB in the AdvancedInputArea also stops the reading
                string inputText = chatWindow.ReadLineInInput(false);
                if (running == false)
                    break;
                if (inputText == "")
                {
                    running = false;
                }
                else
                {
                    interpreter.ProcessMessage(inputText);
                }
            }
        }
        public void Shutdown()
        {
            //interpreter.running = false;
            running = false;
            chatWindow.ClearWholeWindow();
        }
        private void MakeExternalTestKiller()
        {
            Thread th = new Thread(() =>
            {
                Thread.Sleep(3000);
                running = false;
                inputArea.StopReading();
            });
            th.Start();
        }
    }
}
