using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Linq;
using System.Net;
using TheReplacement.PTA.Api.Abstractions;
using TheReplacement.PTA.Api.Messages;
using TheReplacement.PTA.Api.Services.Enums;
using TheReplacement.PTA.Api.Extensions;
using TheReplacement.PTA.Api.Services;
using TheReplacement.PTA.Api.Services.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using TheReplacement.PTA.Api.Objects;
using System.Threading.Tasks;
using TheReplacement.PTA.Api.Models;

namespace TheReplacement.PTA.Api.Controllers
{
    public class GameController : BasePtaController
    {
        private const string RoutePrefix = "v1/game";

        public GameController(ILogger<GameController> log)
        {
            _logger = log;
            Collection = MongoCollection.Games;
        }

        protected override MongoCollection Collection { get; }

        [FunctionName("GetAllGamesWithUser")]
        [OpenApiOperation(operationId: "GetAllGamesWithUser")]
        [OpenApiParameter(name: "userId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "nickname", In = ParameterLocation.Query, Required = false, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(MinifiedGameModel[]))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object))]
        public IActionResult GetAllGamesWithUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/user/{{userId}}")] HttpRequest req,
            Guid userId)
        {
            string nickname = req.Query["nickname"];
            if (!req.VerifyIdentity(userId))
            {
                return new UnauthorizedResult();
            }

            var user = DatabaseUtility.FindUserById(userId);
            if (!string.IsNullOrWhiteSpace(nickname))
            {
                return new OkObjectResult(DatabaseUtility.FindAllGames(nickname)
                    .Where(game => !user.Games.Contains(game.GameId))
                    .Select(game => new MinifiedGameModel(game)));
            }

            return new OkObjectResult(DatabaseUtility.FindMostRecent20Games(user));
        }

        [FunctionName("GetAllUserGames")]
        [OpenApiOperation(operationId: "GetAllUserGames")]
        [OpenApiParameter(name: "userId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(FoundGameMessage))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object))]
        public IActionResult GetAllUserGames(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/user/games/{{userId}}")] HttpRequest req,
            Guid userId)
        {
            if (!req.VerifyIdentity(userId))
            {
                return new UnauthorizedResult();
            }

            var user = DatabaseUtility.FindUserById(userId);
            return new OkObjectResult(DatabaseUtility.FindAllGamesWithUser(user)
                    .Select(game => new FoundGameMessage(game.GameId, game.Nickname)));
        }

        [FunctionName("GetAllSprites")]
        [OpenApiOperation(operationId: "GetAllSprites")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SpriteModel[]))]
        public IActionResult GetAllSprites(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/sprites/all")] HttpRequest req)
        {
            return new OkObjectResult(DatabaseUtility.GetAllSprites().OrderBy(sprite => sprite.FriendlyText));
        }

        [FunctionName("GetGame")]
        [OpenApiOperation(operationId: "GetGame")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(FoundGameMessage))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object))]
        public IActionResult GetGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/getGame/{{gameId}}")] HttpRequest req,
            Guid gameId)
        {
            var document = GetDocument(gameId, Collection, out var notFound);
            if (document is not GameModel)
            {
                return notFound;
            }

            return new OkObjectResult(new FoundGameMessage(gameId));
        }

        [FunctionName("GetTrainerInGame")]
        [OpenApiOperation(operationId: "GetTrainerInGame")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(FoundTrainerMessage))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(InvalidGameIdMessage))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object))]
        public IActionResult GetTrainerInGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}/find")] HttpRequest req,
            Guid gameId,
            Guid trainerId)
        {
            var gameDocument = GetDocument(gameId, Collection, out var notFound);
            if (gameDocument is not GameModel)
            {
                return notFound;
            }

            var trainer = DatabaseUtility.FindTrainerById(trainerId, gameId);
            if (trainer == null)
            {
                return new NotFoundObjectResult(trainerId);
            }

            if (trainer.GameId != gameId)
            {
                return new BadRequestObjectResult(new InvalidGameIdMessage(trainer));
            }

            return new OkObjectResult(new FoundTrainerMessage(trainerId, gameId));
        }

        [FunctionName("GetAllLogs")]
        [OpenApiOperation(operationId: "GetAllLogs")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AllLogsMessage))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object))]
        public IActionResult GetAllLogs(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{gameId}}/all_logs")] HttpRequest req,
            Guid gameId)
        {
            var gameDocument = GetDocument(gameId, Collection, out var notFound);
            if (gameDocument is not GameModel game)
            {
                return notFound;
            }

            return new OkObjectResult(new AllLogsMessage(game));
        }

        [FunctionName("GetLogs")]
        [OpenApiOperation(operationId: "GetLogs")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LogModel[]))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(object))]
        public IActionResult GetLogs(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{gameId}}/logs")] HttpRequest req,
            Guid gameId)
        {
            var gameDocument = GetDocument(gameId, Collection, out var notFound);
            if (gameDocument is not GameModel game)
            {
                return notFound;
            }

            if (game.Logs == null)
            {
                return new OkObjectResult(Array.Empty<LogModel>());
            }

            return new OkObjectResult(game.Logs.OrderByDescending(log => log.LogTimestamp).Take(50).ToArray());
        }

        [FunctionName("ImportGame")]
        [OpenApiOperation(operationId: "ImportGame")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string[]))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(AbstractMessage))]
        public IActionResult ImportGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = $"{RoutePrefix}/import")] HttpRequest req)
        {
            var json = req.GetJsonFromRequest();
            if (string.IsNullOrEmpty(json))
            {
                return new BadRequestObjectResult(new GenericMessage("empty json file"));
            }

            if (!ExportUtility.TryParseImport(json, out var errors))
            {
                return new BadRequestObjectResult(errors);
            }

            return new OkObjectResult(errors);
        }

        [FunctionName("CreateNewGame")]
        [OpenApiOperation(operationId: "CreateNewGame")]
        [OpenApiParameter(name: "userId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "nickname", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "gameSessionPassword", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "username", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CreatedGameMessage))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(AbstractMessage))]
        public IActionResult CreateNewGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = $"{RoutePrefix}/{{userid}}/newGame")] HttpRequest req,
            Guid userId)
        {
            string nickname = req.Query["nickname"];
            string gameSessionPassword = req.Query["gameSessionPassword"];
            string username = req.Query["username"];
            var game = BuildGame(nickname, gameSessionPassword);
            if (!DatabaseUtility.TryAddGame(game, out var error))
            {
                return new BadRequestObjectResult(error);
            }

            var (gm, badRequest) = BuildGM(game.GameId, userId, username);
            if (gm == null)
            {
                return new BadRequestObjectResult(badRequest);
            }

            if (!DatabaseUtility.TryAddTrainer(gm, out error))
            {
                return new BadRequestObjectResult(error);
            }

            var gameCreationLog = new LogModel
            (
                user: gm.TrainerName,
                action: "created a new game and joined as game master"
            );
            DatabaseUtility.UpdateGameLogs(game, gameCreationLog);
            req.HttpContext.Response.RefreshToken(userId);
            return new OkObjectResult(new CreatedGameMessage(gm));
        }

        [FunctionName("AddPlayerToGame")]
        [OpenApiOperation(operationId: "AddPlayerToGame")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "userId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "username", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(FoundTrainerMessage))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(AbstractMessage))]
        public IActionResult AddPlayerToGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = $"{RoutePrefix}/{{gameId}}/{{userid}}/newUser")] HttpRequest req,
            Guid userId,
            Guid gameId)
        {
            string username = req.Query["username"];
            if (gameId == Guid.Empty)
            {
                return new BadRequestObjectResult(nameof(gameId));
            }

            var gameDocument = GetDocument(gameId, Collection, out var notFound);
            if (gameDocument is not GameModel game)
            {
                return notFound;
            }

            if (!DatabaseUtility.HasGM(gameId, out var noGMError))
            {
                return new BadRequestObjectResult(noGMError);
            }

            var (trainer, badRequest) = BuildTrainer(gameId, userId, username);
            if (trainer == null)
            {
                return new BadRequestObjectResult(badRequest);
            }

            if (!DatabaseUtility.TryAddTrainer(trainer, out var error))
            {
                return new BadRequestObjectResult(error);
            }

            var trainerCreationLog = new LogModel
            (
                user: trainer.TrainerName,
                action: "joined"
            );
            DatabaseUtility.UpdateGameLogs(game, trainerCreationLog);
            req.HttpContext.Response.RefreshToken(userId);
            return new OkObjectResult(new FoundTrainerMessage(trainer.TrainerId, gameId));
        }


        [FunctionName("AddPokemon")]
        [OpenApiOperation(operationId: "AddPokemon")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PokemonModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(AbstractMessage))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(AbstractMessage))]
        public IActionResult AddPokemon(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/wild")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            var query = JsonConvert.SerializeObject(req.GetQueryParameterDictionary());
            var wildPokemon = JsonConvert.DeserializeObject<WildPokemon>(query);
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var (pokemon, error) = BuildPokemon(Guid.Empty, gameId, wildPokemon);
            if (pokemon == null)
            {
                return new BadRequestObjectResult(error);
            }

            if (!DatabaseUtility.TryAddPokemon(pokemon, out var writeError))
            {
                return new BadRequestObjectResult(writeError);
            }

            var game = DatabaseUtility.FindGame(gameId);
            var pokemonCreationLog = new LogModel
            (
                user: pokemon.SpeciesName,
                action: "spawned"
            );
            DatabaseUtility.UpdateGameLogs(game, pokemonCreationLog);
            return new OkObjectResult(pokemon);
        }

        [FunctionName("PostLogAsync")]
        [OpenApiOperation(operationId: "PostLogAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(LogModel[]))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PostLogAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}/log")] HttpRequest req,
            Guid gameId,
            Guid trainerId)
        {
            if (!req.VerifyIdentity(trainerId))
            {
                return new UnauthorizedResult();
            }

            var body = await req.GetRequestBody();
            var log = body.ToObject<LogModel>();
            log.Action += $" at {DateTime.UtcNow}";
            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(gameId), log);
            req.HttpContext.Response.RefreshToken(trainerId);
            return new OkObjectResult(log);
        }

        [FunctionName("AllowUser")]
        [OpenApiOperation(operationId: "AllowUser")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(AbstractMessage))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult AllowUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{trainerId}}/allow")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid trainerId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var trainer = DatabaseUtility.FindTrainerById(trainerId, gameId);
            if (trainer == null)
            {
                return new NotFoundObjectResult(trainerId);
            }

            trainer.IsAllowed = true;
            DatabaseUtility.UpdateTrainer(trainer);
            var log = new LogModel
            (
                user: trainer.TrainerName,
                action: "joined the game"
            );

            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(gameId), log);
            req.HttpContext.Response.RefreshToken(gameMasterId);
            return new OkResult();
        }

        [FunctionName("DisallowUser")]
        [OpenApiOperation(operationId: "DisallowUser")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(AbstractMessage))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult DisallowUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{trainerId}}/disallow")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid trainerId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var trainer = DatabaseUtility.FindTrainerById(trainerId, gameId);
            if (trainer == null)
            {
                return new NotFoundObjectResult(trainerId);
            }

            trainer.IsAllowed = false;
            DatabaseUtility.UpdateTrainer(trainer);
            var log = new LogModel
            (
                user: trainer.TrainerName,
                action: "was removed from the game"
            );

            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(gameId), log);
            req.HttpContext.Response.RefreshToken(gameMasterId);
            return new OkResult();
        }

        [FunctionName("AddTrainerStats")]
        [OpenApiOperation(operationId: "AddTrainerStats")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(FoundTrainerMessage))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(GenericMessage))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AddTrainerStats(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}/addStats")] HttpRequest req,
            Guid gameId,
            Guid trainerId)
        {
            if (!req.VerifyIdentity(trainerId))
            {
                return new UnauthorizedResult();
            }

            var gameDocument = GetDocument(gameId, Collection, out var notFound);
            if (gameDocument is not GameModel)
            {
                return notFound;
            }

            var result = await req.TryCompleteTrainer(trainerId, gameId);
            if (!result)
            {
                return new BadRequestObjectResult(new GenericMessage("Failed to update trainer"));
            }
            return new OkObjectResult(new FoundTrainerMessage(trainerId, gameId));
        }

        [FunctionName("StartGame")]
        [OpenApiOperation(operationId: "StartGame")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameSessionPassword", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(FoundGameMessage))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(GenericMessage))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult StartGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/start")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            var gameSessionPassword = req.Query["gameSessionPassword"];
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var gameDocument = GetDocument(gameId, Collection, out var notFound);
            if (gameDocument is not GameModel game)
            {
                return notFound;
            }

            if (!IsGameAuthenticated(gameSessionPassword, game, out var authError))
            {
                return authError;
            }

            var trainer = DatabaseUtility.FindTrainerById(gameMasterId, gameId);
            req.HttpContext.Response.AssignAuthAndToken(trainer.TrainerId);
            return new OkObjectResult(new FoundGameMessage(gameId));
        }

        [FunctionName("EndGame")]
        [OpenApiOperation(operationId: "EndGame")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(GenericMessage))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult EndGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/end")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var gameDocument = GetDocument(gameId, Collection, out var notFound);
            if (gameDocument is not GameModel)
            {
                return notFound;
            }

            SetEndGameStatuses(gameId);
            return new OkResult();
        }

        [FunctionName("AddNPCsToGame")]
        [OpenApiOperation(operationId: "AddNPCsToGame")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "npcIds", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UpdatedNpcListMessage))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(GenericMessage))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult AddNPCsToGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/addNpcs")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            var npcIds = GetNpcs(req, gameMasterId, gameId, out var notFound);
            if (npcIds == null)
            {
                return notFound;
            }

            var gameDocument = GetDocument(gameId, Collection, out notFound);
            if (gameDocument is not GameModel game)
            {
                return notFound;
            }

            var newNpcList = game.NPCs.Union(npcIds);
            req.HttpContext.Response.RefreshToken(gameMasterId);
            return UpdateNpcList
            (
                gameId,
                newNpcList
            );
        }

        [FunctionName("RemovesNPCsFromGame")]
        [OpenApiOperation(operationId: "RemovesNPCsFromGame")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "npcIds", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UpdatedNpcListMessage))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(GenericMessage))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult RemovesNPCsFromGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/removeNpcs")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            var npcIds = GetNpcs(req, gameMasterId, gameId, out var notFound);
            if (npcIds == null)
            {
                return notFound;
            }

            var gameDocument = GetDocument(gameId, Collection, out notFound);
            if (gameDocument is not GameModel game)
            {
                return notFound;
            }

            var newNpcList = game.NPCs.Except(npcIds);
            req.HttpContext.Response.RefreshToken(gameMasterId);
            return UpdateNpcList
            (
                gameId,
                newNpcList
            );
        }

        [FunctionName("DeleteGame")]
        [OpenApiOperation(operationId: "DeleteGame")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameSessionPassword", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(GameDeletionMessagesModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(GenericMessage))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult DeleteGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            string gameSessionPassword = req.Query["gameSessionPassword"];
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var gameDocument = GetDocument(gameId, Collection, out var notFound);
            if (gameDocument is not GameModel game)
            {
                return notFound;
            }

            if (!IsGameAuthenticated(gameSessionPassword, game, out var authError))
            {
                return authError;
            }

            var messages = new GameDeletionMessagesModel
            {
                PokemonDeletionResult = GetMassPokemonDeletion(gameId),
                TrainerDeletionResult = GetMassTrainerDeletion(gameId),
                GameDeletionResult = GetGameDeletion(gameId),
            };

            return new OkObjectResult(messages);
        }

        [FunctionName("ExportGame")]
        [OpenApiOperation(operationId: "ExportGame")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameSessionPassword", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(GameDeletionMessagesModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(GenericMessage))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult ExportGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/export")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            string gameSessionPassword = req.Query["gameSessionPassword"];
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var gameDocument = GetDocument(gameId, Collection, out var notFound);
            if (gameDocument is not GameModel game)
            {
                return notFound;
            }

            if (!IsGameAuthenticated(gameSessionPassword, game, out var authError))
            {
                return authError;
            }

            var gameMaster = DatabaseUtility.FindTrainerById(gameMasterId, gameId);
            var exportLog = new LogModel
            (
                user: gameMaster.TrainerName,
                action: "exported game session"
            );
            DatabaseUtility.UpdateGameOnlineStatus(gameId, false);
            DatabaseUtility.UpdateGameLogs(game, exportLog);
            var exportStream = ExportUtility.GetExportStream(game);

            DeleteGame(req, gameId, gameMasterId);
            var fileStream = new FileStreamResult(exportStream, "application/octet-stream")
            {
                FileDownloadName = $"{game.Nickname}.json"
            };

            return fileStream;
        }

        private static GameModel BuildGame(string nickname, string gameSessionPassword)
        {
            var guid = Guid.NewGuid();
            return new GameModel
            {
                GameId = guid,
                Nickname = string.IsNullOrEmpty(nickname)
                    ? guid.ToString().Split('-')[0]
                    : nickname,
                IsOnline = true,
                PasswordHash = EncryptionUtility.HashSecret(gameSessionPassword),
                NPCs = Array.Empty<Guid>(),
                Logs = Array.Empty<LogModel>()
            };
        }

        private static (TrainerModel GameMaster, AbstractMessage Message) BuildGM(
            Guid gameId,
            Guid userId,
            string username)
        {
            var (gm, badRequestMessage) = BuildTrainer
            (
                gameId,
                userId,
                username,
                true
            );

            if (gm != null)
            {
                gm.IsGM = true;
                gm.Sprite = "acetrainer";
            }

            return (gm, badRequestMessage);
        }

        private static (TrainerModel Trainer, AbstractMessage Message) BuildTrainer(
            Guid gameId,
            Guid userId,
            string username)
        {
            var (trainer, badRequestMessage) = BuildTrainer
            (
                gameId,
                userId,
                username,
                false
            );

            if (badRequestMessage != null)
            {
                return (null, badRequestMessage);
            }

            trainer.Sprite = "acetrainer";
            return (trainer, null);
        }

        private static (TrainerModel Trainer, AbstractMessage Error) BuildTrainer(
            Guid gameId,
            Guid userId,
            string username,
            bool isGM)
        {
            if (DatabaseUtility.FindTrainerByUsername(username, gameId) != null)
            {
                return (null, new GenericMessage($"Duplicate username {username}"));
            }

            var trainer = CreateTrainer(gameId, userId, username);
            trainer.IsGM = isGM;
            trainer.IsAllowed = isGM;
            return (trainer, null);
        }

        private static TrainerModel CreateTrainer(
            Guid gameId,
            Guid userId,
            string username)
        {
            var user = DatabaseUtility.FindUserById(userId);
            user.Games.Add(gameId);
            DatabaseUtility.UpdateUser(user);
            return new TrainerModel
            {
                GameId = gameId,
                TrainerId = userId,
                Honors = Array.Empty<string>(),
                TrainerName = username,
                TrainerClasses = Array.Empty<string>(),
                Feats = Array.Empty<string>(),
                IsOnline = true,
                Items = new List<ItemModel>(),
                TrainerStats = GetStats(),
                CurrentHP = 20,
                Origin = string.Empty
            };
        }

        private static StatsModel GetStats()
        {
            return new StatsModel
            {
                HP = 20,
                Attack = 1,
                Defense = 1,
                SpecialAttack = 1,
                SpecialDefense = 1,
                Speed = 1
            };
        }

        private static IEnumerable<Guid> GetNpcs(
            HttpRequest request,
            Guid gameMasterId,
            Guid gameId,
            out IActionResult notFound)
        {
            if (!request.VerifyIdentity(gameMasterId))
            {
                notFound = new UnauthorizedResult();
                return null;
            }

            var trainer = DatabaseUtility.FindTrainerById(gameMasterId, gameId);
            if (trainer?.IsGM != true)
            {
                notFound = new NotFoundObjectResult(gameMasterId);
                return null;
            }

            var npcIds = request.GetNpcIds(out var error);
            if (!npcIds.Any())
            {
                notFound = new NotFoundObjectResult(error);
                return null;
            }

            notFound = null;
            return npcIds;
        }

        private static IActionResult UpdateNpcList(
            Guid gameId,
            IEnumerable<Guid> newNpcList)
        {
            if (!DatabaseUtility.UpdateGameNpcList(gameId, newNpcList))
            {
                return new UnauthorizedResult();
            }

            return new OkObjectResult(new UpdatedNpcListMessage(newNpcList));
        }

        private static void SetEndGameStatuses(Guid gameId)
        {
            DatabaseUtility.UpdateGameOnlineStatus
            (
                gameId,
                false
            );

            foreach (var trainer in DatabaseUtility.FindTrainersByGameId(gameId))
            {
                DatabaseUtility.UpdateTrainerOnlineStatus
                (
                    trainer.TrainerId,
                    false
                );
            }
        }

        private static GenericMessage GetGameDeletion(Guid gameId)
        {
            string message = DatabaseUtility.DeleteGame(gameId)
                ? $"Successfully deleted game {gameId}"
                : $"Failed to delete {gameId}";
            return new GenericMessage(message);
        }

        private static GenericMessage GetMassTrainerDeletion(Guid gameId)
        {
            string message;
            if (DatabaseUtility.DeleteTrainersByGameId(gameId) > -1)
            {
                message = $"Successfully deleted all trainers associate with {gameId}";
            }
            else
            {
                message = $"Failed to delete trainers";
            }

            return new GenericMessage(message);
        }

        private static IEnumerable<GenericMessage> GetMassPokemonDeletion(Guid gameId)
        {
            return DatabaseUtility.FindTrainersByGameId(gameId)
                .Select(trainer => GetPokemonDeletion(trainer.TrainerId, trainer.GameId))
                .Where(response => response != null);
        }
    }
}
