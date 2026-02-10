using Microsoft.AspNetCore.Mvc;

namespace Takeoff.Api.Controllers
{
    [ApiController]
    [Route("api/interactions")]
    public class InteractionsController : ControllerBase
    {
        [HttpGet("health")]
        public IActionResult Health() => Ok("ok");
    }
}