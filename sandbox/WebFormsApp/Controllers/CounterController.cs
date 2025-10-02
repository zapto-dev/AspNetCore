using Microsoft.AspNetCore.Mvc;
using WebFormsApp.Modules;

namespace WebFormsApp.Controllers
{
    public class CounterController : ControllerBase
    {
        private readonly CounterService _counterService;

        public CounterController(CounterService counterService)
        {
            _counterService = counterService;
        }

        [HttpPost("handler/counter/reset")]
        public IActionResult Reset()
        {
            _counterService.Reset();
            return Ok("Counter reset.");
        }

        [HttpPost("handler/counter/increment")]
        public IActionResult IncrementHandler()
        {
            return Ok($"Count: {_counterService.Increment()}");
        }

        [HttpPost("module/counter/reset")]
        public IActionResult ResetModule()
        {
            _counterService.Reset();
            return Ok("Counter reset.");
        }

        [HttpPost("module/counter/increment")]
        public IActionResult IncrementModule()
        {
            return Ok($"Count: {_counterService.Increment()}");
        }
    }
}
