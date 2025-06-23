using HealthService.Classes;
using HealthService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthService.Controllers
{
    [ApiController]
    public class HeartbeatController : Controller
    {
        private TrackerHandler trackerHandler;
        public HeartbeatController(TrackerHandler trackerHandler)
        {
            //int count = int.Parse(Environment.GetEnvironmentVariable(Program.clientsCheckedPerFrame));
            this.trackerHandler = trackerHandler; 
        }

        [HttpPost("firstHeartbeat"), Authorize]
        public ActionResult FirstHeartBeat([FromBody] ClientHeartbeatDTO heartbeatDTO)
        {
            string? authorizationHeader = HttpContext.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
                return Unauthorized("Bad token.");
            string token = authorizationHeader.Replace("Bearer ", "");
            //Session check here
            bool trackerResult = trackerHandler.AddClientToTrack(heartbeatDTO.Username, token);
            if(trackerResult)
            {
                Console.WriteLine("Added client: " + heartbeatDTO.Username);
                return Ok();
            }
            else
            {
                Console.WriteLine("Client: " + heartbeatDTO.Username + " tried to do first heartbeat, but already existed.");
                return BadRequest();
            }
        }
        /// <summary>
        /// Heartbeat POST endpoint that the Client calls at regular intervals. 
        /// Make async later when it starts contacting SessionService
        /// </summary>
        [HttpPost("heartbeat"), Authorize]
        public ActionResult BeatHeart([FromBody] ClientHeartbeatDTO heartbeatDTO)
        {
            //Session check here
            //Console.WriteLine("Checking from controller: " + heartbeatDTO.Username);
            bool trackerResult = trackerHandler.CheckClientTracker(heartbeatDTO.Username, true);
            if(trackerResult)
            {
                Console.WriteLine("Received beat on time from: " + heartbeatDTO.Username);
                return Ok();
            }
            else
            {
                Console.WriteLine("Received beat late from: " + heartbeatDTO.Username);
                //Kinda weird, maybe use a different one
                return StatusCode(StatusCodes.Status419AuthenticationTimeout); 
            }
        }
    }
}
