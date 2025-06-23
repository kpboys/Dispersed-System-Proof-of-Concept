using Microsoft.AspNetCore.Mvc;

namespace RoomServer.Controllers
{
    [ApiController]
    public class HealthCheckController : Controller
    {
        public HealthCheckController()
        {
            //Console.WriteLine("Healthcheck set up");
        }
        [HttpGet("hp")]
        public ActionResult GetHealth()
        {
            Console.WriteLine("Received health check request");
            return Ok(/*Data in here*/);
        }
    }
}
