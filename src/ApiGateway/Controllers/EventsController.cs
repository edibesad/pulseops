using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Events;

namespace ApiGateway.Controllers
{

    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly IEventPublisher _publisher;

        public EventsController(IEventPublisher publisher)
        {
            _publisher = publisher;
        }

        [HttpPost("incident-started")]
        public async Task<IActionResult> IncidentStarted([FromBody] IncidentStarted evt)
        {

            await _publisher.PublishAsync(evt);
            return Accepted(new { ok = true, received = evt });
        }

        [HttpPost("incident-ended")]
        public async Task<IActionResult> IncidentEnded([FromBody] IncidentEnded evt)
        {
            await _publisher.PublishAsync(evt);
            return Accepted(new { ok = true, received = evt });
        }
    }
}
