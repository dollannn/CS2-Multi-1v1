<br />
<div align="center">
  <h3 align="center">CS2-Multi-1v1 [WIP]</h3>

  <p align="center">
    A spiritual successor to <a href="https://github.com/splewis/csgo-multi-1v1/tree/master">splewis' csgo-multi-1v1</a>
    <br />
    This plugin allows any number of players to be automatically placed into separate arenas to fight 1v1 in a ladder-style system where the winner moves up an arena rank, and the loser moves down. Player weapons are determined by the randomly generated "round type" and they will both use those same weapons for the entire round. Once the round starts, players will fight each other and when one kills the other, that player will receive a point and both players will respawn with full health and weapons, when the round ends, the player with the most points will win.
    <br />
    <br />
    <br />
 <strong>Note: This plugin is still under development and currently contains minimal features.</strong>
 </p>
</div>

<!-- GETTING STARTED -->

## Admin Commands

- !sq - Show players currently in the waiting queue.
- !rq - Empty all arenas and assign all players on the server to the waiting queue.
- !arenainfo - Console log a list of arenas with players and that arenas roundtype.
- !resetarenas - Remove all existing arenas, get current map spawns, then create new arenas.
  - Useful when using maps besides aim_redline_fp.
  - After arenas are reset, players will be to be requeued (!rq)
- !endround - Manually ends the round.

<!-- ROADMAP -->
## Roadmap

- [x] Player Matching
- [x] Arena Climbing
- [x] Random Round Types
- [ ] Round Type Preferences
- [ ] Weapon Preferences
- [ ] Multiple Map Support
- [ ] Persist Player Stats

<!-- CONTRIBUTING -->
## Contributing

Any contributions you make are **greatly appreciated**. Incomplete items from the roadmap are the most requested features and would be most helpful, along with bugfixes, but feel free to implement any other features you think would be fun.

If you would like your code to be reviewed and merged into this repo:

1. Fork the Project
2. Create your Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

<strong>Please keep PRs limited in scope to the feature you are implementing or bug you are fixing (within reason).</strong>

<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE.txt` for more information.

<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

- splewis - Creator of the original mutli-1v1 plugin.
- roflmuffin - Creator of the CounterStrikeSharp library, I would not have made this in SourcePawn :)
- CounterStrikeSharp Community
