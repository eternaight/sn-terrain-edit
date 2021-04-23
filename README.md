# sn-terrain-edit
WIP Application to create and edit Subnautica's terrain.

Project files for Unity 2019.4.9f1.
App can be downloaded [here.](https://drive.google.com/file/d/1V7-iobYlalyzOUS6Rf_Td2TjVUHcHY6S/view?usp=sharing)

Requires Subnautica, experimental branch recommended.

## What it can do:
Edit Subnautica's voxel terrain, and its materials.

## What it cant't do (yet):
- Edit more than 1 batch at a time
- Edit batch objects and loot
- Edit biome map

## Usage
1. Make sure you have Subnautica installed.
2. Paste the path to your 'Subnautica' folder into the 'Settings' window, and apply.
3. Open the 'Materials' window and load materials. This can take quite a long time (~5 minutes on Stable SN, ~30 seconds on Exp)
4. Load the batch by index using the 'Load' window. Batch index can be learned in-game using F1 menu.
5. Edit the batch using the 'Brush' window.
6. When you're finished, save the batch using the 'Export' window.
When you next load the batch in the game, you should see your changes.

## Open source code used:
Asset Studio Scripts - by Perfare [(Asset Studio)](https://github.com/Perfare/AssetStudio/tree/master/AssetStudio)

#### Texture2DDecoder module:
- [Ishotihadus/mikunyan](https://github.com/Ishotihadus/mikunyan)
- [BinomialLLC/crunch](https://github.com/BinomialLLC/crunch)
- [Unity-Technologies/crunch](https://github.com/Unity-Technologies/crunch/tree/unity)
