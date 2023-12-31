using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NoFriendlyGunfire;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public void Awake()
    {
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} v{PluginInfo.PLUGIN_VERSION} loaded!");
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }
}

public static class ShotgunItemExtensions
{
    public static bool PreCheckFriendly(this ShotgunItem shotgun)
    {
        shotgun.isReloading = false; // I put this here because I had to overwrite that line from ShootGun with the transpiler to have enough space for the "call" opcode
        return shotgun.playerHeldBy != null;
    }
}

[HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ShootGun))]
public static class ShotgunPatch
{
    // Changes ShootGun's "bool flag" to the result of PreCheckFriendly, which if true will skip DamagePlayer
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        int step = 0;

        foreach (CodeInstruction inst in instructions)
        {
            if (step == 0)
            {
                // Load instance address
                inst.opcode = OpCodes.Ldarg_0;
            }
            else if (step == 1)
            {
                // Call PreCheckFriendly
                inst.opcode = OpCodes.Call;
                inst.operand = AccessTools.Method(typeof(ShotgunItemExtensions), nameof(ShotgunItemExtensions.PreCheckFriendly));
            }
            else if (step == 2)
            {
                // Store the return value in the "bool flag" variable
                inst.opcode = OpCodes.Stloc_0;
            }
            else if (step == 3)
            {
                // Skip futher instructions until next instance operation - only tested with v45, might crash future versions...
                if (inst.opcode != OpCodes.Ldarg_0) continue;
            }

            yield return inst;
            step++;
        }
    }
}
