using Newtonsoft.Json.Linq;

namespace TheReplacement.PTA.Api.Services.Exceptions
{
    /// <summary>
    /// Represents an error that occurs while casting a json property to a specific type
    /// </summary>
    public class InvalidJsonPropertyException : Exception
    {
        /// <summary>
        /// Initializes a new instances of <see cref="InvalidJsonPropertyException"/> for a json property with an invalid value
        /// </summary>
        /// <param name="property">The json property</param>
        /// <param name="expectedValues">One of the expected values for the json property</param>
        public InvalidJsonPropertyException(
            JProperty property,
            Array expectedValues)
            : base($"Invalid value for Json property {property.Name}: {property.Value}.\n" +
                  $"Expected: {string.Join(',', expectedValues)}")
        {
            PropertyName = property.Name;
            PropertyValue = property.Value;
        }

        /// <summary>
        /// Initializes a new instances of <see cref="InvalidJsonPropertyException"/> for a json property with of invalid type
        /// </summary>
        /// <param name="property">The json property</param>
        /// <param name="expectedType">The expected <see cref="Type"/> of the json property</param>
        public InvalidJsonPropertyException(
            JProperty property,
            Type expectedType)
            : base($"Invalid value for Json property {property.Name}: {property.Value}.\n" +
                  $"Expected typeof({expectedType.Name})")
        {
            PropertyName = property.Name;
            PropertyValue = property.Value;
        }


        /// <summary>
        /// The property's name
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// The property's value
        /// </summary>
        public object PropertyValue { get; }
    }
}
