using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RoomRegistry.DataObjects;

namespace RoomRegistry.Controllers
{
    [Route("api/")]
    [ApiController]
    public class RegistryController : Controller
    {
        private IRoomStorage storage;
        public RegistryController(IRoomStorage storage) 
        {
            this.storage = storage; 
        }

        [HttpPost("register")]
        public async Task<ActionResult> RegisterRoom([FromBody] RoomRegistryDetailsDTO details)
        {
            Console.WriteLine("Post received");
            bool isHealthy = await PerformHealthCheck(details.HealthCheckUrl);

            if (isHealthy)
            {
                string roomName = storage.GetUnusedRoomName();
                RoomDetails storedDetails = new RoomDetails()
                {
                    RoomName = roomName,
                    HealthcheckUrl = details.HealthCheckUrl,
                    TcpIPAddress = details.RoomTcpAddress,
                    TcpPort = details.RoomTcpPort
                };
                if (storage.AddRoom(storedDetails))
                {
                    Console.WriteLine("Successfully registered " + storedDetails.RoomName);
                    return Ok();
                }
            }

            Console.WriteLine("Could not register room: \nHealthcheck: " + isHealthy);
            return BadRequest(new { message = "Could not register room." });
            
        }
        [HttpGet("getroom")] //Add JWT auth here
        public ActionResult GetRandomRoomToJoin()
        {
            return Ok();

        }
        [HttpGet("getroomlist"), Authorize]
        public async Task<ActionResult> GetRoomList()
        {
            Console.WriteLine("Received get all room call");
            List<RoomForClientDTO> clientRooms = new List<RoomForClientDTO>();
            List<RoomDetails> currentDetails = storage.GetAllRooms();
            List<RoomDetails> roomDetails = await DoHealthCheckOnRooms(currentDetails);
            
            for (int i = 0; i < roomDetails.Count; i++)
            {
                RoomForClientDTO room = new RoomForClientDTO()
                {
                    RoomName = roomDetails[i].RoomName,
                    TcpIPAddress = roomDetails[i].TcpIPAddress,
                    TcpPort = roomDetails[i].TcpPort
                };
                clientRooms.Add(room);
            }
            RoomListDTO roomList = new RoomListDTO()
            {
                Rooms = clientRooms
            };
            return Ok(JsonConvert.SerializeObject(roomList));
        }
        private async Task<bool> PerformHealthCheck(string roomUrl)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    Console.WriteLine("Checking on " + roomUrl);
                    var response = await client.GetAsync($"{roomUrl}hp");
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
        /// <summary>
        /// Check the given rooms health status and clear them from the storage if they're bad
        /// </summary>
        /// <param name="roomDetails"></param>
        /// <returns></returns>
        private async Task<List<RoomDetails>> DoHealthCheckOnRooms(List<RoomDetails> roomDetails)
        {
            List<RoomDetails> clearedList = new List<RoomDetails>();
            for (int i = 0; i < roomDetails.Count; i++)
            {
                bool healthCheck = await PerformHealthCheck(roomDetails[i].HealthcheckUrl);
                if(healthCheck)
                {
                    clearedList.Add(roomDetails[i]);
                }
                else
                {
                    storage.RemoveRoom(roomDetails[i].RoomName);
                }
            }
            return clearedList;
        }
    } 
}
