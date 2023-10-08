using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TheReplacement.PTA.Api.Abstractions;
using TheReplacement.PTA.Api.Extensions;
using TheReplacement.PTA.Api.Services.Enums;
using TheReplacement.PTA.Api.Services.Models;
using TheReplacement.PTA.Api.Services;
using TheReplacement.PTA.Api.Models;

namespace TheReplacement.PTA.Api.Controllers
{
    public class ShopController : BasePtaController
    {
        private const string RoutePrefix = "v1/shop";

        public ShopController(ILogger<ShopController> log)
        {
            _logger = log;
        }

        protected override MongoCollection Collection { get; }

        [FunctionName("GetShopGM")]
        [OpenApiOperation(operationId: "GetShopGM")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "shopId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ShopModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult GetShopGM(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{shopId}}/gm")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid shopId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var shop = DatabaseUtility.FindShopById(shopId, gameId);
            if (shop == null)
            {
                return new NotFoundObjectResult(shopId);
            }

            return new OkObjectResult(shop);
        }

        [FunctionName("GetShopTrainer")]
        [OpenApiOperation(operationId: "GetShopTrainer")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "shopId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ShopModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult GetShopTrainer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}/{{shopId}}/trainer")] HttpRequest req,
            Guid gameId,
            Guid trainerId,
            Guid shopId)
        {
            if (!req.VerifyIdentity(trainerId))
            {
                return new UnauthorizedResult();
            }

            var shop = DatabaseUtility.FindShopById(shopId, gameId);
            if (shop?.IsActive != true)
            {
                return new NotFoundObjectResult(shopId);
            }

            return new OkObjectResult(shop);
        }

        [FunctionName("GetShops")]
        [OpenApiOperation(operationId: "GetShops")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "shopId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ShopModel[]))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult GetShops(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var shops = DatabaseUtility.FindShopsByGameId(gameId);
            return new OkObjectResult(shops);
        }

        [FunctionName("GetShopsBySettingGM")]
        [OpenApiOperation(operationId: "GetShopsBySettingGM")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "settingId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ShopModel[]))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult GetShopsBySettingGM(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{settingId}}/setting/gm")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid settingId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var setting = DatabaseUtility.FindSetting(settingId);
            if (setting?.GameId != gameId)
            {
                return new NotFoundResult();
            }

            var shops = DatabaseUtility.FindShopsBySetting(setting);
            return new OkObjectResult(shops);
        }

        [FunctionName("GetShopsBySettingTrainer")]
        [OpenApiOperation(operationId: "GetShopsBySettingTrainer")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "settingId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ShopModel[]))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult GetShopsBySettingTrainer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}/{{settingId}}/setting/trainer")] HttpRequest req,
            Guid gameId,
            Guid trainerId,
            Guid settingId)
        {
            if (!req.VerifyIdentity(trainerId))
            {
                return new UnauthorizedResult();
            }

            var setting = DatabaseUtility.FindSetting(settingId);
            if (setting?.GameId != gameId)
            {
                return new NotFoundResult();
            }

            var shops = DatabaseUtility
                .FindShopsBySetting(setting)
                .Where(shop => shop.IsActive);
            return new OkObjectResult(shops);
        }

        [FunctionName("CreateShop")]
        [OpenApiOperation(operationId: "CreateShop")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ShopModel), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ShopModel))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(MongoWriteError))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateShop(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var shop = await req.GetRequestBody<ShopModel>();
            shop.GameId = gameId;
            shop.ShopId = Guid.NewGuid();
            if (!DatabaseUtility.TryAddShop(shop, out var error))
            {
                return new BadRequestObjectResult(error);
            }
            return new OkObjectResult(shop);
        }

        [FunctionName("UpdateShop")]
        [OpenApiOperation(operationId: "UpdateShop")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "shopId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ShopModel), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ShopModel))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UpdateShop(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{shopId}}/update")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid shopId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            var shop = DatabaseUtility.FindShopById(shopId, gameId);
            var data = await req.GetRequestBody<ShopModel>();
            shop.Name = data.Name;
            shop.Inventory = data.Inventory;
            shop.IsActive = data.IsActive;
            if (!DatabaseUtility.UpdateShop(shop))
            {
                return new BadRequestResult();
            }
            return new OkObjectResult(shop);
        }

        [FunctionName("PurchaseFromShop")]
        [OpenApiOperation(operationId: "PurchaseFromShop")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "trainerId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "shopId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ItemModel[]), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(RecipientModel))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PurchaseFromShop(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{trainerId}}/{{shopId}}/purchase")] HttpRequest req,
            Guid gameId,
            Guid trainerId,
            Guid shopId)
        {
            if (!req.VerifyIdentity(trainerId))
            {
                return new UnauthorizedResult();
            }

            var shop = DatabaseUtility.FindShopById(shopId, gameId);
            if (shop?.IsActive != true)
            {
                return new NotFoundObjectResult(shopId);
            }

            var trainer = DatabaseUtility.FindTrainerById(trainerId, gameId);
            var (validWares, cost) = await GetValidWares(req, shop);

            if (cost > trainer.Money)
            {
                return new BadRequestObjectResult("not enough money");
            }
            foreach (var ware in validWares.Where(ware => shop.Inventory[ware.Name].Quantity != -1))
            {
                shop.Inventory[ware.Name].Quantity -= ware.Amount;
            }

            if (!DatabaseUtility.UpdateShop(shop))
            {
                return new BadRequestResult();
            }

            trainer.Money -= cost;
            var logs = AddItemsToTrainer(trainer, validWares);
            DatabaseUtility.UpdateGameLogs(DatabaseUtility.FindGame(gameId), logs.ToArray());
            return new OkObjectResult(new RecipientModel
            {
                Trainer = new Objects.PublicTrainer(trainer),
                Shop = shop,
            });
        }

        [FunctionName("DeleteShop")]
        [OpenApiOperation(operationId: "DeleteShop")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "shopId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ItemModel[]), Required = true)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult DeleteShop(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}/{{shopId}}")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId,
            Guid shopId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            if (!DatabaseUtility.DeleteShop(shopId, gameId))
            {
                return new BadRequestResult();
            }

            return new OkResult();
        }

        [FunctionName("DeleteShopsByGameId")]
        [OpenApiOperation(operationId: "DeleteShopsByGameId")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "gameMasterId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ItemModel[]), Required = true)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized)]
        public IActionResult DeleteShopsByGameId(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = $"{RoutePrefix}/{{gameId}}/{{gameMasterId}}")] HttpRequest req,
            Guid gameId,
            Guid gameMasterId)
        {
            if (!req.IsUserGM(gameMasterId, gameId))
            {
                return new UnauthorizedResult();
            }

            if (!DatabaseUtility.DeleteShopByGameId(gameId))
            {
                return new BadRequestResult();
            }

            return new OkResult();
        }

        private async Task<(IEnumerable<ItemModel> ValidWares, int Cost)> GetValidWares(HttpRequest request, ShopModel shop)
        {
            var groceries = await request.GetRequestBody<IEnumerable<ItemModel>>();
            var validWares = groceries.Where(item => CheckWare(item, shop));
            var cost = validWares.Aggregate(0, (currentCost, nextWare) =>
            {
                return currentCost + shop.Inventory[nextWare.Name].Cost * nextWare.Amount;
            });

            return (validWares, cost);
        }

        private static bool CheckWare(ItemModel item, ShopModel shop)
        {
            var ware = shop.Inventory.FirstOrDefault(ware => item.Name == ware.Key && item.Type == ware.Value.Type);
            return !(ware.Key == null || item.Amount <= 0 || (ware.Value.Quantity != -1 && item.Amount > ware.Value.Quantity));
        }
    }
}
