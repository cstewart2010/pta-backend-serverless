using System.Collections.Generic;
using TheReplacement.PTA.Api.Abstractions;
using TheReplacement.PTA.Api.Services.Enums;
using TheReplacement.PTA.Api.Services.Models;
using TheReplacement.PTA.Api.Services;
using Microsoft.Extensions.Logging;
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
    internal class OriginsController : BaseStaticController
    {
        private const string RoutePrefix = "v1/origindex";
        private static readonly IEnumerable<OriginModel> Origins = DexUtility.GetDexEntries<OriginModel>(DexType.Origins);

        public OriginsController(ILogger<OriginsController> log)
        {
            _logger = log;
        }

        [FunctionName("GetAllOrigins")]
        [OpenApiOperation(operationId: "GetAllOrigins")]
        [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StaticCollectionMessage))]
        public IActionResult GetAllOrigins(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = RoutePrefix)] HttpRequest req)
        {
            return new OkObjectResult(GetStaticCollectionResponse(Origins, req));
        }

        [FunctionName("GetOriginByName")]
        [OpenApiOperation(operationId: "GetOriginByName")]
        [OpenApiParameter(name: "name", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(OriginModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string))]
        public IActionResult GetOriginByName(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{name}}")] HttpRequest req,
            string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult($"You forgot the {nameof(name)} route parameter");
            }

            var document = Origins.GetStaticDocument(name);
            if (document != null)
            {
                return new OkObjectResult(document);
            }

            return new NotFoundObjectResult(name);
        }
    }
}
