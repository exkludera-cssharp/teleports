using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;

public partial class Plugin : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "Teleports";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "exkludera";

    public static Plugin Instance { get; set; } = new();
    private string mapsFolder = "";

    public override void Load(bool hotReload)
    {
        Instance = this;

        mapsFolder = Path.Combine(ModuleDirectory, "maps");
        Directory.CreateDirectory(mapsFolder);

        RegisterEvents();

        foreach (var cmd in Config.MenuCommand.Split(','))
            AddCommand($"css_{cmd}", "Open menu", (player, command) => Menu.Command(player));

        if (hotReload)
        {
            Teleports.savedPath = Path.Combine(mapsFolder, $"{GetMapName()}.json");
            Teleports.Clear();
            Teleports.Spawn();
        }
    }

    public override void Unload(bool hotReload)
    {
        Teleports.Clear();

        UnregisterEvents();

        foreach (var cmd in Config.MenuCommand.Split(','))
            RemoveCommand($"css_{cmd}", (player, command) => Menu.Command(player));
    }

    public Config Config { get; set; } = new Config();
    public void OnConfigParsed(Config config)
    {
        Config = config;
        Config.Prefix = StringExtensions.ReplaceColorTags(config.Prefix);
    }
}