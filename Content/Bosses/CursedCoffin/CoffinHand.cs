using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Buffs.Boss;
using System.IO;
using FargowiltasSouls.Core.Systems;
using FargowiltasSouls.Content.WorldGeneration;

namespace FargowiltasSouls.Content.Bosses.CursedCoffin
{
    public class CoffinHand : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Banished Baron Scrap");
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            Main.projFrames[Type] = 2;
        }
        public override void SetDefaults()
        {
            Projectile.width = 76;
            Projectile.height = 76;
            Projectile.aiStyle = -1;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.scale = 1f;
            Projectile.light = 1;
            Projectile.timeLeft = 60 * 6;

            Projectile.Opacity = 0.2f;
        }
        public ref float Timer => ref Projectile.localAI[0];
        public ref float RotDir => ref Projectile.localAI[1];
        public ref float State => ref Projectile.ai[1];

        public int TargetPlayer = -1;

        private int CaughtPlayer = -1;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(RotDir);
            writer.Write(Timer);
            writer.Write(TargetPlayer);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            RotDir = reader.ReadSingle();
            Timer = reader.ReadSingle();
            TargetPlayer = reader.ReadInt32();
        }
        
        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            if (target.HasBuff<GrabbedBuff>())
                return;
            target.buffImmune[ModContent.BuffType<CoffinTossBuff>()] = true;
            Projectile.frame = 1;
            Timer = 0;
            if (State == 44)
                State = 66;
            else
                State = 100;
            Projectile.velocity = -Vector2.UnitY * 5;
            Projectile.damage = 0;
            CaughtPlayer = target.whoAmI;
            Projectile.netUpdate = true;
            modifiers.Null();
        }
        public override bool CanHitPlayer(Player target)
        {
            if (State == 100 || State == 101 || State == 1)
                return false;
            return base.CanHitPlayer(target);
        }
        public override void OnKill(int timeLeft)
        {
            if (CaughtPlayer.IsWithinBounds(Main.maxPlayers))
            {
                Player victim = Main.player[CaughtPlayer];
                if (victim.Alive())
                {
                    victim.fullRotation = 0;
                }
            }
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0)
            {
                Projectile.scale = 0.2f;
                Projectile.localAI[0] = 1;
            }
            if (State < 9 && State != 1)
            {
                RotDir = State;
                State = 1;
                Projectile.netUpdate = true;
            }
                
            if (State != 1 && State != 10)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Projectile.Opacity = (float)Utils.Lerp(Projectile.Opacity, 1, 0.1f);
            Projectile.scale = (float)Utils.Lerp(Projectile.scale, 1f, 0.1f);

            NPC owner = Main.npc[(int)Projectile.ai[0]];
            if (!owner.TypeAlive<CursedCoffin>())
            {
                Projectile.Kill();
                return;
            }
            CursedCoffin coffin = owner.As<CursedCoffin>();
            Entity target = Main.player[owner.target];
            if (State == 22 || State == 44)
            {
                TargetPlayer = (int)Projectile.ai[2];
            }
            if (TargetPlayer.IsWithinBounds(Main.maxPlayers))
            {
                target = Main.player[TargetPlayer];
            }
            if (target == null || !target.active)
                return;

            switch (State) // current state
            {
                case 1: // normal grabby hand, circling player
                    {
                        const float RotationSpeed = MathF.Tau * 0.005f;
                        Vector2 offset = target.SafeDirectionTo(Projectile.Center);

                        offset = offset.RotatedBy(RotDir * RotationSpeed) * 350;

                        Vector2 desiredPos = target.Center + offset;
                        Movement(desiredPos, 0.2f, 30, 5, 0.2f, 15);

                        Projectile.rotation = Projectile.SafeDirectionTo(target.Center).ToRotation() + MathHelper.PiOver2;

                        if (!coffin.StateMachine.StateStack.Any() || coffin.StateMachine.CurrentState.Identifier != CursedCoffin.BehaviorStates.GrabbyHands)
                            Projectile.Kill();
                    }
                    break;  
                case 10:
                    {
                        float divisor = WorldSavingSystem.MasochistModeReal ? 2f : 3f;
                        float speed = (Timer - 25) / divisor;
                        const int cap = 24;
                        if (speed > cap)
                            speed = cap;
                        Projectile.velocity = (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * speed;
                        if (Timer > 120)
                            Projectile.Kill();
                        Timer++;
                    }
                    break;
                case 22: //stun punish grab
                    {
                        if (Projectile.velocity.Length() < 30f)
                            Projectile.velocity *= 1.08f;
                        if (WorldSavingSystem.EternityMode && ++Timer < 60)
                            Projectile.velocity = Projectile.velocity.RotateTowards(Projectile.DirectionTo(target.Center).ToRotation(), 0.01f);
                    }
                    break;
                case 44: // YOU CAN'T ESCAPE
                    {
                        Vector2 vectorToIdlePosition = target.Center - Projectile.Center;
                        float speed = 28f;
                        float inertia = 10f;
                        vectorToIdlePosition.Normalize();
                        vectorToIdlePosition *= speed;
                        Projectile.velocity = (Projectile.velocity * (inertia - 1f) + vectorToIdlePosition) / inertia;
                        if (Projectile.velocity == Vector2.Zero)
                        {
                            Projectile.velocity.X = -0.15f;
                            Projectile.velocity.Y = -0.05f;
                        }
                        
                    }
                    break;
                
                case 66: // pull back into arena
                case 100: //grabbed player, toss
                    {
                        if (!CaughtPlayer.IsWithinBounds(Main.maxPlayers))
                        {
                            State = 101;
                            break;
                        }
                        Player victim = Main.player[CaughtPlayer];
                        if (!victim.Alive())
                        {
                            State = 101;
                            break;
                        }
                        victim.buffImmune[ModContent.BuffType<StunnedBuff>()] = true; // cannot be stunned while grabbed, and removes stun

                        if (Timer >= 25 && State != 66)
                        {
                            State = 101;
                            owner.netUpdate = true;
                            victim.AddBuff(ModContent.BuffType<CoffinTossBuff>(), 100);
                            Vector2 arenaCenter = CoffinArena.FightCenter;
                            int sign = Math.Sign(arenaCenter.DirectionTo(target.Center).X);
                            if (sign != 1 && sign != -1)
                                sign = Main.rand.NextBool() ? 1 : -1;
                            victim.velocity = Vector2.UnitX * sign * 30;
                            coffin.MashTimer = 15; // reset mash cap
                            break;
                        }
                        else
                        {
                            int mashCap = coffin.MashTimer;
                            if (WorldSavingSystem.MasochistModeReal) // practically inescapable on maso
                                mashCap += 666;

                            Vector2 arenaCenter = CoffinArena.FightCenter;
                            bool releaseAtCenter = State == 66 && Projectile.Distance(arenaCenter) < 100;

                            if (victim.Alive() && (Projectile.Distance(victim.Center) < 160 || victim.whoAmI != Main.myPlayer) && victim.FargoSouls().MashCounter < mashCap && !releaseAtCenter)
                            {
                                victim.AddBuff(ModContent.BuffType<StunnedBuff>(), 2);
                                victim.Center = Projectile.Center;
                                victim.fullRotation = Projectile.DirectionFrom(owner.Center).ToRotation() + MathHelper.PiOver2;
                                victim.fullRotationOrigin = victim.Center - victim.position;
                                if (State == 66)
                                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(arenaCenter) * 15, 0.3f);
                                else
                                    Projectile.velocity *= 0.96f;
                            }
                            else // escaped
                            {
                                CaughtPlayer = -1;
                                State = 101; //cooldown
                                victim.fullRotation = 0;
                                Projectile.netUpdate = true;

                                coffin.MashTimer += 7; // increment mash cap, each successful mash makes the next one harder
                                owner.netUpdate = true;

                                if (Main.netMode == NetmodeID.Server)
                                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, Projectile.whoAmI);
                            }
                        }
                        
                        Timer++;
                    }
                    break;
                case 101:
                    {
                        Projectile.velocity = Vector2.Zero;
                        Projectile.scale -= 0.05f;
                        Projectile.Opacity -= 0.1f;
                        if (Projectile.Opacity < 0.1f)
                            Projectile.Kill();
                    }
                    break;
            }
        }
        private static readonly Color GlowColor = Color.Purple with { A = 0 }; //new(224, 196, 252, 0);
        public override bool PreDraw(ref Color lightColor)
        {
            //draw projectile
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;
            Vector2 drawOffset = Projectile.rotation.ToRotationVector2() * (texture2D13.Width - Projectile.width) / 2;

            SpriteEffects effects = Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Color color27 = Projectile.GetAlpha(GlowColor);
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i];
                float num165 = Projectile.oldRot[i];
                Main.EntitySpriteDraw(texture2D13, value4 + drawOffset + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Rectangle?(rectangle), color27, num165, origin2, Projectile.scale, effects, 0);
            }
            Main.EntitySpriteDraw(texture2D13, Projectile.Center + drawOffset - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Rectangle?(rectangle), Projectile.GetAlpha(lightColor), Projectile.rotation, origin2, Projectile.scale, effects, 0);
            return false;
        }

        void Movement(Vector2 pos, float accel = 0.03f, float maxSpeed = 20, float lowspeed = 5, float decel = 0.03f, float slowdown = 30)
        {
            if (Projectile.Distance(pos) > slowdown)
            {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, (pos - Projectile.Center).SafeNormalize(Vector2.Zero) * maxSpeed, accel);
            }
            else
            {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, (pos - Projectile.Center).SafeNormalize(Vector2.Zero) * lowspeed, decel);
            }
        }
    }
}
