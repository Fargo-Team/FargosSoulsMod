using Fargowiltas;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core;
using FargowiltasSouls.Core.Systems;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.Projectiles.Masomode
{
    public class PlanteraCrystalLeafRing : MutantCrystalLeaf
    {
        public override string Texture => "FargowiltasSouls/Content/NPCs/EternityModeNPCs/CrystalLeaf";
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 2;
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.scale = 1.5f;
            CooldownSlot = ImmunityCooldownID.General;
            Projectile.FargoSouls().DeletionImmuneRank = 1;
        }

        public override void AI()
        {
            Projectile.frame = 1;
            bool recolor = SoulConfig.Instance.BossRecolors && WorldSavingSystem.EternityMode;
            if (++Projectile.localAI[0] == 0)
            {

                for (int index1 = 0; index1 < 30; ++index1)
                {
                    int dustID = recolor ?
                        (Main.rand.NextBool() ? DustID.GlowingMushroom : DustID.MushroomTorch) :
                        (Main.rand.NextBool() ? DustID.TerraBlade : DustID.ChlorophyteWeapon);
                    Vector2 vel = Main.rand.NextVector2Circular(4, 4);
                    int index2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustID, vel.X, vel.Y, 0, new Color(), 2f);
                    Main.dust[index2].noGravity = true;
                    Main.dust[index2].velocity *= 5f;
                }
            }
            if (recolor)
                Lighting.AddLight(Projectile.Center, 25f / 255, 47f / 255, 64f / 255);
            else
                Lighting.AddLight(Projectile.Center, 0.1f, 0.4f, 0.2f);

            Projectile.scale = (Main.mouseTextColor / 200f - 0.35f) * 0.2f + 0.95f;
            Projectile.scale *= 1.5f;

            int byIdentity = FargoSoulsUtil.GetProjectileByIdentity(Projectile.owner, (int)Projectile.ai[0], ModContent.ProjectileType<MutantMark2>());
            if (byIdentity != -1)
            {
                Vector2 offset = new Vector2(100, 0).RotatedBy(Projectile.ai[1]);
                Projectile.Center = Main.projectile[byIdentity].Center + offset;

                Projectile.localAI[1] = Math.Max(0, 150 - Main.projectile[byIdentity].ai[1]) / 150; //rampup
                if (Projectile.localAI[1] > 1f) //clamp it for use in draw
                    Projectile.localAI[1] = 1f;
                Projectile.ai[1] += 0.15f * Projectile.localAI[1];
            }

            Projectile.rotation = Projectile.ai[1] + (float)Math.PI / 2f;

            if (Projectile.localAI[0] > 20)
            {
                Projectile.localAI[0] = 1;
                NPC plantera = FargoSoulsUtil.NPCExists(NPC.plantBoss, NPCID.Plantera);
                if (plantera != null && Projectile.Distance(plantera.Center) < 1600f && FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, 4f * Projectile.ai[1].ToRotationVector2(), ModContent.ProjectileType<CrystalLeafShot>(), Projectile.damage, Projectile.knockBack, Projectile.owner, ai0: plantera.whoAmI);
            }

            /*
            if (true || Projectile.localAI[0] % 2 == 0)
            {
                Color color = recolor ? Color.SkyBlue : Color.Lime;
                Particle boom = new AlphaBloomParticle(Projectile.Center - Projectile.velocity, Vector2.Zero, color, Vector2.One * 1, Vector2.One * 0.1f, 120, false);
                boom.Spawn();
            }
            */
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<IvyVenomBuff>(), 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            bool recolor = SoulConfig.Instance.BossRecolors && WorldSavingSystem.EternityMode;
            Texture2D texture2D13 = recolor ? Terraria.GameContent.TextureAssets.Projectile[Type].Value : ModContent.Request<Texture2D>("FargowiltasSouls/Content/NPCs/EternityModeNPCs/CrystalLeafVanilla").Value;

            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;

            Color color26 = lightColor;
            color26 = Projectile.GetAlpha(color26);

            Main.spriteBatch.UseBlendState(BlendState.Additive);

            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Color color27 = Color.White * Projectile.Opacity * Projectile.localAI[1];
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i];
                float num165 = Projectile.oldRot[i];
                Main.EntitySpriteDraw(texture2D13, value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color27, num165, origin2, Projectile.scale, SpriteEffects.None, 0);
            }

            for (int j = 0; j < 12; j++)
            {
                Vector2 afterimageOffset = (MathHelper.TwoPi * j / 12).ToRotationVector2() * 2f * Projectile.scale;
                Color glowColor = recolor ? Color.Blue : Color.IndianRed;

                Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + afterimageOffset, rectangle, Projectile.GetAlpha(glowColor), Projectile.rotation, rectangle.Size() / 2, Projectile.scale, SpriteEffects.None);
            }
            Main.spriteBatch.ResetToDefault();

            Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), Color.White, Projectile.rotation, origin2, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}