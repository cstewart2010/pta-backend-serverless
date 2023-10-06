using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using TheReplacement.PTA.Api.Services.Enums;
using TheReplacement.PTA.Api.Services;
using TheReplacement.PTA.Api.Services.Models;
using System.Linq;
using TheReplacement.PTA.Api.Extensions;
using TheReplacement.PTA.Api.Messages;
using TheReplacement.PTA.Api.Models;
using TheReplacement.PTA.Api.Abstractions;

namespace TheReplacement.PTA.Api.Controllers
{
    public class BasePokemonController : BaseStaticController
    {
        private readonly ILogger<BasePokemonController> _logger;
        private const string RoutePrefix = "v1/pokedex";
        private static readonly IEnumerable<BasePokemonModel> AllPokemon = DexUtility.GetDexEntries<BasePokemonModel>(DexType.BasePokemon);
        private static readonly IEnumerable<BasePokemonModel> BasePokemon = AllPokemon
            .GroupBy(pokemon => pokemon.DexNo)
            .Select(group => group.First())
            .OrderBy(pokemon => pokemon.DexNo)
            .ToList();

        public BasePokemonController(ILogger<BasePokemonController> log)
        {
            _logger = log;
        }

        [FunctionName("GetAllPokemon")]
        [OpenApiOperation(operationId: "GetAllPokemon")]
        [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Offset** parameter")]
        [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Limit** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(StaticCollectionMessage), Description = "The OK response")]
        public IActionResult GetAllPokemon(
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

            return new OkObjectResult(GetStaticCollectionResponse(BasePokemon, offset, limit));
        }

        [FunctionName("GetBasePokemonByName")]
        [OpenApiOperation(operationId: "GetBasePokemonByName")]
        [OpenApiParameter(name: "name", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(BasePokemonModel), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Description = "The Not Found response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The Bad response")]
        public IActionResult GetBasePokemonByName(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{name}}")] HttpRequest req,
            string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult($"You forgot the {nameof(name)} route parameter");
            }

            var document = AllPokemon.GetStaticDocument(name);
            if (document != null)
            {
                return new OkObjectResult(document);
            }

            return new NotFoundObjectResult(name);
        }

        [FunctionName("GetPokemonByForm")]
        [OpenApiOperation(operationId: "GetPokemonByForm")]
        [OpenApiParameter(name: "form", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The **Form** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(BasePokemonFormMetadata[]), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Description = "The Not Found response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The Bad response")]
        public IActionResult GetPokemonByForm(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{form}}")] HttpRequest req,
            string form)
        {
            if (string.IsNullOrEmpty(form))
            {
                return new BadRequestObjectResult($"You forgot the {nameof(form)} route parameter");
            }
            var pokemon = AllPokemon.Where(pokemon => pokemon.Form.Contains(form)).Select(pokemon => new BasePokemonFormMetadata
            {
                Name = pokemon.Name,
                Form = pokemon.Form
            }).ToArray();
            if (pokemon.Any())
            {
                return new OkObjectResult(pokemon);
            }

            return new NotFoundObjectResult(form);
        }
    }
}

