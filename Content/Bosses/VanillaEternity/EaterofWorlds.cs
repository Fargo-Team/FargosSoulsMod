using Fargowiltas.Common.Configs;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.NPCs.EternityModeNPCs.VanillaEnemies.Corruption;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.NPCMatching;
using FargowiltasSouls.Core.Systems;
using Humanizer;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace FargowiltasSouls.Content.Bosses.VanillaEternity
{
    public class EaterofWorlds : EModeNPCBehaviour
    {
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchTypeRange(NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail);

        int MassDefenseTimer;
        bool UseMassDefense;

        public override void OnFirstTick(NPC npc)
        {
            base.OnFirstTick(npc);

            npc.buffImmune[BuffID.CursedInferno] = true;
        }

        public override bool SafePreAI(NPC npc)
        {
            if (--MassDefenseTimer < 0)
            {
                MassDefenseTimer = 15;

                //only apply to head and the segment immediately before it
                if (npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody && FargoSoulsUtil.NPCExists(npc.ai[1], NPCID.EaterofWorldsHead) != null)
                {
                    npc.defense = npc.defDefense;
                    UseMassDefense = false;

                    int totalCount = Main.npc.Count(n => n.active && (n.type == NPCID.EaterofWorldsBody || n.type == NPCID.EaterofWorldsHead || n.type == NPCID.EaterofWorldsTail));
                    int headCount = NPC.CountNPCS(NPCID.EaterofWorldsHead);
                    if (totalCount > 12 && headCount < totalCount / 5 + 1)
                    {
                        UseMassDefense = true;
                        npc.defense += 30;

                        if (npc.life < npc.lifeMax / 2)
                            npc.life += 2;
                    }
                }
            }

            return base.SafePreAI(npc);
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (UseMassDefense)
                // TODO: maybe use defense for this?
                modifiers.FinalDamage /= 2;

            base.ModifyIncomingHit(npc, ref modifiers);
        }


        public override bool CheckDead(NPC npc)
        {

            int count = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && i != npc.whoAmI && (Main.npc[i].type == NPCID.EaterofWorldsHead || Main.npc[i].type == NPCID.EaterofWorldsBody || Main.npc[i].type == NPCID.EaterofWorldsTail))
                    count++;
            }

            if (count > 2)
                return false;

            return base.CheckDead(npc);
        }
        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (npc.lifeRegen >= 0)
                return;
            npc.lifeRegen /= 3;
            damage /= 3;
            if (UseMassDefense)
            {
                damage /= 10;
                npc.lifeRegen /= 10;
            }
        }

        public override void SafeModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            base.SafeModifyHitByItem(npc, player, item, ref modifiers);

            if (EaterofWorldsHead.HaveSpawnDR > 0)
                modifiers.FinalDamage /= 10;
        }

        public override void SafeModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            base.SafeModifyHitByProjectile(npc, projectile, ref modifiers);

            if (EaterofWorldsHead.HaveSpawnDR > 0)
                modifiers.FinalDamage /= projectile.numHits + 1;

            if (projectile.FargoSouls().IsAHeldProj)
                modifiers.FinalDamage *= 0.6f;
        }

        public override void SafeOnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (!FargoSoulsUtil.IsSummonDamage(projectile) && projectile.damage > 5 && !projectile.FargoSouls().IsAHeldProj)
                projectile.damage = (int)Math.Min(projectile.damage - 1, projectile.damage * 0.8);

            base.SafeOnHitByProjectile(npc, projectile, hit, damageDone);
        }

        public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
        {
            base.OnHitPlayer(npc, target, hurtInfo);

            target.AddBuff(BuffID.CursedInferno, 180);
            target.AddBuff(ModContent.BuffType<RottingBuff>(), 600);
        }

        public override void LoadSprites(NPC npc, bool recolor)
        {
            base.LoadSprites(npc, recolor);

            LoadNPCSprite(recolor, npc.type);
        }
    }

    public class EaterofWorldsHead : EModeNPCBehaviour
    {
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchType(NPCID.EaterofWorldsHead);

        public int FlamethrowerCDOrUTurnStoredTargetX;

        public int SpecialAITimer;
        public int SpecialAITimer2;
        public static int SpecialCountdownTimer;

        public int UTurnTotalSpacingDistance;
        public int UTurnIndividualSpacingPosition;

        public Vector2 CoilCenter;
        public int CoilSpinDirection;
        public float CoilDesiredRotation;

        public const int CoilDiveTime = 60 * 30; // never reached naturally; set to heads that are designated to dive
        public static int CoilRadius => WorldSavingSystem.MasochistModeReal ? 500 : 600;
        public bool Coiling => Attack == (int)Attacks.Coil && SpecialAITimer < CoilDiveTime;

        public int VileSpitLockoutDuration;

        public static int CursedFlameTimer;
        public static int HaveSpawnDR;

        public int Attack;
        public static bool DoTheWave;

        public bool DroppedSummon;

        public int NoSelfDestructTimer = 15;

        public float CoilBorderOpacity = 0f;

        public enum Attacks
        {
            Normal,
            NormalPostCoil,
            UTurn,
            Coil
        }
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            base.SendExtraAI(npc, bitWriter, binaryWriter);

            binaryWriter.Write7BitEncodedInt(FlamethrowerCDOrUTurnStoredTargetX);
            binaryWriter.Write7BitEncodedInt(UTurnTotalSpacingDistance);
            binaryWriter.Write7BitEncodedInt(UTurnIndividualSpacingPosition);
            binaryWriter.Write7BitEncodedInt(SpecialAITimer);
            binaryWriter.Write7BitEncodedInt(SpecialAITimer2);
            binaryWriter.Write7BitEncodedInt(SpecialCountdownTimer);
            binaryWriter.Write7BitEncodedInt(CursedFlameTimer);
            binaryWriter.Write7BitEncodedInt(Attack);
            binaryWriter.Write7BitEncodedInt(CoilSpinDirection);
            binaryWriter.Write7BitEncodedInt(VileSpitLockoutDuration);
            binaryWriter.Write(CoilDesiredRotation);
            binaryWriter.WriteVector2(CoilCenter);
            bitWriter.WriteBit(DoTheWave);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            base.ReceiveExtraAI(npc, bitReader, binaryReader);

            FlamethrowerCDOrUTurnStoredTargetX = binaryReader.Read7BitEncodedInt();
            UTurnTotalSpacingDistance = binaryReader.Read7BitEncodedInt();
            UTurnIndividualSpacingPosition = binaryReader.Read7BitEncodedInt();
            SpecialAITimer = binaryReader.Read7BitEncodedInt();
            SpecialAITimer2 = binaryReader.Read7BitEncodedInt();
            SpecialCountdownTimer = binaryReader.Read7BitEncodedInt();
            CursedFlameTimer = binaryReader.Read7BitEncodedInt();
            Attack = binaryReader.Read7BitEncodedInt();
            CoilSpinDirection = binaryReader.Read7BitEncodedInt();
            VileSpitLockoutDuration = binaryReader.Read7BitEncodedInt();
            CoilDesiredRotation = binaryReader.ReadSingle();
            CoilCenter = binaryReader.ReadVector2();
            DoTheWave = bitReader.ReadBit();
        }

        public override void SetDefaults(NPC npc)
        {
            base.SetDefaults(npc);

            npc.damage = (int)(npc.damage * 4.0 / 3.0);
        }

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (Main.getGoodWorld)
                cooldownSlot = ImmunityCooldownID.Bosses;
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }

        public override bool SafePreAI(NPC npc)
        {
            EModeGlobalNPC.eaterBoss = npc.whoAmI;
            FargoSoulsGlobalNPC.boss = npc.whoAmI;
            if (!npc.HasValidTarget)
            {
                npc.TargetClosest(false);
            }

            if (!npc.HasValidTarget || npc.Distance(Main.player[npc.target].Center) > 6000)
            {
                npc.velocity.Y += 0.25f;
                if (npc.timeLeft > 120)
                    npc.timeLeft = 120;
            }

            if (npc.HasValidTarget && npc.Distance(Main.player[npc.target].Center) > 2500)
            {
                Vector2 dir = npc.DirectionTo(Main.player[npc.target].Center);
                if (npc.velocity.Length() < 25)
                    npc.velocity += dir * 1f;
                npc.velocity = npc.velocity.RotateTowards(dir.ToRotation(), 0.1f);
            }

            //if (eaterResist > 0 && npc.whoAmI == NPC.FindFirstNPC(npc.type)) eaterResist--;

            if (VileSpitLockoutDuration > 0)
                VileSpitLockoutDuration--;

            int firstEater = NPC.FindFirstNPC(npc.type);

            if (npc.whoAmI == firstEater)
            {
                SpecialCountdownTimer++;
                if (HaveSpawnDR > 0)
                    HaveSpawnDR--;
            }

            if (FargoSoulsUtil.HostCheck && npc.whoAmI == firstEater && ++CursedFlameTimer > 300) //only let one eater increment this
            {
                bool shoot = true;
                for (int i = 0; i < Main.maxNPCs; i++) //cancel if anyone is doing an attack
                {
                    if (Main.npc[i].active && Main.npc[i].type == npc.type && Main.npc[i].GetGlobalNPC<EaterofWorldsHead>().Attack != (int)Attacks.Normal && Main.npc[i].GetGlobalNPC<EaterofWorldsHead>().Attack != (int)Attacks.NormalPostCoil)
                    {
                        if (!WorldSavingSystem.MasochistModeReal || Main.npc[i].GetGlobalNPC<EaterofWorldsHead>().Attack == (int)Attacks.Coil)
                        {
                            shoot = false;
                            CursedFlameTimer -= 30;
                        }
                    }
                }

                if (shoot)
                {
                    CursedFlameTimer = 0;

                    int minimumToShoot = WorldSavingSystem.MasochistModeReal ? 18 : 6;

                    int counter = 0;
                    int delay = 0;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].active)
                        {
                            /*if (Main.npc[i].type == npc.type && !Main.npc[i].GetGlobalNPC<EaterofWorldsHead>().masobool0)
                            {
                                Main.npc[i].GetGlobalNPC<EaterofWorldsHead>().counter2 = 0; //stop others from triggering it
                            }
                            else */
                            if (Main.npc[i].type == NPCID.EaterofWorldsHead || Main.npc[i].type == NPCID.EaterofWorldsBody || Main.npc[i].type == NPCID.EaterofWorldsTail)
                            {
                                if (++counter > (WorldSavingSystem.MasochistModeReal ? 2 : 6)) //wave of redirecting flames
                                {
                                    counter = 0;

                                    minimumToShoot--;

                                    Vector2 vel = (Main.player[npc.target].Center - Main.npc[i].Center) / 45;
                                    Projectile.NewProjectile(npc.GetSource_FromThis(), Main.npc[i].Center, vel,
                                        ModContent.ProjectileType<CursedFireballHoming>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 0.8f), 0f, Main.myPlayer, npc.target, delay);

                                    delay += WorldSavingSystem.MasochistModeReal ? 4 : 10;
                                }
                            }
                        }
                    }

                    for (int i = 0; i < minimumToShoot; i++)
                    {
                        Vector2 vel = (Main.player[npc.target].Center - npc.Center) / 45;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel,
                            ModContent.ProjectileType<CursedFireballHoming>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 0.8f), 0f, Main.myPlayer, npc.target, delay);
                        delay += WorldSavingSystem.MasochistModeReal ? 4 : 8;
                    }
                }
            }

            if (NoSelfDestructTimer <= 0)
            {
                if (FargoSoulsUtil.HostCheck && SpecialCountdownTimer % 6 == 3) //chose this number at random to avoid edge case
                {
                    //die if segment behind me is invalid
                    int ai0 = (int)npc.ai[0];
                    if (!(ai0 > -1 && ai0 < Main.maxNPCs && Main.npc[ai0].active && Main.npc[ai0].ai[1] == npc.whoAmI
                        && (Main.npc[ai0].type == NPCID.EaterofWorldsBody || Main.npc[ai0].type == NPCID.EaterofWorldsTail)))
                    {
                        //Main.NewText("ai0 npc invalid");
                        npc.life = 0;
                        npc.HitEffect();
                        npc.checkDead();
                        npc.active = false;
                        npc.netUpdate = false;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                        return false;
                    }
                }
            }
            else
            {
                NoSelfDestructTimer--;
            }
            switch ((Attacks)Attack)
            {
                case Attacks.UTurn:
                    //flying u-turn ai
                    {
                        if (++SpecialAITimer < 120)
                        {
                            Vector2 target = Main.player[npc.target].Center;
                            if (UTurnTotalSpacingDistance != 0)
                                target.X += 900f / UTurnTotalSpacingDistance * UTurnIndividualSpacingPosition; //space out
                            target.Y += 600f;

                            float speedModifier = 0.6f;
                            float speedCap = 24;
                            if (npc.Top.Y > Main.player[npc.target].Bottom.Y + npc.height)
                            {
                                speedModifier *= 1.5f;
                                speedCap *= 1.5f;
                                npc.position += (Main.player[npc.target].position - Main.player[npc.target].oldPosition) / 2;
                            }

                            if (npc.Center.X < target.X)
                            {
                                npc.velocity.X += speedModifier;
                                if (npc.velocity.X < 0)
                                    npc.velocity.X += speedModifier * 2;
                            }
                            else
                            {
                                npc.velocity.X -= speedModifier;
                                if (npc.velocity.X > 0)
                                    npc.velocity.X -= speedModifier * 2;
                            }
                            if (npc.Center.Y < target.Y)
                            {
                                npc.velocity.Y += speedModifier;
                                if (npc.velocity.Y < 0)
                                    npc.velocity.Y += speedModifier * 2;
                            }
                            else
                            {
                                npc.velocity.Y -= speedModifier;
                                if (npc.velocity.Y > 0)
                                    npc.velocity.Y -= speedModifier * 2;
                            }

                            if (Math.Abs(npc.velocity.X) > speedCap)
                                npc.velocity.X = speedCap * Math.Sign(npc.velocity.X);
                            if (Math.Abs(npc.velocity.Y) > speedCap)
                                npc.velocity.Y = speedCap * Math.Sign(npc.velocity.Y);

                            npc.localAI[0] = 1f;

                            if (Main.netMode == NetmodeID.Server && --npc.netSpam < 0) //manual mp sync control
                            {
                                npc.netSpam = 5;
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                            }
                        }
                        else if (SpecialAITimer == 120) //fly up
                        {
                            SoundEngine.PlaySound(SoundID.Roar, Main.player[npc.target].Center);
                            npc.velocity = Vector2.UnitY * -15f;
                            FlamethrowerCDOrUTurnStoredTargetX = (int)Main.player[npc.target].Center.X; //store their initial location

                            npc.netUpdate = true;
                        }
                        else if (SpecialAITimer < 240) //cancel early and turn once we fly past player
                        {
                            if (npc.Center.Y < Main.player[npc.target].Center.Y - (WorldSavingSystem.MasochistModeReal ? 200 : 450))
                                SpecialAITimer = 239;
                        }
                        else if (SpecialAITimer == 240) //recalculate velocity to u-turn and dive back down in the same spacing over player
                        {
                            Vector2 target;
                            target.X = Main.player[npc.target].Center.X;
                            if (UTurnTotalSpacingDistance != 0)
                                target.X += 900f / UTurnTotalSpacingDistance * UTurnIndividualSpacingPosition; //space out
                            target.Y = npc.Center.Y;

                            float radius = Math.Abs(target.X - npc.Center.X) / 2;
                            float speed = MathHelper.Pi * radius / 30;
                            if (speed < 8f)
                                speed = 8f;
                            npc.velocity = Vector2.Normalize(npc.velocity) * speed;

                            FlamethrowerCDOrUTurnStoredTargetX = Math.Sign(Main.player[npc.target].Center.X - FlamethrowerCDOrUTurnStoredTargetX); //which side player moved to from original pos

                            npc.netUpdate = true;
                        }
                        else if (SpecialAITimer < 270) //u-turn
                        {
                            npc.velocity = npc.velocity.RotatedBy(MathHelper.ToRadians(6f) * FlamethrowerCDOrUTurnStoredTargetX);
                        }
                        else if (SpecialAITimer == 270)
                        {
                            npc.velocity = Vector2.Normalize(npc.velocity) * 15f;
                            npc.netUpdate = true;
                        }
                        else if (SpecialAITimer > 300)
                        {
                            SpecialAITimer = 0;
                            SpecialCountdownTimer = 0;
                            UTurnTotalSpacingDistance = 0;
                            UTurnIndividualSpacingPosition = 0;
                            Attack = (int)Attacks.Normal;

                            //for (int i = 0; i < Main.maxNPCs; i++)
                            //{
                            //    if (Main.npc[i].active)
                            //    {
                            //        if (Main.npc[i].type == npc.type)
                            //        {
                            //            Main.npc[i].GetGlobalNPC<EaterofWorldsHead>().UTurnTotalSpacingDistance = 0;
                            //            Main.npc[i].GetGlobalNPC<EaterofWorldsHead>().UTurnIndividualSpacingPosition = 0;
                            //            Main.npc[i].GetGlobalNPC<EaterofWorldsHead>().UTurn = false;
                            //            Main.npc[i].netUpdate = true;
                            //            if (Main.netMode == NetmodeID.Server)
                            //                NetSync(npc);
                            //        }
                            //        else if (Main.npc[i].type == NPCID.EaterofWorldsBody || Main.npc[i].type == NPCID.EaterofWorldsTail)
                            //        {
                            //            Main.npc[i].netUpdate = true;
                            //        }
                            //    }
                            //}

                            npc.netUpdate = true;
                        }

                        npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + 1.57f;

                        if (npc.netUpdate)
                        {
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                                NetSync(npc);
                            }
                            npc.netUpdate = false;
                        }
                        return false;
                    }
                    break;
                case Attacks.Coil:
                    {

                        int diveDelay = WorldSavingSystem.MasochistModeReal ? 60 : 80; // time between dives
                        float spinSeconds = 3f; // seconds per full spin

                        float spinFrames = spinSeconds * 60f;

                        SpecialCountdownTimer = 350;
                        if (!Main.npc.Any(n => n.TypeAlive(npc.type) && n.GetGlobalNPC<EaterofWorldsHead>().Coiling && n.Distance(CoilCenter) > CoilRadius + 100))
                        {
                            SpecialAITimer++;
                            SpecialAITimer2++;
                        }
                            
                        if (firstEater == npc.whoAmI)
                        {
                            if (firstEater == npc.whoAmI)
                            {
                                for (int i = 0; i < 20; i++) //arena dust
                                {
                                    Vector2 offset = new();
                                    double angle = Main.rand.NextDouble() * 2d * Math.PI;
                                    offset.X += (float)(Math.Sin(angle) * CoilRadius);
                                    offset.Y += (float)(Math.Cos(angle) * CoilRadius);
                                    Dust dust = Main.dust[Dust.NewDust(CoilCenter + offset - new Vector2(4, 4), 0, 0, DustID.Corruption, 0, 0, 100, Color.White, 1f)];
                                    dust.velocity = Vector2.Zero;
                                    if (Main.rand.NextBool(3))
                                        dust.velocity += Vector2.Normalize(offset) * 5f;
                                    dust.noGravity = true;
                                }

                                Player target = Main.player[npc.target];
                                if (target.active && !target.dead) //arena effect
                                {
                                    float distance = target.Distance(CoilCenter);
                                    if (distance > CoilRadius && distance < 3000)
                                    {
                                        Vector2 movement = CoilCenter - target.Center;
                                        float difference = movement.Length() - CoilRadius;
                                        movement.Normalize();
                                        movement *= difference < 34f ? difference : 34f;
                                        target.position += movement;

                                        for (int i = 0; i < 20; i++)
                                        {
                                            int d = Dust.NewDust(target.position, target.width, target.height, DustID.Corruption, 0f, 0f, 0, default, 2f);
                                            Main.dust[d].noGravity = true;
                                            Main.dust[d].velocity *= 5f;
                                        }
                                    }
                                }
                            }
                            int diveTimer = SpecialAITimer2 + 65;
                            if (diveTimer > diveDelay && diveTimer % diveDelay == 0)
                            {
                                List<NPC> coilingHeads = [];
                                for (int i = 0; i < Main.maxNPCs; i++)
                                {
                                    if (Main.npc[i].TypeAlive(npc.type) && Main.npc[i].GetGlobalNPC<EaterofWorldsHead>().Coiling)
                                    {
                                        coilingHeads.Add(Main.npc[i]);
                                    }
                                }
                                if (coilingHeads.Count != 0)
                                {
                                    NPC diveNPC = Main.rand.NextFromCollection(coilingHeads);
                                    if (diveNPC.HasPlayerTarget)
                                    {
                                        diveNPC.velocity *= -1;
                                        diveNPC.velocity += Main.player[diveNPC.target].DirectionTo(diveNPC.Center) * 22f;
                                        diveNPC.GetGlobalNPC<EaterofWorldsHead>().SpecialAITimer = CoilDiveTime;
                                        SoundEngine.PlaySound(SoundID.ForceRoarPitched, diveNPC.Center);
                                        diveNPC.netUpdate = true;
                                        NetSync(diveNPC);
                                    }
                                }
                                
                            }
                        }
                        if (Coiling)
                        {
                            Vector2 desiredPosition = CoilCenter + CoilCenter.DirectionTo(npc.Center).RotatedBy(CoilSpinDirection * MathHelper.PiOver2 / 8f) * CoilRadius;

                            // calculate speed
                            float circumference = MathHelper.TwoPi * CoilRadius;
                            float baseSpeed = circumference / spinFrames;
                            float speed = baseSpeed;

                            float rotationDifference = FargoSoulsUtil.RotationDifference(CoilCenter.DirectionTo(npc.Center), CoilDesiredRotation.ToRotationVector2());
                            speed += (baseSpeed * 0.9f) * rotationDifference / MathHelper.Pi;
                            Vector2 desiredVelocity = npc.DirectionTo(desiredPosition) * speed;
                            npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, 0.2f);

                            CoilDesiredRotation += CoilSpinDirection * MathHelper.TwoPi / spinFrames;

                            VileSpitLockoutDuration = 100;
                        }
                        else
                        {
                            if (SpecialAITimer > CoilDiveTime + 80)
                            {
                                if (!Main.npc.Any(n => n.TypeAlive(NPCID.EaterofWorldsHead) && n.GetGlobalNPC<EaterofWorldsHead>().Coiling))
                                {
                                    SpecialAITimer = 0;
                                    SpecialAITimer2 = 0;
                                    SpecialCountdownTimer = 0;
                                    CoilDesiredRotation = 0;
                                    Attack = (int)Attacks.NormalPostCoil;
                                }
                                else
                                    return true;
                            }
                            else
                            {
                                if (npc.HasPlayerTarget)
                                {
                                    Vector2 vectorToIdlePosition = Main.player[npc.target].Center - npc.Center;
                                    float num = vectorToIdlePosition.Length();
                                    float speed = 32f;
                                    float inertia = 32f;
                                    float deadzone = 150f;
                                    if (num > deadzone)
                                    {
                                        vectorToIdlePosition.Normalize();
                                        vectorToIdlePosition *= speed;
                                        npc.velocity = (npc.velocity * (inertia - 1f) + vectorToIdlePosition) / inertia;
                                    }
                                    else if (npc.velocity == Vector2.Zero)
                                    {
                                        npc.velocity.X = -0.15f;
                                        npc.velocity.Y = -0.05f;
                                    }
                                    if (num < deadzone)
                                    {
                                        SpecialAITimer = CoilDiveTime + 80;
                                    }
                                }
                            }
                        }

                        npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + 1.57f;
                        if (npc.netUpdate)
                        {
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                                NetSync(npc);
                            }
                            npc.netUpdate = false;
                        }
                        return false;
                    }
                    break;
                default:
                    {
                        if (++FlamethrowerCDOrUTurnStoredTargetX >= 6)
                        {
                            FlamethrowerCDOrUTurnStoredTargetX = 0;
                            if (WorldSavingSystem.MasochistModeReal && FargoSoulsUtil.HostCheck) //cursed flamethrower, roughly same direction as head
                            {
                                Vector2 velocity = new Vector2(5f, 0f).RotatedBy(npc.rotation - Math.PI / 2.0 + MathHelper.ToRadians(Main.rand.Next(-15, 16)));
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, velocity, ProjectileID.EyeFire, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 0.8f), 0f, Main.myPlayer);
                            }
                        }

                        if (npc.whoAmI == firstEater)
                        {
                            //faster countup in maso
                            if (SpecialCountdownTimer < 700 - 900 - 6 && WorldSavingSystem.MasochistModeReal)
                                SpecialCountdownTimer++;

                            // coil
                            int headCount = NPC.CountNPCS(npc.type);
                            if (headCount > 1) // only do coil when it's split at least once
                            {
                                //initiate coil
                                if (SpecialCountdownTimer > 350 && Attack == (int)Attacks.Normal)
                                {
                                    SoundEngine.PlaySound(SoundID.ForceRoarPitched, Main.player[npc.target].Center);
                                    if (FargoSoulsUtil.HostCheck && npc.HasValidTarget && npc.Distance(Main.player[npc.target].Center) < 2400)
                                    {
                                        FargoSoulsUtil.ClearHostileProjectiles(2);

                                        Attack = (int)Attacks.Coil;
                                        int headCounter = 0; //determine position of this head in the group
                                        int spinDirection = Main.rand.NextBool() ? 1 : -1;
                                        Player player = Main.player[npc.target];
                                        for (int i = 0; i < Main.maxNPCs; i++) //synchronize
                                        {
                                            if (Main.npc[i].active && Main.npc[i].type == npc.type)
                                            {
                                                EaterofWorldsHead gNPC = Main.npc[i].GetGlobalNPC<EaterofWorldsHead>();
                                                gNPC.Attack = (int)Attacks.Coil;
                                                gNPC.CoilDesiredRotation = headCounter * MathHelper.TwoPi / headCount;
                                                gNPC.CoilDesiredRotation += Main.player[npc.target].DirectionTo(npc.Center).ToRotation();
                                                gNPC.CoilSpinDirection = spinDirection;
                                                Vector2 offset = player.velocity * 20;
                                                gNPC.CoilCenter = player.Center + offset.ClampLength(0, CoilRadius / 2);

                                                Main.npc[i].netUpdate = true;
                                                NetSync(Main.npc[i]);

                                                headCounter++;
                                            }
                                        }

                                        npc.netUpdate = true;
                                    }
                                }
                            }
                            else
                            {
                                Attack = (int)Attacks.NormalPostCoil;
                            }

                            // u-turn
                            if (SpecialCountdownTimer == 700 - 90) //roar telegraph
                                SoundEngine.PlaySound(SoundID.Roar, Main.player[npc.target].Center);

                            //initiate mass u-turn
                            if (SpecialCountdownTimer > 700 && FargoSoulsUtil.HostCheck) 
                            {
                                SpecialCountdownTimer = 0;
                                if (npc.HasValidTarget && npc.Distance(Main.player[npc.target].Center) < 2400)
                                {
                                    Attack = (int)Attacks.UTurn;
                                    DoTheWave = !DoTheWave;
                                    UTurnTotalSpacingDistance = NPC.CountNPCS(npc.type) / 2;
                                    if (WorldSavingSystem.MasochistModeReal)
                                        UTurnTotalSpacingDistance /= 2;

                                    int headCounter = 0; //determine position of this head in the group
                                    bool actuallyDoTheThing = true;
                                    for (int i = 0; i < Main.maxNPCs; i++) //synchronize
                                    {
                                        if (Main.npc[i].active && Main.npc[i].type == npc.type)
                                        {
                                            //in maso, only have every other head participate in group attacks
                                            if (WorldSavingSystem.MasochistModeReal && i != npc.whoAmI)
                                            {
                                                actuallyDoTheThing = !actuallyDoTheThing;
                                                if (!actuallyDoTheThing)
                                                    continue;
                                            }
                                            EaterofWorldsHead gNPC = Main.npc[i].GetGlobalNPC<EaterofWorldsHead>();
                                            gNPC.SpecialAITimer = DoTheWave && UTurnTotalSpacingDistance != 0 ? headCounter * 90 / UTurnTotalSpacingDistance / 2 - 60 : 0;
                                            if (WorldSavingSystem.MasochistModeReal)
                                                gNPC.SpecialAITimer += 60;
                                            gNPC.UTurnTotalSpacingDistance = UTurnTotalSpacingDistance;
                                            gNPC.UTurnIndividualSpacingPosition = headCounter;
                                            gNPC.Attack = (int)Attacks.UTurn;

                                            Main.npc[i].netUpdate = true;
                                            NetSync(Main.npc[i]);

                                            headCounter *= -1; //alternate 0, 1, -1, 2, -2, 3, -3, etc.
                                            if (headCounter >= 0)
                                                headCounter++;
                                        }
                                    }

                                    npc.netUpdate = true;
                                }
                            }
                        }
                    }
                    break;
            }

            //drop summon
            if (npc.HasPlayerTarget && !DroppedSummon)
            {
                Player player = Main.player[npc.target];

                //eater meme
                if (!player.dead && player.FargoSouls().FreeEaterSummon)
                {
                    player.FargoSouls().FreeEaterSummon = false;

                    if (!NPC.downedBoss2 && FargoSoulsUtil.HostCheck && ModContent.TryFind("Fargowiltas", "WormyFood", out ModItem modItem))
                        Item.NewItem(npc.GetSource_Loot(), player.Hitbox, modItem.Type);

                    DroppedSummon = true;
                    SpecialCountdownTimer = 0;
                    HaveSpawnDR = 180;
                    npc.velocity.Y += 6;
                }
            }

            return true;
        }

        public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
        {
            base.OnHitPlayer(npc, target, hurtInfo);

            if (Main.getGoodWorld)
            {
                target.KillMe(PlayerDeathReason.ByCustomReason(Language.GetTextValue("Mods.FargowiltasSouls.DeathMessage.EOW", target.name)), 999999, 0);
            }
        }

        public override void LoadSprites(NPC npc, bool recolor)
        {
            base.LoadSprites(npc, recolor);

            LoadBossHeadSprite(recolor, 2);
            LoadGoreRange(recolor, 24, 29);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            int firstEater = NPC.FindFirstNPC(npc.type);

            if (npc.whoAmI == firstEater && (Attacks)Attack == Attacks.Coil)
            {
                if (CoilBorderOpacity < 1)
                    CoilBorderOpacity += 0.025f;
            }
            else if (CoilBorderOpacity > 0)
                CoilBorderOpacity -= 0.025f;

            if (CoilBorderOpacity > 0)
            {
                Color darkColor = Color.Magenta;
                Color mediumColor = Color.MediumPurple;
                Color lightColor2 = Color.Lerp(Color.Purple, Color.White, 0.35f);
                float greyLerp = 0.3f;
                darkColor = Color.Lerp(darkColor, Color.SlateGray, greyLerp);
                mediumColor = Color.Lerp(mediumColor, Color.SlateGray, greyLerp);
                lightColor2 = Color.Lerp(lightColor2, Color.SlateGray, greyLerp);

                Vector2 auraPos = CoilCenter;
                float radius = CoilRadius;
                var target = Main.LocalPlayer;
                var blackTile = TextureAssets.MagicPixel;
                var diagonalNoise = FargosTextureRegistry.SmokyNoise;
                if (!blackTile.IsLoaded || !diagonalNoise.IsLoaded)
                    return false;
                var maxOpacity = CoilBorderOpacity;

                ManagedShader borderShader = ShaderManager.GetShader("FargowiltasSouls.GenericInnerAura");
                borderShader.TrySetParameter("colorMult", 7.35f);
                borderShader.TrySetParameter("time", Main.GlobalTimeWrappedHourly);
                borderShader.TrySetParameter("radius", radius);
                borderShader.TrySetParameter("anchorPoint", auraPos);
                borderShader.TrySetParameter("screenPosition", Main.screenPosition);
                borderShader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
                borderShader.TrySetParameter("playerPosition", target.Center);
                borderShader.TrySetParameter("maxOpacity", maxOpacity);
                borderShader.TrySetParameter("darkColor", darkColor.ToVector4());
                borderShader.TrySetParameter("midColor", mediumColor.ToVector4());
                borderShader.TrySetParameter("lightColor", lightColor2.ToVector4());
                borderShader.TrySetParameter("opacityAmp", 1f * CoilBorderOpacity);

                Main.spriteBatch.GraphicsDevice.Textures[1] = diagonalNoise.Value;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, borderShader.WrappedEffect, Main.GameViewMatrix.TransformationMatrix);
                Rectangle rekt = new(Main.screenWidth / 2, Main.screenHeight / 2, Main.screenWidth, Main.screenHeight);
                Main.spriteBatch.Draw(blackTile.Value, rekt, null, default, 0f, blackTile.Value.Size() * 0.5f, 0, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
                
            return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
        }
    }

    public class EaterofWorldsSegment : EModeNPCBehaviour
    {
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchTypeRange(NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail);

        public override void SetDefaults(NPC npc)
        {
            base.SetDefaults(npc);

            npc.damage *= 2;
        }
        public override bool SafePreAI(NPC npc)
        {
            //FargoSoulsUtil.PrintAI(npc);
            NPC head = null;
            int headID = (int)npc.ai[1];
            for (int i = 0; i < 200; i++)
            {
                if (headID.IsWithinBounds(Main.maxNPCs) && Main.npc[headID] is NPC ahead)
                {
                    if (ahead.type == NPCID.EaterofWorldsHead)
                    {
                        break;
                    }
                    headID = (int)ahead.ai[1];
                }
                else
                    break; // how
            }
            if (headID.IsWithinBounds(Main.maxNPCs))
                head = Main.npc[headID];
            if (head != null && head.Alive())
            {
                npc.timeLeft = 60 * 60;
                EaterofWorldsHead headEternity = head.GetGlobalNPC<EaterofWorldsHead>();
                if (headEternity.Coiling && head.HasPlayerTarget)
                {
                    Player player = Main.player[head.target];
                    if (player.Distance(npc.Center) < EaterofWorldsHead.CoilRadius)
                    {
                        npc.Center = Vector2.Lerp(npc.Center, headEternity.CoilCenter + headEternity.CoilCenter.DirectionTo(npc.Center) * EaterofWorldsHead.CoilRadius, 0.75f);
                    }
                }
            }
            return base.SafePreAI(npc);
        }
        public override bool CheckDead(NPC npc)
        {
            //no loot unless every other segment is dead (doesn't apply during swarms - if swarm, die and drop loot normally)
            if (!WorldSavingSystem.SwarmActive && Main.npc.Any(n => n.active && n.whoAmI != npc.whoAmI && (n.type == NPCID.EaterofWorldsBody || n.type == NPCID.EaterofWorldsHead || n.type == NPCID.EaterofWorldsTail)))
            {
                npc.active = false;
                if (npc.DeathSound != null)
                    SoundEngine.PlaySound(npc.DeathSound.Value, npc.Center);
                return false;
            }

            return base.CheckDead(npc);
        }
    }

    public class VileSpitEaterofWorlds : VileSpit
    {
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchType(NPCID.VileSpitEaterOfWorlds);

        public int SuicideCounter;

        public override void SetDefaults(NPC npc)
        {
            base.SetDefaults(npc);

            npc.scale *= 2;

            if (WorldSavingSystem.MasochistModeReal)
                npc.dontTakeDamage = true;
        }

        public override void AI(NPC npc)
        {
            base.AI(npc);

            if (++SuicideCounter > 600 || Main.npc.Any(n => n.TypeAlive(NPCID.EaterofWorldsHead) && n.TryGetGlobalNPC(out EaterofWorldsHead eowHead) && eowHead.VileSpitLockoutDuration > 0))
                npc.SimpleStrikeNPC(int.MaxValue, 0, false, 0, null, false, 0, true);
        }

        public override void OnKill(NPC npc)
        {
            base.OnKill(npc);

            if (WorldSavingSystem.MasochistModeReal && Main.getGoodWorld && FargoSoulsUtil.HostCheck)
            {
                for (int i = 0; i < 8; i++)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.UnitY.RotatedBy(2 * Math.PI / 8 * i) * 2f, ProjectileID.CorruptSpray, 0, 0f, Main.myPlayer, 8f);
            }
        }
    }
}
