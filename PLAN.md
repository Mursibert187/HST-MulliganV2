# HST-MulliganV2 – Bauplan

Ziel: Nachbau von Mulligan G-V2 mit gleichem Layout, gleichem Verhalten und gleichen Features
(siehe [`docs/SPEC.md`](docs/SPEC.md)) als **Plugin für Hearthstone Deck Tracker (Windows)**.

## Leitplanken

- **Eigene Daten, keine fremden Bezahl-Daten.** Keine Aufrufe der HSReplay-API, keine Trial-Tricks,
  kein Scraping von HSReplay oder Firestone. Das Overlay ist nachbaubar. Die Zahlen sind das
  eigentliche Produkt von HSReplay und müssen aus eigener Quelle kommen (Phase 5).
- **Kein Code aus HDT kopieren.** HDT steht unter "All Rights Reserved".
  HSTracker ist MIT und darf als Vorlage dienen (Lizenzhinweis in `THIRD_PARTY.md`).
- Das Plugin **nutzt** HDT zur Laufzeit (Log-Parsing, Deck-Erkennung, HearthMirror-Speicherlesen,
  Overlay-Fenster). Das ist der Zweck der Plugin-Schnittstelle.

## Architektur

```
src/
  MulliganCore/      netstandard2.0 – Modelle, Justification-Matching, Statistik-Engine, Farbformel
                     (plattformunabhängig, Unit-Tests laufen überall)
  MulliganCore.Tests/
  MulliganPlugin/    net472 WPF – IPlugin, HDT-Anbindung, Overlay, Lobby, Settings
assets/              going_first.png, going_second.png, mulligan-gv2-elements.png, Chunkfive
.github/workflows/   Windows-Build → Plugin-DLL als Download-Artefakt
```

Die UI bekommt immer ein `MulliganV2Data` (gleiches Format wie die HSReplay-Antwort). Woher es
kommt, entscheidet allein die Engine. So bleibt die UI 1:1, egal wie gut die Daten sind.

---

## Phase 0 – Fundament
1. Solution anlegen (Struktur oben), `.gitignore`, `THIRD_PARTY.md`.
2. GitHub-Actions-Workflow auf `windows-latest`: baut `MulliganPlugin.dll` und lädt sie als
   Artefakt hoch. Die HDT-Referenz-DLLs (`HearthstoneDeckTracker.exe`, `HearthMirror.dll`,
   `HearthDb.dll`) kommen aus einer HDT-Installation (`%LocalAppData%\HearthstoneDeckTracker\app-*`)
   bzw. werden im CI aus einem HDT-Release gezogen.
3. Core-Tests laufen im CI mit.

**Fertig wenn:** Push → grüner Build → DLL downloadbar.

## Phase 1 – Plugin-Skelett und Spielzustand
1. `IPlugin`-Klasse (Name, Beschreibung, Menü, OnLoad/OnUnload/OnUpdate).
2. Mulligan-Phase erkennen (`Core.Game.GameEntity` STEP), Modus/Format prüfen (§1 SPEC).
3. Kontext einsammeln: aktives Deck (DbfIds, Deckstring), angebotene Karten mit Position,
   Gegnerklasse, Initiative (FIRST/COIN), Rang.
4. Live-Auswahl abonnieren: `Watchers.MulliganStateWatcher.Change`
   (liefert `MulliganCards[ZonePosition, CardId, State]` und `WaitingForUserInput`).
5. Debug-Log-Fenster: zeigt alle Werte live an.

**Fertig wenn:** Beim Mulligan im Log stehen Deck, Hand, Gegner, Initiative und jeder Klick auf
Keep/Replace erscheint sofort.

## Phase 2 – Overlay 1:1
1. Farbformel + Positionsberechnung in `MulliganCore` (mit Tests gegen HSTracker-Werte).
2. `LinearGauge`: Verlauf, Replace/Keep, Keep-Range-Band, Marker, Blitz-Animation, Schatten.
3. Tip-Zeile: runde Icons, Pfeile, Tooltips mit Chunkfive.
4. Fehlerbox, Warndreieck, Abdunklung bei ersetzter Karte, "Not Available"-Text.
5. Container: 988 px zentriert, `UniformGrid`, Skalierung `Höhe/1080`, Fade-in.
6. Auf `Core.OverlayCanvas` einhängen, Maus-Hit-Test nur auf Icons/Gauge (Klicks gehen sonst
   an Hearthstone durch).
7. Test mit **Fake-Daten** (fest kodierte `MulliganV2Data`) – Layout vor Datenarbeit fertig.

**Fertig wenn:** Screenshot unseres Overlays neben einem Original-Screenshot zeigt keine
sichtbaren Unterschiede (Vergleich mit den 5 regulären Gratis-Trials pro Woche).

