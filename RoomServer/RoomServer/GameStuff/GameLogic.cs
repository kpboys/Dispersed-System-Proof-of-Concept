using MonkeyClient.DTOs.UpdateDTOs;
using Newtonsoft.Json;
using RoomServer.DTOs;

namespace RoomServer.GameStuff
{
    public class GameLogic
    {
        private static GameLogic _instance;
        public static GameLogic Instance
        {
            get
            {
                if (_instance == null)
                {
                    Console.WriteLine("Error, singleton fail for GameLogic");
                }
                return _instance;
            }
        }
        public const string upCommand = "Up";
        public const string leftCommand = "Left";
        public const string rightCommand = "Right";
        public const string downCommand = "Down";
        public const string setupCommand = "Setup";
        public GameLogic()
        {
            _instance = this;
        }
        public void ReceiveCommand(Puppet puppet, string json)
        {
            //Maybe a try or something here
            GameInputDTO inputDTO = null;
            try
            {
                inputDTO = JsonConvert.DeserializeObject<GameInputDTO>(json);
            }
            catch
            {

            }
            if (inputDTO == null) return;

            bool legal = false;
            switch (inputDTO.Command)
            {
                case upCommand:
                    legal = GridWorld.Instance.TryMove(puppet, Directions.Up);
                    break;
                case leftCommand:
                    legal = GridWorld.Instance.TryMove(puppet, Directions.Left);
                    break;
                case rightCommand:
                    legal = GridWorld.Instance.TryMove(puppet, Directions.Right);
                    break;
                case downCommand:
                    legal = GridWorld.Instance.TryMove(puppet, Directions.Down);
                    break;
                case setupCommand:
                    SendSetupInfo(puppet);
                    legal = true;
                    break;
                default:
                    break;
            }
            if(legal == false)
            {
                //Send command for a bad move back here
            }

        }
        private void SendSetupInfo(Puppet puppet)
        {
            InitialSetupDTO setupDTO = new InitialSetupDTO()
            {
                Width = GridWorld.Instance.width,
                Height = GridWorld.Instance.height,
                Puppets = GridWorld.Instance.GetSimplifiedClientPositions()
            };
            string dtoJSON = JsonConvert.SerializeObject(setupDTO);
            WrappedPuppetUpdateDTO wrappedDTO = new WrappedPuppetUpdateDTO()
            {
                UpdateType = setupCommand,
                JsonUpdateData = dtoJSON
            };
            puppet.SendUpdate(JsonConvert.SerializeObject(wrappedDTO));
        }
    }
}
