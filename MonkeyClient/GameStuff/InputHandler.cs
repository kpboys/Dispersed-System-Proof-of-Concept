using MonkeyClient.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyClient.GameStuff
{
    public class InputHandler
    {
        public const string upCommand = "Up";
        public const string leftCommand = "Left";
        public const string rightCommand = "Right";
        public const string downCommand = "Down";

        public Thread inputThread;
        public Dictionary<ConsoleKey, string> commandBindings;

        private SendToServer sender;
        private bool running;

        public InputHandler(SendToServer sender)
        {
            this.sender = sender;
            inputThread = new Thread(() => InputLoop());
            commandBindings = new Dictionary<ConsoleKey, string>
            {
                //Standard bindings
                { ConsoleKey.W, upCommand },
                { ConsoleKey.A, leftCommand },
                { ConsoleKey.D, rightCommand },
                { ConsoleKey.S, downCommand }
            };
        }
        public void StartLoop()
        {
            running = true;
            inputThread.Start();
        }
        private void InputLoop()
        {
            while (running)
            {
                ConsoleKey inputKey = Console.ReadKey(true).Key;

                if (commandBindings.ContainsKey(inputKey) == false) continue;

                GameInputDTO inputDto = new GameInputDTO()
                {
                    Command = commandBindings[inputKey]
                };
                sender.SendCommand(inputDto);
            }
        }
        public void StateFedInputLoop()
        {
            ConsoleKey inputKey = Console.ReadKey(true).Key;

            if (commandBindings.ContainsKey(inputKey))
            {
                GameInputDTO inputDto = new GameInputDTO()
                {
                    Command = commandBindings[inputKey]
                };
                sender.SendCommand(inputDto);
            }
            else if (inputKey == ConsoleKey.Tab)
            {
                ClientStateHandler.Instance.ChatFocusedFromAny();
            }
        }
    }
}
