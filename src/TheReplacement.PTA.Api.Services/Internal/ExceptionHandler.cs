namespace TheReplacement.PTA.Api.Services.Internal
{
    internal static class ExceptionHandler
    {
        public static ArgumentException IsNullOrEmpty(string argumentName)
        {
            return new ArgumentException("String value was null or empty", argumentName);
        }

        public static ArgumentNullException ArgumentNull(string argumentName)
        {
            return new ArgumentNullException(argumentName);
        }
    }
}
