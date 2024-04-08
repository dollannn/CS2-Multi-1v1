using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Microsoft.Extensions.Logging;

namespace CS2Multi1v1
{
    public class Helper
    {
        internal static CS2Multi1v1Config? Config { get; set; }
        public static void EndRound()
        {
            CS2Multi1v1.Instance.Logger.LogInformation("Ending round");
            var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
            gameRules.TerminateRound(3.0f, RoundEndReason.CTsWin);

        }
        public static List<CCSPlayerController> GetValidPlayers()
        {
            return Utilities.GetPlayers().FindAll(p => p != null && p.IsValid && p.SteamID.ToString().Length == 17 && !string.IsNullOrEmpty(p.IpAddress) && p.Connected == PlayerConnectedState.PlayerConnected && !p.IsBot && !p.IsHLTV);
        }
    }

}