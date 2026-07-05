# Elite Dangerous Tools Hub

A single Windows desktop app that brings together the best third-party tools for **Elite Dangerous** — market data, exploration, ship building, Odyssey, and colonisation — in one sidebar-driven interface. Web tools open in an embedded browser (WebView2), desktop companion apps get one-click install/update/launch, and reference material (like spectral analysis diagrams) gets a dedicated pan-and-zoom viewer.

![.NET](https://img.shields.io/badge/.NET-9.0-blueviolet) ![WPF](https://img.shields.io/badge/UI-WPF-informational) ![Windows](https://img.shields.io/badge/platform-Windows-0078D6)

## Features

- **All-in-one launcher** — every major ED tool, one click away, organised by category in the sidebar.
- **Embedded web view** — web-based tools (Inara, Spansh, Coriolis, etc.) open inside the app via WebView2, with a fallback to open in your default browser.
- **Auto-install & auto-update for desktop tools** — for supported companion apps, the hub checks GitHub Releases for the latest version, and can download and install or update the tool for you.
- **Smart detection** — locates already-installed tools via known paths or the Windows registry, so you're not prompted to reinstall something you already have.
- **Image/diagram viewer** — a dedicated pan/zoom viewer for reference material such as the spectral analysis chart.

## Tool Categories

### Market & Trade
| Tool | Type | Description |
|---|---|---|
| [EDMC (Elite Dangerous Market Connector)](https://github.com/EDCD/EDMarketConnector) | Desktop | Automatic market data submission. Tracks trading, materials, and location. |
| [Inara](https://inara.cz/) | Web | Commander profiles, squadron management, markets, and community features. |
| [ED Tools](https://edtools.cc/) | Web | Station finder, trade routes, material traders, and engineering tools. |

### Route & Exploration
| Tool | Type | Description |
|---|---|---|
| [Spansh](https://spansh.co.uk/) | Web | Neutron star plotting, fleet carrier routing, trade routes, and galaxy utilities. |
| [ED Astro](https://edastro.com/) | Web | Astronomical data, region maps, codex, and exploration statistics. |
| [Galactic Exploration Catalogue (GEC)](https://edastro.com/gec) | Web | Community catalogue of notable exploration discoveries and points of interest. |
| [ED Discovery](https://github.com/EDDiscovery/EDDiscovery) | Desktop | System maps, star classes, travel history, and 3D galaxy map. |
| [ED Observatory](https://observatory.xjph.net/) ([repo](https://github.com/Xjph/ObservatoryCore)) | Desktop | Monitors your journal for exploration-worthy bodies and valuable planets. |

### Ship Building
| Tool | Type | Description |
|---|---|---|
| [Coriolis](https://coriolis.io/) | Web | Ship outfitting simulator. Compare builds, optimise, and share loadouts. |
| [EDCoPilot](https://www.razzafrag.com/) | Desktop | Voice-activated co-pilot assistant. Navigation, ship status, and EDDB integration. Requires a valid licence key. |

### Odyssey & Surface
| Tool | Type | Description |
|---|---|---|
| [SRV Survey](https://github.com/njthomson/SrvSurvey) | Desktop | Surface mapping, Guardian site tools, biological survey tracker, and settlement maps. |
| [EDOdyssey Material Helper](https://github.com/jixxed/ed-odyssey-materials-helper) | Desktop | Track Odyssey materials, plan engineer upgrades, and find optimal farming locations. |
| [Odyssey Map Guide (OMG)](https://elitedangereuse.fr/outils/quizengine/omg_1.1.html) | Web | Interactive maps and guides for Odyssey settlements, missions, and activities. |

### Colonisation & Guides
| Tool | Type | Description |
|---|---|---|
| [Raven Colonial](https://ravencolonial.com/) | Web | Colonisation planning, system construction tracking, and commodity management. |
| [Élite Dangereuse](https://elitedangereuse.fr/) | Web | French Elite Dangerous community site with guides, tools, and resources. |
| Spectral Analysis Diagram | Image | Qohen Leth's Filtered Spectral Analysis Diagram — reference chart for identifying planet types via spectral analysis, with pan/zoom viewer. |

## Installation

Download `EDHub-Setup.exe` from the [latest release](https://github.com/mitamat/Elite-Dangerous-Tools-Hub/releases/latest) and run it. It's a standard installer — Start Menu shortcut, optional Desktop icon, and a normal uninstall entry in Windows Settings. The .NET runtime is bundled in, so there's nothing else to install first.

## Requirements

- Windows 10/11 (64-bit)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (already present on virtually all Windows 10/11 installs)

## Building from Source

```
git clone https://github.com/mitamat/Elite-Dangerous-Tools-Hub.git
cd Elite-Dangerous-Tools-Hub/EDHub
dotnet build
dotnet run
```

To build the installer yourself, you'll need [Inno Setup 6](https://jrsoftware.org/isinfo.php):

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o ..\publish-sc
cd ..\installer
iscc EDHub.iss
```

## Disclaimer

This project is a launcher/aggregator and is not affiliated with Frontier Developments. All linked tools are the work of their respective authors — see each tool's own repository or site for licensing and support. "Elite Dangerous" is a trademark of Frontier Developments plc.
