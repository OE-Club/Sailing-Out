# Sailing Out source sprites

Place cleaned, transparent pixel sprites in this folder and run **Sailing Out > Build Art Package and Demo**.

Use this exact naming scheme:

```text
buoy_01_01.png     buoy_01_02.png
reef_01_01.png
seabird_01_01.png  seabird_01_02.png  seabird_01_03.png  seabird_01_04.png
supply_01_01.png  supply_01_02.png
decoration_01_01.png
water_tile.png
foam_tile.png
```

Replace `01` with variants `01` through `03`; buoy has 2 frames, reef and decoration are static, seabird has 4 frames, and supply has 2 frames. All 30 original transparent source frames are included. Water and foam live in `../Textures/`, horizontal delivery sheets in `../SpriteSheets/`. The builder applies 16 PPU, Point filtering, full-rect sprite meshes, no mipmaps, and uncompressed import settings. Preserve `.meta` files when replacing artwork.
