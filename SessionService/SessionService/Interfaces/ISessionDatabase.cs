using SessionService.Models;

namespace SessionService.Interfaces
{
    public interface ISessionDatabase
    {

        public bool AddSession(Session session);

        public bool RemoveSession(string username);

        public bool UpdateSession(string username, Session updatedSession);

        public bool UpdateRoom(string username, string newRoom);

        public bool CheckSessionId(string username, string sessionId);

        public Session? GetSessionByUsername(string username);

        public List<Session>? GetSessionsInRoomWithUser(string username);

        public List<Session>? GetSessionsByRoom(string roomName);

    }
}
