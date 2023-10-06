using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using TheReplacement.PTA.Api.Abstractions;
using TheReplacement.PTA.Api.Services.Enums;
using TheReplacement.PTA.Api.Services.Models;
using TheReplacement.PTA.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs;
using Microsoft.OpenApi.Models;
using System.Net;
using TheReplacement.PTA.Api.Messages;
using TheReplacement.PTA.Api.Extensions;

namespace TheReplacement.PTA.Api.Controllers
{
    public class BerryController : BaseStaticController
    {
        private readonly ILogger<BerryController> _logger;
        private const string RoutePrefix = "v1/berrydex";
        private static readonly IEnumerable<BerryModel> Berries = DexUtility.GetDexEntries<BerryModel>(DexType.Berries);

        public BerryController(ILogger<BerryController> log)
        {
            _logger = log;
        }

        [FunctionName("GetAllBerries")]
        [OpenApiOperation(operationId: "GetAllBerries")]
        [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "The **Offset** parameter")]
        [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "The **Limit** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StaticCollectionMessage), Description = "The OK response")]
        public IActionResult GetAllBerries(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = RoutePrefix)] HttpRequest req)
        {
            if (!int.TryParse(req.Query["offset"], out var offset))
            {
                offset = 0;
            }
            if (!int.TryParse(req.Query["limit"], out var limit))
            {
                limit = 20;
            }

            return new OkObjectResult(GetStaticCollectionResponse(Berries, offset, limit));
        }

        [FunctionName("GetBerryByName")]
        [OpenApiOperation(operationId: "GetBerryByName")]
        [OpenApiParameter(name: "name", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(BerryModel), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Description = "The Not Found response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The Bad response")]
        public IActionResult GetBerryByName(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{name}}")] HttpRequest req,
            string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult($"You forgot the {nameof(name)} route parameter");
            }

            var document = Berries.GetStaticDocument(name);
            if (document != null)
            {
                return new OkObjectResult(document);
            }

            return new NotFoundObjectResult(name);
        }
    }
}
