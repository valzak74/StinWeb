using Market.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using OzonClasses;
using System.Threading.Tasks;
using WbClasses;
using Microsoft.AspNetCore.Http;
using System;

namespace Market.Controllers
{
    [ApiController]
    [Route("ozon")]
    public class OzonController : ControllerBase
    {
        private IServiceScopeFactory _serviceScopeFactory;
        private IConfiguration _configuration;
        readonly string _apiVersion;
        readonly string _apiName;
        public OzonController(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
            var defFirma = _configuration["Settings:Firma"];
            _apiName = _configuration["Settings:" + defFirma + ":OzonClientName"];
            _apiVersion = _configuration["Settings:" + defFirma + ":OzonClientVersion"];
        }
        [HttpPost("push_notifications")]
        public ActionResult<PushResponse> PushNotification([FromBody] PushRequest requestedPush, CancellationToken cancellationToken)
        {
            var response = new PushResponse();
            if (requestedPush == null) 
            {
                response.Error = new PushResponse.PushError { Code = PushResponse.PushError.ErrorCode.ERROR_PARAMETER_VALUE_MISSED, Message = "No push-request body" };
                return StatusCode((int)StatusCodes.Status415UnsupportedMediaType, response);
            }
            switch (requestedPush.Message_type)
            {
                case PushType.TYPE_PING: 
                    response.Version = _apiVersion;
                    response.Name = _apiName;
                    response.Time = DateTime.UtcNow;
                    break;
                default:
                    response.Error = new PushResponse.PushError { Message = "Message type not supported yet" };
                    return StatusCode((int)StatusCodes.Status415UnsupportedMediaType, response);
            }
            return Ok(response);
        }
    }
}
