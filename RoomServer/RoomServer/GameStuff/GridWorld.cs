using MonkeyClient.DTOs.UpdateDTOs;
using Newtonsoft.Json;
using RoomServer.DTOs;
using System.Drawing;

namespace RoomServer.GameStuff
{
    public enum Directions
    {
        Up,
        Left,
        Right,
        Down
    }
    public class GridWorld
    {
        private static GridWorld _instance;
        public static GridWorld Instance
        {
            get
            {
                if (_instance == null)
                {
                    Console.WriteLine("Error, singleton fail for Gridworld");
                }
                return _instance;
            }
        }

        public int width;
        public int height;
        private Dictionary<Puppet, Point> puppetPositions;
        public GridWorld(int width, int height)
        {
            _instance = this;
            this.width = width;
            this.height = height;
            puppetPositions = new Dictionary<Puppet, Point>();
        }
        public void AddPuppet(Puppet puppet)
        {
            Point pos = new Point(0, 0);
            puppetPositions.Add(puppet, pos);

            string content = ConstructMovementUpdate(puppet.UserName, pos);

            PuppetManager.Instance.SendUpdateToAllButOne(puppet.UserName, content);
        }
        public void RemovePuppet(Puppet puppet)
        {
            puppetPositions.Remove(puppet);
        }
        public bool TryMove(Puppet puppet, Directions dir)
        {
            if (puppetPositions.ContainsKey(puppet) == false) return false; 

            Point newPos = puppetPositions[puppet];
            switch (dir)
            {
                case Directions.Up:
                    newPos.Y = newPos.Y - 1;
                    break;
                case Directions.Left:
                    newPos.X = newPos.X - 1;
                    break;
                case Directions.Right:
                    newPos.X = newPos.X + 1;
                    break;
                case Directions.Down:
                    newPos.Y = newPos.Y + 1;
                    break;
                default:
                    break;
            }
            bool legal = CheckNewPos(newPos);
            if (legal)
            {
                puppetPositions[puppet] = newPos;
                string json = ConstructMovementUpdate(puppet.UserName, newPos);
                PuppetManager.Instance.SendUpdateToAll(json);
            }
            return legal;
        }
        private string ConstructMovementUpdate(string username, Point position)
        {
            var json = JsonConvert.SerializeObject(new PuppetMovementUpdateDTO() { Username = username, Position = position });
            var wrappedUpdate = new WrappedPuppetUpdateDTO()
            {
                UpdateType = PuppetManager.movementUpdateKey,
                JsonUpdateData = json
            };
            string content = JsonConvert.SerializeObject(wrappedUpdate);
            return content;
        }
        private bool CheckNewPos(Point pos)
        {
            if(pos.X >= 0 && pos.X < width && pos.Y >= 0 && pos.Y < height)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public List<(string, Point)> GetSimplifiedClientPositions()
        {
            List<(string, Point)> simpelPositions = new List<(string, Point)>();
            foreach (var pair in puppetPositions)
            {
                simpelPositions.Add((pair.Key.UserName, pair.Value));
            }
            return simpelPositions;
        }
    }
}
