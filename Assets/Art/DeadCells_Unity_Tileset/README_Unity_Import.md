# Dead Cells-inspired Unity Tileset

## Contents
- `Tileset_Atlas_Transparent.png`: full transparent master sheet.
- `Terrain_Tiles/`: stone and moss terrain blocks/platform pieces.
- `Architecture/`: walls, columns, arches, doors, windows and structural pieces.
- `Props_Decoration/`: lights, chests, barrels, crates, banners, chains, statues, crystals and foliage.
- `Large_Structures_Backgrounds/`: larger bridge/arch/background elements.
- `Sprite_Index.csv`: per-sprite file and source coordinates.

## Important note about the source
The original level image was a flattened scene, not a layered PSD or a native tileset. Hidden/occluded pixels cannot be recovered from it. This Unity pack therefore uses a clean, separated reconstruction guided by that scene's visual style and composition, rather than claiming to be pixel-perfect cutouts of every object in the flattened screenshot. Some connected decorative groups remain combined as one sprite.

## Unity import
1. Copy this folder into `Assets/Art/DeadCellsTileset/`.
2. Select the PNGs in Unity. Set **Texture Type = Sprite (2D and UI)**, **Sprite Mode = Single** for the individual files, **Alpha Is Transparency = On**, **Compression = None**.
3. Use the same **Pixels Per Unit** for every file. A starting value is **88 PPU** (the common stone tile is about 88 px wide); adjust once in the scene if your player scale requires it.
4. For pixel-crisp rendering, use **Filter Mode = Point (no filter)**. If you prefer softer painted edges, use Bilinear consistently.
5. Create a Grid + Tilemap. Create Tile assets by dragging terrain sprites into the Tile Palette. Use only solid terrain pieces on the collision Tilemap.
6. Add `TilemapCollider2D` to the collision Tilemap. Optionally add `Rigidbody2D` (Static) and `CompositeCollider2D` to combine collision outlines.
7. Props, doors, lamps, banners and background silhouettes should be placed as normal SpriteRenderers, not painted into the solid Tilemap.

## Sorting recommendation
- Background silhouettes: sorting layer `Background`.
- Architecture/terrain: sorting layer `World`.
- Props and interactive doors: sorting layer `Foreground` / `Interactables`.
- Player and enemies: sorting layer `Characters`.

## Limitation
This is a separated sprite pack, not a ready-made Unity `.unitypackage` or preconfigured Tile Palette. Unity creates its own `.meta` and Tile assets when imported/created in the Editor.

## Optional one-click Tile asset creation
The `Editor/DeadCellsTilesetSetup.cs` helper can generate Unity `Tile` assets from all PNGs in `Terrain_Tiles`:
1. Copy the complete `DeadCells_Unity_Tileset` folder to `Assets/Art/DeadCells_Unity_Tileset/`.
2. Let Unity finish importing and compile the editor script.
3. Choose **Tools → Dead Cells Tileset → Generate Terrain Tile Assets**.
4. The generated `.asset` files appear in `Generated_TileAssets/` and can be added to a Tile Palette.

The helper uses 88 Pixels Per Unit and Grid colliders by default. Change decorative/floating pieces to **Collider Type = None** in the generated Tile asset Inspector if they should not block movement. If you place the folder in a different location, edit `TilesetRoot` near the top of the script.
