using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionService.Interfaces;
using SessionService.Models;
using System.Text.Json;

namespace SessionService.Controllers
{
    [Route("api")]
    [ApiController]
    public class SessionController : ControllerBase
    {

        private readonly ISessionDatabase database;
        private readonly HttpMessageHandler? handler;

        private readonly Random random;
        private const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private readonly Publisher publisher;
        private readonly Consumer consumer;

        public SessionController(ISessionDatabase database, Publisher publisher, Consumer consumer, HttpMessageHandler? handler = null)
        {
            this.database = database;
            if (handler != null)
                this.handler = handler;
            random = new Random();

            this.publisher = publisher;
            this.consumer = consumer;
            this.consumer.SetDatabase(this.database);
        }

        [HttpPost("startsession")]
        public ActionResult StartSession([FromBody] UsernameDto clientInput)
        {
            string sessionId = CreateSessionId(clientInput.Username);
            Session session = new Session() { RoomName = "", Username = clientInput.Username, SessionId = sessionId };

            bool result = database.AddSession(session);

            if (!result)
                return Conflict("Username session conflict.");
            else
            {
                var request = HttpContext.Request;
                var fullUri = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
                
                return Created(fullUri, JsonSerializer.Serialize(new SessionIdDto() { SessionId=sessionId }));
            }
        }

        [HttpPost("endsession"), Authorize]
        public ActionResult EndSession([FromBody] UsernameDto clientInput)
        {
            bool result = database.RemoveSession(clientInput.Username);

            if (!result)
                return NotFound("User not found.");
            else
            {
                if (publisher != null)
                    publisher.Publish(new StatusDto { Username = clientInput.Username, IsAlive = false });
                return Ok();
            }
        }

        [HttpPost("checksessionid")]
        public ActionResult CheckSessionId([FromBody] IdCheckDto checkDto)
        {
            string username = checkDto.Username;
            if (username == null)
                return NotFound("User not found.");

            string sessionId = checkDto.SessionId;
            if (sessionId == null)
                return NotFound("Id not found.");

            bool check = database.CheckSessionId(username, sessionId);
            if (check)
                return Ok();
            else
                return NotFound("Id not found.");
        }

        [HttpPost("isuseronline")]
        public ActionResult IsUserOnline([FromBody] UsernameDto clientInput)
        {
            Session? session = database.GetSessionByUsername(clientInput.Username);
            if (session == null)
                return NotFound("User not found.");
            else
                return Ok();
        }

        [HttpGet("getuserstate/{username}")]
        public ActionResult GetUserState(string username)
        {
            Session? session = database.GetSessionByUsername(username);
            if (session == null)
                return NotFound("User not found.");
            else
            {
                StateDto stateDto = new StateDto() { RoomName = session.RoomName, SessionId = session.SessionId, Username = session.Username };
                return Ok(stateDto);
            }
        }

        [HttpGet("getusersinroom/{room}")]
        public ActionResult GetUsersInRoom(string room)
        {
            List<Session>? sessions = database.GetSessionsByRoom(room);
            if (sessions == null)
                return NotFound("Room not found.");
            else
            {
                List<UsernameDto> usernames = new List<UsernameDto>();
                sessions.ForEach(x => usernames.Add(new UsernameDto() { Username = x.Username }));
                return Ok(usernames);
            }
        }

        [HttpGet("getuserroom/{username}")]
        public ActionResult GetUserRoom(string username)
        {
            Session? session = database.GetSessionByUsername(username);
            if (session == null)
                return NotFound("User not found.");
            else
            {
                RoomDto roomDto = new RoomDto() { RoomName = session.RoomName, };
                return Ok(roomDto);
            }
        }

        [HttpGet("getusersinroomwithuser/{username}")]
        public ActionResult GetUsersInRoomWithUser(string username)
        {
            List<Session>? sessions = database.GetSessionsInRoomWithUser(username);
            if (sessions == null)
                return NotFound("User not found.");
            else
            {
                List<UsernameDto> usernames = new List<UsernameDto>();
                foreach (Session session in sessions)
                {
                    if (session.Username == username)
                        continue;
                    else
                        usernames.Add(new UsernameDto() { Username = session.Username });
                }
                return Ok(usernames);
            }
        }

        private string CreateSessionId(string username)
        {
            string time = DateTime.Now.ToString();
            string ranString = RandomString(10);

            string sessionId = username + time + ranString;
            return sessionId;
        }

        private string RandomString(int size)
        {
            char[] buffer = new char[size];

            for (int i = 0; i < size; i++)
            {
                buffer[i] = alphabet[random.Next(alphabet.Length)];
            }
            return new string(buffer);
        }

    }
}
