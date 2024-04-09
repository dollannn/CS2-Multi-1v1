using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace CS2Multi1v1.Models;

// A single 1v1 arena, holds 2 players, tracks their round stats, and handles much of the functionality relating to playing the round

internal class Arena
{
    private Tuple<SpawnPoint, SpawnPoint> _spawns;
    private int _rank;
    private RoundType _roundType;
    private ILogger _logger;

    private bool _isArenaActive;

    public ArenaPlayer? _player1;
    private int _player1Kills;
    private bool _player1HasLastKill;

    public ArenaPlayer? _player2; // these should be private, use util methods for interation
    private int _player2Kills;

    public Arena(ILogger logger, Tuple<SpawnPoint, SpawnPoint> spawns)
    {
        _spawns = spawns;
        _rank = 0;
        _logger = logger;
        _isArenaActive = false;
        _player1Kills = 0;
        _player1HasLastKill = true;
        _player2Kills = 0;
        _roundType = RoundType.Rifle;
    }

    public void AddPlayers(ArenaPlayer? player1, ArenaPlayer? player2, int rank = -1)
    {
        _rank = rank;
        _player1 = player1;
        _player1Kills = 0;
        _player1HasLastKill = true;
        _player2 = player2;
        _player2Kills = 0;

        // TODO: Check player prefs for acceptable roundtypes?
        List<RoundType> roundTypes = new List<RoundType>(){
            RoundType.Rifle,
            RoundType.Smg,
            RoundType.Scout,
            RoundType.Awp,
            RoundType.Pistol,
            RoundType.Deagle
        };

        Random rng = new Random();
        int roundTypeIndex = rng.Next(0, 5);

        _roundType = roundTypes[roundTypeIndex];

        // Set player teams, notify of arena and opponent
        if (isPValid(_player1))
        {
            _logger.LogInformation("Switch p1 team..");
            _player1?.PlayerController.SwitchTeam(CsTeam.Terrorist);

            string opponentName = isPValid(_player2) ? _player2!.PlayerController.PlayerName : "No Opponent";

            _player1?.PrintToChat($"Arena:      {ChatColors.Gold}{_rank}");
            _player1?.PrintToChat($"Round Type: {ChatColors.Gold}{_roundType.Name}");
            _player1?.PrintToChat($"Opponent:   {ChatColors.Gold}{opponentName}");

            _player1!.PlayerController.Clan = $"Arena {_rank} | ";
        }

        if (isPValid(_player2))
        {
            _logger.LogInformation("Switch p2 team..");
            _player2?.PlayerController.SwitchTeam(CsTeam.CounterTerrorist);

            string opponentName = isPValid(_player1) ? _player1!.PlayerController.PlayerName : "No Opponent";

            _player2?.PrintToChat($"Arena:      {ChatColors.Gold}{_rank}");
            _player2?.PrintToChat($"Round Type: {ChatColors.Gold}{_roundType.Name}");
            _player2?.PrintToChat($"Opponent:   {ChatColors.Gold}{opponentName}");

            _player2!.PlayerController.Clan = $"Arena {_rank} |";
        }

        LogCurrentInfo();
    }

    public void OnPlayerSpawn(CCSPlayerController playerController)
    {
        _isArenaActive = anyPValid();

        bool wasPlayer1 = isPValid(_player1) && _player1!.PlayerController == playerController;
        bool wasPlayer2 = isPValid(_player2) && _player2!.PlayerController == playerController;

        // If a player in this arena respawned
        if (wasPlayer1 || wasPlayer2)
        {
            // Randomly assign which player spawns at which of the 2 arena spawns
            SpawnPoint p1Spawn;
            SpawnPoint p2Spawn;
            Random rng = new Random();
            int p1SpawnNum = rng.Next(0, 2);

            if (p1SpawnNum == 1)
            {
                p1Spawn = _spawns.Item1;
                p2Spawn = _spawns.Item2;
            }
            else
            {
                p1Spawn = _spawns.Item2;
                p2Spawn = _spawns.Item1;
            }

            // Prepare the players
            PreparePlayer(_player1, p1Spawn, _roundType);
            PreparePlayer(_player2, p2Spawn, _roundType);
        }
    }

