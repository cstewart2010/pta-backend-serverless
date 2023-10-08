using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
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
using System.Text;
using System.Threading.Tasks;
using TheReplacement.PTA.Api.Abstractions;
using TheReplacement.PTA.Api.Extensions;
using TheReplacement.PTA.Api.Messages;
using TheReplacement.PTA.Api.Objects;
using TheReplacement.PTA.Api.Services;
using TheReplacement.PTA.Api.Services.Enums;
using TheReplacement.PTA.Api.Services.Models;
using TheReplacement.PTA.Api.Models;

namespace TheReplacement.PTA.Api.Controllers
{
    public class PokemonController : BasePtaController
    {
        private const string RoutePrefix = "v1/pokemon";

        public PokemonController(ILogger<PokemonController> log)
        {
            _logger = log;
            Collection = MongoCollection.Pokemon;
        }

        protected override MongoCollection Collection { get; }

        [FunctionName("GetPokemon")]
        [OpenApiOperation(operationId: "GetPokemon")]
        [OpenApiParameter(name: "pokemonId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PokemonModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(Guid))]
        public IActionResult GetPokemon(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{pokemonId}}")] HttpRequest req,
            Guid pokemonId)
        {
            var document = GetDocument(pokemonId, Collection, out var notFound);
            if (document is not PokemonModel pokemon)
            {
                return notFound;
            }

            return new OkObjectResult(pokemon);
        }

        [FunctionName("GetPossibleEvolutions")]
        [OpenApiOperation(operationId: "GetPossibleEvolutions")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "pokemonId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(BasePokemonModel[]))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult GetPossibleEvolutions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}/{{pokemonId}}/possibleEvolutions")] HttpRequest req,
            Guid gameId,
            Guid trainerId,
            Guid pokemonId)
        {
            if (!req.VerifyIdentity(trainerId))
            {
                return new UnauthorizedResult();
            }

            var pokemon = GetPokemonFromTrainer(gameId, trainerId, pokemonId, out var error);
            if (pokemon == null)
            {
                return error;
            }

            var possibleEvolutions = DexUtility.GetPossibleEvolutions(pokemon).ToArray();
            return new OkObjectResult(possibleEvolutions);
        }

        [FunctionName("TradePokemon")]
        [OpenApiOperation(operationId: "TradePokemon")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "leftPokemonId", In = ParameterLocation.Query, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "rightPokemonId", In = ParameterLocation.Query, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(TradeResultModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult TradePokemon(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/trade")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            var leftPokemonId = Guid.Parse(req.Query["leftPokemonId"].ToString());
            var rightPokemonId = Guid.Parse(req.Query["rightPokemonId"].ToString());
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var gameMaster = DatabaseUtility.FindTrainerById(gameMasterId, gameId);
            var (leftPokemon, rightPokemon) = GetTradePokemon(leftPokemonId, rightPokemonId, out var badRequest);
            if (badRequest != null)
            {
                return badRequest;
            }

            UpdatePokemonTrainerIds
            (
                leftPokemon,
                rightPokemon
            );

            var leftTrainer = DatabaseUtility.FindTrainerById(rightPokemon.TrainerId, gameId);
            var rightTrainer = DatabaseUtility.FindTrainerById(leftPokemon.TrainerId, gameId);
            var tradeLog = new LogModel
            (
                user: gameMaster.TrainerName,
                action: $"authorized a trade between {leftTrainer.TrainerName} and {rightTrainer.TrainerName}"
            );
            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(gameId), tradeLog);
            req.HttpContext.Response.RefreshToken(gameMasterId);

            return new OkObjectResult(new TradeResultModel
            {
                LeftPokemon = leftPokemon,
                RightPokemon = rightPokemon,
            });
        }

        [FunctionName("UpdateHP")]
        [OpenApiOperation(operationId: "UpdateHP")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "pokemonId", In = ParameterLocation.Query, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "hp", In = ParameterLocation.Query, Required = true, Type = typeof(int))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult UpdateHP(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}/{{pokemonId}}/hp/{{hp}}")] HttpRequest req,
            Guid gameId,
            Guid trainerId,
            Guid pokemonId,
            int hp)
        {
            if (!req.VerifyIdentity(trainerId))
            {
                return new UnauthorizedResult();
            }

            var pokemon = DatabaseUtility.FindPokemonById(pokemonId);
            if (!(pokemon?.TrainerId == trainerId || DatabaseUtility.FindTrainerById(trainerId, gameId)?.IsGM == true))
            {
                return new UnauthorizedResult();
            }

            if (hp > pokemon.PokemonStats.HP || hp < -pokemon.PokemonStats.HP)
            {
                return new BadRequestObjectResult(nameof(hp));
            }

            DatabaseUtility.UpdatePokemonHP(pokemonId, hp);
            return new OkResult();
        }

