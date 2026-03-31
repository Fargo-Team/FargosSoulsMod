using FargowiltasSouls.Core.AccessoryEffectSystem;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.Items.Accessories.Masomode
{
    public class QueenStinger : SoulsItem
    {
        public override bool Eternity => true;
        public override List<AccessoryEffect> ActiveSkillTooltips =>
            [AccessoryEffectLoader.GetEffect<SpecialDashEffect>()];
        public override void SetStaticDefaults()
        {
            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.accessory = true;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(0, 3);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.buffImmune[Terraria.ModLoader.ModContent.BuffType<Buffs.Masomode.InfestedBuff>()] = true;

            // Bees
            player.npcTypeNoAggro[NPCID.Bee] = true;
            player.npcTypeNoAggro[NPCID.BeeSmall] = true;

            // Hornets
            player.npcTypeNoAggro[NPCID.Hornet] = true;
            player.npcTypeNoAggro[NPCID.HornetFatty] = true;
            player.npcTypeNoAggro[NPCID.HornetHoney] = true;
            player.npcTypeNoAggro[NPCID.HornetLeafy] = true;
            player.npcTypeNoAggro[NPCID.HornetSpikey] = true;
            player.npcTypeNoAggro[NPCID.HornetStingy] = true;

            // Stringer immune
            player.FargoSouls().QueenStingerItem = Item;
            player.AddEffect<SpecialDashEffect>(Item);

            if (player.honey)
                player.GetArmorPenetration(DamageClass.Generic) += 10;
        }
    }

    public class SpecialDashEffect : AccessoryEffect
    {
        public override Header ToggleHeader => null;
        public override bool ActiveSkill => true;
        public override int ToggleItemType => ModContent.ItemType<QueenStinger>();
        public override void ActiveSkillJustPressed(Player player, bool stunned)
        {
            if (stunned)
                return;
            player.FargoSouls().SpecialDashKey(0);
        }
    }
}