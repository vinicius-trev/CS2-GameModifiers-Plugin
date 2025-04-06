
using System.Text.Json.Serialization;

using CounterStrikeSharp.API.Core;

namespace GameModifiers;

public class GameModifiersConfig : BasePluginConfig
{
    [JsonPropertyName("ShowCentreMsg")] public bool ShowCentreMsg { get; set; } = true;
    [JsonPropertyName("CanRepeat")] public bool CanRepeat { get; set; } = false;
    [JsonPropertyName("MinRandomModifiersPerRound")] public int MinRandomModifiersPerRound { get; set; } = 1;
    [JsonPropertyName("MaxRandomModifiersPerRound")] public int MaxRandomModifiersPerRound { get; set; } = 1;
    [JsonPropertyName("DisabledModifiers")] public string[] DisabledModifiers { get; set; } = new string[] { };
}
