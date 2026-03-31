using Fargowiltas.Projectiles;
using FargowiltasSouls.Content.Items.Armor;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.Projectiles.Minions
{
    public class EridanusRitual : ModProjectile
    {
        private const float PI = (float)Math.PI;
        private const float rotationPerTick = PI / 57f;
        private const float threshold = 175f / 2f;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();

            // DisplayName.SetDefault("Lunar Ritual");
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.FargoSouls().TimeFreezeImmune = true;
        }

        public override void AI()
        {
            Projectile.GetGlobalProjectile<FargoGlobalProjectile>().lowRender = false;
            if (Main.player[Projectile.owner].active && !Main.player[Projectile.owner].dead && !Main.player[Projectile.owner].ghost && Main.player[Projectile.owner].FargoSouls().EridanusSet
                && (Projectile.owner != Main.myPlayer || Main.player[Projectile.owner].FargoSouls().EridanusEmpower))
            {
                Projectile.alpha = 0;
            }
            else
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = Main.player[Projectile.owner].Center;

            Projectile.timeLeft = 2;
            Projectile.scale = (1f - Projectile.alpha / 255f) * 1.5f + (Main.mouseTextColor / 200f - 0.35f) * 0.5f; //throbbing
            Projectile.scale /= 2f;
            if (Projectile.scale < 0.1f)
                Projectile.scale = 0.1f;
            /*Projectile.ai[0] += rotationPerTick;
            if (Projectile.ai[0] > PI)
            {
                Projectile.ai[0] -= 2f * PI;
                Projectile.netUpdate = true;
            }
            Projectile.rotation = Projectile.ai[0];*/
            Projectile.rotation += rotationPerTick;
            if (Projectile.rotation > PI)
                Projectile.rotation -= 2f * PI;

            Projectile.frame = (Main.player[Projectile.owner].FargoSouls().EridanusTimer / EridanusHat.ClassDuration) switch
            {
                0 => 1,
                1 => 2,
                2 => 0,
                _ => 3,
            };

            //handle countdown between phase changes
            Projectile.localAI[0] = Main.player[Projectile.owner].FargoSouls().EridanusTimer % (float)EridanusHat.ClassDuration / EridanusHat.ClassDuration * 12f - 1f;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //spriteBatch.End();
            //spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;

            Color color26 = Projectile.GetAlpha(lightColor);

            const int max = 12;
            for (int x = 0; x < max; x++)
            {
                if (x < Projectile.localAI[0])
                    continue;
                Vector2 drawOffset = new(0f, -threshold * Projectile.scale);
                drawOffset = drawOffset.RotatedBy((x + 1) * PI / max * 2).RotatedBy(Projectile.ai[0]);
                Main.EntitySpriteDraw(texture2D13, Projectile.Center + drawOffset - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color26, Projectile.rotation, origin2, Projectile.scale, SpriteEffects.None, 0);
            }

            //spriteBatch.End();
            //spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White * Projectile.Opacity * 0.8f;
        }
    }
}