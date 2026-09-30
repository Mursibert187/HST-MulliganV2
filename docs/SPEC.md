# Mulligan G-V2 – Referenz-Spezifikation (Soll-Zustand des Clones)

Quelle: Code-Studium von HSTracker (MIT, Stand 24.09.2026) und Hearthstone Deck Tracker
(nur gelesen, "All Rights Reserved" – **kein Code daraus übernehmen**).
Alle Pixelwerte beziehen sich auf eine 1080p-Referenz; skaliert wird mit `Höhe / 1080`.

## 1. Wann das Overlay erscheint

| Bedingung | Wert |
|---|---|
| Format | Standard |
| Modus | Ranked oder Friendly (Tavern Brawl optional, dann als Ranked Standard behandeln) |
| Nicht | Zuschauer, gegen KI, Wild/Casual/Arena/Twist |
| Phase | `STEP == BEGIN_MULLIGAN`, bis die Tausch-Animation durch ist |

## 2. Layout pro Karte (Header über jeder der 3–4 Karten)

- Container: 988 px breit, zentriert, Margin `6,-30,0,0`, Items in `UniformGrid Rows=1`,
  jede Zelle 272 × 555 px, ItemsControl-Margin `-16,0`.
- Header = StackPanel aus zwei Zeilen, jeweils 3 Spalten `23 | 212 | Auto`:
  1. **Tips-Zeile** (45 px hoch): horizontal zentrierte Tip-Icons.
  2. **Gauge-Zeile** (35 px hoch): Linear Gauge oder Fehlerbox, rechts optional Warndreieck.
- Einblenden: Fade + Slide nach unten, 20 px, 0,2 s.

### 2.1 Linear Gauge ("Confidence Bar")
- 212 × 20 px, CornerRadius 4, schwarzer 1-px-Rand, DropShadow (Blur 6, Depth 2, 270°).
- Horizontaler Verlauf: `negative` (0) → `neutral` (0.5) → `positive` (1).
  Farben: `getColorString(MULLIGAN_CONFIDENCE, delta = -10 | 0 | +10, intensity = 75)`, siehe §6.
- Beschriftung links "Replace", rechts "Keep": fett, 12 pt, schwarz, Innenabstand 6 px.
- **Keep Range**: Rechteck 18.5 px hoch, Top 11, Farbe `#66D3D3D3`, von min bis max aller
  Justification-Confidences.
- **Marker**: weißes Dreieck `0,0 10,0 5,8` mit schwarzem Rand, Top 6, Margin -5, plus weiße
  2-px-Linie Y 8→30. Position `x = clamp(confidence * (212 - 10), 0, 202)`.
- **Änderungs-Blitz**: Rechteck zwischen alter und neuer Markerposition (Höhe 20, Top 10),
  Farbe animiert `#80FFFFFF → #00FFFFFF` in 1,5 s, jedes Mal wenn sich die Breite ändert.
- **Ersetzte Karte** (confidence == null, kein Fehler): Marker weg, Overlay-Rechteck
  212 × 21, Radius 4, `#66000000`.

### 2.2 Tip-Icons
- Zelle 40 × 40, Margin 2; Kreis 32 × 32, schwarzer Rand 2 px, Bild als ImageBrush.
- Pfeile oben rechts, vertikal gestapelt: je 14 × 8, Pfad `M0 8 L7 0 L14 8`,
  schwarze Outline 3 px + Farbe 2 px (LimeGreen = hoch, Red = runter, 180° gedreht).
  Anzahl Pfeile = `|arrows|`.
- Bildquelle je Tip-Typ: Kartenporträt (Scale 1.6 um 16,12), Klassen-Icon (Scale 1.2),
  `going_first.png` / `going_second.png` (unskaliert).
- Tooltip (sofort, InitialShowDelay 0), Schrift Chunkfive:
  Titel 13 pt fett, Text 12 pt mit Umbruch, dann zwei Zeilen Base/Adjusted Keep Rate.

### 2.3 Fehler / Warnungen
- `NO_DATA` / `UNKNOWN_CARD` → graue Box (`DarkGray`, 212 × 20.5, Radius 4, Inset-Gradient)
  mit "Insufficient Data" statt Gauge.
- `LOW_DATA`, `LOW_DATA_WITH_INVALID_NEIGHBORS` → gelbes Dreieck 22 × 22 rechts neben dem Gauge,
  Tooltip "Low Data Warning …".
- `VALID_WITH_INVALID_NEIGHBORS` → gleiches Dreieck, Tooltip "…not in combination…".
- Deck-Status `NONE` → nur Text "Mulligan G-V2 Not Available for this Deck", 18 pt,
  zentriert, 270 px von oben.

## 3. Logik

