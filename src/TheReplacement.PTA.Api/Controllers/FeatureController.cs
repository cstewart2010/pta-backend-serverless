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
    internal class FeatureController : BaseStaticController
    {
        private const string RoutePrefix = "v1/featuredex";
        private static readonly IEnumerable<FeatureModel> GeneralFeatures = DexUtility.GetDexEntries<FeatureModel>(DexType.Features);
        private static readonly IEnumerable<FeatureModel> LegendaryFeatures = DexUtility.GetDexEntries<FeatureModel>(DexType.LegendaryFeatures);
        private static readonly IEnumerable<FeatureModel> Passives = DexUtility.GetDexEntries<FeatureModel>(DexType.Passives);
        private static readonly IEnumerable<FeatureModel> Skills = DexUtility.GetDexEntries<FeatureModel>(DexType.Skills);

        public FeatureController(ILogger<FeatureController> log)
        {
            _logger = log;
        }

        [FunctionName("GetAllGeneralFeatures")]
        [OpenApiOperation(operationId: "GetAllGeneralFeatures")]
        [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StaticCollectionMessage))]
        public IActionResult GetAllGeneralFeatures(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/general")] HttpRequest req)
        {
            return new OkObjectResult(GetStaticCollectionResponse(GeneralFeatures, req));
        }

        [FunctionName("GetGeneralFeatureByName")]
        [OpenApiOperation(operationId: "GetGeneralFeatureByName")]
        [OpenApiParameter(name: "name", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(FeatureModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string))]
        public IActionResult GetGeneralFeatureByName(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/general/{{name}}")] HttpRequest req,
            string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult($"You forgot the {nameof(name)} route parameter");
            }

            var document = GeneralFeatures.GetStaticDocument(name);
            if (document != null)
            {
                return new OkObjectResult(document);
            }

            return new NotFoundObjectResult(name);
        }

        [FunctionName("GetAllLegendaryFeatures")]
        [OpenApiOperation(operationId: "GetAllLegendaryFeatures")]
        [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StaticCollectionMessage))]
        public IActionResult GetAllLegendaryFeatures(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/legendary")] HttpRequest req)
        {
            return new OkObjectResult(GetStaticCollectionResponse(LegendaryFeatures, req));
        }

        [FunctionName("GetLegendaryFeatureByName")]
        [OpenApiOperation(operationId: "GetLegendaryFeatureByName")]
        [OpenApiParameter(name: "name", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(FeatureModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string))]
        public IActionResult GetLegendaryFeatureByName(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/legendary/{{name}}")] HttpRequest req,
            string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult($"You forgot the {nameof(name)} route parameter");
            }

            var document = LegendaryFeatures.GetStaticDocument(name);
            if (document != null)
            {
                return new OkObjectResult(document);
            }

            return new NotFoundObjectResult(name);
        }

        [FunctionName("GetAllPassives")]
        [OpenApiOperation(operationId: "GetAllPassives")]
        [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StaticCollectionMessage))]
        public IActionResult GetAllPassives(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/passives")] HttpRequest req)
        {
            return new OkObjectResult(GetStaticCollectionResponse(Passives, req));
        }

        [FunctionName("GetPassiveByName")]
        [OpenApiOperation(operationId: "GetPassiveByName")]
        [OpenApiParameter(name: "name", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(FeatureModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string))]
        public IActionResult GetPassiveByName(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/passives/{{name}}")] HttpRequest req,
            string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult($"You forgot the {nameof(name)} route parameter");
            }

            var document = Passives.GetStaticDocument(name);
            if (document != null)
            {
                return new OkObjectResult(document);
            }

            return new NotFoundObjectResult(name);
        }

        [FunctionName("GetAllSkills")]
        [OpenApiOperation(operationId: "GetAllSkills")]
        [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StaticCollectionMessage))]
        public IActionResult GetAllSkills(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/skills")] HttpRequest req)
        {
            return new OkObjectResult(GetStaticCollectionResponse(Skills, req));
        }

        [FunctionName("GetSkillByName")]
        [OpenApiOperation(operationId: "GetSkillByName")]
        [OpenApiParameter(name: "name", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(FeatureModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string))]
        public IActionResult GetSkillByName(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/skills/{{name}}")] HttpRequest req,
            string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult($"You forgot the {nameof(name)} route parameter");
            }

            var document = Skills.GetStaticDocument(name);
            if (document != null)
            {
                return new OkObjectResult(document);
            }

            return new NotFoundObjectResult(name);
        }
    }
}
