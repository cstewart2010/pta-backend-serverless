using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;
using TheReplacement.PTA.Api.Models;

namespace TheReplacement.PTA.Api.Controllers
{
    internal class HomeController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> log)
        {
            _logger = log;
        }

        [FunctionName("GetIndicies")]
        [OpenApiOperation(operationId: "GetIndicies")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IndicesModel))]
        public IActionResult GetIndicies(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "")] HttpRequest req)
        {
            return new OkObjectResult(new IndicesModel(req));
        }
    }
}
