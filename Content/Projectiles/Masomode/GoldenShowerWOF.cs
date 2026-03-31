using FargowiltasSouls.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminance.Core.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.Projectiles.Masomode
{
    public class GoldenShowerWOF : ModProjectile, IPixelatedPrimitiveRenderer
    {
        public override string Texture => "Terraria/Images/Projectile_288";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Golden Shower");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 15;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.aiStyle = -1;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.hostile = true;
        }

        public override bool? CanCutTiles()
        {
            return false;
        }

        public override void AI()
        {
            Projectile.extraUpdates = (int)Projectile.ai[1];
            if (Projectile.localAI[1] == 0)
            {
                Projectile.localAI[1] = 1;
                SoundEngine.PlaySound(SoundID.Item17, Projectile.Center);
            }

            /*for (int i = 0; i < 2; i++) //vanilla dusts
            {
                for (int j = 0; j < 2; ++j)
                {
                    int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 170, 0.0f, 0.0f, 100, default, 0.75f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 0.1f;
                    Main.dust[d].velocity += Projectile.velocity * 0.5f;
                    Main.dust[d].position -= Projectile.velocity / 3 * j;
                }
                if (Main.rand.NextBool(8))
                {
                    int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 170, 0.0f, 0.0f, 100, default, 0.325f);
                    Main.dust[d].velocity *= 0.25f;
                    Main.dust[d].velocity += Projectile.velocity * 0.5f;
                }
            }*/

            if (--Projectile.ai[0] < 0)
                Projectile.tileCollide = true;

            if (Projectile.localAI[0] == 0)
            {
                Projectile.velocity.Y += 0.5f * Projectile.ai[2];
                Projectile.rotation = Projectile.velocity.ToRotation() + (float)Math.PI / 2f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Ichor, 900);
            target.AddBuff(BuffID.OnFire, 300);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = Vector2.Zero;
            if (Projectile.timeLeft > 10)
                Projectile.timeLeft = 10;
            return false;
        }

        public float WidthFunction(float completionRatio)
        {
            float baseWidth = Projectile.scale * Projectile.width * 1.3f;
            return MathHelper.SmoothStep(baseWidth, 3.5f, completionRatio);
        }

        public static Color ColorFunction(float completionRatio)
        {
            return Color.Lerp(new(250, 250, 0), Color.Transparent, completionRatio) * 0.7f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int num156 = TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = new(0, y3, texture.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), Color.White, Projectile.rotation, origin2, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.BlobTrail");
            FargoSoulsUtil.SetTexture1(FargosTextureRegistry.FadedStreak.Value);
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(WidthFunction, ColorFunction, _ => Projectile.Size * 0.5f, Pixelate: true, Shader: shader), 5);
        }
    }
}