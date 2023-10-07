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
using Microsoft.Extensions.Logging;
using TheReplacement.PTA.Api.Extensions;

namespace TheReplacement.PTA.Api.Controllers
{
    public class ItemController : BaseStaticController
    {
        private const string RoutePrefix = "v1/itemdex";
        private static readonly IEnumerable<BaseItemModel> KeyItems = DexUtility.GetDexEntries<BaseItemModel>(DexType.KeyItems);
        private static readonly IEnumerable<BaseItemModel> MedicalItems = DexUtility.GetDexEntries<BaseItemModel>(DexType.MedicalItems);
        private static readonly IEnumerable<BaseItemModel> Pokeballs = DexUtility.GetDexEntries<BaseItemModel>(DexType.Pokeballs);
        private static readonly IEnumerable<BaseItemModel> PokemonItems = DexUtility.GetDexEntries<BaseItemModel>(DexType.PokemonItems);
        private static readonly IEnumerable<BaseItemModel> TrainerEquipment = DexUtility.GetDexEntries<BaseItemModel>(DexType.TrainerEquipment);

        public ItemController(ILogger<ItemController> log)
        {
            _logger = log;
        }

        [FunctionName("GetKeyItems")]
        [OpenApiOperation(operationId: "GetKeyItems")]
        [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StaticCollectionMessage))]
        public IActionResult GetKeyItems(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/key")] HttpRequest req)
        {
            return new OkObjectResult(GetAlphabetizeStaticCollectionResponse(KeyItems, req));
        }

        [FunctionName("GetKeyByName")]
        [OpenApiOperation(operationId: "GetKeyByName")]
        [OpenApiParameter(name: "name", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(BaseItemModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string))]
        public IActionResult GetKeyByName(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/key/{{name}}")] HttpRequest req,
            string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult($"You forgot the {nameof(name)} route parameter");
            }

            var document = KeyItems.GetStaticDocument(name);
            if (document != null)
            {
                return new OkObjectResult(document);
            }

            return new NotFoundObjectResult(name);
        }

        [FunctionName("GetMedicalItems")]
        [OpenApiOperation(operationId: "GetMedicalItems")]
        [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StaticCollectionMessage))]
        public IActionResult GetMedicalItems(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/medical")] HttpRequest req)
        {
            return new OkObjectResult(GetAlphabetizeStaticCollectionResponse(MedicalItems, req));
        }

        [FunctionName("GetMedicalByName")]
        [OpenApiOperation(operationId: "GetMedicalByName")]
        [OpenApiParameter(name: "name", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(BaseItemModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string))]
        public IActionResult GetMedicalByName(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/medical/{{name}}")] HttpRequest req,
            string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult($"You forgot the {nameof(name)} route parameter");
            }

            var document = MedicalItems.GetStaticDocument(name);
            if (document != null)
            {
                return new OkObjectResult(document);
            }

            return new NotFoundObjectResult(name);
        }

        [FunctionName("GetPokeballItems")]
        [OpenApiOperation(operationId: "GetPokeballItems")]
        [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StaticCollectionMessage))]
        public IActionResult GetPokeballItems(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/pokeball")] HttpRequest req)
        {
            return new OkObjectResult(GetAlphabetizeStaticCollectionResponse(Pokeballs, req));
        }

        [FunctionName("GetPokeballByName")]
        [OpenApiOperation(operationId: "GetPokeballByName")]
        [OpenApiParameter(name: "name", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(BaseItemModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string))]
        public IActionResult GetPokeballByName(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/pokeball/{{name}}")] HttpRequest req,
            string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult($"You forgot the {nameof(name)} route parameter");
            }

            var document = Pokeballs.GetStaticDocument(name);
            if (document != null)
            {
                return new OkObjectResult(document);
            }

            return new NotFoundObjectResult(name);
        }

        [FunctionName("GetPokemonItems")]
        [OpenApiOperation(operationId: "GetPokemonItems")]
        [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StaticCollectionMessage))]
        public IActionResult GetPokemonItems(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/pokemon")] HttpRequest req)
        {
            return new OkObjectResult(GetAlphabetizeStaticCollectionResponse(PokemonItems, req));
        }

        [FunctionName("GetPokemonItemByName")]
        [OpenApiOperation(operationId: "GetPokemonItemByName")]
        [OpenApiParameter(name: "name", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(BaseItemModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string))]
        public IActionResult GetPokemonItemByName(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/pokemon/{{name}}")] HttpRequest req,
            string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult($"You forgot the {nameof(name)} route parameter");
            }

            var document = PokemonItems.GetStaticDocument(name);
            if (document != null)
            {
                return new OkObjectResult(document);
            }

            return new NotFoundObjectResult(name);
        }

        [FunctionName("GetTrainerItems")]
        [OpenApiOperation(operationId: "GetTrainerItems")]
        [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StaticCollectionMessage))]
        public IActionResult GetTrainerItems(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/trainer")] HttpRequest req)
        {
            return new OkObjectResult(GetAlphabetizeStaticCollectionResponse(TrainerEquipment, req));
        }

        [FunctionName("GetTrainerItemByName")]
        [OpenApiOperation(operationId: "GetTrainerItemByName")]
        [OpenApiParameter(name: "name", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(BaseItemModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string))]
        public IActionResult GetTrainerItemByName(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/trainer/{{name}}")] HttpRequest req,
            string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult($"You forgot the {nameof(name)} route parameter");
            }

            var document = TrainerEquipment.GetStaticDocument(name);
            if (document != null)
            {
                return new OkObjectResult(document);
            }

            return new NotFoundObjectResult(name);
        }
    }
}
