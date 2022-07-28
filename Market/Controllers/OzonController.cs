using Market.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StinClasses;
using System.Threading;
using System.Threading.Tasks;

namespace Market.Controllers
{
    [ApiController]
    [Route("ozon")]
    public class OzonController : ControllerBase
    {
        private IServiceScopeFactory _serviceScopeFactory;
        private IConfiguration _configuration;
        public OzonController(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
        }
        [HttpPost("order/int_items")]
        public async Task<ActionResult<string>> OrderIntReduceItems([FromBody] RequestedDocId requestedDocId, CancellationToken cancellationToken)
        {
            using IBridge1C bridge = _serviceScopeFactory.CreateScope()
                .ServiceProvider.GetService<IBridge1C>();
            var result = await bridge.ReduceCancelItems(requestedDocId.DocId, cancellationToken);
            return Ok(result);
        }
    }
}
