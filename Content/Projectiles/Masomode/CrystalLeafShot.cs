using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Assets.Particles;
using FargowiltasSouls.Common.Graphics.Metaballs;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Dusts;
using FargowiltasSouls.Core;
using FargowiltasSouls.Core.Systems;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.UI.Chat;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.Projectiles.Masomode
{
    public class CrystalLeafShot : ModProjectile
    {

        public bool hasRedirect = false;

        public int rotLerp;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 40;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            Main.projFrames[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 900;
            Projectile.aiStyle = -1;//ProjAIStyleID.CrystalLeafShot;
            AIType = ProjectileID.CrystalLeafShot;
            Projectile.penetrate = -1;
        }
        public void VanillaAIStyleCrystalLeafShot()
        {
            Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) + 3.14f;

            float num347 = 1f - Projectile.timeLeft / 180f;
            float num348 = ((num347 * -6f * 0.85f + 0.33f) % 1f + 1f) % 1f;

            bool recolor = SoulConfig.Instance.BossRecolors && WorldSavingSystem.EternityMode;
            Color color = recolor ? Color.Blue : Color.LimeGreen;

            Color value3 = recolor ? color : Main.hslToRgb(num348, 1f, 0.5f);
            value3 = Color.Lerp(value3, recolor ? Color.DarkSlateGray : Color.Red, Utils.Remap(num348, 0.33f, 0.7f, 0f, 1f));
            value3 = Color.Lerp(value3, Color.Lerp(color, Color.Gold, 0.3f), value3.R / 255f * 1f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.05f, 0.2f, 0.1f) * 1.5f);
            if (Main.rand.NextBool(5))
            {
                Dust dust12 = Dust.NewDustDirect(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.WhiteTorch);
                dust12.noGravity = true;
                Dust dust2 = dust12;
                dust2.velocity *= 0.1f;
                dust12.scale = 1.5f;
                dust2 = dust12;
                dust2.velocity += Projectile.velocity * Main.rand.NextFloat();
                dust12.color = value3;
                dust12.color.A /= 4;
                dust12.alpha = 100;
                dust12.noLight = true;
            }
        }

        public override void AI()
        {
            if (Projectile.timeLeft == ContentSamples.ProjectilesByType[Type].timeLeft)
                Projectile.frame = Main.rand.Next(3);

            if (!Collision.SolidCollision(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height))
            {
                bool recolor = SoulConfig.Instance.BossRecolors && WorldSavingSystem.EternityMode;
                if (recolor)
                    Lighting.AddLight(Projectile.Center, 25f / 255, 47f / 255, 64f / 255);
                else
                    Lighting.AddLight(Projectile.Center + Projectile.velocity, 0.1f, 0.4f, 0.2f);

            }

            float metaballVelocityMult = 1f;

            if (Projectile.timeLeft < 900 - 120)
                Projectile.tileCollide = true;

            const int netWindow = 10; // extra leniency time to let variables net sync
            const float redirectTime = 60f + netWindow;
            if (Projectile.ai[1] > 0) // redirect
            {
                if (Projectile.ai[1] > netWindow)
                {

                    Player player = Main.player[(int)Projectile.ai[2]];
                    if (player.Alive())
                    {
                        
                        Vector2 LV = Projectile.velocity;
                        Vector2 PV = Projectile.SafeDirectionTo(player.Center);
                        float anglediff = FargoSoulsUtil.RotationDifference(LV, PV);
                        //change rotation towards target
                        Projectile.velocity = Projectile.velocity.RotatedBy(Math.Sign(anglediff) * Math.Min(Math.Abs(anglediff), MathHelper.Pi / redirectTime));
                        Projectile.rotation = Projectile.velocity.ToRotation();
                        
                        /*
                        float angledif = FargoSoulsUtil.RotationDifference(Projectile.rotation.ToRotationVector2(), Projectile.SafeDirectionTo(player.Center));
                        float amt = MathHelper.Min(Math.Abs(angledif), MathHelper.Pi / redirectTime);
                        Projectile.rotation += amt * Math.Sign(angledif);
                        Projectile.velocity = Projectile.rotation.ToRotationVector2() * Projectile.velocity
                        */
                    }

                }
                Projectile.position -= Projectile.velocity * 0.9f;
                metaballVelocityMult *= 0.1f;
                Projectile.ai[1]++;
                if (Projectile.ai[1] > redirectTime)
                {
                    Projectile.ai[1] = 0;
                    hasRedirect = true;
                    Projectile.netUpdate = true;
                }
            }
            if (hasRedirect)
            {
                Projectile.ai[2]++;
            }

            if (Projectile.ai[1] > netWindow || hasRedirect)
            {
                var metaball = ModContent.GetInstance<PlanteraMetaballPink>();
                Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 4f, Projectile.width / 4f);
                pos += Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.width / 3;
                metaball.CreateParticle(pos, Main.rand.NextVector2Circular(1f, 1f) + metaballVelocityMult * Projectile.velocity * 0.65f, Projectile.width * 0.8f, hasRedirect ? 1 : 0);
            }
            else
            {
                var metaball = ModContent.GetInstance<PlanteraMetaball>();
                Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 4f, Projectile.width / 4f);
                pos += Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.width / 3;
                metaball.CreateParticle(pos, Main.rand.NextVector2Circular(1f, 1f) + metaballVelocityMult * Projectile.velocity * 0.65f, Projectile.width * 0.8f, hasRedirect ? 1 : 0);
            }

            VanillaAIStyleCrystalLeafShot();
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            if (target.hurtCooldowns[1] == 0)
            {
                target.AddBuff(ModContent.BuffType<IvyVenomBuff>(), 240);
            }
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            bool recolor = SoulConfig.Instance.BossRecolors && WorldSavingSystem.EternityMode;
            Texture2D texture2D13 = recolor ? Terraria.GameContent.TextureAssets.Projectile[Type].Value : ModContent.Request<Texture2D>("FargowiltasSouls/Content/Projectiles/Masomode/CrystalLeafShotVanilla").Value;

            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;

            float rotation = Projectile.rotation - MathHelper.PiOver2 + (Projectile.ai[1] > 0 || hasRedirect == true? 0 : ((float)Math.Sin(15 * Main.GlobalTimeWrappedHourly) * 0.15f));
            if (hasRedirect)
            {
                float extrarotation = Projectile.ai[2] * MathHelper.Lerp(0.1f, 0.34f, ++rotLerp * 0.03f);
                if (extrarotation >= Projectile.ai[2] * 0.34f)
                {
                    extrarotation = Projectile.ai[2] * 0.34f;
                }
                rotation += extrarotation;
                
            }
            SpriteEffects spriteEffects = Projectile.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Color color26 = lightColor;
            color26 = Projectile.GetAlpha(Color.White);

            float scale = Projectile.scale;

            /*
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (int j = 0; j < 12; j++)
            {
                Vector2 afterimageOffset = (MathHelper.TwoPi * j / 12).ToRotationVector2() * 2f * scale;
                Color glowColor = hasRedirect ? Color.HotPink : Color.WhiteSmoke;

                Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + afterimageOffset, rectangle, Projectile.GetAlpha(glowColor), rotation, rectangle.Size() / 2, scale, spriteEffects);
            }
            Main.spriteBatch.ResetToDefault();
            */
            float redirStrength = hasRedirect ? 1 : Projectile.ai[1] / 70f;
            if (Projectile.ai[1] > 0 || hasRedirect)
            {
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color26 * redirStrength, rotation, origin2, scale, spriteEffects, 1);
                Main.spriteBatch.ResetToDefault();
            }
            if (!hasRedirect)
                Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color26 * (1 - redirStrength), rotation, origin2, scale, spriteEffects, 1);
            return false;
        }
    }
}