### 3.1 Datenmodell (identisch zur HSReplay-Antwort, damit UI und Engine entkoppelt sind)
```
MulliganV2Data
  general_info.deck_status: SUPPORTED | PARTIAL | NONE
  cards_by_position: { "1".."4": MulliganCard }
MulliganCard
  dbf_id, card_status, justification: { "[dbf,...]": confidence 0..1 }, tips: [MulliganTip]
MulliganTip
  tip_enum, arrows (+/-n), dbf_id?, base_keep_rate?, adjusted_keep_rate?,
  opponent_class?, this_opponent_keep_rate?, other_opponent_keep_rate?,
  this_init_keep_rate?, other_init_keep_rate?
```
Der Schlüssel einer Justification ist die Menge der **anderen behaltenen** Karten
(`"[]"`, `"[118222]"`, `"[118222, 121064]"`), der Wert die Keep-Rate dieser Karte in dem Fall.
Der Eintrag "alle anderen behalten" ist die **Contextual Keep Rate**.

### 3.2 Live-Berechnung (bei jedem Tick des Mulligan-States, ~16 ms)
1. Hovert die Maus gerade über einer Karte (`CARD_PLAYABLE_MOUSE_OVER`) → nichts tun.
2. Ist die Eingabe bestätigt (`WaitingForUserInput` wurde false) → einfrieren.
3. `isKeepingAll` = alle Karten `CARD_SELECTED`.
4. Diese Karte nicht behalten → `confidence = null` (ersetzt).
5. Sonst: Multiset der anderen behaltenen Karten-IDs (Duplikate zählen) bilden und die
   Justification mit exakt gleichem Multiset suchen → `confidence`.

### 3.3 Tooltip des Gauge
- Alles behalten: Titel "Contextual Keep Rate: X%", Text "Top players keep {card} X% of the time
  with this hand, deck, initiative, and opponent class."
- Sonst: Titel "Dynamic Keep Rate: X%", Text "…would keep {card} N% more/less often if the cards
  marked to be replaced (X) were not offered." + Zeile "Contextual Keep Rate: Y%".

### 3.4 Tip-Typen
| Typ | Icon | Titel | Tooltip-Zeilen |
|---|---|---|---|
| KEPT_MORE / KEPT_LESS | Porträt der anderen Karte | "Card Interaction: ±N%" | Base / Adjusted Keep Rate |
| KEPT_MORE/LESS_2ND_COPY | Porträt | "Card Interaction: ±N%" | Base / Adjusted |
| KEPT_MORE/LESS_VS_THIS_OPPONENT | Klassen-Icon | "Opponent Class Interaction: ±N%" | vs. andere Klassen / vs. diese Klasse |
| KEPT_MORE/LESS_GOING_FIRST / _SECOND | going_first / going_second | "Initiative Interaction: ±N%" | first / coin |

`N = round(adjusted*100) - round(base*100)`.

### 3.5 Nach dem Mulligan
- Wurde getauscht: neu berechnen mit `mulligan_state = DONE` und der finalen Hand als
  angebotenen Karten → die Header bewerten jetzt die tatsächliche Hand.
- Ausblenden nach `2375 ms + max(1, getauscht) * 475 ms`, spätestens wenn `STEP > BEGIN_MULLIGAN`
  oder zurück im Menü.

## 4. Lobby
- **Deckbox-Badges**: Status pro Deck auf der sichtbaren Seite.
  | Zustand | Rand | Hintergrund | Label |
  |---|---|---|---|
  | V2 bereit | `#CC00AA00` | `#CC002200` | "Mulligan G-V2 Ready" |
  | Teilweise | `#CCCCAA00` | `#CC373700` | "Partial Mulligan G-V2" |
  | Keine Daten | `#CCE3D000` | `#CC1A1100` | "No Data" (durchgestrichenes Icon) |
  | Lädt | `#CC555555` | `#CC000000` | "Loading..." |
  Label nur beim fokussierten Deck; Padding `18,16,15,0` (mit Runen `18,16,29,0`).
- **Pre-Lobby-Widget**: Logo, Titel "Mulligan Guide", "?"-Button (nur Standard), einklappbar.
- **Onboarding**: einmalige Notification "New: Mulligan G-V2" + "Learn more"; Modal mit drei
  Boxen (Confidence Bar, Keep Range, Card Synergies) und der Grafik `mulligan-gv2-elements.png`.

## 5. Einstellungen
- Mulligan G-V2 aktivieren
- Pre-Lobby-Widget anzeigen
- Onboarding gesehen (intern)

## 6. Farbformel (aus HSTracker, MIT)
```
colorWinrate = 50 + clamp(5 * delta, -50, 50)
severity     = |0.5 - colorWinrate / 100| * 2
scale(x, a, b) = a + (b - a) * x^(1 - intensity/100)
MULLIGAN_CONFIDENCE (HSL):
  positive = (120, 55, 30)   neutral = (50, 80, 45)   negative = (0, 80, 40)
delta > 0 → scale(severity, neutral, positive); delta < 0 → scale(severity, neutral, negative)
```

## 7. Texte
Englisch und Deutsch 1:1 aus `HSTracker/Translations/macOS/Localizable.xcstrings`
(Keys `MulliganGV2_*`, `ConstructedPreLobbyWidget_*`, `ConstructedMulliganGuidePreLobby_*`).
