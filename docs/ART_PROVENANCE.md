# Art and Font Provenance

## CrewSeated.png

The four-direction seated sailor image was generated with OpenAI imagegen for this prototype on 2026-10-22 and supplied by the user for implementation. It depicts a generic adult sailor in an orange life vest, teal sleeves and navy trousers. No web-scraped game character, real-person photograph, company logo or third-party game asset is included.

The supplied transparent 1254x1254 PNG is imported unchanged. Runtime Sprite pivots and a common pixels-per-unit scale align each seated pelvis to the vessel seat; the source poses are not warped. The scene uses four static directional images, not a complete animation set.

## SailingOutUI.ttf

This approximately 35 KB font is a static weight-400 subset of Noto Sans SC from the official Google Fonts repository, renamed **Sailing Out UI**. It contains the current player UI glyphs, ASCII keys/numbers and required punctuation. The original copyright notice and SIL Open Font License 1.1 are included under `Assets/Resources/Voyage/FontLicense/` together with the source URL and modification notice.

The original reserved font name is not used as the derivative's primary family name. No macOS system font is copied or redistributed. Font changes remain under the accompanying OFL license; adding UI characters requires regenerating the subset.

## Procedural artwork

The vessel, sail, seat, wake and foam are small deterministic pixel drawings in `VoyagePixelArt.cs` authored for this prototype. The sea grid and session infrastructure are existing repository components.
