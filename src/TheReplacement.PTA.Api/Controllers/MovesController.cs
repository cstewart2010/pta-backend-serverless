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
    public class MovesController : BaseStaticController
    {
        private const string RoutePrefix = "v1/movedex";
        private static readonly IEnumerable<MoveModel> Moves = DexUtility.GetDexEntries<MoveModel>(DexType.Moves);

        public MovesController(ILogger<MovesController> log)
        {
            _logger = log;
        }

        [FunctionName("GetAllMoves")]
        [OpenApiOperation(operationId: "GetAllMoves")]
        [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StaticCollectionMessage))]
        public IActionResult GetAllMoves(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = RoutePrefix)] HttpRequest req)
        {
            return new OkObjectResult(GetStaticCollectionResponse(Moves, req));
        }

        [FunctionName("GetMoveByName")]
        [OpenApiOperation(operationId: "GetMoveByName")]
        [OpenApiParameter(name: "name", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(BerryModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string))]
        public IActionResult GetMoveByName(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{name}}")] HttpRequest req,
            string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult($"You forgot the {nameof(name)} route parameter");
            }

            var document = Moves.GetStaticDocument(name);
            if (document != null)
            {
                return new OkObjectResult(document);
            }

            return new NotFoundObjectResult(name);
        }
    }
}
