using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheReplacement.PTA.Api.Abstractions;
using TheReplacement.PTA.Api.Messages;
using TheReplacement.PTA.Api.Objects;
using TheReplacement.PTA.Api.Services.Enums;
using TheReplacement.PTA.Api.Services.Models;
using TheReplacement.PTA.Api.Services;
using TheReplacement.PTA.Api.Extensions;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs;
using Microsoft.OpenApi.Models;
using System.Net;
using TheReplacement.PTA.Api.Models;
using Microsoft.Extensions.Logging;

namespace TheReplacement.PTA.Api.Controllers
{
    public class SettingController : BasePtaController
    {
        private const string RoutePrefix = "v1/setting";
        private static readonly byte[] Buffer = new byte[36];

        public SettingController(ILogger<SettingController> log)
        {
            _logger = log;
            Collection = MongoCollection.Settings;
        }

        protected override MongoCollection Collection { get; }

        [FunctionName("GetEnvironments")]
        [OpenApiOperation(operationId: "GetEnvironments")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string[]))]
        public IActionResult GetEnvironments(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = RoutePrefix)] HttpRequest req)
        {
            var environments = Enum.GetNames<Environments>().Where(environment => environment != "Default");

            return new OkObjectResult(environments);
        }

        [FunctionName("GetActiveSetting")]
        [OpenApiOperation(operationId: "GetActiveSetting")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetActiveSettingAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{gameId}}")] HttpRequest req,
            Guid gameId)
        {
            if (req.HttpContext.WebSockets.IsWebSocketRequest)
            {
                await StreamSetting(req, gameId);
            }

            return new BadRequestResult();
        }

        [FunctionName("GetAllSettings")]
        [OpenApiOperation(operationId: "GetAllSettings")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SettingModel[]))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult GetAllSettings(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/all")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var settings = DatabaseUtility.FindAllSettings(gameId).ToList();
            return new OkObjectResult(settings);
        }

        [FunctionName("CreateSettingAsync")]
        [OpenApiOperation(operationId: "CreateSettingAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SettingCreationModel), Required = true)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(AbstractMessage))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateSettingAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var (name, type, message) = await GetCreationParametersAsync(req);
            if (message != null)
            {
                return new BadRequestObjectResult(message);
            }

            var encounter = new SettingModel
            {
                SettingId = Guid.NewGuid(),
                GameId = gameId,
                Name = name,
                Type = type,
                ActiveParticipants = Array.Empty<SettingParticipantModel>(),
                Environment = Array.Empty<string>(),
                Shops = Array.Empty<Guid>()
            };

            var (addResult, error) = DatabaseUtility.TryAddSetting(encounter);
            if (!addResult)
            {
                return new BadRequestObjectResult(error);
            }

            return new OkResult();
        }

        [FunctionName("SetEnvironment")]
        [OpenApiOperation(operationId: "SetEnvironment")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "environments", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        public IActionResult SetEnvironment(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/environment")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            string environments = req.Query["environments"];
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var setting = DatabaseUtility.FindActiveSetting(gameId);
            if (setting == null)
            {
                return new NotFoundObjectResult(gameId);
            }

            setting.Environment = environments.Split(',');
            if (DatabaseUtility.UpdateSetting(setting))
            {
                return new OkResult();
            }
            return new BadRequestResult();
        }

        [FunctionName("AddToActiveSettingAsync")]
        [OpenApiOperation(operationId: "AddToActiveSettingAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SettingParticipantModel), Required = true)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict)]
        public async Task<IActionResult> AddToActiveSettingAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}")] HttpRequest req,
            Guid gameId,
            Guid trainerId)
        {
            if (!req.VerifyIdentity(trainerId))
            {
                return new UnauthorizedResult();
            }

            var encounter = DatabaseUtility.FindActiveSetting(gameId);
            if (encounter == null)
            {
                return new NotFoundResult();
            }

            var json = await req.GetRequestBody();
            var participant = json.ToObject<SettingParticipantModel>();
            if (encounter.ActiveParticipants.Any(activeParticipant => activeParticipant.ParticipantId == participant.ParticipantId))
            {
                return new ConflictResult();
            }

            var isPositionBlocked = encounter.ActiveParticipants.Any(activeParticipant =>
            {
                return (activeParticipant.Position.X == participant.Position.X) && (activeParticipant.Position.Y == participant.Position.Y);
            });
            if (isPositionBlocked)
            {
                return new ConflictResult();
            }

            encounter.ActiveParticipants = encounter.ActiveParticipants.Append(participant);
            if (!DatabaseUtility.UpdateSetting(encounter))
            {
                return new BadRequestResult();
            }

            return new OkResult();
        }

        [FunctionName("RemoveFromActiveSetting")]
        [OpenApiOperation(operationId: "RemoveFromActiveSetting")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "participantId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        public IActionResult RemoveFromActiveSetting(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{participantId}}/remove")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid participantId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            return RemoveFromParticipants(gameId, participantId);
        }

        [FunctionName("ReturnToPokeball")]
        [OpenApiOperation(operationId: "ReturnToPokeball")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "pokemonId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        public IActionResult ReturnToPokeball(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}/{{pokemonId}}/catch")] HttpRequest req,
            Guid gameId,
            Guid trainerId,
            Guid pokemonId)
        {
            if (!req.VerifyIdentity(trainerId))
            {
                return new UnauthorizedResult();
            }

            var pokemon = DatabaseUtility.FindPokemonById(pokemonId);
            if (pokemon.TrainerId != trainerId)
            {
                return new BadRequestResult();
            }

            return RemoveFromParticipants(gameId, pokemonId);
        }

        [FunctionName("CatchPokemon")]
        [OpenApiOperation(operationId: "CatchPokemon")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "pokemonId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "catchRate", In = ParameterLocation.Query, Required = true, Type = typeof(int))]
        [OpenApiParameter(name: "pokeball", In = ParameterLocation.Query, Required = true, Type = typeof(Pokeball))]
        [OpenApiParameter(name: "nickname", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        public IActionResult CatchPokemon(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}/{{pokemonId}}/return")] HttpRequest req,
            Guid gameId,
            Guid trainerId,
            Guid pokemonId)
        {
            if (!int.TryParse(req.Query["catchRate"], out var catchRate))
            {
                return new BadRequestObjectResult(nameof(catchRate));
            }

            string pokeball = req.Query["pokeball"];
            string nickname = req.Query["nickname"];
            if (!req.VerifyIdentity(trainerId))
            {
                return new UnauthorizedResult();
            }

            var pokemon = DatabaseUtility.FindPokemonById(pokemonId);
            if (pokemon.TrainerId != Guid.Empty)
            {
                return new BadRequestObjectResult(pokemonId);
            }

            var encounter = DatabaseUtility.FindActiveSetting(gameId);
            if (encounter == null || pokemon.GameId != gameId)
            {
                return new BadRequestObjectResult(gameId);
            }

            if (!Enum.TryParse<Pokeball>(pokeball, true, out var pokeballEnum))
            {
                return new BadRequestObjectResult(pokeball);
            }

            var trainer = DatabaseUtility.FindTrainerById(trainerId, gameId);
            if (trainer?.IsOnline != true)
            {
                return new NotFoundObjectResult(trainerId);
            }

            var items = new[]
            {
                new ItemModel
                {
                    Name = pokeballEnum.ToString().Replace("_", " "),
                    Amount = 1
                }
            };
            RemoveItemsFromTrainer(trainer, items);

            var pokeballModifier = GetPokeballModifier(pokemon, trainerId, pokeballEnum, encounter.Environment);
            var random = new Random();
            var check = random.Next(1, 101) + pokeballModifier;
            var log = new LogModel
            (
                user: DatabaseUtility.FindTrainerById(trainerId, gameId).TrainerName,
                action: $"failed to catch {pokemon.Nickname}"
            );

            if (check < catchRate)
            {
                encounter.ActiveParticipants = encounter.ActiveParticipants.Where(participant => participant.ParticipantId != pokemonId);
                DatabaseUtility.UpdateSetting(encounter);
                pokemon.Pokeball = pokeballEnum.ToString().Replace("_", "");
                pokemon.OriginalTrainerId = trainerId;
                pokemon.TrainerId = trainerId;
                var allMons = DatabaseUtility.FindPokemonByTrainerId(trainerId, gameId).Where(pokemon => pokemon.IsOnActiveTeam).Count();
                pokemon.IsOnActiveTeam = allMons < 6;
                if (!string.IsNullOrWhiteSpace(nickname))
                {
                    pokemon.Nickname = nickname;
                }
                DatabaseUtility.UpdatePokemon(pokemon);
                log.Action = $"successfully caught a {pokemon.SpeciesName} named '{pokemon.Nickname}' at {DateTime.UtcNow}";
            }

            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(gameId), log);
            return new OkResult();
        }

        [FunctionName("UpdatePositionAsync")]
        [OpenApiOperation(operationId: "UpdatePositionAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "participantId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(MapPositionModel), Required = true)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdatePositionAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{participantId}}/position")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid participantId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var encounter = DatabaseUtility.FindActiveSetting(gameId);
            if (encounter == null)
            {
                return new NotFoundResult();
            }

            var position = (await req.GetRequestBody()).ToObject<MapPositionModel>();
            var isPositionBlocked = encounter.ActiveParticipants.Any(activeParticipant =>
            {
                if (activeParticipant.Position.X == position.X)
                {
                    if (activeParticipant.Position.Y == position.Y)
                    {
                        return true;
                    }
                }

                return false;
            });
            if (isPositionBlocked)
            {
                return new ConflictResult();
            }

            var participant = encounter.ActiveParticipants.First(participant => participant.ParticipantId == participantId);
            encounter.ActiveParticipants = encounter.ActiveParticipants.Select(participant =>
            {
                if (participant.ParticipantId == participantId)
                {
                    participant.Position = position;
                }

                return participant;
            });

            if (!DatabaseUtility.UpdateSetting(encounter))
            {
                return new BadRequestResult();
            }


            SendRepositionLog(gameId, participant.Name, position);
            return new OkResult();
        }

        [FunctionName("UpdateTrainerPositionAsync")]
        [OpenApiOperation(operationId: "UpdateTrainerPositionAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(MapPositionModel), Required = true)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateTrainerPositionAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}/trainer_position")] HttpRequest req,
            Guid gameId,
            Guid trainerId)
        {
            if (!req.VerifyIdentity(trainerId))
            {
                return new UnauthorizedResult();
            }

            var encounter = DatabaseUtility.FindActiveSetting(gameId);
            if (encounter == null)
            {
                return new NotFoundResult();
            }

            var position = (await req.GetRequestBody()).ToObject<MapPositionModel>();
            var participant = encounter.ActiveParticipants.First(participant => participant.ParticipantId == trainerId);
            if (GetDistance(position, participant.Position) > participant.Speed)
            {
                return new StatusCodeResult(411);
            }
            var isPositionBlocked = encounter.ActiveParticipants.Any(activeParticipant =>
            {
                if (activeParticipant.Position.X == position.X)
                {
                    if (activeParticipant.Position.Y == position.Y)
                    {
                        return true;
                    }
                }

                return false;
            });
            if (isPositionBlocked)
            {
                return new ConflictResult();
            }

            encounter.ActiveParticipants = encounter.ActiveParticipants.Select(participant =>
            {
                if (participant.ParticipantId == trainerId)
                {
                    participant.Position = position;
                }

                return participant;
            });

            if (!DatabaseUtility.UpdateSetting(encounter))
            {
                return new BadRequestResult();
            }


            SendRepositionLog(gameId, participant.Name, position);
            return new OkResult();
        }

        [FunctionName("UpdateTrainerPokemonPositionAsync")]
        [OpenApiOperation(operationId: "UpdateTrainerPokemonPositionAsync")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(MapPositionModel), Required = true)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateTrainerPokemonPositionAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}/{{pokemonId}}/pokemon_position")] HttpRequest req,
            Guid gameId,
            Guid trainerId,
            Guid pokemonId)
        {
            if (!req.VerifyIdentity(trainerId))
            {
                return new UnauthorizedResult();
            }

            var trainer = DatabaseUtility.FindTrainerById(trainerId, gameId);
            var encounter = DatabaseUtility.FindActiveSetting(gameId);
            if (encounter == null)
            {
                return new NotFoundResult();
            }

            var pokemon = DatabaseUtility.FindPokemonById(pokemonId);
            if (pokemon.TrainerId != trainerId)
            {
                return new ConflictResult();
            }

            var position = (await req.GetRequestBody()).ToObject<MapPositionModel>();
            var participant = encounter.ActiveParticipants.First(participant => participant.ParticipantId == pokemonId);
            if (GetDistance(position, participant.Position) > participant.Speed)
            {
                return new StatusCodeResult(411);
            }
            var isPositionBlocked = encounter.ActiveParticipants.Any(activeParticipant =>
            {
                if (activeParticipant.Position.X == position.X)
                {
                    if (activeParticipant.Position.Y == position.Y)
                    {
                        return true;
                    }
                }

                return false;
            });
            if (isPositionBlocked)
            {
                return new ConflictResult();
            }

            encounter.ActiveParticipants = encounter.ActiveParticipants.Select(participant =>
            {
                if (participant.ParticipantId == pokemonId)
                {
                    participant.Position = position;
                }

                return participant;
            });

            if (!DatabaseUtility.UpdateSetting(encounter))
            {
                return new BadRequestResult();
            }


            SendRepositionLog(gameId, participant.Name, position);
            return new OkResult();
        }

        [FunctionName("SetSettingToActive")]
        [OpenApiOperation(operationId: "SetSettingToActive")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "settingId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        public IActionResult SetSettingToActive(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{settingId}}/active")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid settingId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var gameMaster = DatabaseUtility.FindTrainerById(gameMasterId, gameId);
            if (DatabaseUtility.FindActiveSetting(gameId) != null)
            {
                return new ConflictResult();
            }

            var encounter = DatabaseUtility.FindSetting(settingId);
            if (encounter == null)
            {
                return new NotFoundObjectResult(settingId);
            }

            encounter.IsActive = true;
            if (!DatabaseUtility.UpdateSetting(encounter))
            {
                return new BadRequestResult();
            }

            var newSettingLog = new LogModel(user: gameMaster.TrainerName, action: $"activated a new encounter ({encounter.Name})");
            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(gameMaster.GameId), newSettingLog);
            return new OkResult();
        }

        [FunctionName("SetSettingToInactive")]
        [OpenApiOperation(operationId: "SetSettingToInactive")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "settingId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        public IActionResult SetSettingToInactive(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{settingId}}/inactive")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid settingId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var gameMaster = DatabaseUtility.FindTrainerById(gameMasterId, gameId);
            var encounter = DatabaseUtility.FindSetting(settingId);
            if (encounter == null)
            {
                return new NotFoundObjectResult(settingId);
            }

            encounter.IsActive = false;
            if (!DatabaseUtility.UpdateSetting(encounter))
            {
                return new BadRequestResult();
            }

            var closedSettingLog = new LogModel(user: gameMaster.TrainerName, action: $"has closed encounter ({encounter.Name})");
            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(gameMaster.GameId), closedSettingLog);
            return new OkResult();
        }

        [FunctionName("UpdateParticipantsHp")]
        [OpenApiOperation(operationId: "UpdateParticipantsHp")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "settingId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        public IActionResult UpdateParticipantsHp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{settingId}}/hp")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid settingId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var encounter = DatabaseUtility.FindSetting(settingId);
            if (encounter == null)
            {
                return new NotFoundObjectResult(settingId);
            }

            encounter.ActiveParticipants = encounter.ActiveParticipants.Select(participant => GetWithUpdatedHP(participant, gameId));
            if (!DatabaseUtility.UpdateSetting(encounter))
            {
                return new BadRequestResult();
            }

            return new OkResult();
        }

        [FunctionName("DeleteSetting")]
        [OpenApiOperation(operationId: "DeleteSetting")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "settingId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        public IActionResult DeleteSetting(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{settingId}}")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid settingId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            if (!DatabaseUtility.DeleteSetting(settingId))
            {
                return new BadRequestResult();
            }

            return new OkResult();
        }

        [FunctionName("DeleteAllSettings")]
        [OpenApiOperation(operationId: "DeleteAllSettings")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        public IActionResult DeleteAllSettings(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            if (!DatabaseUtility.DeleteSettingsByGameId(gameId))
            {
                return new BadRequestResult();
            }

            return new OkResult();
        }

        private static async Task<(string Name, string Type, AbstractMessage Message)> GetCreationParametersAsync(HttpRequest request)
        {
            var data = (await request.GetRequestBody()).ToObject<SettingCreationModel>();
            if (data.Name == null)
            {
                return (null, null, new GenericMessage($"Missing Body parameter: name"));
            }
            if (data.Type == null)
            {
                return (null, null, new GenericMessage($"Missing Body parameter: type"));
            }

            if (string.IsNullOrWhiteSpace(data.Name) || data.Name.Length > 30)
            {
                return (null, null, new GenericMessage("Make sure the name parameter is between 1 and 30 characters"));
            }
            if (!Enum.TryParse<SettingType>(data.Type, true, out _))
            {
                return (null, null, new GenericMessage($"Make sure the type parameter is one of {string.Join(',', Enum.GetNames(typeof(SettingType)))}"));
            }

            return (data.Name, data.Type, null);
        }

        private async Task StreamSetting(HttpRequest request, Guid gameId)
        {
            using var webSocket = await request.HttpContext.WebSockets.AcceptWebSocketAsync();
            var recieved = await RecieveAsync(webSocket);
            while (!recieved.CloseStatus.HasValue)
            {
                await SendAsync
                (
                    webSocket,
                    gameId,
                    recieved.MessageType,
                    recieved.EndOfMessage
                );

                recieved = await RecieveAsync(webSocket);
            }

            await webSocket.CloseAsync
            (
                recieved.CloseStatus.Value,
                recieved.CloseStatusDescription,
                CancellationToken.None
            );
        }

        private ActionResult RemoveFromParticipants(Guid gameId, Guid participantId)
        {
            var encounter = DatabaseUtility.FindActiveSetting(gameId);
            var removedParticipant = encounter.ActiveParticipants.First(participant => participant.ParticipantId == participantId);
            encounter.ActiveParticipants = encounter.ActiveParticipants.Where(participant => participant.ParticipantId != participantId);

            if (!DatabaseUtility.UpdateSetting(encounter))
            {
                return new BadRequestResult();
            }

            var removalLog = new LogModel(user: removedParticipant.Name, action: $"has been removed from {encounter.Name}");
            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(gameId), removalLog);
            return new OkResult();
        }

        private static async Task<WebSocketReceiveResult> RecieveAsync(WebSocket webSocket)
        {
            return await webSocket.ReceiveAsync(new ArraySegment<byte>(Buffer), CancellationToken.None);
        }

        private static async Task SendAsync(
            WebSocket webSocket,
            Guid gameId,
            WebSocketMessageType messageType,
            bool endOfMessage)
        {
            var encounter = DatabaseUtility.FindActiveSetting(gameId);
            var message = JsonConvert.SerializeObject(encounter);
            var messageAsBytes = Encoding.ASCII.GetBytes(message);
            await webSocket.SendAsync
            (
                new ArraySegment<byte>(messageAsBytes),
                messageType,
                endOfMessage,
                CancellationToken.None
            );
        }

        private static int GetPokeballModifier(PokemonModel pokemon, Guid trainerId, Pokeball pokeball, string[] environments)
        {
            if (pokemon.CurrentHP < 1 && pokeball != Pokeball.Save_Ball)
            {
                return 100;
            }
            var consideredBasic = 5;
            var consideredGreat = 0;
            var consideredUltra = -5;
            var environment = Environments.Default;
            var types = PokemonTypes.None;
            foreach (var type in pokemon.Type.Split('/'))
            {
                types |= Enum.Parse<PokemonTypes>(type, true);
            }
            if (environments != null)
            {
                foreach (var env in environments)
                {
                    environment |= Enum.Parse<Environments>(env, true);
                }
            }

            return pokeball switch
            {
                Pokeball.Park_Ball => environment.HasFlag(Environments.Safari) ? -20 : consideredBasic,
                Pokeball.Cherish_Ball => consideredUltra,
                Pokeball.Premier_Ball => consideredUltra,
                Pokeball.Sport_Ball => environment.HasFlag(Environments.Safari) ? -20 : consideredBasic,
                Pokeball.Heavy_Ball => Enum.TryParse<Weight>(pokemon.Weight, true, out var result) && (result == Weight.Heavy || result == Weight.Superweight) ? -15 : consideredBasic,
                Pokeball.Level_Ball => consideredBasic,
                Pokeball.Nest_Ball => consideredBasic,
                Pokeball.Rainforest_Ball => environment.HasFlag(Environments.Rainforest) ? -12 : consideredBasic,
                Pokeball.Great_Ball => consideredGreat,
                Pokeball.Safari_Ball => environment.HasFlag(Environments.Safari) ? -20 : consideredBasic,
                Pokeball.Luxury_Ball => consideredUltra,
                Pokeball.Lure_Ball => environment.HasFlag(Environments.InCombat) ? -10 : consideredBasic,
                Pokeball.Heat_Ball => types.HasFlag(PokemonTypes.Electric) || types.HasFlag(PokemonTypes.Fire) ? -15 : consideredBasic,
                Pokeball.Cave_Ball => environment.HasFlag(Environments.Cave) ? -12 : consideredBasic,
                Pokeball.Earth_Ball => types.HasFlag(PokemonTypes.Grass) || types.HasFlag(PokemonTypes.Ground) ? -15 : consideredBasic,
                Pokeball.Fine_Ball => types.HasFlag(PokemonTypes.Normal) || types.HasFlag(PokemonTypes.Fairy) ? -15 : consideredBasic,
                Pokeball.Taiga_Ball => environment.HasFlag(Environments.Taiga) ? -12 : consideredBasic,
                Pokeball.Save_Ball => pokemon.CurrentHP < 1 ? -10 : consideredBasic,
                Pokeball.Artic_Ball => environment.HasFlag(Environments.Artic) ? -12 : consideredBasic,
                Pokeball.Desert_Ball => environment.HasFlag(Environments.Desert) ? -12 : consideredBasic,
                Pokeball.Haunt_Ball => types.HasFlag(PokemonTypes.Dark) || types.HasFlag(PokemonTypes.Ghost) ? -15 : consideredBasic,
                Pokeball.Urban_Ball => environment.HasFlag(Environments.Urban) ? -12 : consideredBasic,
                Pokeball.Net_Ball => types.HasFlag(PokemonTypes.Water) || types.HasFlag(PokemonTypes.Bug) ? -15 : consideredBasic,
                Pokeball.Freshwater_Ball => environment.HasFlag(Environments.Freshwater) ? -12 : consideredBasic,
                Pokeball.Beach_Ball => environment.HasFlag(Environments.Beach) ? -12 : consideredBasic,
                Pokeball.Timer_Ball => consideredUltra,
                Pokeball.Mystic_Ball => types.HasFlag(PokemonTypes.Dragon) || types.HasFlag(PokemonTypes.Psychic) ? -15 : consideredBasic,
                Pokeball.Air_Ball => types.HasFlag(PokemonTypes.Flying) || types.HasFlag(PokemonTypes.Ice) ? -15 : consideredBasic,
                Pokeball.Fast_Ball => consideredUltra,
                Pokeball.Basic_Ball => consideredUltra,
                Pokeball.Heal_Ball => consideredGreat,
                Pokeball.Master_Ball => -100,
                Pokeball.Tundra_Ball => environment.HasFlag(Environments.Tundra) ? -12 : consideredBasic,
                Pokeball.Friend_Ball => consideredGreat,
                Pokeball.Grassland_Ball => environment.HasFlag(Environments.Grassland) ? -12 : consideredBasic,
                Pokeball.Marsh_Ball => environment.HasFlag(Environments.Marsh) ? -12 : consideredBasic,
                Pokeball.Quick_Ball => consideredBasic,
                Pokeball.Repeat_Ball => DatabaseUtility.GetPokedexItem(trainerId, pokemon.GameId, pokemon.DexNo)?.IsCaught == true ? -10 : consideredBasic,
                Pokeball.Dream_Ball => Enum.TryParse<Status>(pokemon.PokemonStatus, true, out var result) && result == Status.Asleep ? -10 : consideredBasic,
                Pokeball.Moon_Ball => consideredBasic,
                Pokeball.Dusk_Ball => environment.HasFlag(Environments.NoSunlight) ? -12 : consideredBasic,
                Pokeball.Mold_Ball => types.HasFlag(PokemonTypes.Poison) || types.HasFlag(PokemonTypes.Fighting) ? -15 : consideredBasic,
                Pokeball.Solid_Ball => types.HasFlag(PokemonTypes.Rock) || types.HasFlag(PokemonTypes.Steel) ? -15 : consideredBasic,
                Pokeball.Forest_Ball => environment.HasFlag(Environments.Forest) ? -12 : consideredBasic,
                Pokeball.Love_Ball => consideredBasic,
                Pokeball.Mountain_Ball => environment.HasFlag(Environments.Mountain) ? -12 : consideredBasic,
                _ => 1000
            };
        }

        private static SettingParticipantModel GetWithUpdatedHP(SettingParticipantModel participant, Guid gameId)
        {
            var type = Enum.Parse<SettingParticipantType>(participant.Type, true);
            return type switch
            {
                SettingParticipantType.Trainer => SettingParticipantModel.FromTrainer(participant.ParticipantId, gameId, participant.Position),
                SettingParticipantType.Pokemon => SettingParticipantModel.FromPokemon(participant.ParticipantId, participant.Position, type),
                SettingParticipantType.EnemyNpc => SettingParticipantModel.FromNpc(participant.ParticipantId, participant.Position, type),
                SettingParticipantType.EnemyPokemon => SettingParticipantModel.FromPokemon(participant.ParticipantId, participant.Position, type),
                SettingParticipantType.NeutralNpc => SettingParticipantModel.FromNpc(participant.ParticipantId, participant.Position, type),
                SettingParticipantType.NeutralPokemon => SettingParticipantModel.FromPokemon(participant.ParticipantId, participant.Position, type),
                _ => throw new ArgumentOutOfRangeException(nameof(participant.Type)),
            };
        }

        private static void SendRepositionLog(Guid gameId, string participantName, MapPositionModel position)
        {
            var repositionLog = new LogModel
            (
                user: participantName,
                action: $"moved to point ({position.X}, {position.Y})"
            );
            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(gameId), repositionLog);
        }

        private static double GetDistance(MapPositionModel start, MapPositionModel end)
        {
            return Math.Sqrt(Math.Pow(start.X - end.X, 2) + Math.Pow(start.Y - end.Y, 2));
        }
    }
}
