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
        public IActionResult IncidentStarted([FromBody] IncidentStarted evt)
        {

            _publisher.Publish(evt);
            return Accepted(new { ok = true, received = evt });
        }

        [HttpPost("incident-ended")]
        public IActionResult IncidentEnded([FromBody] IncidentEnded evt)
        {
            _publisher.Publish(evt);
            return Accepted(new { ok = true, received = evt });
        }
    }
}
