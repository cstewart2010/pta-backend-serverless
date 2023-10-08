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
    public class UserController : BasePtaController
    {
        private const string RoutePrefix = "v1/user";
        protected override MongoCollection Collection => throw new NotImplementedException();

        public UserController(ILogger<UserController> log)
        {
            _logger = log;
        }

        [FunctionName("GetUsername")]
        [OpenApiOperation(operationId: "GetUsername")]
        [OpenApiParameter(name: "userId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string))]
        public IActionResult GetUsername(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{userId}}")] HttpRequest req,
            Guid userId)
        {
            var user = DatabaseUtility.FindUserById(userId);
            if (user == null)
            {
                return new NotFoundObjectResult(userId);
            }

            return new OkObjectResult(user.Username);
        }

        [FunctionName("GetUsers")]
        [OpenApiOperation(operationId: "GetUsers")]
        [OpenApiParameter(name: "adminId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "offset", In = ParameterLocation.Query, Required = true, Type = typeof(int))]
        [OpenApiParameter(name: "limit", In = ParameterLocation.Query, Required = true, Type = typeof(int))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserPageModel))]
        public IActionResult GetUsers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{adminId}}/admin/allUsers")] HttpRequest req,
            Guid adminId)
        {
            if (!(int.TryParse(req.Query["offset"], out var offset) && offset > 0))
            {
                return new BadRequestResult();
            }
            if (!(int.TryParse(req.Query["limit"], out var limit) && limit > 0))
            {
                return new BadRequestResult();
            }

            if (!IsUserAdmin(adminId))
            {
                return new UnauthorizedResult();
            }

            var users = DatabaseUtility.FindUsers();
            var result = new UserPageModel
            {
                Previous = GetPrevious(offset, limit),
                Next = GetNext(offset, limit, users.Count()),
                Users = users.GetSubset(offset, limit).Select(user => new PublicUser(user))
            };
            return new OkObjectResult(result);
        }

        [FunctionName("ForceGetMessage")]
        [OpenApiOperation(operationId: "ForceGetMessage")]
        [OpenApiParameter(name: "adminId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "messageId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserMessageThreadModel))]
        public IActionResult ForceGetMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{adminId}}/{{messageId}}/admin/message")] HttpRequest req,
            Guid adminId,
            Guid messageId)
        {
            if (!IsUserAdmin(adminId))
            {
                return new UnauthorizedResult();
            }

            return new OkObjectResult(DatabaseUtility.FindMessageById(messageId));
        }

        [FunctionName("GetMessage")]
        [OpenApiOperation(operationId: "GetMessage")]
        [OpenApiParameter(name: "userId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "messageId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserMessageThreadModel))]
        public IActionResult GetMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"{RoutePrefix}/{{userId}}/{{messageId}}")] HttpRequest req,
            Guid userId,
            Guid messageId)
        {
            if (!req.VerifyIdentity(userId))
            {
                return new UnauthorizedResult();
            }

            var user = DatabaseUtility.FindUserById(userId);
            if (!user.Messages.Contains(messageId))
            {
                return new ConflictResult();
            }

            return new OkObjectResult(DatabaseUtility.FindMessageById(messageId));
        }

        [FunctionName("CreateNewUser")]
        [OpenApiOperation(operationId: "CreateNewUser")]
        [OpenApiParameter(name: "username", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiParameter(name: "password", In = ParameterLocation.Query, Required = true, Type = typeof(string))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(FoundUserMessage))]
        public IActionResult CreateNewUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = RoutePrefix)] HttpRequest req)
        {
            string username = req.Query["username"];
            string password = req.Query["password"];
            if (username.Length < 6 || password.Length < 6)
            {
                return new BadRequestResult();
            }

            if (DatabaseUtility.FindUserByUsername(username) != null)
            {
                return new ConflictObjectResult(username);
            }

            var user = new UserModel(username, password);
            if (DatabaseUtility.TryAddUser(user, out var error))
            {
                req.HttpContext.Response.AssignAuthAndToken(user.UserId);
                return new OkObjectResult(new FoundUserMessage(user));
            }

            return new BadRequestObjectResult(error);
        }

        [FunctionName("SendMessageAsync")]
        [OpenApiOperation(operationId: "SendMessageAsync")]
        [OpenApiParameter(name: "userId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "recipientId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(MessageContentModel), Required = true)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        public async Task<IActionResult> SendMessageAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = $"{RoutePrefix}/{{userId}}/{{recipientId}}/sendMessage")] HttpRequest req,
            Guid userId,
            Guid recipientId)
        {
            if (!req.VerifyIdentity(userId))
            {
                return new UnauthorizedResult();
            }

            var user = DatabaseUtility.FindUserById(userId);
            var recipient = DatabaseUtility.FindUserById(recipientId);

            if (recipient == null)
            {
                return new NotFoundObjectResult(recipientId);
            }

            var data = await req.GetRequestBody<MessageContentModel>();
            if (string.IsNullOrEmpty(data.MessageContent))
            {
                return new BadRequestResult();
            }

            var error = AddNewThreadToUsers(user, recipient, data.MessageContent);
            if (error != null)
            {
                return new BadRequestObjectResult(error);
            }

            req.HttpContext.Response.RefreshToken(userId);
            return new OkResult();
        }

        [FunctionName("ReplyMessageAsync")]
        [OpenApiOperation(operationId: "ReplyMessageAsync")]
        [OpenApiParameter(name: "userId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "messageId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(MessageContentModel), Required = true)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        public async Task<IActionResult> ReplyMessageAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{userId}}/{{messageId}}/replyMessage")] HttpRequest req,
            Guid userId,
            Guid messageId)
        {
            if (!req.VerifyIdentity(userId))
            {
                return new UnauthorizedResult();
            }

            var user = DatabaseUtility.FindUserById(userId);
            var thread = DatabaseUtility.FindMessageById(messageId);
            if (!user.Messages.Contains(messageId))
            {
                return new ConflictResult();
            }

            var data = await req.GetRequestBody<MessageContentModel>();
            if (string.IsNullOrEmpty(data.MessageContent))
            {
                return new BadRequestResult();
            }

            AddNewReplyToThread(user, thread, data.MessageContent);
            req.HttpContext.Response.RefreshToken(userId);
            return new OkResult();
        }

        [FunctionName("RefreshInGame")]
        [OpenApiOperation(operationId: "RefreshInGame")]
        [OpenApiParameter(name: "gameId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "userId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "isGM", In = ParameterLocation.Query, Required = true, Type = typeof(bool))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AbstractMessage))]
        public IActionResult RefreshInGame(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{gameId}}/{{userId}}/refresh")] HttpRequest req,
            Guid gameId,
            Guid userId)
        {
            if (!bool.TryParse(req.Query["isGM"], out var isGM))
            {
                return new BadRequestResult();
            }

            if (isGM)
            {
                return GetUpdatedGM(req, userId, gameId);
            }

            return GetUpdatedTrainer(req, userId, gameId);
        }

        [FunctionName("LoginAsync")]
        [OpenApiOperation(operationId: "LoginAsync")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UserCredentialModel), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(FoundUserMessage))]
        public async Task<IActionResult> LoginAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/login")] HttpRequest req)
        {
            var (username, password, credentialErrors) = await req.GetUserCredentials();
            if (credentialErrors.Any())
            {
                return new BadRequestObjectResult(credentialErrors);
            }

            if (!IsUserAuthenticated(username, password, out var authError))
            {
                return authError;
            }

            var user = DatabaseUtility.FindUserByUsername(username);
            req.HttpContext.Response.RefreshToken(user.UserId);
            return new OkObjectResult(new FoundUserMessage(user));
        }

        [FunctionName("Logout")]
        [OpenApiOperation(operationId: "Logout")]
        [OpenApiParameter(name: "userId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        public IActionResult Logout(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = $"{RoutePrefix}/{{userId}}/logout")] HttpRequest req,
            Guid userId)
        {
            if (!req.VerifyIdentity(userId))
            {
                return new UnauthorizedResult();
            }

            DatabaseUtility.UpdateUserOnlineStatus(userId, false);
            return new OkResult();
        }

        [FunctionName("DeleteUser")]
        [OpenApiOperation(operationId: "DeleteUser")]
        [OpenApiParameter(name: "userId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        public IActionResult DeleteUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = $"{RoutePrefix}/{{userId}}")] HttpRequest req,
            Guid userId)
        {
            if (!req.VerifyIdentity(userId))
            {
                return new UnauthorizedResult();
            }

            if (IsUserAdmin(userId))
            {
                return new BadRequestResult();
            }

            if (DatabaseUtility.DeleteUser(userId))
            {
                return new OkResult();
            }

            return new BadRequestResult();
        }

        [FunctionName("ForceDeleteUser")]
        [OpenApiOperation(operationId: "ForceDeleteUser")]
        [OpenApiParameter(name: "adminId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiParameter(name: "userId", In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK)]
        public IActionResult ForceDeleteUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = $"{RoutePrefix}/{{adminId}}/{{userId}}/admin")] HttpRequest req,
            Guid adminId,
            Guid userId)
        {
            if (!IsUserAdmin(adminId))
            {
                return new UnauthorizedResult();
            }

            if (IsUserAdmin(userId))
            {
                return new BadRequestResult();
            }

            if (DatabaseUtility.DeleteUser(userId))
            {
                return new OkResult();
            }

            return new BadRequestResult();
        }

        private ActionResult GetUpdatedTrainer(HttpRequest request, Guid userId, Guid gameId)
        {
            if (!request.VerifyIdentity(userId))
            {
                return new UnauthorizedResult();
            }

            request.HttpContext.Response.RefreshToken(userId);
            return new OkObjectResult(new FoundTrainerMessage(userId, gameId));
        }

        private ActionResult GetUpdatedGM(HttpRequest request, Guid userId, Guid gameId)
        {
            if (!request.IsUserGM(userId, gameId))
            {
                return new UnauthorizedResult();
            }

            request.HttpContext.Response.RefreshToken(userId);
            return new OkObjectResult(new GameMasterMessage(userId, gameId));
        }

        private static MongoWriteError AddNewThreadToUsers(UserModel sender, UserModel recipient, string messageContent)
        {
            var message = new UserMessageModel(sender.UserId, messageContent);
            var thread = new UserMessageThreadModel
            {
                MessageId = Guid.NewGuid(),
                Messages = new[] { message }
            };

            if (DatabaseUtility.TryAddThread(thread, out var error))
            {
                sender.Messages = sender.Messages.Append(thread.MessageId);
                recipient.Messages = recipient.Messages.Append(thread.MessageId);
                DatabaseUtility.UpdateUser(sender);
                DatabaseUtility.UpdateUser(recipient);
            }

            return error;
        }

        private static void AddNewReplyToThread(UserModel sender, UserMessageThreadModel thread, string messageContent)
        {
            var message = new UserMessageModel(sender.UserId, messageContent);
            thread.Messages = thread.Messages.Append(message);
            DatabaseUtility.UpdateThread(thread);
        }

        public static bool IsUserAdmin(Guid userId)
        {
            var siteAdmin = DatabaseUtility.FindUserById(userId);
            return Enum.TryParse<UserRoleOnSite>(siteAdmin.SiteRole, out var role) && role == UserRoleOnSite.SiteAdmin;
        }

        private static PageDataModel GetPrevious(int offset, int limit)
        {
            int previousOffset = Math.Max(0, offset - limit);
            int previousLimit = offset - limit < 0 ? offset : limit;
            if (previousOffset == offset)
            {
                return new PageDataModel
                {
                    Offset = offset,
                    Limit = limit
                };
            }

            return new PageDataModel
            {
                Offset = previousOffset,
                Limit = previousLimit
            };
        }

        private static PageDataModel GetNext(int offset, int limit, int count)
        {
            int nextOffset = Math.Min(offset + limit, count - limit);
            if (nextOffset <= offset)
            {
                return new PageDataModel
                {
                    Offset = offset,
                    Limit = limit
                };
            }


            return new PageDataModel
            {
                Offset = nextOffset,
                Limit = limit
            };
        }
    }
}