## Phase 3 – Live-Verhalten
1. Justification-Matching (Multiset, Duplikate) in `MulliganCore` + Tests.
2. Contextual vs. Dynamic Keep Rate, Tooltip-Texte.
3. Hover-Sperre, Einfrieren nach Bestätigen.
4. Nach dem Mulligan: Neuberechnung für die finale Hand, Ausblend-Timing.
5. Texte EN/DE aus HSTracker übernehmen.

**Fertig wenn:** Marker springt beim Klicken auf X genau wie im Original, inkl. Blitz.

## Phase 4 – Lobby, Onboarding, Settings
1. Deckbox-Badges über `Watchers.DeckPickerWatcher` (Farben/Labels laut SPEC §4).
2. Pre-Lobby-Widget: gleiches Layout. Statt Trial/Abo zeigt es die eigene Datenlage
   ("N Spiele mit diesem Deck").
3. Onboarding-Notification + Modal mit den drei Erklärboxen und der Grafik.
4. Settings-Fenster: G-V2 an/aus, Pre-Lobby an/aus.

## Phase 5 – Daten-Engine (der eigentliche Aufwand)
1. **Aufzeichnen**: Jede Partie speichert offered/kept/final Hand, Deck, Gegnerklasse,
   Initiative, Rang, Ergebnis, Züge (lokal, SQLite oder JSON-Lines).
2. **Nachholen**: vorhandene lokale HDT-Replays importieren (Format prüfen) → sofort
   Startdaten aus der eigenen Historie.
3. **Modell**: pro Deck-Archetyp logistische Regression
   `logit P(keep c) = a[c] + Σ b[c,o]·kept(o) + g[c,Gegner] + d[c,Initiative] + e[c]·2.Kopie`,
   mit Bayes-Shrinkage auf einen Prior. Daraus entstehen:
   - Justifications = Vorhersage für jede Teilmenge der anderen Karten,
   - Tips = die stärksten Effekte `b`, `g`, `d`, `e` (Pfeile = Stärke in Stufen),
   - Card-Status = aus Stichprobengröße (`VALID` / `LOW_DATA` / `NO_DATA` / `…_INVALID_NEIGHBORS`),
   - Deck-Status = Abdeckung (`SUPPORTED` / `PARTIAL` / `NONE`).
4. **Kaltstart-Prior**: Heuristik aus Kartendaten (Kosten, Typ, Keywords, Kurve, Initiative),
   damit von Tag 1 sinnvolle Werte kommen.
5. **Optional – Daten teilen**: kleines Backend, in das Freunde ihre Aufzeichnungen hochladen.
   Mehr Spieler = bessere Zahlen. So sind auch HSReplay und Firestone entstanden.
6. Deck-Archetyp-Erkennung: Karten-Überlappung (Jaccard) statt exaktem Deckstring.

**Realistisch:** Die Werte werden anfangs nicht dieselben sein wie bei HSReplay (dort Millionen
Spiele). Mit eigener Historie + Prior sind sie brauchbar, mit geteilten Daten werden sie besser.

## Phase 6 – Feinschliff und Release
1. Fehlerfälle: Reconnect, Spieler ohne Deck, unbekannte Karten, Brawl.
2. Performance: Berechnung nur bei Zustandsänderung, keine Allokation pro 16-ms-Tick.
3. Installationsanleitung (DLL nach `%AppData%\HearthstoneDeckTracker\Plugins`).
4. Release-Workflow: getaggter Build → GitHub Release mit ZIP.

---

## Arbeitsweise
- Code und Core-Tests entstehen in der Cloud-Session. Die DLL baut GitHub Actions auf Windows.
- **Testen im Spiel macht der Nutzer**: DLL installieren, Mulligan spielen, Screenshot und
  HDT-Log zurückschicken. Danach wird nachgebessert.
- Jede Phase = eigener Commit-Block, Stand in dieser Datei abhaken.

## Status
- [x] Phase 0 – Fundament (Solution, CI-Build, Kern-Tests; Laden in HDT noch im Spiel zu bestätigen)
- [ ] Phase 1 – Plugin-Skelett und Spielzustand (Live-State wird schon geloggt)
- [ ] Phase 2 – Overlay 1:1 (Farbformel und Balken-Geometrie fertig und getestet)
- [ ] Phase 3 – Live-Verhalten (Kernlogik fertig und getestet: Keep-Rate-Matching, Gate, Timing)
- [ ] Phase 4 – Lobby, Onboarding, Settings
- [ ] Phase 5 – Daten-Engine
- [ ] Phase 6 – Feinschliff und Release
