using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.UI.Elements;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.Toggler.Content;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.Items.Accessories.Forces
{
    public class EarthForce : BaseForce
    {
        public override void SetStaticDefaults()
        {
            Enchants[Type] =
            [
                ModContent.ItemType<CobaltEnchant>(),
                ModContent.ItemType<PalladiumEnchant>(),
                ModContent.ItemType<MythrilEnchant>(),
                ModContent.ItemType<OrichalcumEnchant>(),
                ModContent.ItemType<AdamantiteEnchant>(),
                ModContent.ItemType<TitaniumEnchant>()
            ];
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            SetActive(player);
            player.AddEffect<EarthForceEffect>(Item);
            // COBALT
            if (!player.HasEffect<EarthForceEffect>())
                player.AddEffect<AncientCobaltEffect>(Item);
            player.AddEffect<AncientCobaltFallEffect>(Item);
            player.AddEffect<CobaltEffect>(Item);
            // PALLADIUM
            if (!player.HasEffect<EarthForceEffect>())
                player.AddEffect<PalladiumEffect>(Item);
            player.AddEffect<PalladiumHealing>(Item);
            // MYTHRIL
            player.AddEffect<MythrilEffect>(Item);
            // ORICHALCUM
            player.AddEffect<OrichalcumEffect>(Item);
            // ADAMANTITE
            player.AddEffect<AdamantiteEffect>(Item);
            // TITANIUM
            player.AddEffect<TitaniumEffect>(Item);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            foreach (int ench in Enchants[Type])
                recipe.AddIngredient(ench);
            recipe.AddTile(ModContent.Find<ModTile>("Fargowiltas", "CrucibleCosmosSheet"));
            recipe.Register();
        }
    }
    public class EarthForceEffect : AccessoryEffect
    {
        public override Header ToggleHeader => null;
        //public override int ToggleItemType => ModContent.ItemType<EarthForce>();

        //modify this number and equation to balance out the timer stuff
        public static int EarthMaxCharge = 400;
        public static float GetEarthForceLerpValue(Player player)
        {
            //values will be at max when timer is between 300-400
            //values will be scaling between 100-300
            //values will not be changed from player's default when between 0 and 100
            //(unless you change it)
            return MathHelper.Clamp((player.FargoSouls().EarthTimer - 100) / 200f, 0, 1);
        }
        public override void PostUpdateEquips(Player player)
        {
            FargoSoulsPlayer farg = player.FargoSouls();
            bool attacking =  farg.WeaponUseTimer > 0;
            int adamTime = 300;
            float adamSpeed = 0.6f;

            if (!attacking && farg.EarthTimer < EarthMaxCharge)
            {
                if (farg.MythrilDelay > 0)
                    farg.MythrilDelay--;
                else
                    farg.EarthTimer += 2;
            }
            else if (attacking && farg.EarthTimer > 0)
            {
                farg.EarthTimer--;
                farg.MythrilDelay = 20;
                if (farg.EarthAdamantiteCharge < adamTime)
                    farg.EarthAdamantiteCharge++;
            }
            if (!attacking || farg.EarthTimer == 0)
                farg.EarthAdamantiteCharge = 0;
            if (player.whoAmI == Main.myPlayer && player.HasEffect<MythrilEffect>())
                CooldownBarManager.Activate("EarthForceCharge", ModContent.Request<Texture2D>("FargowiltasSouls/Content/Items/Accessories/Forces/EarthForce").Value, MythrilEnchant.NameColor, 
                    () => (float)Main.LocalPlayer.FargoSouls().EarthTimer / EarthMaxCharge, true, activeFunction: () => player.HasEffect<MythrilEffect>());

            float lerper = GetEarthForceLerpValue(player);
            //player.GetDamage(DamageClass.Generic) *= MathHelper.Lerp(1, 0.3f, lerper);
            
            if (player.HasEffect<MythrilEffect>())
                farg.AttackSpeed *= MathHelper.Lerp(1, 2f, lerper);

            if (player.HasEffect<PalladiumHealing>())
                player.lifeRegen += (int)MathHelper.Lerp(5, 25, lerper);

            if (player.HasEffect<TitaniumEffect>())
                player.endurance += MathHelper.Lerp(0.1f, 0.25f, lerper);

            if (player.HasEffect<AdamantiteEffect>())
                farg.AttackSpeed += adamSpeed * (float)farg.EarthAdamantiteCharge / adamTime;

            //Main.NewText(player.GetAttackSpeed(DamageClass.Generic));

            //one below or two below because it increments by 2 so it could skip this if it was just one number
            if (farg.EarthTimer >= EarthMaxCharge -2 && farg.EarthTimer < EarthMaxCharge && !player.controlUseItem && Main.myPlayer == player.whoAmI && player.HasEffect<MythrilEffect>())
            {
                if (farg.MythrilSoundCooldown <= 0)
                {
                    float pitch = -0.2f;
                    farg.MythrilSoundCooldown = 90;
                    SoundEngine.PlaySound(new SoundStyle($"{nameof(FargowiltasSouls)}/Assets/Sounds/Accessories/MythrilCharged") with { Volume = 0.6f, Pitch = pitch }, player.Center);
                }

                for (int i = 0; i < 5; i++)
                {
                    Vector2 position = player.Center + new Vector2(0, Main.rand.NextFloat(20, 40)).RotatedByRandom(MathHelper.TwoPi);
                    Particle green = new SparkParticle(position, (player.Center - position).SafeNormalize(Vector2.Zero) * 0.5f, Color.LightSeaGreen, 0.5f, 20);
                    green.Spawn();
                    position = player.Center + new Vector2(0, Main.rand.NextFloat(20, 40)).RotatedByRandom(MathHelper.TwoPi);
                    Particle red = new SparkParticle(position, (player.Center - position).SafeNormalize(Vector2.Zero) * 0.5f, Color.Red, 0.5f, 20);
                    red.Spawn();
                    position = player.Center + new Vector2(0, Main.rand.NextFloat(20, 40)).RotatedByRandom(MathHelper.TwoPi);
                    Particle white = new SparkParticle(position, (player.Center - position).SafeNormalize(Vector2.Zero) * 0.5f, Color.White, 0.5f, 20);
                    white.Spawn();
                }
            }
        }
        public override void OnHitNPCEither(Player player, NPC target, NPC.HitInfo hitInfo, DamageClass damageClass, int baseDamage, Projectile projectile, Item item)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            if (modPlayer.EarthTimer <= 100)
                return;
            float lerper = GetEarthForceLerpValue(player);
            int debuffDamage = (int)(baseDamage * MathHelper.Lerp(1, 0.75f, lerper));
            //divide by 2.3 because want to deal that damage over the course of ~6.6 seconds, deal a bit more than the actual missing damage to compensate for constant re-application of debuff without increasing the duration
            // Change damage to average of old and new damage to make it less affected by random extreme variation in damage
            target.FargoSouls().EarthDoTValue = (int)MathHelper.Lerp(target.FargoSouls().EarthDoTValue, debuffDamage / 2.3f, 0.5f);
            target.AddBuff(ModContent.BuffType<EarthPoison>(), 400);
        }
       
    }
}
