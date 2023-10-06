namespace TheReplacement.PTA.Api.Services.Exceptions
{
    /// <summary>
    /// Represents an error that occurs due to a missing json property
    /// </summary>
    public class MissingJsonPropertyException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MissingJsonPropertyException"/>
        /// </summary>
        /// <param name="property">The json property</param>
        public MissingJsonPropertyException(string property)
            : base($"Missing Json property {property}")
        {
            PropertyName = property;
        }

        /// <summary>
        /// The property's name
        /// </summary>
        public string PropertyName { get; }
    }
}
