using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TheReplacement.PTA.Api.Abstractions;
using TheReplacement.PTA.Api.Objects;
using TheReplacement.PTA.Api.Services;
using TheReplacement.PTA.Api.Services.Enums;
using TheReplacement.PTA.Api.Services.Models;
using TheReplacement.PTA.Api.Messages;
using TheReplacement.PTA.Api.Extensions;
using TheReplacement.PTA.Api.Models;

namespace TheReplacement.PTA.Api.Controllers
{
    public class TrainerController : BasePtaController
    {
        private const string RoutePrefix = "v1/trainer";
        protected override MongoCollection Collection { get; }

        public TrainerController(ILogger<TrainerController> log)
        {
            _logger = log;
            Collection = MongoCollection.Trainers;
        }

        [FunctionName("GetAllTrainerInGame")]
        [OpenApiOperation(operationId: "GetAllTrainerInGame")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PublicTrainer[]))]
        public IActionResult GetAllTrainerInGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{gameId}}/trainers")] HttpRequest req,
            Guid gameId)
        {
            return new OkObjectResult(GetTrainers(gameId));
        }

        [FunctionName("GetTrainerByUsername")]
        [OpenApiOperation(operationId: "GetTrainerByUsername")]
        [OpenApiParameter(name: "trainerName", In = ParameterLocation.Path, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Query, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(FoundTrainerMessage))]
        public IActionResult GetTrainerByUsername(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/trainers/{{trainerName}}")] HttpRequest req,
            string trainerName)
        {
            string guid = req.Query["gameId"];
            var gameId = Guid.Parse(guid);
            var trainer = DatabaseUtility.FindTrainerByUsername(trainerName, gameId);
            return new OkObjectResult(new FoundTrainerMessage(trainer.TrainerId, gameId));
        }

        [FunctionName("GetTrainerPokemon")]
        [OpenApiOperation(operationId: "GetTrainerPokemon")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "pokemonId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PokemonModel))]
        public IActionResult GetTrainerPokemon(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}/{{pokemonId}}")] HttpRequest req,
            Guid gameId,
            Guid trainerId,
            Guid pokemonId)
        {
            var document = GetDocument(pokemonId, MongoCollection.Pokemon, out var notFound);
            if (document is not PokemonModel pokemon)
            {
                return notFound;
            }
            if (pokemon.TrainerId != trainerId && pokemon.GameId != gameId)
            {
                return new BadRequestObjectResult(new PokemonTrainerMismatchMessage(pokemon.TrainerId, trainerId));
            }
            return new OkObjectResult(pokemon);
        }

        [FunctionName("Trainer_AddPokemon")]
        [OpenApiOperation(operationId: "Trainer_AddPokemon")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "pokemon", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "nature", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "gender", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "status", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "form", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "forceShiny", In = ParameterLocation.Query, Required = true, Type = typeof(bool))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PokemonModel))]
        public IActionResult AddPokemon(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{trainerId}}")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid trainerId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var query = JsonConvert.SerializeObject(req.GetQueryParameterDictionary());
            var wildPokemon = JsonConvert.DeserializeObject<WildPokemon>(query);
            var (pokemon, error) = BuildPokemon(trainerId, gameId, wildPokemon);
            if (pokemon == null)
            {
                return new BadRequestObjectResult(error);
            }

            if (!DatabaseUtility.TryAddPokemon(pokemon, out var writeError))
            {
                return new BadRequestObjectResult(writeError);
            }

            req.HttpContext.Response.RefreshToken(gameMasterId);
            return new OkObjectResult(pokemon);
        }

        [FunctionName("AddGroupHonorAsync")]
        [OpenApiOperation(operationId: "AddGroupHonorAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(HonorContainerModel), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(GenericMessage))]
        public async Task<IActionResult> AddGroupHonorAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/groupHonor")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var body = await req.GetRequestBody<HonorContainerModel>();
            var honor = body.Honor;
            if (string.IsNullOrEmpty(honor))
            {
                return new BadRequestObjectResult(nameof(honor));
            }

            var trainers = DatabaseUtility.FindTrainersByGameId(gameId);
            foreach (var trainer in trainers)
            {
                DatabaseUtility.UpdateTrainerHonors(trainer.TrainerId, trainer.Honors.Append(honor));
            }

            var updatedHonorsLog = new LogModel
            (
                user: "The party",
                action: $"has earned a new honor: {honor}"
            );

            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(gameId), updatedHonorsLog);
            req.HttpContext.Response.RefreshToken(gameMasterId);
            return new OkObjectResult(new GenericMessage($"Granted the party honor: {honor}"));
        }

        [FunctionName("AddSingleHonorAsync")]
        [OpenApiOperation(operationId: "AddSingleHonorAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(HonorContainerModel), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(GenericMessage))]
        public async Task<IActionResult> AddSingleHonorAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/honor")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var body = await req.GetRequestBody<HonorContainerModel>();
            var honor = body.Honor;
            var trainerId = body.TrainerId;
            var trainer = DatabaseUtility.FindTrainerById(trainerId, gameId);
            var gameMaster = DatabaseUtility.FindTrainerById(gameMasterId, gameId);
            if (gameMaster.GameId != trainer.GameId)
            {
                return new UnauthorizedResult();
            }
            if (string.IsNullOrEmpty(honor))
            {
                return new BadRequestObjectResult(nameof(honor));
            }
            if (!DatabaseUtility.UpdateTrainerHonors(trainerId, trainer.Honors.Append(honor)))
            {
                throw new Exception();
            }

            var updatedHonorsLog = new LogModel(user: gameMaster.TrainerName, action: $"has granted {trainer.TrainerName} a new honor");
            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(gameMaster.GameId), updatedHonorsLog);
            req.HttpContext.Response.RefreshToken(gameMasterId);
            return new OkObjectResult(new GenericMessage($"Granted the party honor: {honor}"));
        }

        [FunctionName("AddItemsToTrainerAsync")]
        [OpenApiOperation(operationId: "AddItemsToTrainerAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ItemModel[]), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(FoundTrainerMessage))]
        public async Task<IActionResult> AddItemsToTrainerAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{trainerId}}/addItems")] HttpRequest req,
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

            var items = await req.GetRequestBody<IEnumerable<ItemModel>>();
            var addedItemsLogs = AddItemsToTrainer(trainer, items);
            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(trainer.GameId), addedItemsLogs.ToArray());
            req.HttpContext.Response.RefreshToken(gameMasterId);
            return new OkObjectResult(new FoundTrainerMessage(trainerId, gameId));
        }

        [FunctionName("AddItemsToAllTrainersAsync")]
        [OpenApiOperation(operationId: "AddItemsToAllTrainersAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ItemModel[]), Required = true)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        public async Task<IActionResult> AddItemsToAllTrainersAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/addItems/all")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var trainers = DatabaseUtility.FindTrainersByGameId(gameId);
            if (trainers.Any())
            {
                var items = await req.GetRequestBody<IEnumerable<ItemModel>>();
                foreach (var trainer in trainers)
                {
                    var addedItemsLogs = AddItemsToTrainer(trainer, items);
                    DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(trainer.GameId), addedItemsLogs.ToArray());
                }
            }

            req.HttpContext.Response.RefreshToken(gameMasterId);
            return new OkResult();
        }

        [FunctionName("RemoveItemsFromTrainerAsync")]
        [OpenApiOperation(operationId: "RemoveItemsFromTrainerAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ItemModel[]), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(FoundTrainerMessage))]
        public async Task<IActionResult> RemoveItemsFromTrainerAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}/removeItems")] HttpRequest req,
            Guid gameId,
            Guid trainerId)
        {
            if (!req.VerifyIdentity(trainerId))
            {
                return new UnauthorizedResult();
            }

            var trainer = DatabaseUtility.FindTrainerById(trainerId, gameId);
            if (trainer?.IsOnline != true)
            {
                return new NotFoundObjectResult(trainerId);
            }

            var items = await req.GetRequestBody<IEnumerable<ItemModel>>();
            var removedItemsLogs = RemoveItemsFromTrainer(trainer, items);
            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(trainer.GameId), removedItemsLogs.ToArray());
            req.HttpContext.Response.RefreshToken(trainerId);
            return new OkObjectResult(new FoundTrainerMessage(trainerId, gameId));
        }

        [FunctionName("RemoveItemsFromTrainerGMAsync")]
        [OpenApiOperation(operationId: "RemoveItemsFromTrainerGMAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ItemModel[]), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(FoundTrainerMessage))]
        public async Task<IActionResult> RemoveItemsFromTrainerGMAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{trainerId}}/removeItems")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid trainerId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var trainer = DatabaseUtility.FindTrainerById(trainerId, gameId);
            if (trainer?.IsOnline != true)
            {
                return new NotFoundObjectResult(trainerId);
            }

            var items = await req.GetRequestBody<IEnumerable<ItemModel>>();
            var removedItemsLogs = RemoveItemsFromTrainer(trainer, items);
            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(trainer.GameId), removedItemsLogs.ToArray());
            req.HttpContext.Response.RefreshToken(trainerId);
            return new OkObjectResult(new FoundTrainerMessage(trainerId, gameId));
        }

        [FunctionName("RemoveItemsFromAllTrainersAsync")]
        [OpenApiOperation(operationId: "RemoveItemsFromAllTrainersAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ItemModel[]), Required = true)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        public async Task<IActionResult> RemoveItemsFromAllTrainersAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/removeItems/all")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var trainers = DatabaseUtility.FindTrainersByGameId(gameId);
            if (trainers.Any())
            {
                var items = await req.GetRequestBody<IEnumerable<ItemModel>>();
                foreach (var trainer in trainers)
                {
                    var removedItemsLogs = RemoveItemsFromTrainer(trainer, items);
                    DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(trainer.GameId), removedItemsLogs.ToArray());
                }
            }

            req.HttpContext.Response.RefreshToken(gameMasterId);
            return new OkResult();
        }

        [FunctionName("UpdateTrainerMoney")]
        [OpenApiOperation(operationId: "UpdateTrainerMoney")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "addition", In = ParameterLocation.Query, Required = true, Type = typeof(int))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        public IActionResult UpdateTrainerMoney(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{trainerId}}/money")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid trainerId)
        {
            if (!int.TryParse(req.Query["addition"], out var addition))
            {
                return new BadRequestResult();
            }
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var trainer = DatabaseUtility.FindTrainerById(trainerId, gameId);
            if (trainer == null)
            {
                return new NotFoundObjectResult(trainerId);
            }

            trainer.Money += addition;
            if (!DatabaseUtility.UpdateTrainer(trainer))
            {
                return new BadRequestResult();
            }

            return new OkResult();
        }

        [FunctionName("UpdateAllTrainersMoney")]
        [OpenApiOperation(operationId: "UpdateAllTrainersMoney")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "addition", In = ParameterLocation.Query, Required = true, Type = typeof(int))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        public IActionResult UpdateAllTrainersMoney(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{trainerId}}/money")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            if (!int.TryParse(req.Query["addition"], out var addition))
            {
                return new BadRequestResult();
            }
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var trainers = DatabaseUtility.FindTrainersByGameId(gameId);
            foreach (var trainer in trainers.Where(trainer => !trainer.IsGM))
            {
                trainer.Money += addition;
                DatabaseUtility.UpdateTrainer(trainer);
            }

            return new OkResult();
        }

        [FunctionName("DeleteTrainer")]
        [OpenApiOperation(operationId: "DeleteTrainer")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "addition", In = ParameterLocation.Query, Required = true, Type = typeof(int))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(GenericMessage))]
        public IActionResult DeleteTrainer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{trainerId}}")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid trainerId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var gameMaster = DatabaseUtility.FindTrainerById(trainerId, gameId);
            if (gameMaster?.IsGM != true)
            {
                return new NotFoundObjectResult(trainerId);
            }

            var trainer = DatabaseUtility.FindTrainerById(trainerId, gameId);
            foreach (var pokemon in DatabaseUtility.FindPokemonByTrainerId(trainerId))
            {
                DatabaseUtility.DeletePokemon(pokemon.PokemonId);
            }

            if (!(DatabaseUtility.DeleteTrainer(gameId, trainerId) && DatabaseUtility.FindTrainerById(trainerId, gameId) == null))
            {
                return new NotFoundResult();
            }

            var deleteTrainerLog = new LogModel(user: gameMaster.TrainerName, action: $"removed {trainer.TrainerName} and all of their pokemon from the game");
            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(gameMaster.GameId), deleteTrainerLog);
            req.HttpContext.Response.RefreshToken(gameMasterId);
            return new OkObjectResult(new GenericMessage($"Successfully deleted all pokemon associated with {trainerId}"));
        }

        private static List<PublicTrainer> GetTrainers(Guid gameId)
        {
            return DatabaseUtility.FindTrainersByGameId(gameId)
                .Select(trainer => new PublicTrainer(trainer)).ToList();
        }
    }
}
