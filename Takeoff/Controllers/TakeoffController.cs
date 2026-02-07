using System;
using Microsoft.AspNetCore.Mvc;
using Contracts;
using Takeoff.StateManagement;

namespace Takeoff.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class TakeoffController : ControllerBase
    {
        [HttpGet]
        public ActionResult<TakeoffState> GetQuantities()
        {
            var service = new StateService();
            var state = service.GetDefaultTakeoffState();
            return Ok(state);
        }
    }
}
