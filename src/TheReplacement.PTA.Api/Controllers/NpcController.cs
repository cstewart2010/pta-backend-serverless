using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;
using System;
using TheReplacement.PTA.Api.Abstractions;
using TheReplacement.PTA.Api.Services;
using TheReplacement.PTA.Api.Services.Enums;
using TheReplacement.PTA.Api.Services.Models;
using TheReplacement.PTA.Api.Extensions;
using TheReplacement.PTA.Api.Objects;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using TheReplacement.PTA.Api.Messages;
using TheReplacement.PTA.Api.Models;

namespace TheReplacement.PTA.Api.Controllers
{
    public class NpcController : BasePtaController
    {
        private const string RoutePrefix = "v1/npc";

        public NpcController(ILogger<NpcController> log)
        {
            _logger = log;
            Collection = MongoCollection.Npcs;
        }

        protected override MongoCollection Collection { get; }

        [FunctionName("GetNpc")]
        [OpenApiOperation(operationId: "GetNpc")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "npcId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PublicNpc))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult GetNpc(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{npcId}}")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid npcId)
        {
            if (!req.VerifyIdentity(gameMasterId))
            {
                return new UnauthorizedResult();
            }

            var npc = DatabaseUtility.FindNpc(npcId);
            var gameMaster = DatabaseUtility.FindTrainerById(gameMasterId, gameId);
            if (npc == null)
            {
                return new NotFoundObjectResult(npcId);
            }

            if (gameMaster.GameId != npc.GameId)
            {
                return new ConflictResult();
            }

            return new OkObjectResult(new PublicNpc(npc));
        }

        [FunctionName("GetNpcPokemon")]
        [OpenApiOperation(operationId: "GetNpcPokemon")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "npcId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "pokemonId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PokemonModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult GetNpcPokemon(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{npcId}}/{{pokemonId}}")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid npcId,
            Guid pokemonId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var pokemon = DatabaseUtility.FindPokemonByTrainerId(npcId).SingleOrDefault(pokemon => pokemon.PokemonId == pokemonId);
            if (pokemon == null)
            {
                return new NotFoundObjectResult(npcId);
            }

            return new OkObjectResult(pokemon);
        }

        [FunctionName("GetAllNpcsInGame")]
        [OpenApiOperation(operationId: "GetAllNpcsInGame")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PublicNpc[]))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult GetAllNpcsInGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/npcs/all")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var npcs = DatabaseUtility.FindNpcsByGameId(gameId)
                .Select(npc => new PublicNpc(npc))
                .ToArray();
            return new OkObjectResult(npcs);
        }

        [FunctionName("CreateNewNpcAsync")]
        [OpenApiOperation(operationId: "CreateNewNpcAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(NewNpcDataModel), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(NpcModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(MongoWriteError))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateNewNpcAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/new")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var gameMaster = DatabaseUtility.FindTrainerById(gameMasterId, gameId);
            var npc = await CreateNpcAsync(req);
            npc.GameId = gameMaster.GameId;
            if (!DatabaseUtility.TryAddNpc(npc, out var error))
            {
                return new BadRequestObjectResult(error);
            }

            return new OkObjectResult(npc);
        }

        [FunctionName("CreateNewNpcPokemonAsync")]
        [OpenApiOperation(operationId: "CreateNewNpcPokemonAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "npcId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(NewPokemon[]), Required = true)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateNewNpcPokemonAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{npcId}}/new")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid npcId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var npc = DatabaseUtility.FindNpc(npcId);
            if (npc == null)
            {
                return new NotFoundObjectResult(npcId);
            }

            var newPokemon = (await req.GetRequestBody()).ToObject<IEnumerable<NewPokemon>>();
            AddNpcPokemon(newPokemon, npcId, gameId);
            return new OkResult();
        }

        [FunctionName("AddNpcStatsAsync")]
        [OpenApiOperation(operationId: "AddNpcStatsAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "npcId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(PublicNpc), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PublicNpc))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(GenericMessage))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AddNpcStatsAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{npcId}}/addStats")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid npcId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var result = await req.TryCompleteNpc();
            if (!result)
            {
                return new BadRequestObjectResult(new GenericMessage("Failed to update Npc"));
            }

            var npc = DatabaseUtility.FindNpc(npcId);
            return new OkObjectResult(new PublicNpc(npc));
        }

        [FunctionName("DeleteNpc")]
        [OpenApiOperation(operationId: "DeleteNpc")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "npcId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult DeleteNpc(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{npcId}}")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid npcId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var npc = DatabaseUtility.FindNpc(npcId);
            if (npc == null)
            {
                return new NotFoundObjectResult(npcId);
            }

            if (gameId != npc.GameId)
            {
                return new ConflictResult();
            }

            if (!DatabaseUtility.DeleteNpc(npcId))
            {
                return new BadRequestObjectResult(npcId);
            }

            return new OkResult();
        }

        [FunctionName("DeleteNpcsInGame")]
        [OpenApiOperation(operationId: "DeleteNpcsInGame")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult DeleteNpcsInGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/npcs/all")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            if (!DatabaseUtility.DeleteNpcByGameId(gameId))
            {
                return new BadRequestResult();
            }

            return new OkResult();
        }

        private static async Task<NpcModel> CreateNpcAsync(HttpRequest request)
        {
            var data = (await request.GetRequestBody()).ToObject<NewNpcDataModel>();

            var feats = data.Feats.Select(feat => DexUtility.GetDexEntry<FeatureModel>(DexType.Features, feat.ToString()))
                .Where(feat => feat != null)
                .Select(feat => feat.Name);

            var classes = data.Classes.Select(@class => DexUtility.GetDexEntry<TrainerClassModel>(DexType.TrainerClasses, @class.ToString()))
                .Where(@class => @class != null)
                .Select(@class => @class.Name);

            // add gameMaster's GameId to npc
            return new NpcModel
            {
                NPCId = Guid.NewGuid(),
                Feats = feats,
                TrainerClasses = classes,
                TrainerName = data.TrainerName,
                TrainerStats = new StatsModel(),
                CurrentHP = 0,
                Sprite = "acetrainer"
            };
        }

        private static void AddNpcPokemon(IEnumerable<NewPokemon> pokemon, Guid npcId, Guid gameId)
        {
            foreach (var data in pokemon.Where(data => data != null))
            {
                var nickname = data.Nickname.Length > 18 ? data.Nickname.Substring(0, 18) : data.Nickname;
                var pokemonModel = DexUtility.GetNewPokemon(data.SpeciesName, nickname, data.Form);
                pokemonModel.IsOnActiveTeam = data.IsOnActiveTeam;
                pokemonModel.OriginalTrainerId = npcId;
                pokemonModel.TrainerId = npcId;
                pokemonModel.GameId = gameId;
                DatabaseUtility.TryAddPokemon(pokemonModel, out _);
            }
        }
    }
}
