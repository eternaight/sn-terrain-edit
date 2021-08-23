# sn-terrain-edit
WIP Application to create and edit Subnautica's terrain.

Project files for Unity 2019.4.9f1.
App can be downloaded [here.](https://www.nexusmods.com/subnautica/mods/728?tab=files)

Requires Subnautica, experimental branch recommended.

## What it can do:
Edit Subnautica's voxel terrain, and its materials.

## What it can't do (yet):
- open .optoctreepatches for editing
- edit batch objects and loot
- edit biome map

## usage tutorial
1. Browse to your game folder (SN or BZ) via the Settings window
2. To load textures from the selected game, use the Materials window. 
3. Enter the indexes of batches you would like to load.
Batch index can be learned in-game using F1 menu.
4. Edit the batch using the Brush window. Use Ctrl(command) or Shift for extra convenience.
5. When you're finished, save the batch using the Export window.

## camera controls:
WASD or arrows for FPS movement
space/c for moving up and down respectively
mouse to open various 

## Open source code used:
Asset Studio Scripts - by Perfare [(Asset Studio)](https://github.com/Perfare/AssetStudio/tree/master/AssetStudio)

C# Protocol Buffers module - by mgravell & others [(protobuf-net-core)](https://github.com/protobuf-net/protobuf-net/tree/1bddeafb3e1e68c29b89b67a68ee16f42e059537)

Standalone File Browser - by gokhangokce-infosfer [(Standalone File Browser)](https://github.com/gkngkc/UnityStandaloneFileBrowser)
