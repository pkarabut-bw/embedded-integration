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
        private readonly StateService _service;

        public TakeoffController(StateService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<TakeoffState> GetQuantities()
        {
            var state = _service.GetState();
            return Ok(state);
        }
    }
}
