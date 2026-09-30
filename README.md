# HST-MulliganV2

Plugin für [Hearthstone Deck Tracker](https://hsdecktracker.net), das das Mulligan-Overlay
im Stil von Mulligan G-V2 nachbaut – mit eigenen Daten statt HSReplay-Daten.

- Plan und Fortschritt: [`PLAN.md`](PLAN.md)
- Soll-Zustand (Layout, Logik, Texte): [`docs/SPEC.md`](docs/SPEC.md)

## Installieren

1. Unter **Actions → Build** den letzten grünen Lauf öffnen und das Artefakt `HstMulliganV2` herunterladen.
2. `HstMulliganV2.dll` nach `%AppData%\HearthstoneDeckTracker\Plugins` kopieren.
3. HDT neu starten und unter *Optionen → Tracker → Plugins* aktivieren.

## Bauen

```powershell
./scripts/fetch-hdt.ps1                        # HDT-Assemblies nach hdt-libs/
dotnet build src/MulliganPlugin -c Release     # -> src/MulliganPlugin/bin/Release/net472/HstMulliganV2.dll
dotnet test src/MulliganCore.Tests             # Kernlogik, läuft auch unter Linux/macOS
```

Statt `fetch-hdt.ps1` geht auch die eigene Installation:
`-p:HdtDir="$env:LOCALAPPDATA\HearthstoneDeckTracker\app-<version>"`.
