using FargowiltasSouls.Core.Systems;
using Luminance.Core.MenuInfoUI;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace FargowiltasSouls.Common
{
    
    public class EternityWorldIconManager : ModSystem
    {
        public override void SaveWorldHeader(TagCompound tag)
        {
            tag["EternityWorld"] = WorldSavingSystem.EternityMode;
            tag["MasochistWorld"] = WorldSavingSystem.MasochistModeReal;
        }
    }
    public class EternityInfoUIManager : InfoUIManager
    {
        public static bool EternityWorld(TagCompound tag) => tag.ContainsKey("EternityWorld") && tag.GetBool("EternityWorld");
        public static bool MasoWorld(TagCompound tag) => tag.ContainsKey("MasochistWorld") && tag.GetBool("MasochistWorld");
        public override IEnumerable<WorldInfoIcon> GetWorldInfoIcons()
        {
            yield return new WorldInfoIcon(
                EternityIconPath, 
                "Mods.FargowiltasSouls.UI.EternityEnabled", 
                worldFileData =>
                {
                    if (worldFileData.TryGetHeaderData<EternityWorldIconManager>(out TagCompound tag))
                        if (EternityWorld(tag) && !MasoWorld(tag))
                            return true;
                    return false;
                },
                byte.MinValue);
            yield return new WorldInfoIcon(
                MasochistIconPath,
                "Mods.FargowiltasSouls.UI.MasochistEnabled",
                worldFileData =>
                {
                    if (worldFileData.TryGetHeaderData<EternityWorldIconManager>(out TagCompound tag))
                        if (MasoWorld(tag))
                            return true;
                    return false;
                },
                byte.MinValue);
        }

        internal static string EternityIconPath => "FargowiltasSouls/Assets/UI/OncomingMutant";
        internal static string MasochistIconPath => "FargowiltasSouls/Assets/UI/OncomingMutantWithAura";
    }
}
