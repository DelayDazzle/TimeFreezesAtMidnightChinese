using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;

namespace TimeFreezesAtMidnightChinese
{
    public class ModEntry : Mod
    {
        private Harmony? _harmony;

        private static readonly Dictionary<string, string> Translations = new()
        {
            ["Options"] = "选项",
            ["Enabled"] = "启用",
            ["Enables or disables the mod"] = "启用或禁用此模组",
            ["Time freezes at"] = "时间冻结于",
            ["Set at what point the game should prevent time from advancing"] = "设置游戏阻止时间推进的时间点（2400代表凌晨2点）",
            ["Use legacy method"] = "使用旧版方法",
            ["Not recommended"] = "不推荐",
            ["PS: The legacy method, rather than freezing time, kept checking whenever the clock went over the set time, reverting it back each time. This caused problems if you picked something close to 2AM, ticking JUST over it and causing the player to collapse into the next day."] = "附注：旧版方法不是冻结时间，而是在时钟超过设定时间时不断检查并将其重置。如果在接近凌晨2点设置，会导致时间刚刚超过就触发玩家晕倒进入下一天的问题。"
        };

        public override void Entry(IModHelper helper)
        {
            _harmony = new Harmony(ModManifest.UniqueID);

            Assembly? targetMod = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name?.Contains("TimeFreezesAtMidnight", StringComparison.OrdinalIgnoreCase) == true);

            if (targetMod == null)
            {
                Monitor.Log("未找到 TimeFreezesAtMidnight 程序集，跳过汉化。", LogLevel.Warn);
                return;
            }

            MethodInfo transpiler = typeof(ModEntry).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.NonPublic)!;
            var harmonyTranspiler = new HarmonyMethod(transpiler);
            int patchedCount = 0;

            foreach (Type type in targetMod.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (!ShouldPatch(method)) continue;
                    try
                    {
                        _harmony.Patch(method, transpiler: harmonyTranspiler);
                        patchedCount++;
                    }
                    catch { }
                }
            }
            Monitor.Log($"TimeFreezesAtMidnight 汉化补丁已加载，共修补 {patchedCount} 个方法。", LogLevel.Info);
        }

        private static bool ShouldPatch(MethodBase method)
        {
            string name = method.Name;
            if (!name.Contains("RegisterConfig") && !name.Contains("RegisterControls") && 
                !name.Contains("AddBoolOption") && !name.Contains("AddSectionTitle") && !name.Contains("AddParagraph")) return false;
            try { return method.GetMethodBody() != null; } catch { return false; }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr && instruction.operand is string original && Translations.TryGetValue(original, out string? translated))
                {
                    instruction.operand = translated;
                }
                yield return instruction;
            }
        }
    }
}
