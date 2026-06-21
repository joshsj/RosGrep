using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RosGrep.Cli.Definitions;

internal static class Constants
{
    public static class Formatting
    {
        public static readonly JsonSerializerOptions PrettyJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // Default encoder escapes <, >, & (HTML-safety) which mangles generic
            // signatures like Send<TRequest>; relaxed encoder emits them literally.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter() },
        };
    }
}