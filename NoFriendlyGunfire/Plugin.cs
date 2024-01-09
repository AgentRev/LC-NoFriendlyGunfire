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

[HarmonyPatch(typeof(ShotgunItem))]
public static class ShotgunPatch
{
    public static bool PreCheckFriendly(this ShotgunItem shotgun)
    {
        return shotgun.playerHeldBy != null;
    }

    // Changes ShootGun's "bool flag" to the result of PreCheckFriendly, which if true will skip DamagePlayer
    [HarmonyPatch(nameof(ShotgunItem.ShootGun))]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var pending = true;

        foreach (var inst in instructions)
        {
            yield return inst;

            // Wait until after the 1st occurrence of Stloc_0 (the "bool flag = false" line) to insert the pre-check
            if (pending && inst.opcode == OpCodes.Stloc_0)
            {
                // Load the object address
                yield return new CodeInstruction(OpCodes.Ldarg_0);

                // Call PreCheckFriendly
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ShotgunPatch), nameof(PreCheckFriendly)));

                // Store the return value in the "bool flag" variable
                yield return new CodeInstruction(OpCodes.Stloc_0);

                pending = false;
            }
        }
    }
}
