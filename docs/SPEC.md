# HST Mulligan V2 - Feature Parity Spec

This document is the source of truth for what the plugin must do at v2.0.

## 1. Behavioral parity

| # | Behavior                                                    | Owner module                     |
|---|-------------------------------------------------------------|----------------------------------|
| 1 | Detect mulligan phase begin/end                             | `Live/MulliganStateGate`         |
| 2 | Read 3 or 4 mulligan candidate entities (coin included)     | `Bindings/HearthstoneMirrorAdapter` |
| 3 | Track user's per-card keep/mulligan toggles in real time    | `Live/LiveMulliganState`         |
| 4 | Detect opponent class from opponent portrait                | `Bindings/HearthstoneMirrorAdapter` |
| 5 | Detect format (Standard / Wild / Twist)                     | `Bindings/GameEventBridge`       |
| 6 | Detect current deck code when a tracked deck is active      | `Bindings/GameEventBridge`       |
| 7 | Load keep-rate dataset for (format, opponent, deck?)        | `Data/IMulliganDataSource`       |
| 8 | Compute per-card keep-rate + confidence + decision grade    | `Live/KeepRateCalculator`, `Live/ConfidenceScorer` |
| 9 | Render badge over each mulligan card slot                   | `MulliganPlugin/Overlay/CardBadge` |
| 10 | Toggle details panel with per-opponent breakdown           | `MulliganPlugin/Overlay/DetailsPanel` |
| 11 | Fall back gracefully when data is missing                  | `Data/CachingDataSource` + core |
| 12 | Persist user settings between sessions                     | `Config/PluginSettings`          |

## 2. Data shape

Both `FirestoneDataSource` and `HsReplayDataSource` normalize into
`MulliganDataset`:

```jsonc
{
  "format": "standard",           // "standard" | "wild" | "twist"
  "rankBracket": "diamond-legend", // free-form; source-specific
  "generatedAt": "2026-09-15T00:00:00Z",
  "cards": [
    {
      "dbfId": 12345,
      "overall": { "kept": 1200, "mulliganed": 400, "keptWinrate": 0.53 },
      "byOpponent": {
        "MAGE":    { "kept": 300, "mulliganed": 90,  "keptWinrate": 0.55 },
        "WARRIOR": { "kept": 150, "mulliganed": 210, "keptWinrate": 0.42 }
      },
      "byDeck": {
        "AAECAf0EBu...=": { "kept": 80, "mulliganed": 20, "keptWinrate": 0.58 }
      }
    }
  ]
}
```

Any source that produces this envelope will slot into the pipeline.

## 3. Keep-rate math

For each candidate card:

```
kept       = sample.kept
mulliganed = sample.mulliganed
n          = kept + mulliganed
keepRate   = kept / n            when n > 0, else NaN
lift       = keptWinrate - baseWinrate   (baseWinrate is dataset-average)
```

Decision grade is a bucketing of `keepRate` weighted by `lift`:

| Grade         | keepRate   | notes                                     |
|---------------|-----------|--------------------------------------------|
| StrongKeep    | >= 0.75    | or keepRate >= 0.65 AND lift >= +0.03      |
| Keep          | >= 0.55    | or lift >= +0.015                          |
| Neutral       | 0.45-0.55  |                                            |
| Toss          | < 0.45     | or lift <= -0.015                          |
| StrongToss    | < 0.25     | or keepRate < 0.35 AND lift <= -0.03       |

## 4. Confidence

```
confidence = clamp01(log10(n + 1) / log10(N_full + 1))
```

`N_full` defaults to 500 samples (configurable). Confidence < 0.25 dims the
badge and adds a hatched border; a card that appears in zero samples is
rendered as an outline-only ring with a "?" glyph.

## 5. Overlay layout

- Badge is anchored to the top-right of each mulligan card slot, offset by
  `(-24, +24)` in a 1080p reference frame; scales linearly with the game
  window height.
- Ring outer diameter = 22% of card width.
- Percent label uses the numeric font shipped with the plugin (Inter Tight,
  see `MulliganPlugin/Assets/fonts/`).
- Details panel is anchored bottom-center of the mulligan area, collapsible.

Everything above is functional layout, not any third party's proprietary art.

## 6. Non-goals for v2.0

- Post-mulligan game analytics (that's a separate plugin scope).
- Deck-recommendation. This plugin only advises on the four cards on screen.
- Any modification of the underlying game process. Read-only + WPF overlay.
