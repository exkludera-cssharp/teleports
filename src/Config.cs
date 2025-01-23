using CounterStrikeSharp.API.Core;

public class Config : BasePluginConfig
{
    public string Prefix { get; set; } = "{purple}[Teleports]{default}";
    public string MenuCommand { get; set; } = "teleports,teleportsmenu,teleportmenu";
    public string Permission { get; set; } = "@css/root";
    public string Sound { get; set; } = "sounds/player/geiger3.vsnd";
    public string EntryModel { get; set; } = "models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl";
    public string EntryColor { get; set; } = "0 255 0 200";
    public string ExitModel { get; set; } = "models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl";
    public string ExitColor { get; set; } = "255 0 0 200";
    public bool BothWays { get; set; } = false;
    public bool ForceAngles { get; set; } = false;
    public bool DisableShadows { get; set; } = true;
}