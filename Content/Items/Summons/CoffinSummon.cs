
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using static Terraria.ModLoader.ModContent;
using Terraria.Audio;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
using FargowiltasSouls.Content.WorldGeneration;
using Microsoft.Xna.Framework;
using FargowiltasSouls.Content.Items.Weapons.SwarmDrops;
using FargowiltasSouls.Content.Items.Consumables;

namespace FargowiltasSouls.Content.Items.Summons
{

    public class CoffinSummon : SoulsItem
    {
        public override void SetStaticDefaults()
        {
            //DisplayName.SetDefault("Coffin Summon");
            //Tooltip.SetDefault("While in the underground Desert, summon the Cursed Coffin");
            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 5;

            ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<CoffinRoominator>();
        }

        public override bool IsLoadingEnabled(Mod mod) => CursedCoffin.Enabled;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.Orange;
            Item.consumable = true;
            Item.maxStack = 20;
            Item.noUseGraphic = false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                //.AddRecipeGroup("FargowiltasSouls:AnyDemoniteBar", 4)
                .AddIngredient(ItemID.ClayBlock, 15)
                .AddIngredient(ItemID.FossilOre, 8)
                .AddRecipeGroup("FargowiltasSouls:AnyGem", 4)
                .AddTile(TileID.DemonAltar)
                //.DisableDecraft()
                .Register();
        }

        public override bool CanUseItem(Player player)
        {
            if (CoffinArena.Rectangle.Contains(player.Center.ToTileCoordinates()))// && (Player.ZoneDirtLayerHeight || Player.ZoneRockLayerHeight))
                return !NPC.AnyNPCs(NPCType<CursedCoffin>());
            return false;
        }

        public override bool? UseItem(Player player)
        {
            if (!NPC.AnyNPCs(NPCType<CursedCoffinInactive>())) // no dormant coffin, just summon the boss
            {
                Vector2 coffinArenaCenter = CoffinArena.FightCenter;
                SoundEngine.PlaySound(CursedCoffin.ShotSFX with { Pitch = -0.75f }, coffinArenaCenter);
                int n = NPC.NewNPC(player.GetSource_ItemUse(Item), (int)coffinArenaCenter.X, (int)coffinArenaCenter.Y, ModContent.NPCType<CursedCoffin>());
                if (n.IsWithinBounds(Main.maxNPCs))
                {
                    if (Main.npc[n].ModNPC is CursedCoffin coffin)
                        coffin.LockVector1 = coffinArenaCenter;
                    Main.npc[n].netUpdate = true;
                }
                return true;
            }

            // else: dormant coffin exists, turn it into boss

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.TypeAlive<CursedCoffinInactive>())
                {
                    npc.Transform(ModContent.NPCType<CursedCoffin>());
                    SoundEngine.PlaySound(CursedCoffin.ShotSFX with { Pitch = -0.75f }, npc.Center);
                    if (npc.ModNPC is CursedCoffin coffin)
                        coffin.LockVector1 = npc.Center + Vector2.UnitY * 16;
                    npc.velocity.Y = -8f;
                    npc.netUpdate = true;
                }
            }
            return true;
        }
    }
}
