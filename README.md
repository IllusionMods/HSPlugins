# HSPlugins
A collection of useful studio plugins.

This is a fork of [HSPlugins](https://bitbucket.org/Joan6694/hsplugins/src/master/) by [Joan6694](https://joan6694.bitbucket.io/). Main reason for the fork is that Joan disappeared and the plugins needed to be ported to KKS.

Main changes in the fork:
- Added KKS support to some of the plugins
- Removed HS and IPA support to simplify the codebase
- Fixed compiling with VisualStudio, no longer require external scripts or dlls
- Code refactoring, created separate projects for each game
- Use BepInEx config instead of custom xml config files
- Changed default user content folders to be inside UserData
- Some new features like studio toolbar buttons

## How to use
1. Install latest BepInEx5 and BepisPlugins.
2. Download the latest release zip for your game from the releases page.
3. Pick the plugins that you want to install from the release zip and extract them into your game directory (the .dll files should end up inside the BepInEx\plugins folder).

To see hotkeys used to use the plugins check plugin settings or the config files in BepInEx\config.
