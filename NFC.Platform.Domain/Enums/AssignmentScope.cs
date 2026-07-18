using System.Text.Json.Serialization;

namespace NFC.Platform.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AssignmentScope
    {
        [JsonPropertyName("all_employees")]
        AllEmployees = 1,

        [JsonPropertyName("specific_employees")]
        SpecificEmployees = 2,

        [JsonPropertyName("individual")]
        Individual = 3
    }
}
