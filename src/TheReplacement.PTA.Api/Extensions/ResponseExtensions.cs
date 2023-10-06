using Microsoft.AspNetCore.Http;
using System;
using TheReplacement.PTA.Api.Services;

namespace TheReplacement.PTA.Api.Extensions
{
    internal static class ResponseExtensions
    {
        public static void AssignAuthAndToken(
            this HttpResponse response,
            Guid trainerId)
        {
            var token = EncryptionUtility.GenerateToken();
            DatabaseUtility.UpdateUserActivityToken
            (
                trainerId,
                token
            );

            response.Headers.Append("pta-session-auth", GetSessionAuth());
            response.Headers.Append("pta-activity-token", token);
        }

        public static void RefreshToken(
            this HttpResponse response,
            Guid id)
        {
            var updatedToken = EncryptionUtility.GenerateToken();
            DatabaseUtility.UpdateUserActivityToken(id, updatedToken);
            response.Headers.Append("pta-activity-token", updatedToken);
        }

        private static string GetSessionAuth()
        {
            return EncryptionUtility.HashSecret(RequestExtensions.AuthKey);
        }
    }
}
