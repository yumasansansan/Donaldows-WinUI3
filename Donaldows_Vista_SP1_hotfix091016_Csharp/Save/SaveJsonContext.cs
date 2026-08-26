using System.Text.Json.Serialization;
using Donaldows_Vista_SP1_hotfix091016_Csharp.State;

namespace Donaldows_Vista_SP1_hotfix091016_Csharp.Save
{
    // Source-generated (de)serialization for SaveData, avoiding the reflection-based
    // JsonSerializer overloads that are incompatible with PublishAot trimming.
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(SaveData))]
    internal sealed partial class SaveJsonContext : JsonSerializerContext
    {
    }
}
