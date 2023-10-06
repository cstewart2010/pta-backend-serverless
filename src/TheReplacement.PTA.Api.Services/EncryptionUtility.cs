namespace TheReplacement.PTA.Api.Services
{
    /// <summary>
    /// Provides a collection of methods for encrypting/decrypting tokens and secrets
    /// </summary>
    public static class EncryptionUtility
    {
        /// <summary>
        /// Generates a time-based access token for checking against idle users
        /// </summary>
        public static string GenerateToken()
        {
            byte[] time = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
            return Convert.ToBase64String(time);
        }

        /// <summary>
        /// Encrypts a password for storage
        /// </summary>
        /// <param name="secret">The secret to hash</param>
        public static string HashSecret(string secret)
        {
            return string.IsNullOrWhiteSpace(secret)
                ? null
                : BCrypt.Net.BCrypt.HashPassword(secret);
        }

        /// <summary>
        /// Returns true if the token falls withing the time span
        /// </summary>
        /// <param name="token"></param>
        public static bool ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            byte[] data = Convert.FromBase64String(token);
            if (data.Length != 8)
            {
                return false;
            }

            DateTime tokenTime = DateTime.FromBinary(BitConverter.ToInt64(data));
            var now = DateTime.UtcNow;
            return tokenTime >= now.AddHours(-1) && tokenTime <= now;
        }

        /// <summary>
        /// Verifys that the secret matches the encryption
        /// </summary>
        /// <param name="secret">The secret to validate</param>
        /// <param name="hashedSecret">The hashed form of the correct secret</param>
        /// <exception cref="ArgumentNullException" />
        public static bool VerifySecret(
            string secret,
            string hashedSecret)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(secret, hashedSecret);
            }
            catch
            {
                return false;
            }
        }
    }
}