        [FunctionName("SwitchForm")]
        [OpenApiOperation(operationId: "SwitchForm")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "pokemonId", In = ParameterLocation.Query, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "form", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PokemonModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult SwitchForm(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}/{{pokemonId}}/form/{{form}}")] HttpRequest req,
            Guid gameId,
            Guid trainerId,
            Guid pokemonId,
            string form)
        {
            if (!req.VerifyIdentity(trainerId))
            {
                return new UnauthorizedResult();
            }

            var pokemon = GetPokemonFromTrainer(gameId, trainerId, pokemonId, out var error);
            if (pokemon == null)
            {
                return error;
            }

            form = form.Replace('_', '/');
            if (!pokemon.AlternateForms.Contains(form))
            {
                return new BadRequestObjectResult(nameof(form));
            }

            var result = GetDifferentForm(pokemon, form);
            result.PokemonId = pokemon.PokemonId;
            result.OriginalTrainerId = pokemon.OriginalTrainerId;
            result.TrainerId = pokemon.TrainerId;
            result.IsOnActiveTeam = pokemon.IsOnActiveTeam;
            result.IsShiny = pokemon.IsShiny;
            result.CanEvolve = pokemon.CanEvolve;
            result.Pokeball = pokemon.Pokeball;
            if (!DatabaseUtility.TryChangePokemonForm(result, out var writeError))
            {
                return new BadRequestObjectResult(writeError);
            }

            var trainer = DatabaseUtility.FindTrainerById(trainerId, gameId);
            var changedFormLog = new LogModel
            (
                user: trainer.TrainerName,
                action: $"changed their {pokemon.Nickname} to its {form} form"
            );
            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(trainer.GameId), changedFormLog);
            req.HttpContext.Response.RefreshToken(trainerId);
            return new OkObjectResult(result);
        }

        [FunctionName("MarkPokemonAsEvolvable")]
        [OpenApiOperation(operationId: "MarkPokemonAsEvolvable")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "pokemonId", In = ParameterLocation.Query, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult MarkPokemonAsEvolvable(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{pokemonId}}/canEvolve")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid pokemonId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var pokemon = DatabaseUtility.FindPokemonById(pokemonId);
            var trainer = DatabaseUtility.FindTrainerById(pokemon.TrainerId, gameId);
            if (pokemon == null)
            {
                return new BadRequestObjectResult(nameof(pokemonId));
            }

            if (!DatabaseUtility.UpdatePokemonEvolvability(pokemonId, true))
            {
                return new BadRequestObjectResult(new GenericMessage($"Failed to mark pokemon {pokemonId} as evolvable"));
            }
            var evolutionLog = new LogModel
            (
                user: trainer.TrainerName,
                action: $"can now evolve their {pokemon.Nickname}"
            );
            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(trainer.GameId), evolutionLog);
            req.HttpContext.Response.RefreshToken(gameMasterId);
            return new OkResult();
        }

        [FunctionName("EvolvePokemonAsync")]
        [OpenApiOperation(operationId: "EvolvePokemonAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "pokemonId", In = ParameterLocation.Query, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType:  "application/json", bodyType: typeof(PokemonEvolutionDataModel), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PokemonModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> EvolvePokemonAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}/{{pokemonId}}/evolve")] HttpRequest req,
            Guid gameId,
            Guid trainerId,
            Guid pokemonId)
        {
            if (!req.VerifyIdentity(trainerId))
            {
                return new UnauthorizedResult();
            }

            var pokemon = GetPokemonFromTrainer(gameId, trainerId, pokemonId, out var error);
            if (pokemon == null)
            {
                return error;
            }

            var data = (await req.GetRequestBody()).ToObject<PokemonEvolutionDataModel>();
            var evolvedForm = GetEvolved
            (
                pokemon,
                data.NextForm,
                data.KeptMoves,
                data.NewMoves,
                out var badRequest
            );
            if (evolvedForm == null)
            {
                return badRequest;
            }

            if (!DatabaseUtility.UpdatePokemonWithEvolution(pokemonId, evolvedForm))
            {
                return new BadRequestObjectResult(new GenericMessage("Evolution failed"));
            }
            var trainer = DatabaseUtility.FindTrainerById(trainerId, gameId);
            var evolutionLog = new LogModel
            (
                user: trainer.TrainerName,
                action: $"evolved their {pokemon.Nickname} to an {evolvedForm.SpeciesName}"
            );
            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(trainer.GameId), evolutionLog);

            var dexItem = DatabaseUtility.GetPokedexItem(trainerId, gameId, evolvedForm.DexNo);
            if (dexItem != null)
            {
                if (!dexItem.IsCaught)
                {
                    if (!DatabaseUtility.UpdateDexItemIsCaught(trainerId, gameId, evolvedForm.DexNo))
                    {
                        return new BadRequestObjectResult(new GenericMessage("Failed to update Dex Item"));
                    }
                }
            }
            else if (!DatabaseUtility.UpdateDexItemIsCaught(trainerId, gameId, evolvedForm.DexNo))
            {
                return new BadRequestObjectResult(new GenericMessage("Failed to update Dex Item"));
            }
            req.HttpContext.Response.RefreshToken(trainerId);
            return new OkObjectResult(evolvedForm);
        }

        [FunctionName("UpdateDexItemIsSeen")]
        [OpenApiOperation(operationId: "UpdateDexItemIsSeen")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "dexNo", In = ParameterLocation.Query, Required = true, Type = typeof(int))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AbstractMessage))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(AbstractMessage))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult UpdateDexItemIsSeen(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{trainerId}}/saw")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid trainerId)
        {
            if (!int.TryParse(req.Query["dexNo"], out var dexNo))
            {
                return new BadRequestObjectResult("Missing integer type query parameter 'dexNo'");
            }

            var actionResult = GetDexNoForPokedexUpdate(req, gameId, gameMasterId, trainerId);
            if (dexNo < 1 || dexNo >= DexUtility.GetDexEntries<BasePokemonModel>(DexType.BasePokemon).Count())
            {
                return actionResult;
            }

            var dexItem = DatabaseUtility.GetPokedexItem(trainerId, gameId, dexNo);
            if (dexItem != null)
            {
                if (dexItem.IsSeen)
                {
                    return new OkObjectResult(new GenericMessage("Pokemon was already seen"));
                }
                if (!DatabaseUtility.UpdateDexItemIsSeen(trainerId, gameId, dexNo))
                {
                    return new BadRequestObjectResult(new GenericMessage("Failed to update Dex Item"));
                }

                return new OkObjectResult(new GenericMessage("Pokedex updated successfully"));
            }

            return AddDexItem(trainerId, gameId, dexNo, isSeen: true);
        }

        [FunctionName("UpdateDexItemIsCaught")]
        [OpenApiOperation(operationId: "UpdateDexItemIsCaught")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "dexNo", In = ParameterLocation.Query, Required = true, Type = typeof(int))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AbstractMessage))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(AbstractMessage))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult UpdateDexItemIsCaught(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{trainerId}}/caught")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid trainerId)
        {
            if (!int.TryParse(req.Query["dexNo"], out var dexNo))
            {
                return new BadRequestObjectResult("Missing integer type query parameter 'dexNo'");
            }

            var actionResult = GetDexNoForPokedexUpdate(req, gameId, gameMasterId, trainerId);
            if (dexNo < 1 || dexNo >= DexUtility.GetDexEntries<BasePokemonModel>(DexType.BasePokemon).Count())
            {
                return actionResult;
            }

            var dexItem = DatabaseUtility.GetPokedexItem(trainerId, gameId, dexNo);
            if (dexItem != null)
            {
                if (dexItem.IsCaught)
                {
                    return new OkObjectResult(new GenericMessage("Pokemon was already caught"));
                }
                if (!DatabaseUtility.UpdateDexItemIsCaught(trainerId, gameId, dexNo))
                {
                    return new BadRequestObjectResult(new GenericMessage("Failed to update Dex Item"));
                }

                return new OkObjectResult(new GenericMessage("Pokedex updated successfully"));
            }

            return AddDexItem(trainerId, gameId, dexNo, isCaught: true);
        }

        [FunctionName("DeletePokemon")]
        [OpenApiOperation(operationId: "DeletePokemon")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "pokemonId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AbstractMessage))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult DeletePokemon(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{pokemonId}}")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid pokemonId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            if (!DatabaseUtility.DeletePokemon(pokemonId))
            {
                return new NotFoundObjectResult(pokemonId);
            }

            req.HttpContext.Response.RefreshToken(gameMasterId);
            return new OkObjectResult(new GenericMessage($"Successfully deleted {pokemonId}"));
        }

        private static PokemonModel GetDifferentForm(PokemonModel pokemon, string form)
        {
            return DexUtility.GetNewPokemon
            (
                pokemon.SpeciesName,
                Enum.Parse<Nature>(pokemon.Nature),
                Enum.Parse<Gender>(pokemon.Gender),
                Enum.Parse<Status>(pokemon.PokemonStatus),
                pokemon.Nickname,
                form
            );
        }

        private IActionResult AddDexItem(Guid trainerId, Guid gameId, int dexNo, bool isSeen = false, bool isCaught = false)
        {
            if (isCaught)
            {
                isSeen = true;
            }

            var result = DatabaseUtility.TryAddDexItem(
                trainerId,
                gameId,
                dexNo,
                isSeen,
                isCaught,
                out var error
            );
            if (!result)
            {
                return new BadRequestObjectResult(error);
            }

            return new OkObjectResult(new GenericMessage("Pokedex item added successfully"));
        }

        private IActionResult GetDexNoForPokedexUpdate(HttpRequest request, Guid gameId, Guid gameMasterId, Guid trainerId)
        {
            if (!request.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var trainer = DatabaseUtility.FindTrainerById(trainerId, gameId);
            if (trainer == null)
            {
                return new NotFoundObjectResult(trainerId);
            }

            return new OkResult();
        }

        private static void UpdatePokemonTrainerIds(
            PokemonModel leftPokemon,
            PokemonModel rightPokemon)
        {

            DatabaseUtility.UpdatePokemonTrainerId
            (
                leftPokemon.PokemonId,
                rightPokemon.TrainerId
            );

            DatabaseUtility.UpdatePokemonLocation
            (
                leftPokemon.PokemonId,
                rightPokemon.IsOnActiveTeam
            );

            DatabaseUtility.UpdatePokemonTrainerId
            (
                rightPokemon.PokemonId,
                leftPokemon.TrainerId
            );

            DatabaseUtility.UpdatePokemonLocation
            (
                rightPokemon.PokemonId,
                leftPokemon.IsOnActiveTeam
            );
        }

        private (PokemonModel LeftPokemon, PokemonModel RightPokemon) GetTradePokemon(
            Guid leftPokemonId,
            Guid rightPokemonId,
            out IActionResult notFound)
        {
            var leftDocument = GetDocument(leftPokemonId, Collection, out notFound);
            if (leftDocument is not PokemonModel leftPokemon)
            {
                return default;
            }

            var rightDocument = GetDocument(rightPokemonId, Collection, out notFound);
            if (rightDocument is not PokemonModel rightPokemon)
            {
                return default;
            }

            if (leftPokemon.TrainerId == rightPokemon.TrainerId)
            {
                notFound = new BadRequestObjectResult(new GenericMessage("Cannot trade pokemon to oneself"));
                return default;
            }

            return (leftPokemon, rightPokemon);
        }

        private PokemonModel GetPokemonFromTrainer(
            Guid gameId,
            Guid trainerId,
            Guid pokemonId,
            out IActionResult error)
        {
            var trainerDocument = DatabaseUtility.FindTrainerById(trainerId, gameId);
            if (trainerDocument is not TrainerModel trainer)
            {
                error = new NotFoundObjectResult(trainerId);
                return null;
            }

            var pokemonDocument = GetDocument(pokemonId, Collection, out error);
            if (pokemonDocument is not PokemonModel pokemon)
            {
                return null;
            }

            if (!(trainer.TrainerId == pokemon.TrainerId || trainer.IsGM))
            {
                error = new UnauthorizedResult();
                return null;
            }

            return pokemon;
        }

        private PokemonModel GetEvolved(
            PokemonModel currentForm,
            string evolvedFormName,
            IEnumerable<string> keptMoves,
            IEnumerable<string> newMoves,
            out BadRequestObjectResult badRequest)
        {
            badRequest = null;
            if (string.IsNullOrEmpty(evolvedFormName))
            {
                badRequest = new BadRequestObjectResult(new GenericMessage("Missing evolvedFormName"));
                return null;
            }

            var total = keptMoves.Count() + newMoves.Count();
            if (total < 3)
            {
                badRequest = new BadRequestObjectResult(new GenericMessage("Too few moves"));
                return null;
            }
            if (total > 6)
            {
                badRequest = new BadRequestObjectResult(new GenericMessage("Too many moves"));
                return null;
            }

            var moveComparer = currentForm.Moves.Select(move => move.ToLower());
            if (!keptMoves.All(move => moveComparer.Contains(move.ToLower())))
            {
                badRequest = new BadRequestObjectResult(new GenericMessage($"{currentForm.Nickname} doesn't contain one of {string.Join(", ", keptMoves)}"));
                return null;
            }

            var evolvedForm = DexUtility.GetEvolved(currentForm, keptMoves, evolvedFormName, newMoves);
            if (evolvedForm == null)
            {
                badRequest = new BadRequestObjectResult(new GenericMessage($"Could not evolve {currentForm.Nickname} to {evolvedFormName}"));
            }

            evolvedForm.Pokeball = currentForm.Pokeball;
            return evolvedForm;
        }
    }
}
