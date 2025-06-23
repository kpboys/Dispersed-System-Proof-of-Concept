using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RoomServer.GameStuff;

namespace RoomServer.Controllers
{
    [ApiController]
    public class PlayerInfoController : Controller
    {
        [HttpGet("getplayerlist")]
        public ActionResult GetPlayerList()
        {
            List<string> listJson = PuppetManager.Instance.activePuppets.Keys.ToList<string>();
            return Ok(JsonConvert.SerializeObject(listJson));
        }
    }
}
