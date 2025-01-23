using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

public partial class Plugin : BasePlugin, IPluginConfig<Config>
{
    public void RegisterEvents()
    {
        RegisterListener<OnMapStart>(OnMapStart);
        RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);

        RegisterEventHandler<EventRoundStart>(EventRoundStart);

        HookEntityOutput("trigger_multiple", "OnStartTouch", OnStartTouch, HookMode.Pre);
    }

    public void UnregisterEvents()
    {
        RemoveListener<OnMapStart>(OnMapStart);
        RemoveListener<OnServerPrecacheResources>(OnServerPrecacheResources);

        DeregisterEventHandler<EventRoundStart>(EventRoundStart);

        UnhookEntityOutput("trigger_multiple", "OnStartTouch", OnStartTouch, HookMode.Pre);
    }

    public Dictionary<CCSPlayerController, List<TeleportPair>> playerCooldowns = new Dictionary<CCSPlayerController, List<TeleportPair>>();

    HookResult OnStartTouch(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
    {
        if (activator.DesignerName != "player") return HookResult.Continue;

        var pawn = activator.As<CCSPlayerPawn>();

        if (!pawn.IsValid) return HookResult.Continue;
        if (!pawn.Controller.IsValid || pawn.Controller.Value is null) return HookResult.Continue;

        var player = pawn.Controller.Value.As<CCSPlayerController>();

        if (!playerCooldowns.ContainsKey(player))
            playerCooldowns.Add(player, new List<TeleportPair>());

        if (player.IsBot) return HookResult.Continue;

        if (Teleports.Triggers.TryGetValue(caller, out CEntityInstance? teleport))
        {
            var pair = Teleports.teleportPairs.FirstOrDefault(pair => pair.Entry.Entity == teleport || pair.Exit.Entity == teleport);

            if (pair != null && pair.Entry != null && pair.Exit != null)
            {
                if (playerCooldowns[player].Contains(pair))
                    return HookResult.Continue;

                if (teleport.Entity!.Name.StartsWith("teleport_entry"))
                {
                    player.PlayerPawn.Value!.Teleport(pair.Exit.Entity.AbsOrigin, Config.ForceAngles ? pair.Exit.Entity.AbsRotation : player.PlayerPawn.Value.EyeAngles, player.PlayerPawn.Value.AbsVelocity);

                    if (!String.IsNullOrEmpty(Config.Sound))
                        player.ExecuteClientCommand($"play {Config.Sound}");
                }

                if (Config.BothWays && teleport.Entity!.Name.StartsWith("teleport_exit"))
                {
                    player.PlayerPawn.Value!.Teleport(pair.Entry.Entity.AbsOrigin, Config.ForceAngles ? pair.Entry.Entity.AbsRotation : player.PlayerPawn.Value.EyeAngles, player.PlayerPawn.Value.AbsVelocity);

                    if (!String.IsNullOrEmpty(Config.Sound))
                        player.ExecuteClientCommand($"play {Config.Sound}");
                }

                playerCooldowns[player].Add(pair);

                AddTimer(0.5f, () =>{
                    if (player != null && player.IsValid)
                        playerCooldowns[player].Remove(pair);
                });

            }
        }

        return HookResult.Continue;
    }

    public void OnServerPrecacheResources(ResourceManifest manifest)
    {
        manifest.AddResource(Config.EntryModel);
        manifest.AddResource(Config.ExitModel);
    }

    public void OnMapStart(string mapname)
    {
        Teleports.savedPath = Path.Combine(mapsFolder, $"{GetMapName()}.json");
    }

    HookResult EventRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Teleports.Spawn();

        return HookResult.Continue;
    }
}