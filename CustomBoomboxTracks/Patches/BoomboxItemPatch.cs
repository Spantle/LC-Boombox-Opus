using CustomBoomboxTracks.Configuration;
using CustomBoomboxTracks.Managers;
using CustomBoomboxTracks.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CustomBoomboxTracks.Patches
{
    [HarmonyPatch(typeof(BoomboxItem))]
    internal class BoomboxItemPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Start_Postfix(BoomboxItem __instance)
        {
            BoomboxPlugin.LogInfo("Start postfix run");

            if (Config.EnableOnDemandLoading)
            {
                SharedCoroutineStarter.StartCoroutine(AudioManager.OnDemandLoad(__instance));
            }

            if (AudioManager.FinishedLoading)
            {
                AudioManager.ApplyClips(__instance);
            }
            else
            {
                BoomboxPlugin.LogInfo("Adding event");
                AudioManager.OnAllSongsLoaded += () => AudioManager.ApplyClips(__instance);
            }
        }

        [HarmonyPatch("StartMusic")]
        [HarmonyPostfix]
        public static void StartMusic_Postfix(BoomboxItem __instance, bool startMusic, bool pitchDown = false)
        {
            if (startMusic) BoomboxPlugin.LogInfo($"Playing {__instance.boomboxAudio.clip.name}");

            if (Config.EnableOnDemandLoading)
            {
                if (!startMusic && !pitchDown)
                {
                    SharedCoroutineStarter.StartCoroutine(AudioManager.RemoveSong(__instance));
                }
                else
                {
                    SharedCoroutineStarter.StartCoroutine(AudioManager.OnDemandLoad(__instance));
                }
            }
        }

        [HarmonyPatch("PocketItem")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PocketItem_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var patchedInstructions = instructions.ToList();

            var skippedFirstCall = false;

            for (int i = 0; i < patchedInstructions.Count; i++)
            {
                if (!skippedFirstCall)
                {
                    if (patchedInstructions[i].opcode == OpCodes.Call)
                        skippedFirstCall = true;

                    continue;
                }

                if (patchedInstructions[i].opcode == OpCodes.Ret) break;

                patchedInstructions[i].opcode = OpCodes.Nop;
            }

            return patchedInstructions;
        }
    }
}
