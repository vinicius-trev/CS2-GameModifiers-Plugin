
using System.Text.Json.Serialization;

using CounterStrikeSharp.API.Core;

namespace GameModifiers;

public class GameModifiersConfig : BasePluginConfig
{
    [JsonPropertyName("RandomRoundsEnabledByDefault")] public bool RandomRoundsEnabledByDefault { get; set; } = false;
    [JsonPropertyName("ShowCentreMsg")] public bool ShowCentreMsg { get; set; } = true;
    [JsonPropertyName("CanRepeat")] public bool CanRepeat { get; set; } = false;
    [JsonPropertyName("MinRandomRounds")] public int MinRandomRounds { get; set; } = 1;
    [JsonPropertyName("MaxRandomRounds")] public int MaxRandomRounds { get; set; } = 1;
    [JsonPropertyName("DisabledModifiers")] public string[] DisabledModifiers { get; set; } = new string[] { };
}