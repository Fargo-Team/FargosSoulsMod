using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.Bosses.Champions.Spirit
{
    public class SpiritChampionHand : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Champion of Spirit");
            //DisplayName.AddTranslation((int)GameCulture.CultureName.Chinese, "魂灵英灵");
            NPCID.Sets.TrailCacheLength[NPC.type] = 6;
            NPCID.Sets.TrailingMode[NPC.type] = 1;
            NPCID.Sets.CantTakeLunchMoney[Type] = true;

            this.ExcludeFromBestiary();

            NPC.AddDebuffImmunities(
            [
                BuffID.Confused,
                    BuffID.Chilled,
                    BuffID.OnFire,
                    BuffID.Suffocation,
                    ModContent.BuffType<LethargicBuff>(),
                    ModContent.BuffType<ClippedWingsBuff>(),
                    ModContent.BuffType<LightningRodBuff>()
            ]);
        }

        public override void SetDefaults()
        {
            NPC.width = 90;
            NPC.height = 90;
            NPC.damage = 125;
            NPC.defense = 140;
            NPC.lifeMax = 550000;
            NPC.HitSound = SoundID.NPCHit54;
            NPC.DeathSound = SoundID.NPCDeath52;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.aiStyle = -1;

            NPC.dontTakeDamage = true;
            NPC.chaseable = false;
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            //NPC.damage = (int)(NPC.damage * 0.5f);
            NPC.lifeMax = (int)(NPC.lifeMax * balance);
        }

        public override bool CanHitPlayer(Player target, ref int CooldownSlot)
        {
            if (target.HasBuff<GrabbedBuff>())
                return false;
            NPC head = FargoSoulsUtil.NPCExists(NPC.ai[1], ModContent.NPCType<SpiritChampion>());
            if (head == null)
                return false;
            if (head.As<SpiritChampion>().GrabCooldown > 0)
                return false;

            CooldownSlot = 1;
            return NPC.localAI[3] == 0;
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            modifiers.FinalDamage *= 0.25f;
        }

        public override void AI()
        {
            NPC head = FargoSoulsUtil.NPCExists(NPC.ai[1], ModContent.NPCType<SpiritChampion>());
            if (head == null)
            {
                NPC.life = 0;
                NPC.checkDead();
                NPC.active = false;
                return;
            }

            NPC.target = head.target;
            NPC.realLife = head.whoAmI;
            NPC.position += head.velocity * 0.75f;
            NPC.chaseable = false;

            Player player = Main.player[NPC.target];
            Vector2 targetPos;

            void Heal()
            {
                if (++NPC.localAI[1] > 10) //heal 6 times per second
                {
                    NPC.localAI[1] = 0;
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Vector2 speed = Main.rand.NextFloat(1, 2) * Vector2.UnitX.RotatedByRandom(Math.PI * 2);
                        int heal = (int)(head.lifeMax / 100f * Main.rand.NextFloat(0.95f, 1.05f)); //heal back roughly 1 percent per heal
                        float ai1 = 30 + Main.rand.Next(30);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, speed, ModContent.ProjectileType<SpiritHeal>(), heal, 0f, Main.myPlayer, head.whoAmI, ai1);
                    }
                }
            }

            NPC.localAI[3] = 0;

            switch ((int)NPC.ai[0])
            {
                case 0: //float near head
                    {
                        targetPos = head.Center;
                        float offset = head.ai[0] % 2 == 0 ? 50 : 150;
                        float distance = head.ai[0] % 2 == 0 ? 50 : 100;
                        if (head.ai[0] % 2 == 0)
                            NPC.localAI[3] = 1;
                        targetPos.X += offset * NPC.ai[2];
                        targetPos.Y += offset * NPC.ai[3];
                        if (NPC.Distance(targetPos) > distance)
                            Movement(targetPos, 0.8f, 24f);
                    }
                    break;

                case 1: //you think you're safe?
                    {
                        if (head.ai[0] != -3 && NPC.ai[0] == 3 && NPC.ai[3] != 1) // special maso hand
                        {
                            NPC.ai[3] = 1; // mark it as the special maso hand
                            NPC.netUpdate = true;
                        }
                        if (head.ai[0] != 3 && head.ai[0] != -3 && !(WorldSavingSystem.MasochistModeReal && NPC.ai[0] == 3)) //return to normal when head no longer wants to grab
                        {
                            NPC.ai[0] = 0;
                            NPC.netUpdate = true;
                        }

                        bool targetPlayer = Math.Sign(player.Center.X - head.Center.X) * Math.Sign(NPC.Center.X - head.Center.X) == 1
                            && Math.Sign(player.Center.Y - head.Center.Y) * Math.Sign(NPC.Center.Y - head.Center.Y) == 1; //in same quadrant as you
                        if (head.ai[0] == -3) //four hands never target you during last stand
                            targetPlayer = false;
                        if (NPC.ai[0] == 3) //FIFTH hand always targets you
                            targetPlayer = true;

                        if (targetPlayer)
                        {
                            targetPos = player.Center;
                        }
                        else //wave around
                        {
                            targetPos = head.Center + head.SafeDirectionTo(NPC.Center) * head.Distance(player.Center);
                        }

                        if (NPC.Distance(targetPos) > 50)
                            Movement(targetPos, 0.15f, 7f);

                        if (NPC.Hitbox.Intersects(player.Hitbox) && !player.HasBuff(ModContent.BuffType<Buffs.Boss.GrabbedBuff>())
                            && player.FargoSouls().MashCounter <= 0) //GOTCHA
                        {
                            SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                            NPC.ai[0] = 2;
                            NPC.netUpdate = true;

                            if (head.ai[0] != -3) //don't change head state in last stand
                            {
                                head.ai[0] = -1;
                                head.ai[1] = 0;
                                head.netUpdate = true;
                            }
                        }
                    }
                    break;

                case 2: //successful grab
                    if (head.ai[0] != -1 && head.ai[0] != -3 || !player.active || player.dead || player.FargoSouls().MashCounter > 30)
                    {
                        if (NPC.Hitbox.Intersects(player.Hitbox)) //throw aside
                        {
                            player.FargoSouls().MashCounter += 30;
                            player.velocity.X = player.Center.X < head.Center.X ? -15f : 15f;
                            player.velocity.Y = -10f;
                            SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                        }

                        NPC.ai[0] = head.ai[0] == -3 ? 1 : 0;
                        if (NPC.ai[3] == 1)
                            NPC.ai[0] = 3;
                        NPC.netUpdate = true;
                    }
                    else //keep trying to grab
                    {
                        if (NPC.Hitbox.Intersects(player.Hitbox))
                        {
                            Heal();

                            player.Center = NPC.Center;
                            player.velocity.X = 0;
                            player.velocity.Y = -0.4f;

                            Movement(head.Center, 0.8f, 24f);

                            player.AddBuff(ModContent.BuffType<Buffs.Boss.GrabbedBuff>(), 2);
                            head.As<SpiritChampion>().GrabCooldown = (int)(60 * 2.5f);
                            head.netUpdate = true;

                            if (!player.immune || player.immuneTime < 2)
                            {
                                player.immune = true;
                                player.immuneTime = 2;
                            }
                        }
                        else
                        {
                            Movement(player.Center, 2.4f, 48f);
                        }
                    }
                    break;

                case 3:
                    goto case 1;

                case 4: //enrage grab
                    if (NPC.Hitbox.Intersects(player.Hitbox))
                    {
                        Heal();

                        player.Center = NPC.Center;
                        player.velocity.X = 0;
                        player.velocity.Y = -0.4f;

                        Movement(head.Center, 0.8f, 24f);
                    }
                    else
                    {
                        Movement(player.Center, 2.4f, 48f);
                    }

                    if (++NPC.localAI[0] > 120)
                    {
                        if (head.Hitbox.Intersects(player.Hitbox)) //throw aside
                        {
                            player.velocity.X = player.Center.X < head.Center.X ? -15f : 15f;
                            player.velocity.Y = -10f;
                            SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                        }

                        NPC.life = 0;
                        NPC.SimpleStrikeNPC(NPC.lifeMax, 0, noPlayerInteraction: true);
                        NPC.active = false;
                    }
                    break;

                default:
                    NPC.ai[0] = 0;
                    goto case 0;
            }

            NPC.direction = NPC.spriteDirection = -(int)NPC.ai[2];
            NPC.rotation = NPC.DirectionFrom(head.Center).ToRotation();
            if (NPC.spriteDirection < 0)
                NPC.rotation += (float)Math.PI;

            //dust tendrils connecting hands to base
            Vector2 dustHead = head.Center + head.SafeDirectionTo(NPC.Center) * 50;
            Vector2 headOffset = NPC.Center - dustHead;
            for (int i = 0; i < headOffset.Length(); i += 16)
            {
                if (Main.rand.NextBool())
                    continue;

                int d = Dust.NewDust(dustHead + Vector2.Normalize(headOffset) * i, 0, 0, DustID.Wraith,
                    head.velocity.X * 0.4f, head.velocity.Y * 0.4f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 3f;
            }
            Main.dust[Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Wraith, 0f, 0f, 0, default, 2f)].noGravity = true;

            NPC.dontTakeDamage = head.ai[0] < 0;

            if (!NPC.dontTakeDamage)
                NPC.dontTakeDamage = head.dontTakeDamage;
        }

        public override bool CheckDead()
        {
            if (!(NPC.ai[1] > -1 && NPC.ai[1] < Main.maxNPCs && Main.npc[(int)NPC.ai[1]].active
                && Main.npc[(int)NPC.ai[1]].type == ModContent.NPCType<SpiritChampion>()))
            {
                return true;
            }

            NPC head = Main.npc[(int)NPC.ai[1]];
            if (head.ai[0] != -3)
            {
                NPC.active = true;
                NPC.life = 1;
                return false;
            }

            return true;
        }

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage /= 2;
        }

        private void Movement(Vector2 targetPos, float speedModifier, float cap = 12f)
        {
            if (NPC.Center.X < targetPos.X)
            {
                NPC.velocity.X += speedModifier;
                if (NPC.velocity.X < 0)
                    NPC.velocity.X += speedModifier * 2;
            }
            else
            {
                NPC.velocity.X -= speedModifier;
                if (NPC.velocity.X > 0)
                    NPC.velocity.X -= speedModifier * 2;
            }
            if (NPC.Center.Y < targetPos.Y)
            {
                NPC.velocity.Y += speedModifier;
                if (NPC.velocity.Y < 0)
                    NPC.velocity.Y += speedModifier * 2;
            }
            else
            {
                NPC.velocity.Y -= speedModifier;
                if (NPC.velocity.Y > 0)
                    NPC.velocity.Y -= speedModifier * 2;
            }
            if (Math.Abs(NPC.velocity.X) > cap)
                NPC.velocity.X = cap * Math.Sign(NPC.velocity.X);
            if (Math.Abs(NPC.velocity.Y) > cap)
                NPC.velocity.Y = cap * Math.Sign(NPC.velocity.Y);
        }
        
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (WorldSavingSystem.EternityMode)
            {
                target.AddBuff(ModContent.BuffType<InfestedBuff>(), 360);
                target.AddBuff(ModContent.BuffType<ClippedWingsBuff>(), 180);
            }
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
            //int num156 = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type]; //ypos of lower right corner of sprite to draw
            //int y3 = num156 * NPC.frame.Y; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = NPC.frame;//new Rectangle(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;

            Color color26 = drawColor;
            color26 = NPC.GetAlpha(color26);

            SpriteEffects effects = NPC.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;


            for (int i = 0; i < NPCID.Sets.TrailCacheLength[NPC.type]; i++)
            {
                Color color27 = color26 * 0.5f;
                color27 *= (float)(NPCID.Sets.TrailCacheLength[NPC.type] - i) / NPCID.Sets.TrailCacheLength[NPC.type];
                Vector2 value4 = NPC.oldPos[i];
                float num165 = NPC.rotation; //NPC.oldRot[i];
                Main.EntitySpriteDraw(texture2D13, value4 + NPC.Size / 2f - screenPos + new Vector2(0, NPC.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color27, num165, origin2, NPC.scale, effects, 0);
            }

            if (NPC.ai[0] == 1 || NPC.ai[0] == 4 || NPC.ai[0] == 3)
            {
                Main.spriteBatch.UseBlendState(BlendState.Additive);
                for (int j = 0; j < 12; j++)
                {
                    Vector2 afterimageOffset = (MathHelper.TwoPi * j / 12).ToRotationVector2() * 12f * NPC.scale;
                    Color glowColor = Color.Purple;

                    Main.EntitySpriteDraw(texture2D13, NPC.Center + afterimageOffset - screenPos + new Vector2(0f, NPC.gfxOffY), rectangle, glowColor, NPC.rotation, origin2, NPC.scale, effects);
                }
                Main.spriteBatch.ResetToDefault();
            }

            Main.EntitySpriteDraw(texture2D13, NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), NPC.GetAlpha(drawColor), NPC.rotation, origin2, NPC.scale, effects, 0);
            return false;
        }
    }
}
