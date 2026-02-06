using Fargowiltas;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace FargowiltasSouls.Content.NPCs.EternityModeNPCs
{
    public class CrystalLeaf : ModNPC
    {
        public override string Texture => "Terraria/Images/Projectile_226";

        public override void SetStaticDefaults()
        {
            NPCID.Sets.TrailCacheLength[NPC.type] = 6;
            NPCID.Sets.TrailingMode[NPC.type] = 1;
            NPCID.Sets.CantTakeLunchMoney[Type] = true;
            NPCID.Sets.ImmuneToAllBuffs[Type] = true;
            Main.npcFrameCount[NPC.type] = 2;

            this.ExcludeFromBestiary();
        }

        public override void SetDefaults()
        {
            NPC.width = 28;
            NPC.height = 28;
            NPC.damage = 60;
            NPC.defense = WorldSavingSystem.MasochistModeReal ? 9999 : 20;
            NPC.lifeMax = WorldSavingSystem.MasochistModeReal ? 9999 : 800;
            NPC.HitSound = SoundID.NPCHit1;
            //NPC.DeathSound = SoundID.Grass;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.dontCountMe = true;
            NPC.knockBackResist = 0f;
            NPC.alpha = 255;
            NPC.lavaImmune = true;
            NPC.aiStyle = -1;
            NPC.chaseable = false;

        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            if (WorldSavingSystem.MasochistModeReal)
            {
                NPC.lifeMax = 9999;
                NPC.life = 9999;
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.localAI[2]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.localAI[2] = reader.ReadSingle();
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if (ProjectileID.Sets.IsAWhip[projectile.type])
                return false;

            return base.CanBeHitByProjectile(projectile);
        }
        public override void OnSpawn(IEntitySource source)
        {
            NPC.frame = new(0, 0, 36, 40);
        }
        public override void AI()
        {
            bool recolor = SoulConfig.Instance.BossRecolors && WorldSavingSystem.EternityMode;
            if (NPC.buffType[0] != 0)
                NPC.DelBuff(0);
            NPC plantera = FargoSoulsUtil.NPCExists(NPC.ai[0], NPCID.Plantera);
            if (plantera == null)
            {
                NPC.life = 0;
                NPC.HitEffect();
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            if (NPC.localAI[1] == 0)
            {
                NPC.localAI[1] = 1;
                for (int index1 = 0; index1 < 30; ++index1)
                {
                    int dustID = recolor ?
                        (Main.rand.NextBool() ? DustID.GlowingMushroom : DustID.MushroomTorch) :
                        (Main.rand.NextBool() ? DustID.TerraBlade : DustID.ChlorophyteWeapon);
                    Vector2 vel = Main.rand.NextVector2Circular(4, 4);
                    int index2 = Dust.NewDust(NPC.position, NPC.width, NPC.height, dustID, vel.X, vel.Y, 0, new Color(), 2f);
                    Main.dust[index2].noGravity = true;
                    Main.dust[index2].velocity *= 5f;
                }
            }

            NPC.target = plantera.target;

            /*if (NPC.HasPlayerTarget && Main.player[NPC.target].active)
            {
                if (++NPC.localAI[2] > 300) //projectile timer
                {
                    NPC.localAI[2] = 0;
                    NPC.netUpdate = true;
                    if (NPC.ai[1] == 130 && plantera.life > plantera.lifeMax / 2)
                    {
                        SoundEngine.PlaySound(SoundID.Grass, NPC.position);
                        if (Main.netMode != -1)
                        {
                            for (int i = -2; i <= 2; i++)
                            {
                                Vector2 target = plantera.Center + (Main.player[NPC.target].Center - plantera.Center).RotatedBy(MathHelper.ToRadians(80 / 2) * i);
                                Projectile.NewProjectile(NPC.Center, 18f * NPC.SafeDirectionTo(target), ModContent.ProjectileType<CrystalLeafShot>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                            }
                        }
                        for (int index1 = 0; index1 < 30; ++index1)
                        {
                            int index2 = Dust.NewDust(NPC.position, NPC.width, NPC.height, 157, 0f, 0f, 0, new Color(), 2f);
                            Main.dust[index2].noGravity = true;
                            Main.dust[index2].velocity *= 5f;
                        }
                    }
                }
            }*/

            if (recolor)
                Lighting.AddLight(NPC.Center, 25f / 255, 47f / 255, 64f / 255);
            else
                Lighting.AddLight(NPC.Center, 0.1f, 0.4f, 0.2f);

            NPC.scale = (Main.mouseTextColor / 200f - 0.35f) * 0.2f + 0.95f;
            //NPC.life = NPC.lifeMax;

            NPC.position = plantera.Center + new Vector2(NPC.ai[1], 0f).RotatedBy(NPC.ai[3]);
            NPC.position.X -= NPC.width / 2;
            NPC.position.Y -= NPC.height / 2;

            bool phase3 = false;
            if (plantera.GetLifePercent() < 0.25f) // when shooting in p3
            {
                if (plantera.ai[1] == 1)
                    NPC.scale *= 1.5f;
                phase3 = true;
            }


            if (!phase3 && plantera.GetGlobalNPC<Plantera>().RingTossTimer > 120 && plantera.GetGlobalNPC<Plantera>().RingTossTimer < 120 + 45 && NPC.ai[1] == 130) //pause before shooting
            {
                NPC.localAI[3] = 1;
                NPC.scale *= 1.3f;
                NPC.frame = new(0, 40, 36, 80);
            }
            else
            {
                NPC.localAI[3] = 0;

                float modifier = 1f; //NPC.localAI[0]++ / 90f;

                if (NPC.localAI[0] > 90)
                    NPC.localAI[0] = 90;

                float rotation = NPC.ai[1] == 130f ? 0.03f : -0.015f;
                NPC.ai[3] += rotation * modifier;
                if (NPC.ai[3] > (float)Math.PI)
                {
                    NPC.ai[3] -= 2f * (float)Math.PI;
                    NPC.netUpdate = true;
                }
                NPC.rotation = NPC.ai[3] + (float)Math.PI / 2f;

                if (NPC.ai[1] > 130)
                {
                    /*if (plantera.GetGlobalNPC<Plantera>().TentacleTimer < 0)
                    {
                        NPC.localAI[0] -= 2;
                        if (NPC.localAI[0] < 0)
                            NPC.localAI[0] = 0;
                    }*/

                    NPC.ai[2] += 2 * (float)Math.PI / 360 * modifier;
                    if (NPC.ai[2] > (float)Math.PI)
                        NPC.ai[2] -= 2 * (float)Math.PI;
                    NPC.ai[1] += (float)Math.Sin(NPC.ai[2]) * 7 * modifier;
                    NPC.scale *= 1.5f;
                }
            }

            NPC.alpha -= NPC.ai[1] > 130 ? 2 : 3;
            if (NPC.alpha < 0)
                NPC.alpha = 0;

            NPC.dontTakeDamage = NPC.alpha > 0;
        }

        public override void FindFrame(int frameHeight)
        {
            base.FindFrame(frameHeight);
        }

        public override bool CanHitPlayer(Player target, ref int CooldownSlot)
        {
            return NPC.alpha == 0;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<IvyVenomBuff>(), 240);
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                bool recolor = SoulConfig.Instance.BossRecolors && WorldSavingSystem.EternityMode;
                for (int index1 = 0; index1 < 30; ++index1)
                {
                    int dustID = recolor ?
                        (Main.rand.NextBool() ? DustID.MushroomTorch : DustID.GlowingMushroom) :
                        (Main.rand.NextBool() ? DustID.TerraBlade : DustID.ChlorophyteWeapon);
                    Vector2 vel = Main.rand.NextVector2Circular(4, 4);
                    int index2 = Dust.NewDust(NPC.position - NPC.Size, NPC.width * 2, NPC.height * 2, dustID, vel.X, vel.Y, 0, new Color(), 2f);
                    Main.dust[index2].noGravity = true;
                    Main.dust[index2].velocity *= 5f;
                }
            }
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override bool PreKill()
        {
            return false;
        }

        public override bool? DrawHealthBar(byte hbPos, ref float scale, ref Vector2 Pos)
        {
            return !WorldSavingSystem.MasochistModeReal;
        }

        public override Color? GetAlpha(Color drawColor)
        {
            float num4 = Main.mouseTextColor / 200f - 0.3f;
            int num5 = (int)(byte.MaxValue * num4) + 50;
            if (num5 > byte.MaxValue)
                num5 = byte.MaxValue;
            return new Color(num5, num5, num5, 200) * NPC.Opacity;
        }


        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            bool recolor = SoulConfig.Instance.BossRecolors && WorldSavingSystem.EternityMode;
            Texture2D texture2D13 = recolor ? Terraria.GameContent.TextureAssets.Npc[Type].Value : ModContent.Request<Texture2D>("FargowiltasSouls/Content/NPCs/EternityModeNPCs/CrystalLeafVanilla").Value;

            Rectangle rectangle = NPC.frame;
            Vector2 origin2 = rectangle.Size() / 2f;

            Color color26 = drawColor;
            color26 = NPC.GetAlpha(color26);

            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            //Main.EntitySpriteDraw(texture2D13, NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color26, NPC.rotation, origin2, NPC.scale, effects, 0);

            color26 *= 0.75f;

            if (NPC.alpha == 0)
            {
                for (int i = 0; i < NPCID.Sets.TrailCacheLength[NPC.type]; i++)
                {
                    Color color27 = color26;
                    color27.A = (byte)(NPC.localAI[3] == 0 ? 150 : 0);
                    color27 *= (float)(NPCID.Sets.TrailCacheLength[NPC.type] - i) / NPCID.Sets.TrailCacheLength[NPC.type];
                    Vector2 value4 = NPC.oldPos[i];
                    float num165 = NPC.rotation; //NPC.oldRot[i];
                    Main.EntitySpriteDraw(texture2D13, value4 + NPC.Size / 2f - Main.screenPosition + new Vector2(0, NPC.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color27, num165, origin2, NPC.scale, effects, 0);
                }
            }

            Main.spriteBatch.UseBlendState(BlendState.Additive);
            for (int j = 0; j < 12; j++)
            {
                Vector2 afterimageOffset = (MathHelper.TwoPi * j / 12).ToRotationVector2() * 2f * NPC.scale;
                Color glowColor = recolor ? Color.Blue : Color.IndianRed;

                Main.EntitySpriteDraw(texture2D13, NPC.Center - Main.screenPosition + afterimageOffset, rectangle, NPC.GetAlpha(glowColor), NPC.rotation, rectangle.Size() / 2, NPC.scale, SpriteEffects.None);
            }
            Main.spriteBatch.ResetToDefault();

            Main.EntitySpriteDraw(texture2D13, NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), drawColor, NPC.rotation, origin2, NPC.scale, effects, 0);
            return false;
        }
    }
}