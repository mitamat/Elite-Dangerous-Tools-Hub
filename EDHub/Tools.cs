using System.Collections.Generic;

namespace EDHub;

public enum ToolType { Web, Desktop, Image }

public record Tool(
    string Name,
    string FullName,
    string Icon,
    ToolType Type,
    string Desc,
    string? Url = null,
    string? ExePath = null,
    string? Github = null,
    string? InstallNote = null,
    string? ImageResource = null,
    string? GitHubRepo = null,
    string? RegistryHint = null
);

public static class Tools
{
    public static readonly Dictionary<string, Tool> All = new()
    {
        ["edmc"] = new("EDMC", "Elite Dangerous Market Connector", "📡", ToolType.Desktop,
            "Automatic market data submission. Tracks trading, materials, and location.",
            ExePath: @"C:\Program Files (x86)\EDMarketConnector\EDMarketConnector.exe",
            Github: "https://github.com/EDCD/EDMarketConnector",
            InstallNote: "Required for market data tools.",
            GitHubRepo: "EDCD/EDMarketConnector",
            RegistryHint: "Elite Dangerous Market Connector"),

        ["inara"] = new("Inara", "Inara", "🌐", ToolType.Web,
            "Commander profiles, squadron management, markets, and community features.",
            Url: "https://inara.cz/"),

        ["edtools"] = new("ED Tools", "ED Tools", "🔧", ToolType.Web,
            "Station finder, trade routes, material traders, and engineering tools.",
            Url: "https://edtools.cc/"),

        ["spansh"] = new("Spansh", "Spansh", "🗺️", ToolType.Web,
            "Neutron star plotting, fleet carrier routing, trade routes, and galaxy utilities.",
            Url: "https://spansh.co.uk/"),

        ["edastro"] = new("ED Astro", "ED Astro", "🔭", ToolType.Web,
            "Astronomical data, region maps, codex, and exploration statistics.",
            Url: "https://edastro.com/"),

        ["gec"] = new("GEC", "Galactic Exploration Catalogue", "📚", ToolType.Web,
            "Community catalogue of notable exploration discoveries and points of interest.",
            Url: "https://edastro.com/gec"),

        ["eddiscovery"] = new("ED Discovery", "ED Discovery", "🚀", ToolType.Desktop,
            "System maps, star classes, travel history, and 3D galaxy map.",
            ExePath: @"C:\Program Files\EDDiscovery\EDDiscovery.exe",
            Github: "https://github.com/EDDiscovery/EDDiscovery",
            InstallNote: "Installs to Program Files.",
            GitHubRepo: "EDDiscovery/EDDiscovery",
            RegistryHint: "EDDiscovery"),

        ["edobservatory"] = new("ED Observatory", "ED Observatory", "🔬", ToolType.Desktop,
            "Monitors journal for exploration-worthy bodies and valuable planets.",
            ExePath: @"C:\Program Files\Elite Observatory\ObservatoryCore.exe",
            Github: "https://observatory.xjph.net/",
            InstallNote: "Installs via setup executable.",
            GitHubRepo: "Xjph/ObservatoryCore",
            RegistryHint: "Elite Observatory"),

        ["coriolis"] = new("Coriolis", "Coriolis Ship Builder", "🛸", ToolType.Web,
            "Ship outfitting simulator. Compare builds, optimise, and share loadouts.",
            Url: "https://coriolis.io/"),

        ["edcopilot"] = new("EDCoPilot", "EDCoPilot", "🤖", ToolType.Desktop,
            "Voice-activated co-pilot assistant. Navigation, ship status, and EDDB integration.",
            ExePath: @"C:\Program Files\EDCoPilot\EDCoPilot.exe",
            Github: "https://www.razzafrag.com/",
            InstallNote: "Installs via MSI. Requires a valid licence key.",
            GitHubRepo: "Razzafrag/EDCoPilot-Installer",
            RegistryHint: "EDCoPilot"),

        // SrvSurvey ships as a zip — extract target is %LOCALAPPDATA%\SrvSurvey
        ["srvsurvey"] = new("SRV Survey", "SRV Survey", "🌍", ToolType.Desktop,
            "Surface mapping, Guardian site tools, biological survey tracker, and settlement maps.",
            ExePath: @"%LOCALAPPDATA%\SrvSurvey\SrvSurvey.exe",
            Github: "https://github.com/njthomson/SrvSurvey",
            InstallNote: "Extracted to your AppData folder.",
            GitHubRepo: "njthomson/SrvSurvey"),

        ["edmaterialhelper"] = new("Material Helper", "EDOdyssey Material Helper", "⚗️", ToolType.Desktop,
            "Track Odyssey materials, plan engineer upgrades, and find optimal farming locations.",
            ExePath: @"C:\Program Files\EDOdysseyMaterialHelper\EDOdysseyMaterialHelper.exe",
            Github: "https://github.com/jixxed/ed-odyssey-materials-helper",
            InstallNote: "Installs via MSI.",
            GitHubRepo: "jixxed/ed-odyssey-materials-helper",
            RegistryHint: "Odyssey Materials Helper"),

        ["omg"] = new("Odyssey Map Guide", "Odyssey Map Guide (OMG)", "🗺️", ToolType.Web,
            "Interactive maps and guides for Odyssey settlements, missions, and activities.",
            Url: "https://elitedangereuse.fr/outils/quizengine/omg_1.1.html"),

        ["ravencolonial"] = new("Raven Colonial", "Raven Colonial", "🏗️", ToolType.Web,
            "Colonisation planning, system construction tracking, and commodity management.",
            Url: "https://ravencolonial.com/"),

        ["elitedangereuse"] = new("Élite Dangereuse", "Élite Dangereuse", "🇫🇷", ToolType.Web,
            "French ED community site with guides, tools, and resources.",
            Url: "https://elitedangereuse.fr/"),

        ["spectral"] = new("Spectral Analysis", "Qohen Leth's Filtered Spectral Analysis Diagram", "🌈", ToolType.Image,
            "Reference diagram for identifying planet types via spectral analysis. Scroll to pan, Ctrl+Wheel to zoom.",
            ImageResource: "pack://application:,,,/Assets/spectral_analysis.png"),
    };
}
