﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CS2Multi1v1.Models;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;

namespace CS2Multi1v1;

public class CS2Multi1v1 : BasePlugin, IPluginConfig<CS2Multi1v1Config>
{

    public static CS2Multi1v1 Instance { get; private set; } = new();
    public override string ModuleName => "CS2Multi1v1";
    public override string ModuleVersion => "beta_1.0.0";
    public override string ModuleAuthor => "rockCityMath, dollan";
    public override string ModuleDescription => "Supports multiple automatic 1v1 arenas with rank climbing.";

    private bool _aimMapLoaded = false;

    internal static ILogger? _logger;
    private Queue<ArenaPlayer> _waitingArenaPlayers = new Queue<ArenaPlayer>();
    private List<Arena> _rankedArenas = new List<Arena>();

    public CS2Multi1v1Config Config { get; set; } = new();

    public override void Load(bool hotReload)
    {
        Instance = this;
        _logger = Logger;

        _logger.LogInformation("Loaded CS2Multi1v1!");

        RegisterEvents();

        if (hotReload)
        {
            _logger.LogInformation("Detected hot reload...");
            // requeue/calc spawns??
        }
    }

    public void OnConfigParsed(CS2Multi1v1Config config)
    {
        Config = config;
    }

    private void RegisterEvents()
    {
        RegisterEventHandler<EventPlayerActivate>(OnPlayerActivate);
        RegisterEventHandler<EventPlayerConnect>(OnPlayerConnect);
        RegisterEventHandler<EventRoundPrestart>(OnRoundPrestart);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventSwitchTeam>(OnSwitchTeam, HookMode.Pre);
        RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam, HookMode.Pre);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    }

    // ----------------------------- SERVER RELATED GAME EVENT HOOKS -------------------------------------//

    public HookResult OnPlayerActivate(EventPlayerActivate @event, GameEventInfo info)
    {
        CCSPlayerController playerController = @event.Userid;
        _logger?.LogInformation($"Player Activated: {playerController.Connected.ToString()}");



        if (!playerController.IsValid) return HookResult.Continue;
        if (playerController.Connected != PlayerConnectedState.PlayerConnected) return HookResult.Continue;
        if (!playerController.Pawn.IsValid) return HookResult.Continue;
        if (_rankedArenas.FirstOrDefault(x => x?._player1?.PlayerController == playerController) != null) return HookResult.Continue;
        if (_rankedArenas.FirstOrDefault(x => x?._player2?.PlayerController == playerController) != null) return HookResult.Continue;
        playerController.ChangeTeam(CsTeam.Spectator);
        ArenaPlayer arenaPlayer = new ArenaPlayer(playerController);
        _waitingArenaPlayers.Enqueue(arenaPlayer);
        // If all players are in the waiting queue, end the round
        if (Helper.GetValidPlayers().Count == _waitingArenaPlayers.Count)
        {
            Helper.EndRound();
        }


        _logger?.LogInformation($"Player {arenaPlayer.PlayerController.PlayerName} added to waiting queue.");
        arenaPlayer.PrintToChat($"{ChatColors.Gold}You have been added to the waiting queue.");
        arenaPlayer.PrintToChat($"{ChatColors.Gold}Type {ChatColors.LightRed}!help{ChatColors.Gold} in chat to see info.");

        return HookResult.Continue;
    }

    public HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo info)
    {
        SetupArenasIfNeeded();
        return HookResult.Continue;
    }

    // ---------------------- ROUND RELATED GAME EVENT HOOKS -----------------------------//

    public HookResult OnRoundPrestart(EventRoundPrestart @event, GameEventInfo info)
    {
        Queue<ArenaPlayer> arenaWinners = new();
        Queue<ArenaPlayer> arenaLosers = new();

        _logger?.LogInformation("Prestart triggered");

        // Get winner and loser from each arena and add to appropriate queue | Going from arena 1 down
        foreach (Arena arena in _rankedArenas)
        {
            ArenaResult arenaResult = arena.GetArenaResult();

            // If there was a winner, handle normally
            if (arenaResult.ResultType == ArenaResultType.Win)
            {
                arenaWinners.Enqueue(arenaResult.Winner!);
                arenaLosers.Enqueue(arenaResult.Loser!);
            }
            // If someone had no opponent, consider it a win so they move up
            else if (arenaResult.ResultType == ArenaResultType.NoOpponent)
            {
                arenaWinners.Enqueue(arenaResult.Winner!);
            }
        }

        Queue<ArenaPlayer> rankedPlayers = new Queue<ArenaPlayer>(); // Holds players going from top rank to lowest

        // Top 2 winners should be in arena 1
        if (arenaWinners.Count > 1)
        {
            var p1 = arenaWinners.Dequeue();
            var p2 = arenaWinners.Dequeue();
            rankedPlayers.Enqueue(p1);
            rankedPlayers.Enqueue(p2);
        }

        // Middle arenas have loser from higher arena, and winner from lower arena
        while (arenaWinners.Count > 0)
        {
            var player = arenaWinners.Dequeue();
            rankedPlayers.Enqueue(player);

            // If there arent any losers to add, just keep adding winners
            if (arenaLosers.Count > 0)
            {
                player = arenaLosers.Dequeue();
                rankedPlayers.Enqueue(player);
            }
        }

        // If there are any remaining losers, add them
        while (arenaLosers.Count > 0)
        {
            rankedPlayers.Enqueue(arenaLosers.Dequeue());
        }

        // Add waiting users to the back of the queue
        while (_waitingArenaPlayers.Count > 0)
        {
            ArenaPlayer arenaPlayer = _waitingArenaPlayers.Dequeue();
            rankedPlayers.Enqueue(arenaPlayer);
        }

        _logger?.LogInformation("Ranked Queue: ");
        foreach (ArenaPlayer p in rankedPlayers)
        {
            _logger?.LogInformation(p.PlayerController.PlayerName);
        }

        // Shuffle arenas (gives player varied spawnpoints)
        Helper.Shuffle(_rankedArenas);

        int currentArenaIndex = 0;
        while (currentArenaIndex < _rankedArenas.Count)
        {
            // If 2+ players in ranked queue, add both to current arena
            if (rankedPlayers.Count > 1)
            {
                ArenaPlayer player1 = rankedPlayers.Dequeue();
                ArenaPlayer player2 = rankedPlayers.Dequeue();
                _rankedArenas[currentArenaIndex].AddPlayers(player1, player2, currentArenaIndex + 1);
                currentArenaIndex += 1;
            }
            // If 1 player in ranked queue, add them to the current arena with no opponent
            else if (rankedPlayers.Count == 1)
            {
                ArenaPlayer player1 = rankedPlayers.Dequeue();
                _rankedArenas[currentArenaIndex].AddPlayers(player1, null, currentArenaIndex + 1);
                currentArenaIndex += 1;
            }
            // If no more players in ranked queue, set the arena to have no players
            else
            {
                _rankedArenas[currentArenaIndex++].AddPlayers(null, null, currentArenaIndex + 1);
            }
        }

        return HookResult.Continue;
    }

    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        foreach (Arena arena in _rankedArenas) arena.OnRoundEnd();
        return HookResult.Continue;
    }

    // ---------------------- PLAYER RELATED GAME EVENT HOOKS -----------------------------//

    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        info.DontBroadcast = true;
        return HookResult.Continue;
    }

    public HookResult OnSwitchTeam(EventSwitchTeam @event, GameEventInfo info)
    {
        info.DontBroadcast = true;
        return HookResult.Continue;
    }

    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        foreach (Arena arena in _rankedArenas) arena.OnPlayerDeath(@event.Userid);
        return HookResult.Continue;
    }

    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        foreach (Arena arena in _rankedArenas) arena.OnPlayerSpawn(@event.Userid);
        return HookResult.Continue;
    }

    // ------------------------------ COMMANDS ------------------------ //

    // General user information in long form
    [ConsoleCommand("css_help", "Help")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnHelp(CCSPlayerController player, CommandInfo commandInfo)
    {
        PrintToChatCustom(player, "----------------- CS2 Multi 1v1 ----------");
        PrintToChatCustom(player, "1. You will begin in the bottom arena.");
        PrintToChatCustom(player, "2. A win promotes you an arena.");
        PrintToChatCustom(player, "3. A loss demotes you an arena.");
        PrintToChatCustom(player, "4. Whoever has the most kills at the end of the round wins.");
        PrintToChatCustom(player, "5. Whoever got the last kill wins in the event of a tie.");
        PrintToChatCustom(player, "--- Round types are random.");
        PrintToChatCustom(player, "--- Challenging players is not supported yet.");
        PrintToChatCustom(player, "--- Selecting guns is not supported yet.");
    }

    // Show current players in waiting queue
    [RequiresPermissions("#css/admin")]
    [ConsoleCommand("css_sq", "Show Players in Waiting Queue")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnShowQueue(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid) return;

        player.PrintToChat("Current Queue: ");
        foreach (ArenaPlayer p in _waitingArenaPlayers)
        {
            player.PrintToChat(p.PlayerController.PlayerName);
        }
    }

    // Remove all players from arenas, reset waiting queue, then add all currently connected players to waiting queue
    [RequiresPermissions("#css/admin")]
    [ConsoleCommand("css_rq", "Requeue All Current Players")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRequeue(CCSPlayerController? player, CommandInfo commandInfo)
    {
        SetupArenasIfNeeded();
        _waitingArenaPlayers.Clear();
        foreach (Arena arena in _rankedArenas) arena.AddPlayers(null, null);

        foreach (CCSPlayerController playerController in Utilities.GetPlayers())
        {
            if (playerController.IsValid && playerController.Connected == PlayerConnectedState.PlayerConnected)
            {
                _waitingArenaPlayers.Enqueue(new ArenaPlayer(playerController));
            }
        }

        Server.PrintToChatAll("Requeued");
    }

    // Console logs information for all arenas with 1+ players
    [RequiresPermissions("#css/admin")]
    [ConsoleCommand("css_arenainfo", "Console Log Arena Info")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnArenaInfo(CCSPlayerController? player, CommandInfo commandInfo)
    {
        foreach (Arena arena in _rankedArenas)
        {
            arena.LogCurrentInfo();
        }
    }

    // Remove all existing arenas, get map spawns, create new arenas
    [RequiresPermissions("#css/admin")]
    [ConsoleCommand("resetarenas", "Re-fetch Map Spawns and Fully Re-instantiate all Areanas")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnAsize(CCSPlayerController? player, CommandInfo commandInfo)
    {
        List<Tuple<SpawnPoint, SpawnPoint>> arenasSpawns = getArenasSpawns();
        foreach (Arena arena in _rankedArenas) arena.AddPlayers(null, null); // Neccesary to prevent memory leaks?
        _rankedArenas.Clear();

        int count = 0;
        foreach (Tuple<SpawnPoint, SpawnPoint> arenaSpawns in arenasSpawns)
        {
            Arena arena = new Arena(_logger!, arenaSpawns);
            _rankedArenas.Add(arena);
            count++;
        }

        _aimMapLoaded = true;

        if (player != null && player.IsValid)
        {
            player.PrintToChat($"Successfully instantiated {_rankedArenas.Count} arenas.");
        }
    }

    [RequiresPermissions("#css/admin")]
    [ConsoleCommand("css_endround", "End the Current Round")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnEndRound(CCSPlayerController? player, CommandInfo commandInfo)
    {
        Helper.EndRound();
    }

    // ---------------------------------- UTIL ---------------------------------------//

    public void SetupArenasIfNeeded()
    {
        // If the map is in the config and the aim map has not been loaded yet, load it
        if (Config.Maps.Contains(Server.MapName) && !_aimMapLoaded)
        {
            List<Tuple<SpawnPoint, SpawnPoint>> arenasSpawns = getArenasSpawns();
            _rankedArenas.Clear();

            int count = 0;
            foreach (Tuple<SpawnPoint, SpawnPoint> arenaSpawns in arenasSpawns)
            {
                Arena arena = _logger != null ? new Arena(_logger, arenaSpawns) : throw new ArgumentNullException(nameof(_logger));
                _rankedArenas.Add(arena);
                count++;
            }
            _aimMapLoaded = true;
        }
    }

    private List<Tuple<SpawnPoint, SpawnPoint>> getArenasSpawns()
    {
        // Get all ct and t SpawnPoints on map
        var ctSpawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_counterterrorist").ToList();
        var tSpawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>("info_player_terrorist").ToList();

        var spawnPairs = new List<Tuple<SpawnPoint, SpawnPoint>>();

        // Assumes map has one ct and one t spawn per individual arena | Iterate thru ct spawns, finding the closest t spawn to pair it with | O(n)^2 but oh well for now
        foreach (var ctSpawn in ctSpawns)
        {
            SpawnPoint? closestTSpawn = null;
            float closestDistance = float.MaxValue;

            foreach (var tSpawn in tSpawns)
            {
                var ctVec = ctSpawn.CBodyComponent!.SceneNode!.AbsOrigin;
                var tVec = tSpawn.CBodyComponent!.SceneNode!.AbsOrigin;

                float distance = Helper.DistanceTo(ctVec, tVec);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTSpawn = tSpawn;
                }
            }

            if (closestTSpawn != null)
            {
                spawnPairs.Add(new Tuple<SpawnPoint, SpawnPoint>(ctSpawn, closestTSpawn));
            }
        }
        return spawnPairs;
    }






    public void PrintToChatCustom(CCSPlayerController playerController, string text)
    {
        playerController.PrintToChat($" {ChatColors.Olive}  CS2Multi1v1 \u2022 {ChatColors.Default}{text}");
    }
}
