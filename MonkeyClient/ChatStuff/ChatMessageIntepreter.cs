using MonkeyClient.DTOs;
using MonkeyClient.GameStuff;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyClient.ChatStuff
{
    public class ChatMessageIntepreter
    {
        private readonly string privateMessageType = "Private";
        private readonly string publicMessageType = "Public";
        private readonly string groupMessageType = "Group";

        private readonly string notInRoomMessage = "Group message only available when connected to a room";

        private TcpClient client;
        private StreamWriter writer;
        private StreamReader reader;
        public ChatMessageIntepreter(TcpClient client)
        {
            this.client = client;
            writer = new StreamWriter(client.GetStream());
            reader = new StreamReader(client.GetStream());
        }

        public void ProcessMessage(string message)
        {
            string[] messageArr = message.Split(' ');
            string firstWord = messageArr[0];
            switch (firstWord)
            {
                case "/w":
                case "/whisper":
                    SendPrivateMessage(messageArr);
                    break;
                case "/g":
                case "/group":
                    SendGroupMessage(messageArr);
                    break;
                default:
                    SendPublicMessage(message);
                    break;
            }
        }
        private void SendPrivateMessage(string[] messageArr)
        {
            string sender = ClientInfo.username;
            string recipient = messageArr[1];
            //Recombine the text
            string text = "";
            for (int i = 2; i < messageArr.Length; i++)
            {
                text += messageArr[i];
                text += " ";
            }
            Program.WriteInDebug("Sending: " + sender + " to " + recipient + " " + text);
            //Wrapping object
            PrivateMessageDTO pmDTO = new PrivateMessageDTO()
            {
                SendingUser = sender,
                ReceivingUser = recipient,
                Message = text
            };
            WrappedChatMessageDTO wrappedDTO = new WrappedChatMessageDTO()
            {
                MessageType = privateMessageType,
                JsonContent = JsonConvert.SerializeObject(pmDTO)
            };
            string data = JsonConvert.SerializeObject(wrappedDTO);
            SendData(data);
        }
        private void SendPublicMessage(string message)
        {
            string sender = ClientInfo.username;
            string text = message;
            PublicMessageDTO publicMessage = new PublicMessageDTO()
            {
                SendingUser = sender,
                Message = text
            };
            WrappedChatMessageDTO wrappedDTO = new WrappedChatMessageDTO()
            {
                MessageType = publicMessageType,
                JsonContent = JsonConvert.SerializeObject(publicMessage)
            };
            string data = JsonConvert.SerializeObject(wrappedDTO);
            SendData(data);
        }
        private void SendGroupMessage(string[] messageArr)
        {
            //If we're not in a room, group messages are unavailable
            if(ClientStateHandler.Instance.State != ClientStates.Playing)
            {
                ChatHandler.Instance.ChatWindow.WriteLine(notInRoomMessage);
                return;
            }

            string sender = ClientInfo.username;
            //Recombine the text
            string text = "";
            for (int i = 1; i < messageArr.Length; i++)
            {
                text += messageArr[i];
                text += " ";
            }
            //Wrapping object
            GroupMessageDTO pmDTO = new GroupMessageDTO()
            {
                SendingUser = sender,
                Message = text
            };
            WrappedChatMessageDTO wrappedDTO = new WrappedChatMessageDTO()
            {
                MessageType = groupMessageType,
                JsonContent = JsonConvert.SerializeObject(pmDTO)
            };
            string data = JsonConvert.SerializeObject(wrappedDTO);
            SendData(data);
        }
        private void SendData(string data)
        {
            if (client.Connected)
            {
                writer.WriteLine(data);
                writer.Flush();
            }
        }

        public void StartListening()
        {
            Thread chatterListener = new Thread(ChatterLoop);
            chatterListener.IsBackground = true;
            chatterListener.Start();

            void ChatterLoop()
            {
                while (client.Connected)
                {
                    string? message = null;
                    try
                    {
                        message = reader.ReadLine();
                    }
                    catch
                    {

                    }
                    if (message != null)
                    {
                        ChatHandler.Instance.ChatWindow.WriteLine(message);
                    }
                }
                if (client.Connected == false)
                {
                    Program.WriteInDebug("Chat connection lost");
                    //Handle what happens if we lose connection to relay here
                }
            }
        }
    }
}
