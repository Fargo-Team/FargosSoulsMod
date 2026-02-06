using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.Bosses.MutantBoss
{
    public class MutantCrystalLeaf : ModProjectile
    {
        public override string Texture => FargoSoulsUtil.AprilFools ?
            "FargowiltasSouls/Content/Bosses/MutantBoss/MutantCrystalLeaf_April"
            : "FargowiltasSouls/Content/NPCs/EternityModeNPCs/CrystalLeaf";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Crystal Leaf");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            Main.projFrames[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 900;
            Projectile.aiStyle = -1;
            Projectile.scale = 2.5f;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            if (++Projectile.localAI[0] == 0)
            {
                for (int index1 = 0; index1 < 30; ++index1)
                {
                    int index2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ChlorophyteWeapon, 0f, 0f, 0, new Color(), 2f);
                    Main.dust[index2].noGravity = true;
                    Main.dust[index2].velocity *= 5f;
                }
            }

            Lighting.AddLight(Projectile.Center, 0.1f, 0.4f, 0.2f);
            Projectile.scale = (Main.mouseTextColor / 200f - 0.35f) * 0.2f + 0.95f;
            Projectile.scale *= 2.5f;

            int byIdentity = FargoSoulsUtil.GetProjectileByIdentity(Projectile.owner, (int)Projectile.ai[0], ModContent.ProjectileType<MutantMark2>());
            if (byIdentity != -1)
            {
                Vector2 offset = new Vector2(100, 0).RotatedBy(Projectile.ai[1]);
                Projectile.Center = Main.projectile[byIdentity].Center + offset;

                Projectile.localAI[1] = Math.Max(0, 150 - Main.projectile[byIdentity].ai[1]) / 150; //rampup
                Projectile.ai[1] += 0.15f * Projectile.localAI[1];

                if (Projectile.localAI[1] > 1f) //clamp it for use in draw
                    Projectile.localAI[1] = 1f;
            }

            Projectile.rotation = Projectile.ai[1] + (float)Math.PI / 2f;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, Main.rand.Next(60, 300));
            if (WorldSavingSystem.EternityMode)
            {
                target.AddBuff(ModContent.BuffType<InfestedBuff>(), Main.rand.Next(60, 300));
                target.AddBuff(ModContent.BuffType<IvyVenomBuff>(), Main.rand.Next(60, 300));
                target.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180);
            }
        }

        public override Color? GetAlpha(Color drawColor)
        {
            float num4 = Main.mouseTextColor / 200f - 0.3f;
            int num5 = (int)(byte.MaxValue * num4) + 50;
            if (num5 > byte.MaxValue)
                num5 = byte.MaxValue;
            return new Color(num5, num5, num5, 200);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            bool recolor = true;
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