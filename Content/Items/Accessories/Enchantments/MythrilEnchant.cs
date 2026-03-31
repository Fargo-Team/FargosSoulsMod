using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Content.Items.Weapons.BossDrops;
using FargowiltasSouls.Content.UI.Elements;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.Items.Accessories.Enchantments
{
    public class MythrilEnchant : BaseEnchant
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }
        public static readonly Color NameColor = new(157, 210, 144);
        public override Color nameColor => NameColor;


        public override void SetDefaults()
        {
            base.SetDefaults();

            Item.rare = ItemRarityID.Pink;
            Item.value = 100000;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.AddEffect<MythrilEffect>(Item);
        }


        public override void AddRecipes()
        {
            CreateRecipe()
            .AddRecipeGroup("FargowiltasSouls:AnyMythrilHead")
            .AddIngredient(ItemID.MythrilChainmail)
            .AddIngredient(ItemID.MythrilGreaves)
            .AddIngredient(ItemID.ClockworkAssaultRifle)
            .AddIngredient(ItemID.Gatligator)
            .AddIngredient(ItemID.OnyxBlaster)

            .AddTile(TileID.CrystalBall)
            .Register();
        }
    }

    public class MythrilEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<EarthHeader>();
        public override int ToggleItemType => ModContent.ItemType<MythrilEnchant>();

        public static void CalcMythrilAttackSpeed(FargoSoulsPlayer modPlayer, Item item)
        {
            if (!modPlayer.Player.HasEffectEnchant<MythrilEffect>())
                return;

            if (item.DamageType != DamageClass.Default && item.pick == 0 && item.axe == 0 && item.hammer == 0 && item.type != ModContent.ItemType<PrismaRegalia>())
            {
                float ratio = Math.Max((float)modPlayer.MythrilTimer / modPlayer.MythrilMaxTime, 0);
                modPlayer.AttackSpeed += modPlayer.MythrilMaxSpeedBonus * ratio;
            }
        }

        public override void PostUpdateEquips(Player player)
        {
            if (!HasEffectEnchant(player))
                return;

            FargoSoulsPlayer modPlayer = player.FargoSouls();

            const int cooldown = 60 * 5;
            int mythrilEndTime = modPlayer.MythrilMaxTime - cooldown;

            if (modPlayer.WeaponUseTimer > 0)
            {
                modPlayer.MythrilTimer--;
                modPlayer.MythrilDelay = 20;
            }
            else
            {
                if (modPlayer.MythrilDelay > 0)
                    modPlayer.MythrilDelay--;
                else
                {
                    modPlayer.MythrilTimer += 1.125f;
                    float diff = (modPlayer.MythrilMaxTime - modPlayer.MythrilTimer);
                    if (diff > 0 && diff < 2 && player.whoAmI == Main.myPlayer && modPlayer.MythrilSoundCooldown <= 0)
                    {
                        SoundEngine.PlaySound(new SoundStyle($"{nameof(FargowiltasSouls)}/Assets/Sounds/Accessories/MythrilCharged") with { Volume = 0.6f }, player.Center);
                        modPlayer.MythrilSoundCooldown = 90;
                        //Projectile.NewProjectile(GetSource_EffectItem(player), player.Top, Vector2.Zero, ModContent.ProjectileType<EffectVisual>(), 0, 0, player.whoAmI, (float)EffectVisual.Effects.MythrilEnchant);
                    }
                }
            }

            if (modPlayer.MythrilTimer > modPlayer.MythrilMaxTime)
                modPlayer.MythrilTimer = modPlayer.MythrilMaxTime;
            if (modPlayer.MythrilTimer < mythrilEndTime)
                modPlayer.MythrilTimer = mythrilEndTime;

            if (player.whoAmI == Main.myPlayer)
                CooldownBarManager.Activate("MythrilEnchantCharge", ModContent.Request<Texture2D>("FargowiltasSouls/Content/Items/Accessories/Enchantments/MythrilEnchant").Value, MythrilEnchant.NameColor, 
                    () => (float)Main.LocalPlayer.FargoSouls().MythrilTimer / Main.LocalPlayer.FargoSouls().MythrilMaxTime, true, 60 * 10, activeFunction: () => player.HasEffect<MythrilEffect>() && player.HasEffectEnchant<MythrilEffect>());
        }
    }

}
