using System.Collections.Generic;
using FargowiltasSouls.Content.NPCs.EternityModeNPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.Projectiles
{
    public class BeeDash : ModProjectile
    {
        public override string Texture => "FargowiltasSouls/Content/NPCs/EternityModeNPCs/RoyalSubject";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Dash");
            Main.projFrames[Projectile.type] = Main.npcFrameCount[ModContent.NPCType<RoyalSubject>()];
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Player.defaultHeight;
            Projectile.height = Player.defaultHeight;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 15;
            Projectile.penetrate = -1;
            Projectile.FargoSouls().CanSplit = false;
            Projectile.FargoSouls().TimeFreezeImmune = true;
            Projectile.FargoSouls().DeletionImmuneRank = 2;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            if (player.HasBuff(ModContent.BuffType<Buffs.Souls.TimeFrozenBuff>()))
            {
                Projectile.Kill();
                return;
            }

            if (++Projectile.frameCounter > 2)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= 3)
                    Projectile.frame = 0;
            }

            player.dashDelay = 5;
            player.FargoSouls().IsDashingTimer = 0;

            player.Center = Projectile.Center;
            if (Projectile.timeLeft > 1) //trying to avoid wallclipping
                player.position += Projectile.velocity;
            player.velocity = Projectile.velocity * .5f;
            if (Projectile.velocity.X != 0)
                player.direction = Projectile.velocity.X > 0 ? 1 : -1;

            /*player.controlLeft = false;
            player.controlRight = false;
            player.controlJump = false;
            player.controlDown = false;*/
            player.controlUseItem = false;
            player.controlUseTile = false;
            player.controlHook = false;
            //player.controlMount = false;

            //if (player.mount.Active)
                //player.mount.Dismount(player);

            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation();

            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = 1;
                SoundEngine.PlaySound(SoundID.Item97, Projectile.Center);
                for (int i = 0; i < 20; i++)
                {
                    int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Bee, -player.velocity.X * 0.2f, -player.velocity.Y * 0.2f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 600);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item97, Projectile.Center);
            for (int i = 0; i < 20; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Bee, 0, 0, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 4f;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return false; //dont kill proj when hits tiles
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.localAI[0] != 0)
            {
                Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
                int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
                int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
                Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
                Vector2 origin2 = rectangle.Size() / 2f;

                Color color26 = Projectile.GetAlpha(lightColor);

                SpriteEffects effects = Projectile.direction < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                float rotationOffset = Projectile.direction < 0 ? MathHelper.Pi : 0;

                float scale = Projectile.scale;
                Vector2 posOffset = Vector2.Zero;
                if (Projectile.velocity != Vector2.Zero)
                    posOffset = -8f * Projectile.velocity.SafeNormalize(Vector2.Zero);

                for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
                {
                    Color color27 = color26 * 0.5f;
                    color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                    Vector2 value4 = Projectile.oldPos[i];
                    float num165 = Projectile.oldRot[i] + rotationOffset;
                    Main.EntitySpriteDraw(texture2D13, posOffset + value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color27, num165, origin2, scale, effects, 0);
                }

                Main.EntitySpriteDraw(texture2D13, posOffset + Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color26, Projectile.rotation + rotationOffset, origin2, scale, effects, 0);
            }
            return false;
        }
    }
}