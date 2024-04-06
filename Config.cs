using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace CS2Multi1v1;

public class CS2Multi1v1Config : BasePluginConfig
{
    [JsonPropertyName("ConfigVersion")] public override int Version { get; set; } = 0;

    [JsonPropertyName("Maps")] public string[] Maps { get; set; } = ["am_plain2_gg"];
}
