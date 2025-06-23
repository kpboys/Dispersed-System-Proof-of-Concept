using MonkeyClient.ChatStuff;
using MonkeyClient.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyClient.GameStuff
{
    public enum ClientStates
    {
        Login,
        ChatAuthenticating,
        ChatSetup,
        SetupServerBrowser,
        ServerBrowserActive,
        Authenticating,
        PlaySetup,
        Playing
    }
    public class ClientStateHandler
    {
        private static ClientStateHandler _instance;
        public static ClientStateHandler Instance
        {
            get
            {
                if( _instance == null)
                {
                    Program.WriteInDebug("Failure in singleton for ClientStateHandler");
                }
                return _instance;
            }
        }

        

        //Debug stuff
        private bool useLocalRoom = false;
        private bool useHeartbeat = true;
        private bool chatOnly = false;

        //Real stuff
        private ClientStates state;
        private LoginHandler loginHandler;
        private ServerBrowser browser;
        private AuthenticateRoomConnection authConnect;
        private SendToServer sender;
        private ReceiveFromServer receiver;
        private PuppetManager manager;
        private InputHandler inputHandler;
        private HeartbeatHandler heartbeatHandler;

        private AuthenticateRoomConnection chatAuth;
        private ChatMessageIntepreter interpreter;
        private ChatHandler chat;

        private bool alreadyShutdown = false;

        public ClientStates State { get => state; }

        public ClientStateHandler()
        {
            _instance = this;
            state = ClientStates.Login;
            StateMachine();
        }
        private void StateMachine()
        {
            while (true)
            {
                switch (state)
                {
                    case ClientStates.Login:
                        LoginState();
                        break;
                    case ClientStates.ChatAuthenticating:
                        ChatAuthenticate();
                        break;
                    case ClientStates.ChatSetup:
                        ChatWindowSetup();
                        break;
                    case ClientStates.SetupServerBrowser:
                        SetupServerBrowserState();
                        break;
                    case ClientStates.ServerBrowserActive:
                        ServerBrowserActiveState();
                        break;
                    case ClientStates.Authenticating:
                        AuthState();
                        break;
                    case ClientStates.PlaySetup:
                        PlaySetupState();
                        break;
                    case ClientStates.Playing:
                        PlayingState();
                        break;
                    default:
                        break;
                }
            }
        }
        private void LoginState()
        {
            //If this is pre-decided for debugging, skip this state
            if(ClientInfo.jwt != null)
            {
                Program.WriteInDebug("Debug login. Press any key to continue");
                Console.ReadKey(true);
                state = ClientStates.ChatAuthenticating;
            }
            else
            {
                loginHandler = new LoginHandler(Program.loginAddress, Program.accountRegisterAddress);
                loginHandler.StartLoginScreen();

                if (loginHandler.LoginSuccess)
                {
                    state = ClientStates.ChatAuthenticating;
                }
            }
            if (useHeartbeat)
            {
                heartbeatHandler = new HeartbeatHandler(Program.heartbeatAddress, Program.heartbeatInterval);
                heartbeatHandler.StartHeartbeat();
            }
        }
        private void ChatAuthenticate()
        {
            chatAuth = new AuthenticateRoomConnection(Program.chatRelayAddress, Program.chatRelayPort);
            chatAuth.ConnectAndAuth();
            if(chatAuth.AuthSuccess)
            {
                interpreter = new ChatMessageIntepreter(chatAuth.Client);
                state = ClientStates.ChatSetup;
            }
        }
        private void ChatWindowSetup()
        {
            chat = new ChatHandler(interpreter);
            if (chatOnly)
            {
                while (true)
                {
                    ChatFocusedFromAny();
                }
            }
            else
            {
                state = ClientStates.SetupServerBrowser;
            }
        }
        private void SetupServerBrowserState()
        {
            if(useLocalRoom)
            {
                authConnect = new AuthenticateRoomConnection("127.0.0.1", 7777);
                state = ClientStates.Authenticating;
                return;
            }
            browser = new ServerBrowser(Program.roomRegistryAddress);
            browser.Start();
            state = ClientStates.ServerBrowserActive;
        }
        private void ServerBrowserActiveState()
        {
            browser.StateFedInputLoop();
        }
        public void RoomSelected(RoomForClientDTO room)
        {
            authConnect = new AuthenticateRoomConnection(room.TcpIPAddress, room.TcpPort);
            state = ClientStates.Authenticating;
        }
        private void AuthState()
        {
            authConnect.ConnectAndAuth();
            if (authConnect.AuthSuccess)
            {
                state = ClientStates.PlaySetup;
            }
        }
        private void PlaySetupState()
        {
            sender = new SendToServer(authConnect.Client);
            receiver = new ReceiveFromServer(authConnect.Client);
            receiver.ReceiveUpdateLoop();
            manager = new PuppetManager(sender, receiver);
            manager.GetGameWorld();
            inputHandler = new InputHandler(sender);

            state = ClientStates.Playing;
        }
        private void PlayingState()
        {
            inputHandler.StateFedInputLoop();
        }
        /// <summary>
        /// Call this from any input area where you want to switch to the chat window instead.
        /// When client presses tab again, execution will go back up, out to where you were when
        /// you called ChatFocusedFromAny.
        /// </summary>
        public void ChatFocusedFromAny()
        {
            chat.ChatFocused();
        }
        public void LostServerConnection()
        {
            if (alreadyShutdown)
            {
                alreadyShutdown = false;
                return;
            }
            Program.WriteInDebug("Server shutdown");
            ResetGame();
            ResetReceiver();
            ResetSender();
            ResetAuth();

            state = ClientStates.SetupServerBrowser;
        }
        public void FailedHeartbeat()
        {
            alreadyShutdown = true;
            ResetGame();
            ResetReceiver();
            ResetSender();
            ResetAuth();
            ResetBrowser();
            ResetChat();
            Program.WriteInDebug("Heartbeat failed, press any key to continue");
            state = ClientStates.Login;
        }
        private void ResetSender()
        {
            if (sender == null) return;
            sender.Shutdown();
            sender = null;
        }
        private void ResetReceiver()
        {
            if (receiver == null) return;
            receiver.Shutdown();
            receiver = null;
        }
        private void ResetAuth()
        {
            if (authConnect == null) return;
            authConnect.Client.Close();
            authConnect = null;
        }
        private void ResetHeartbeat()
        {
            if (heartbeatHandler == null) return;
            heartbeatHandler.StopHeartbeat();
            heartbeatHandler = null;
        }
        private void ResetBrowser()
        {
            if (browser == null) return;
            browser.Shutdown();
            browser = null;
        }
        private void ResetChat()
        {
            if (chat == null) return;
            chat.Shutdown();
            chat = null;
        }
        private void ResetGame()
        {
            if(manager == null) return;
            manager.Shutdown();
            manager = null;
        }
    }
}
