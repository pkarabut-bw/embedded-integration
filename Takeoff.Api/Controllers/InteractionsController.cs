using Microsoft.AspNetCore.Mvc;
using Takeoff.Api.Services;
using Contracts;

namespace Takeoff.Api.Controllers
{
    [ApiController]
    [Route("api/interactions")]
    public class InteractionsController : ControllerBase
    {
        private readonly TakeoffDataStore _store;

        public InteractionsController(TakeoffDataStore store)
        {
            _store = store;
        }

        [HttpGet("projects/{projectId:guid}/conditions")]
        public ActionResult<List<Condition>> GetAll(Guid projectId)
        {
            var list = _store.GetAll(projectId);
            return Ok(list);
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok("ok");
    }
}