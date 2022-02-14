using System.Threading.Tasks;
using MassTransit;
using MassTransitDemo.Common.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MassTransitDemo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : Controller
    {
        readonly IPublishEndpoint _publishEndpoint;

        public MessageController(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        [HttpGet]
        public async Task<ActionResult> Post(string message)
        {
            await _publishEndpoint.Publish<Message>(new Message
            {
                Text = message
            });

            return Ok();
        }
    }
}