    private void PreparePlayer(ArenaPlayer? player, SpawnPoint spawnPoint, RoundType roundType)
    {
        if (isPValid(player))
        {
            // Get spawnpoint from the arena
            Vector? pos = spawnPoint.AbsOrigin;
            QAngle? angle = spawnPoint.AbsRotation;
            Vector? velocity = new Vector(0, 0, 0);

            // Teleport player there
            if (pos != null && angle != null)
            {
                player?.PlayerController?.Pawn.Value?.Teleport(pos, angle, velocity);
            }

            // Reset weapons and health
            player!.ResetPlayerWeapons(roundType);
            player!.PlayerController!.Pawn!.Value!.Health = 100;
        }
    }

    // Show both players opponent's and their own kills
    private void showPlayersCurrentScore()
    {
        if (isPValid(_player1) && isPValid(_player2))
        {
            _player1?.PrintToChat($"You: {ChatColors.Green}{_player1Kills}{ChatColors.Default} | {_player2?.PlayerController.PlayerName}: {ChatColors.LightRed}{_player2Kills}");
            _player2?.PrintToChat($"You: {ChatColors.Green}{_player2Kills}{ChatColors.Default} | {_player1?.PlayerController.PlayerName}: {ChatColors.LightRed}{_player1Kills}");
        }
    }

    public void OnPlayerDeath(CCSPlayerController playerController)
    {
        bool wasPlayer1 = isPValid(_player1) && _player1!.PlayerController == playerController;
        bool wasPlayer2 = isPValid(_player2) && _player2!.PlayerController == playerController;

        if (wasPlayer2)
        {
            _player1Kills += 1;
            _player1HasLastKill = true;
            showPlayersCurrentScore();
        }

        if (wasPlayer1)
        {
            _player2Kills += 1;
            _player1HasLastKill = false;
            showPlayersCurrentScore();
        }
    }

    public void LogCurrentInfo()
    {
        if (_isArenaActive)
        {
            _logger.LogInformation($"------ ARENA {_rank} -----");
            if (isPValid(_player1)) _logger.LogInformation($"Player1: {_player1?.PlayerController.PlayerName}");
            if (isPValid(_player2)) _logger.LogInformation($"Player2: {_player2?.PlayerController.PlayerName}");
            _logger.LogInformation($"Round Type: {_roundType.Name}");
        }
    }

    public void OnRoundEnd()
    {
        if (isPValid(_player1) && isPValid(_player2))
        {
            string loserMsg = $"{ChatColors.Red}You lost!";
            string winnerMsg = $"{ChatColors.Green}You won!";
            string winnerMessage = _player1Kills > _player2Kills ? winnerMsg : loserMsg;
            string loserMessage = _player1Kills > _player2Kills ? loserMsg : winnerMsg;

            _player1!.PrintToChat(winnerMessage);
            _player2!.PrintToChat(loserMessage);
        }
    }

    public ArenaResult GetArenaResult()
    {
        // If both players valid, use normal logic to determine winner
        if (isPValid(_player1) && isPValid(_player2))
        {
            if (_player1Kills > _player2Kills)
            {
                return new ArenaResult(ArenaResultType.Win, _player1, _player2);
            }
            else if (_player2Kills > _player1Kills)
            {
                return new ArenaResult(ArenaResultType.Win, _player2, _player1);
            }
            else if (_player1HasLastKill)
            {
                return new ArenaResult(ArenaResultType.Win, _player1, _player2);
            }
            else
            {
                return new ArenaResult(ArenaResultType.Win, _player2, _player1);
            }
        }

        // If player1 or player2 was valid, give them the win
        if (_isArenaActive)
        {
            ArenaPlayer? winner = isPValid(_player1) ? _player1 : _player2;
            return new ArenaResult(ArenaResultType.NoOpponent, winner, null);
        }
        // If this point is reached, the arena either started with or now has no players
        return new ArenaResult(ArenaResultType.Empty, null, null);
    }

    private bool isPValid(ArenaPlayer? player)
    {
        return player != null && player.PlayerController.IsValid && player.PlayerController.Connected == PlayerConnectedState.PlayerConnected && player.PlayerController.Pawn.IsValid;
    }

    private bool anyPValid()
    {
        return isPValid(_player1) || isPValid(_player2);
    }
}
