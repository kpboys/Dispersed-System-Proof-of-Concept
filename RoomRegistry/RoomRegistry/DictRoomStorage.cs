using RoomRegistry.DataObjects;

namespace RoomRegistry
{
    public interface IRoomStorage
    {
        bool AddRoom(RoomDetails details);
        bool RemoveRoom(string roomName);
        RoomDetails GetRoom(string roomName);
        List<RoomDetails> GetAllRooms();
        string GetUnusedRoomName();
    }
    public class DictRoomStorage : IRoomStorage
    {
        private Dictionary<string, RoomDetails> rooms;
        private string[] names;
        private int counter;
        public DictRoomStorage()
        {
            rooms = new Dictionary<string, RoomDetails>();
            names =["Europe", "NorthAmerica", "SouthAmerica", "Asia", "Africa"];
            counter = 0;
        }
        public bool AddRoom(RoomDetails details)
        {
            if (rooms.ContainsKey(details.RoomName)) { return false; }
            rooms.Add(details.RoomName, details);
            return true;
        }
        public bool RemoveRoom(string roomName)
        {
            if (rooms.ContainsKey(roomName) == false) return false;
            rooms.Remove(roomName);
            return true;
        }

        public List<RoomDetails> GetAllRooms()
        {
            List<RoomDetails> simpRooms = new List<RoomDetails>();
            foreach (var pair in rooms)
            {
                simpRooms.Add(pair.Value);
            }
            return simpRooms;
        }

        public RoomDetails GetRoom(string roomName)
        {
            throw new NotImplementedException();
        }

        //Make this just count up, adding a number to the name, later
        public string GetUnusedRoomName()
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (rooms.ContainsKey(names[i]) == false)
                {
                    return names[i];
                }
            }
            return "";
            //string name = names[counter];
            //counter++;
            //return name;
        }
    }
    
}
