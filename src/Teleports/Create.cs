using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public partial class Teleports
{
    private static Plugin instance = Plugin.Instance;

    public static List<TeleportPair> teleportPairs = new List<TeleportPair>();

    private static bool isNextTeleportEntry = true;
    public static void Create(CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value!;
        var position = new Vector(playerPawn.AbsOrigin!.X, playerPawn.AbsOrigin.Y, playerPawn.AbsOrigin.Z);
        var rotation = new QAngle(playerPawn.AbsRotation!.X, playerPawn.AbsRotation.Y, playerPawn.AbsRotation.Z);

        try
        {
            string teleportType = isNextTeleportEntry ? "entry" : "exit";
            var teleportData = CreateTeleport(position, rotation, teleportType);

            if (teleportData != null)
            {
                instance.PrintToChat(player, $"Created {teleportType} teleport");

                if (!isNextTeleportEntry)
                {
                    var incompletePair = teleportPairs.FirstOrDefault(p => p.Exit == null);

                    if (incompletePair != null)
                    {
                        incompletePair.Exit = teleportData;
                        instance.PrintToChat(player, $"Paired teleports");
                    }
                    else
                    {
                        teleportPairs.Add(new TeleportPair(null!, teleportData));
                        instance.PrintToChat(player, $"Pairing failed when creating a new exit teleport");
                    }
                }
                else teleportPairs.Add(new TeleportPair(teleportData, null!));

                isNextTeleportEntry = !isNextTeleportEntry;
            }
            else instance.PrintToChat(player, $"Failed to create {teleportType} teleport");

        }
        catch (Exception ex)
        {
            instance.Logger.LogError($"Exception: {ex}");
        }
    }

    public static TeleportsData? CreateTeleport(Vector position, QAngle rotation, string name)
    {
        var teleport = Utilities.CreateEntityByName<CPhysicsPropOverride>("prop_physics_override");

        if (teleport != null && teleport.IsValid)
        {
            teleport.Entity!.Name = "teleport_" + name;
            teleport.EnableUseOutput = true;

            teleport.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
            teleport.ShadowStrength = instance.Config.DisableShadows ? 0.0f : 1.0f;
            teleport.Render = instance.ParseColor(name == "entry" ? instance.Config.EntryColor : instance.Config.ExitColor);

            teleport.SetModel(name == "entry" ? instance.Config.EntryModel : instance.Config.ExitModel);
            teleport.DispatchSpawn();

            teleport.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            teleport.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;

            teleport.AcceptInput("DisableMotion", teleport, teleport);
            teleport.Teleport(new Vector(position.X, position.Y, position.Z), new QAngle(rotation.X, rotation.Y, rotation.Z));

            CreateTrigger(teleport);

            var teleportData = new TeleportsData(teleport, name == "entry" ? instance.Config.EntryModel : instance.Config.ExitModel, name);

            return teleportData;
        }
        else
        {
            instance.Logger.LogError("(CreateTeleport) Failed to create teleport");
            return null;
        }
    }

    public static Dictionary<CEntityInstance, CEntityInstance> Triggers = new Dictionary<CEntityInstance, CEntityInstance>();
    public static void CreateTrigger(CBaseEntity teleport)
    {
        var trigger = Utilities.CreateEntityByName<CTriggerMultiple>("trigger_multiple");

        if (trigger != null && trigger.IsValid)
        {
            trigger.Entity!.Name = "teleport_" + teleport.Entity!.Name + "_trigger";
            trigger.Spawnflags = 1;
            trigger.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
            trigger.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
            trigger.Collision.SolidFlags = 0;
            trigger.Collision.CollisionGroup = 14;

            trigger.SetModel(teleport.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);
            trigger.DispatchSpawn();
            trigger.Teleport(teleport.AbsOrigin, teleport.AbsRotation);
            trigger.AcceptInput("FollowEntity", teleport, trigger, "!activator");
            trigger.AcceptInput("Enable");

            Triggers.Add(trigger, teleport);
        }

        else instance.Logger.LogError("(CreateTrigger) Failed to create trigger");
    }

    public static void Spawn()
    {
        try
        {
            if (!File.Exists(savedPath))
                return;

            var jsonString = File.ReadAllText(savedPath);

            if (string.IsNullOrWhiteSpace(jsonString))
            {
                instance.Logger.LogInformation($"Save file for {instance.GetMapName()} is empty");
                return;
            }

            instance.Timers.Clear();
            instance.playerCooldowns.Clear();
            teleportPairs.Clear();
            Triggers.Clear();

            var pairedDataList = JsonSerializer.Deserialize<List<TeleportPairDTO>>(jsonString);

            if (pairedDataList == null || pairedDataList.Count == 0)
            {
                instance.Logger.LogInformation($"No teleport pairs found in save file for {instance.GetMapName()}");
                return;
            }

            foreach (var pair in pairedDataList)
            {
                TeleportsData? entryTeleport;
                TeleportsData? exitTeleport;

                var entryData = pair.Entry;
                entryTeleport = CreateTeleport(
                    new Vector(entryData.Position.X, entryData.Position.Y, entryData.Position.Z),
                    new QAngle(entryData.Rotation.Pitch, entryData.Rotation.Yaw, entryData.Rotation.Roll),
                    entryData.Name
                );

                var exitData = pair.Exit;
                exitTeleport = CreateTeleport(
                    new Vector(exitData.Position.X, exitData.Position.Y, exitData.Position.Z),
                    new QAngle(exitData.Rotation.Pitch, exitData.Rotation.Yaw, exitData.Rotation.Roll),
                    exitData.Name
                );

                if (entryTeleport != null && exitTeleport != null)
                {
                    var existingPair = teleportPairs.FirstOrDefault(p =>
                        (p.Entry == entryTeleport && p.Exit == exitTeleport) ||
                        (p.Entry == exitTeleport && p.Exit == entryTeleport)
                    );

                    if (existingPair == null)
                        teleportPairs.Add(new TeleportPair(entryTeleport, exitTeleport));
                }
            }
        }
        catch (Exception ex)
        {
            instance.Logger.LogError($"Failed to spawn teleports: {ex.Message}");
        }
    }
}
