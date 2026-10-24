# Sailing Out art pipeline

The art target is a top-down pixel-art sea scene for a 16:9 PC window:

- 16 px grid and 16 pixels per Unity unit
- Point filtering, no mipmaps, uncompressed textures
- Transparent sprites with a shared cold ocean palette
- Three variants per category
- Buoy and supply: two-frame loop
- Reef: static
- Seabird: four-frame flap loop
- Floating debris: three static non-blocking variants
- Water: opaque tileable PNG; foam: transparent tileable PNG overlay

Original pixel artwork is included, as chosen by the user when AI image generation was unavailable. `AI_PROMPTS.md` provides the optional future concept workflow; the current release does not claim AI-generated art.

The package editor builder turns named source sprites into Prefabs, animations, collider roles, `SpawnableDefinition` assets, a staged `GenerationProfile`, and a demo scene. AI-generated concepts must be manually pixel-cleaned and checked for transparent edges before they are added here.
