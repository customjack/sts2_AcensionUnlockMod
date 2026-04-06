# AscensionUnlockMod

Adds an **Ascension** button to the main menu so you can set each character's ascension unlock level without having to reach it through normal play.

**Features:**
- Set ascension unlock level (0–20) per character from the main menu
- Changes take effect immediately — no run required
- Uses ModManagerSettings for persistent settings storage

## Dependencies

- Requires [ModManagerSettings](https://github.com/customjack/sts2_ModManagerSettings)

Install and enable ModManagerSettings before installing this mod.

## Install

1. Install [ModManagerSettings](https://github.com/customjack/sts2_ModManagerSettings) first.
2. Download the latest release zip from the [Releases](../../releases) page.
3. Close Slay the Spire 2.
4. In Steam, right-click `Slay the Spire 2` -> `Properties` -> `Installed Files` -> `Browse`.
5. Create a `mods` folder in the game directory if it does not exist.
6. Extract the zip and drag the `AscensionUnlockMod` folder into `mods`.
7. Confirm these files are present in `mods/AscensionUnlockMod`:
   - `AscensionUnlockMod.dll`
   - `AscensionUnlockMod.pck`
8. Launch Slay the Spire 2. If prompted to enable mods, accept and relaunch.
9. In-game, go to `Settings` -> `General` -> `Mods` and enable both `ModManagerSettings` and `AscensionUnlockMod`.

## Usage

From the main menu, click the **Settings** button next to AscensionUnlockMod in the mod list (via ModManagerSettings), or navigate to the ascension settings directly. Set the unlock level for each character and click Apply.

## Developer Notes

**Requirements:** .NET SDK, Godot 4 export templates, WSL or Linux shell.

**Setup:**
1. Copy `.env.example` to `.env`.
2. Set `STS2_INSTALL_DIR` to your game install path.

**Build and install:**
```bash
./scripts/bash/build_and_stage.sh
./scripts/bash/make_pck.sh
./scripts/bash/install_to_game.sh
```

## License

MIT — see [LICENSE](LICENSE).
