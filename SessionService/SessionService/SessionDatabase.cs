using SessionService.Interfaces;
using SessionService.Models;

namespace SessionService
{
    public class SessionDatabase : ISessionDatabase
    {

        private readonly Dictionary<string, Session> allUsers; //List of all users sessions, Key = username, Value = user session
        private readonly Dictionary<string, Dictionary<string, Session>> allRooms; //Key = room name, Value = List of user sessions in room. Inner list Key = username, Value = user session

        public SessionDatabase()
        {
            allUsers = new Dictionary<string, Session>();
            allRooms = new Dictionary<string, Dictionary<string, Session>>();
        }

        public bool AddSession(Session session)
        {
            if (allUsers.ContainsKey(session.Username))
                return false;
            else
            {
                allUsers.Add(session.Username, session);
                if (!string.IsNullOrWhiteSpace(session.RoomName))
                    AddToRoom(session, session.RoomName);
                return true;
            }
        }

        public bool RemoveSession(string username)
        {
            if (!allUsers.ContainsKey(username))
                return false;
            else
            {
                Session user = allUsers[username];
                if (!string.IsNullOrWhiteSpace(user.RoomName))
                {
                    RemoveUserFromRoom(username, user.RoomName);
                }

                allUsers.Remove(username);
                return true;
            }
        }

        public bool UpdateSession(string username, Session updatedSession)
        {
            if (!allUsers.ContainsKey(username))
                return false;

            Session user = allUsers[username];

            string newRoom = updatedSession.RoomName;
            string oldRoom = user.RoomName;

            allUsers[username] = updatedSession;

            if (newRoom != oldRoom)
            {
                if (!string.IsNullOrWhiteSpace(oldRoom) && allRooms.ContainsKey(oldRoom))
                    RemoveUserFromRoom(username, oldRoom);
                if (!string.IsNullOrWhiteSpace(newRoom))
                    AddToRoom(user, newRoom);
            }

            return true;
        }

        public bool UpdateRoom(string username, string roomName)
        {
            if (!allUsers.ContainsKey(username))
                return false;

            Session user = allUsers[username];

            string newRoom = roomName;
            string oldRoom = user.RoomName;

            if (newRoom != oldRoom)
            {
                allUsers[username].RoomName = roomName;
                if (!string.IsNullOrWhiteSpace(oldRoom) && allRooms.ContainsKey(oldRoom))
                    RemoveUserFromRoom(username, oldRoom);
                if (!string.IsNullOrWhiteSpace(newRoom))
                    AddToRoom(user, newRoom);
            }
            return true;
        }

        public bool CheckSessionId(string username, string sessionId)
        {
            if (!allUsers.ContainsKey(username))
                return false;

            Session user = allUsers[username];
            return user.SessionId == sessionId;
        }

        public Session? GetSessionByUsername(string username)
        {
            if (!allUsers.ContainsKey(username))
                return null;
            else
                return allUsers[username];
        }

        public List<Session>? GetSessionsInRoomWithUser(string username)
        {
            if (!allUsers.ContainsKey(username))
                return null;
            string roomName = allUsers[username].RoomName;

            if (!allRooms.ContainsKey(roomName))
                return null;

            Dictionary<string, Session> room = allRooms[roomName];
            return room.Values.ToList();
        }

        public List<Session>? GetSessionsByRoom(string roomName)
        {
            if (!allRooms.ContainsKey(roomName))
                return null;

            Dictionary<string, Session> room = allRooms[roomName];
            return room.Values.ToList();
        }

        private void AddToRoom(Session session, string roomName)
        {
            if (!allRooms.ContainsKey(roomName))
            {
                allRooms.Add(roomName, new Dictionary<string, Session>());
            }

            session.RoomName = roomName;

            allRooms[roomName].Add(session.Username, session); ;
        }

        private bool RemoveUserFromRoom(string username, string roomName)
        {
            if (!allRooms.ContainsKey(roomName))
                return false;

            Dictionary<string, Session> room = allRooms[roomName];

            if (!room.ContainsKey(username))
                return false;

            room.Remove(username);

            if (room.Count <= 0)
                allRooms.Remove(roomName);

            return true;
        }

    }
}
