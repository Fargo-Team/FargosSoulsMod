using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Content.Items.Materials;
using FargowiltasSouls.Content.Items.Weapons.BossDrops;
using FargowiltasSouls.Content.Projectiles.BossWeapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.Items.Weapons.SwarmDrops
{
    public class NukeFishron : SoulsItem
    {
        public override void SetStaticDefaults()
        {
            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            // DisplayName.SetDefault("Nuke Fishron");
            // Tooltip.SetDefault("Uses rockets for ammo\n'The highly weaponized remains of a defeated foe...'");
            //DisplayName.AddTranslation((int)GameCulture.CultureName.Chinese, "核子猪鲨");
            //Tooltip.AddTranslation((int)GameCulture.CultureName.Chinese, "'高度武器化的遗骸...'");
        }

        public override void SetDefaults()
        {
            Item.damage = 1000;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 24;
            Item.height = 24;
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 7.7f;
            Item.UseSound = FargosSoundRegistry.NukeFishronFire with { Volume = 0.6f };
            Item.useAmmo = AmmoID.Rocket;
            Item.value = Item.sellPrice(0, 25);
            Item.rare = ItemRarityID.Purple;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<FishNuke>();
            Item.shootSpeed = 7f;
        }

        /*public override void SafeModifyTooltips(List<TooltipLine> list)
        {
            foreach (TooltipLine line2 in list)
            {
                if (line2.mod == "Terraria" && line2.Name == "ItemName")
                {
                    line2.overrideColor = new Color(Main.DiscoR, 51, 255 - (int)(Main.DiscoR * 0.4));
                }
            }
        }*/

        //make them hold it different
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-12, 0);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, Item.shoot, damage, knockback, player.whoAmI, -1f, 0);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
            .AddIngredient(ModContent.ItemType<FishStick>())
            .AddIngredient(ModContent.ItemType<AbomEnergy>(), 10)
            .AddIngredient(ModContent.Find<ModItem>("Fargowiltas", "EnergizerFish"))
            .AddTile(ModContent.Find<ModTile>("Fargowiltas", "CrucibleCosmosSheet"))

            .Register();
        }
    }
}