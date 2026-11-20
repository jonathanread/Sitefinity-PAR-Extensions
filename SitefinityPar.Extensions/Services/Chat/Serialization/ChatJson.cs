using System.Text.Json;

namespace SitefinityWebApp.Services.Chat.Serialization
{
    internal static class ChatJson
    {
        public static readonly JsonSerializerOptions ResponseOptions = CreateResponseOptions();

        private static JsonSerializerOptions CreateResponseOptions()
        {
            var options = new JsonSerializerOptions();
            // Parsers validate fields they use; unrelated malformed fields must remain ignorable.
            options.Converters.Add(new OptionalStringConverter());
            options.Converters.Add(new OptionalBooleanConverter());
            return options;
        }
    }
}
