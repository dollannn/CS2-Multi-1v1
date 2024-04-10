using CounterStrikeSharp.API.Modules.Utils;
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
        // In-place shuffle
        public static void Shuffle<T>(IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        public static float DistanceTo(Vector a, Vector b)
        {
            return (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) + Math.Pow(a.Z - b.Z, 2));
        }
        public static bool IsPlayerValid(CCSPlayerController? p)
        {
            return p != null && p.IsValid && (p.SteamID.ToString().Length == 17 || (p.SteamID == 0 && p.IsBot)) && p.Connected == PlayerConnectedState.PlayerConnected && !p.IsHLTV;
        }
    }

}