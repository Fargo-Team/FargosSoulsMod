using Fargowiltas.Common.Configs;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Common.Utilities;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.Projectiles.Deathrays;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.NPCMatching;
using FargowiltasSouls.Core.Systems;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Color = Microsoft.Xna.Framework.Color;

namespace FargowiltasSouls.Content.Bosses.VanillaEternity
{
    public class WallofFlesh : EModeNPCBehaviour
    {
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchType(NPCID.WallofFlesh);

        public int WorldEvilAttackCycleTimer = 600;
        public int ChainBarrageTimer;
        public int TongueTimer;

        public bool UseCorruptAttack;
        public bool InPhase2;
        public bool InPhase3;
        public bool InDesperationPhase;
        public bool MadeEyeInvul;

        public bool DroppedSummon;

        public bool DidGrowl;

        public Vector2 AuraCenter = Vector2.Zero;



        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            base.SendExtraAI(npc, bitWriter, binaryWriter);

            binaryWriter.Write7BitEncodedInt(WorldEvilAttackCycleTimer);
            binaryWriter.Write7BitEncodedInt(ChainBarrageTimer);
            bitWriter.WriteBit(UseCorruptAttack);
            bitWriter.WriteBit(InPhase2);
            bitWriter.WriteBit(InPhase3);
            bitWriter.WriteBit(InDesperationPhase);
            bitWriter.WriteBit(MadeEyeInvul);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            base.ReceiveExtraAI(npc, bitReader, binaryReader);

            WorldEvilAttackCycleTimer = binaryReader.Read7BitEncodedInt();
            ChainBarrageTimer = binaryReader.Read7BitEncodedInt();
            UseCorruptAttack = bitReader.ReadBit();
            InPhase2 = bitReader.ReadBit();
            InPhase3 = bitReader.ReadBit();
            InDesperationPhase = bitReader.ReadBit();
            MadeEyeInvul = bitReader.ReadBit();
        }

        public override void SetDefaults(NPC npc)
        {
            base.SetDefaults(npc);

            npc.lifeMax = (int)Math.Round(npc.lifeMax * 2f);
            if (Main.masterMode) //master mode is already long enough
                npc.lifeMax = (int)Math.Round(npc.lifeMax * 0.9f);
            npc.defense = 0;
            npc.HitSound = SoundID.NPCHit41;
        }

        public override void OnFirstTick(NPC npc)
        {
            base.OnFirstTick(npc);

            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.OnFire3] = true;

            //DrawAura
        }

        public override bool SafePreAI(NPC npc)
        {
            bool result = base.SafePreAI(npc);

            EModeGlobalNPC.wallBoss = npc.whoAmI;

            if (!MadeEyeInvul && npc.ai[3] == 0f) //when spawned in, make one eye invul
            {
                for (int i = 0; i < Main.maxNPCs; i++) //not in on-spawn because need vanilla ai to spawn eyes first
                {
                    if (Main.npc[i].active && Main.npc[i].type == NPCID.WallofFleshEye && Main.npc[i].realLife == npc.whoAmI)
                    {
                        Main.npc[i].ai[2] = -1f;
                        Main.npc[i].netUpdate = true;

                        //npc.ai[3] = 1f;
                        MadeEyeInvul = true;
                        npc.netUpdate = true;
                        NetSync(npc);
                        break;
                    }
                }
            }

            if (InPhase2) //phase 2
            {
                if (++WorldEvilAttackCycleTimer > 600)
                {
                    WorldEvilAttackCycleTimer = 0;
                    UseCorruptAttack = !UseCorruptAttack;
                    DidGrowl = false;

                    npc.netUpdate = true;
                    NetSync(npc);
                }
                else if (WorldEvilAttackCycleTimer > (InDesperationPhase ? 300 : 600 - 120)) //telegraph for special attacks
                {
                    if (!DidGrowl)
                    {
                        DidGrowl = true;
                        if (!Main.dedServ)
                        {
                            SoundEngine.PlaySound(FargosSoundRegistry.WoFSuck,
                                npc.HasValidTarget && Main.player[npc.target].ZoneUnderworldHeight ? Main.player[npc.target].Center : npc.Center);
                        }
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        int type = !UseCorruptAttack ? 75 : 170; //corruption dust, then crimson dust
                        Color color = !UseCorruptAttack ? new(96, 248, 2) : Color.Gold;
                        int speed = !UseCorruptAttack ? 10 : 8;
                        float scale = !UseCorruptAttack ? 6f : 4f;
                        float speedModifier = !UseCorruptAttack ? 12f : 5f;

                        Vector2 direction = npc.SafeDirectionTo(Main.player[npc.target].Center).RotatedByRandom(MathHelper.Pi / 10);
                        Vector2 vel = speed * direction * Main.rand.NextFloat(0.4f, 0.8f);

                        /*
                        int d = Dust.NewDust(npc.Center + 32f * direction, 0, 0, type, vel.X, vel.Y, 100, Color.White, scale);
                        Main.dust[d].velocity *= speedModifier;
                        Main.dust[d].noGravity = true;
                        */
                        Particle p = new ExpandingBloomParticle(npc.Center + 32f * direction + vel * 50, -vel / 2, color, startScale: Vector2.Zero, endScale: Vector2.One * scale, lifetime: 25);
                        p.Velocity *= 2f;
                        p.Spawn();
                    }
                }
                else if (WorldEvilAttackCycleTimer < 240) //special attacks
                {
                    if (UseCorruptAttack) //cursed inferno attack
                    {
                        if (WorldEvilAttackCycleTimer == 10 && FargoSoulsUtil.HostCheck)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.UnitY, ModContent.ProjectileType<CursedDeathrayWOFS>(), 0, 0f, Main.myPlayer, Math.Sign(npc.velocity.X), npc.whoAmI);
                        }

                        if (WorldEvilAttackCycleTimer % 4 == 0)
                        {
                            float xDistance = (2500f - 1800f * WorldEvilAttackCycleTimer / 240f) * Math.Sign(npc.velocity.X);
                            Vector2 spawnPos = new(npc.Center.X + xDistance, npc.Center.Y);

                            SoundEngine.PlaySound(SoundID.Item34, spawnPos);

                            const int offsetY = 800;
                            const int speed = 14;
                            if (FargoSoulsUtil.HostCheck)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos + Vector2.UnitY * offsetY, Vector2.UnitY * -speed, ModContent.ProjectileType<CursedFlamethrower>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos + Vector2.UnitY * offsetY / 2, Vector2.UnitY * speed, ModContent.ProjectileType<CursedFlamethrower>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos + Vector2.UnitY * -offsetY / 2, Vector2.UnitY * -speed, ModContent.ProjectileType<CursedFlamethrower>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos + Vector2.UnitY * -offsetY, Vector2.UnitY * speed, ModContent.ProjectileType<CursedFlamethrower>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer);
                            }
                        }
                    }
                    else //ichor attack
                    {
                        if (WorldEvilAttackCycleTimer % 8 == 0)
                        {
                            if (FargoSoulsUtil.HostCheck)
                            {
                                int max = WorldSavingSystem.MasochistModeReal ? 12 : 8;
                                int flip = 1;
                                for (int i = 0; i < max; i++)
                                {
                                    if (WorldSavingSystem.MasochistModeReal)
                                        flip *= -1;
                                    Vector2 target = npc.Center;
                                    target.X += Math.Sign(npc.velocity.X) * 1000f * WorldEvilAttackCycleTimer / 240f; //gradually targets further and further
                                    target.X += Main.rand.NextFloat(-100, 100);
                                    target.Y += Main.rand.NextFloat(-450, 450);

                                    const float gravity = 0.5f;
                                    float time = 60f;
                                    Vector2 distance = target - npc.Center;
                                    distance.X /= time;
                                    distance.Y = distance.Y / time - 0.5f * gravity * time;
                                    distance.Y *= flip;

                                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + Vector2.UnitX * Math.Sign(npc.velocity.X) * 32f, distance,
                                        ModContent.ProjectileType<GoldenShowerWOF>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 0.75f), 0f, Main.myPlayer, time, 2f, flip);
                                }
                            }
                        }
                    }
                }
            }
            else if (npc.life < npc.lifeMax * (WorldSavingSystem.MasochistModeReal ? 0.9 : .75)) //enter phase 2
            {
                InPhase2 = true;
                npc.netUpdate = true;
                NetSync(npc);

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(FargosSoundRegistry.WoFScreech,
                        npc.HasValidTarget && Main.player[npc.target].ZoneUnderworldHeight ? Main.player[npc.target].Center : npc.Center);

                    if (Main.LocalPlayer.active)
                        ScreenShakeSystem.StartShake(15, shakeStrengthDissipationIncrement: 15f / 90);
                }
            }

            if (InPhase3)
            {
                if (InDesperationPhase)
                {
                    //ChainBarrageTimer -= 0.5f; //increment faster

                    if (WorldEvilAttackCycleTimer % 4 == 1) //always make sure its even in here
                        WorldEvilAttackCycleTimer--;
                }

                int floor = 240 - (InDesperationPhase ? 120 : 0);
                int ceiling = 600 - 180 - (InDesperationPhase ? 120 : 0);

                if (WorldEvilAttackCycleTimer >= floor && WorldEvilAttackCycleTimer <= ceiling)
                {
                    if (--ChainBarrageTimer < 0)
                    {
                        ChainBarrageTimer = 80;
                        if (npc.HasValidTarget && Main.player[npc.target].ZoneUnderworldHeight)
                        {
                            if (FargoSoulsUtil.HostCheck) //spawn reticles for chain barrages
                            {
                                Vector2 spawnPos = Main.player[npc.target].Center;

                                float offset = 1000f * (ceiling - WorldEvilAttackCycleTimer) / (ceiling - floor); //progress further as attack continues
                                spawnPos.X += Math.Sign(npc.velocity.X) * offset;
                                spawnPos.Y += Main.rand.NextFloat(-100, 100);

                                if (spawnPos.Y / 16 < Main.maxTilesY - 200) //clamp so it stays in hell
                                    spawnPos.Y = (Main.maxTilesY - 200) * 16;
                                if (spawnPos.Y / 16 >= Main.maxTilesY)
                                    spawnPos.Y = Main.maxTilesY * 16 - 16;
                                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<WOFReticle>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage, 4f / 6), 0f, Main.myPlayer);
                            }
                        }
                    }
                }
                else
                {
                    ChainBarrageTimer = 0;
                }
            }
            else if (InPhase2 && npc.life < npc.lifeMax * (WorldSavingSystem.MasochistModeReal ? .8 : .5)) //enter phase 3
            {
                InPhase3 = true;
                npc.netUpdate = true;
                NetSync(npc);

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(FargosSoundRegistry.WoFScreech,
                        npc.HasValidTarget && Main.player[npc.target].ZoneUnderworldHeight ? Main.player[npc.target].Center : npc.Center);

                    if (Main.LocalPlayer.active)
                        ScreenShakeSystem.StartShake(15, shakeStrengthDissipationIncrement: 15f / 90);
                }
            }

            if (npc.life < npc.lifeMax / (WorldSavingSystem.MasochistModeReal ? 4 : 10)) //final phase
            {
                WorldEvilAttackCycleTimer++;

                if (!InDesperationPhase)
                {
                    InDesperationPhase = true;

                    //temporarily stop eyes from attacking during the transition to avoid accidental insta-lasers
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].active && Main.npc[i].type == NPCID.WallofFleshEye && Main.npc[i].realLife == npc.whoAmI)
                        {
                            Main.npc[i].GetGlobalNPC<WallofFleshEye>().PreventAttacks = 60;
                        }
                    }

                    npc.netUpdate = true;
                    NetSync(npc);

                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(FargosSoundRegistry.WoFGrowl with { Volume = 1.5f },
                            npc.HasValidTarget && Main.player[npc.target].ZoneUnderworldHeight ? Main.player[npc.target].Center : npc.Center);

                        if (Main.LocalPlayer.active)
                            ScreenShakeSystem.StartShake(20, shakeStrengthDissipationIncrement: 20f  / 180);
                    }
                }
            }

            float maxSpeed = WorldSavingSystem.MasochistModeReal ? 4f : 3.5f; //don't let wof move faster than this normally
            if (!Main.getGoodWorld)
            {
                if (npc.HasPlayerTarget && (Main.player[npc.target].dead || Vector2.Distance(npc.Center, Main.player[npc.target].Center) > 3000))
                {
                    npc.TargetClosest(true);
                    //if (Main.player[npc.target].dead || Vector2.Distance(npc.Center, Main.player[npc.target].Center) > 3000)
                    //{
                    //    npc.position.X += 60 * Math.Sign(npc.velocity.X); //move faster to despawn
                    //}
                    //else if (Math.Abs(npc.velocity.X) > maxSpeed)
                    //{
                    //    npc.position.X -= (Math.Abs(npc.velocity.X) - maxSpeed) * Math.Sign(npc.velocity.X);
                    //}
                }
                else if (Math.Abs(npc.velocity.X) > maxSpeed)
                {
                    npc.position.X -= (Math.Abs(npc.velocity.X) - maxSpeed) * Math.Sign(npc.velocity.X);
                }
            }

            if (Main.LocalPlayer.active & !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost && Main.LocalPlayer.ZoneUnderworldHeight)
            {
                float velX = npc.velocity.X;
                if (velX > maxSpeed)
                    velX = maxSpeed;
                else if (velX < -maxSpeed)
                    velX = -maxSpeed;


                /*for (int i = 0; i < 10; i++) //dust
                {
                    Vector2 dustPos = new Vector2(2000 * npc.direction, 0f).RotatedBy(Math.PI / 3 * (-0.5 + Main.rand.NextDouble()));
                    int d = Dust.NewDust(npc.Center + dustPos, 0, 0, DustID.Torch);
                    Main.dust[d].scale += 1f;
                    Main.dust[d].velocity.X = velX;
                    Main.dust[d].velocity.Y = npc.velocity.Y;
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                }
                */

                if (++TongueTimer > 15)
                {
                    TongueTimer = 0; //tongue the player if they're 2000-2800 units away
                    if (Math.Abs(2400 - npc.Distance(Main.LocalPlayer.Center)) < 400)
                    {
                        if (!Main.LocalPlayer.tongued)
                            SoundEngine.PlaySound(SoundID.ForceRoarPitched, Main.LocalPlayer.Center); //eoc roar
                        Main.LocalPlayer.AddBuff(BuffID.TheTongue, 10);
                    }
                }
            }

            EModeUtils.DropSummon(npc, "FleshyDoll", Main.hardMode, ref DroppedSummon);
            
            

            return result;
        }

        public override void OnKill(NPC npc)
        {
            base.OnKill(npc);

            if (!WorldSavingSystem.WOFDroppedDeviGift2)
            {
                WorldSavingSystem.WOFDroppedDeviGift2 = true;

                npc.DropItemInstanced(npc.position, npc.Size, ItemID.GreaterHealingPotion, 15);
                Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, ModContent.Find<ModItem>("Fargowiltas", "PylonCleaner").Type);
                Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, ItemID.HallowedSeeds, 30);
            }
        }

        public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
        {
            base.OnHitPlayer(npc, target, hurtInfo);

            target.AddBuff(BuffID.Burning, 300);
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage /= 3;
            base.ModifyIncomingHit(npc, ref modifiers);
        }

        public override void LoadSprites(NPC npc, bool recolor)
        {
            base.LoadSprites(npc, recolor);

            LoadNPCSprite(recolor, npc.type);
            LoadBossHeadSprite(recolor, 22);
            LoadGoreRange(recolor, 132, 142);

            LoadSpecial(recolor, ref TextureAssets.Chain12, ref FargowiltasSouls.TextureBuffer.Chain12, "Chain12");
            LoadSpecial(recolor, ref TextureAssets.Wof, ref FargowiltasSouls.TextureBuffer.Wof, "WallOfFlesh");
        }
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Vector2 auraPosition = npc.Center;
            DrawAura(npc, spriteBatch, auraPosition);
            return true;
        }
        public void DrawAura(NPC npc, SpriteBatch spriteBatch, Vector2 position)
        {
            Vector2 auraPos = npc.Center;
            float radius = 2000;
            var blackTile = TextureAssets.MagicPixel;
            var diagonalNoise = FargosTextureRegistry.WavyNoise;
            if (!blackTile.IsLoaded || !diagonalNoise.IsLoaded)
                return;
            var maxOpacity = npc.Opacity;

            ManagedShader borderShader = ShaderManager.GetShader("FargowiltasSouls.WoFAuraShader");
            borderShader.TrySetParameter("colorMult", 7.35f);
            borderShader.TrySetParameter("time", Main.GlobalTimeWrappedHourly);
            borderShader.TrySetParameter("radius", radius);
            borderShader.TrySetParameter("anchorPoint", auraPos);
            borderShader.TrySetParameter("screenPosition", Main.screenPosition);
            borderShader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
            borderShader.TrySetParameter("maxOpacity", maxOpacity);

            Main.spriteBatch.GraphicsDevice.Textures[1] = diagonalNoise.Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, borderShader.WrappedEffect, Main.GameViewMatrix.TransformationMatrix);
            Rectangle rekt = new(Main.screenWidth / 2, Main.screenHeight / 2, Main.screenWidth, Main.screenHeight);
            Main.spriteBatch.Draw(blackTile.Value, rekt, null, default, 0f, blackTile.Value.Size() * 0.5f, 0, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }

    public class WallofFleshEye : EModeNPCBehaviour
    {
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchType(NPCID.WallofFleshEye);

        public int PreventAttacks;

        public bool RepeatingAI;
        public bool HasTelegraphedNormalLasers;

        public bool TelegraphingLasers;
        public int TelegraphTimer;


        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            base.SendExtraAI(npc, bitWriter, binaryWriter);

            binaryWriter.Write7BitEncodedInt(PreventAttacks);
            bitWriter.WriteBit(RepeatingAI);
            bitWriter.WriteBit(HasTelegraphedNormalLasers);
            bitWriter.WriteBit(TelegraphingLasers);
            binaryWriter.Write7BitEncodedInt(TelegraphTimer);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            base.ReceiveExtraAI(npc, bitReader, binaryReader);

            PreventAttacks = binaryReader.Read7BitEncodedInt();
            RepeatingAI = bitReader.ReadBit();
            HasTelegraphedNormalLasers = bitReader.ReadBit();
            TelegraphingLasers = bitReader.ReadBit();
            TelegraphTimer = binaryReader.Read7BitEncodedInt();

        }

        public override void SetDefaults(NPC npc)
        {
            base.SetDefaults(npc);

            npc.lifeMax = (int)Math.Round(npc.lifeMax * 2.2);
        }

        public override void OnFirstTick(NPC npc)
        {
            base.OnFirstTick(npc);

            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.OnFire3] = true;
            npc.buffImmune[ModContent.BuffType<ClippedWingsBuff>()] = true;
            npc.buffImmune[ModContent.BuffType<LethargicBuff>()] = true;
        }

        public override bool SafePreAI(NPC npc)
        {
            ref float ai_Timer = ref npc.ai[1];
            ref float ai_State = ref npc.ai[2];

            NPC mouth = FargoSoulsUtil.NPCExists(npc.realLife, NPCID.WallofFlesh);
            if (RepeatingAI || mouth == null)
                return true;

            if (PreventAttacks > 0)
                PreventAttacks--;


            float maxTime = 540f;

            if (TelegraphingLasers) //Telegraph imminent lasers with converging sparks
            {
                if (!(HasTelegraphedNormalLasers || Main.netMode == NetmodeID.MultiplayerClient))
                {
                    TelegraphTimer = 0;
                }

                const float TelegraphTime = 645;

                float rot = npc.rotation + (npc.direction > 0 ? 0 : MathHelper.Pi);
                Vector2 direction = rot.ToRotationVector2();
                Vector2 eyeCenter = npc.Center + (npc.width - 52) * Vector2.UnitX.RotatedBy(rot);

                if (TelegraphTimer < npc.localAI[1])
                    TelegraphTimer = (int)npc.localAI[1];

                
                const float TelegraphFraction = 0.2f; // time fraction that actual telegraph lasts for

                float totalProgress = (float)Math.Cos(Math.PI / 2f / TelegraphTime * TelegraphTimer);

                if (totalProgress < TelegraphFraction)
                {
                    float progress = totalProgress / (2 * TelegraphFraction);
                    Color color = new Color(255, 0, 255, 100) * ((1f - progress) / 4 + 0.75f);
                    //float alpha = (int)(255f * progress);
                    int frequency = 2 + (int)Math.Ceiling(progress * 6);
                    if (frequency <= 0)
                        frequency = 1;
                    float coneHalfWidth = MathHelper.PiOver2 * 0.8f * progress;
                    float speed = 6 + (1 - progress) * 6;
                    Vector2 vel = direction.RotatedByRandom(coneHalfWidth);
                    float offsetAmt = 25 + (30 * progress);
                    Vector2 offset = vel * Main.rand.NextFloat(offsetAmt, offsetAmt * 2);
                    vel *= Main.rand.NextFloat(speed, speed + 4);

                    if (TelegraphTimer % frequency == 0)
                    {
                        Particle p = new SparkParticle(eyeCenter + offset, vel, color, Main.rand.NextFloat(1.25f, 2f), 20);
                        p.Spawn();
                    }
                }

                if (--TelegraphTimer <= 0)
                {
                    TelegraphTimer = 0;
                    TelegraphingLasers = false;
                }
            }

            if (mouth.GetGlobalNPC<WallofFlesh>().InDesperationPhase)
            {
                if (ai_Timer < maxTime - 180) //dont lower this if it's already telegraphing laser
                    maxTime = 240f;

                if (!WorldSavingSystem.MasochistModeReal)
                {
                    npc.localAI[1] = -1f; //no more regular lasers
                    npc.localAI[2] = 0f;
                }
            }

            if (++ai_Timer >= maxTime)
            {
                ai_Timer = 0f;
                if (ai_State == 0f)
                    ai_State = 1f;
                else
                    ai_State *= -1f;

                if (ai_State > 0) //FIRE LASER
                {
                    Vector2 speed = Vector2.UnitX.RotatedBy(npc.ai[3]);
                    if (FargoSoulsUtil.HostCheck && PreventAttacks <= 0)
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, speed, ModContent.ProjectileType<PhantasmalDeathrayWOF>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0f, Main.myPlayer, 0, npc.whoAmI);
                }
                /*
                else //ring dust to denote i am vulnerable now
                {
                    if (FargoSoulsUtil.HostCheck)
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, npc.type);
                }
                */

                npc.netUpdate = true;
                NetSync(npc);
            }

            if (ai_State >= 0f)
            {
                //npc.alpha = (int)MathHelper.Lerp(npc.alpha, 175, 0.1f);
                npc.dontTakeDamage = true;

                if (ai_Timer <= 90) //still firing laser rn
                {
                    RepeatingAI = true;
                    npc.AI();
                    RepeatingAI = false;

                    npc.localAI[1] = -1f;
                    npc.localAI[2] = 0f;

                    npc.rotation = npc.ai[3];
                    return false;
                }
                else
                {
                    ai_State = 1;
                }
            }
            else
            {
                //npc.alpha = (int)MathHelper.Lerp(npc.alpha, 0, 0.1f);
                npc.dontTakeDamage = false;

                if (ai_Timer == maxTime - 3 * 5 && FargoSoulsUtil.HostCheck)
                {
                    if (FargoSoulsUtil.HostCheck && PreventAttacks <= 0)
                    {
                        float ai0 = npc.realLife != -1 && Main.npc[npc.realLife].velocity.X > 0 ? 1f : 0f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<WOFBlast>(), 0, 0f, Main.myPlayer, ai0, npc.whoAmI);
                    }
                }

                if (ai_Timer > maxTime - 180f)
                {
                    /*
                    if (Main.rand.Next(4) < 3) //dust telegraphs switch
                    {
                        int dust = Dust.NewDust(npc.position - new Vector2(2f, 2f), npc.width + 4, npc.height + 4, DustID.GemSapphire, npc.velocity.X * 0.4f, npc.velocity.Y * 0.4f, 114, default, 3.5f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity *= 1.8f;
                        Main.dust[dust].velocity.Y -= 0.5f;
                        if (Main.rand.NextBool(4))
                        {
                            Main.dust[dust].noGravity = false;
                            Main.dust[dust].scale *= 0.5f;
                        }
                    }
                    */
                    float stopTime = maxTime - 90f;
                    if (ai_Timer == stopTime) //shoot warning dust in phase 2
                    {
                        int t = npc.HasPlayerTarget ? npc.target : npc.FindClosestPlayer();
                        if (t != -1)
                        {
                            if (npc.Distance(Main.player[t].Center) < 3000)
                                SoundEngine.PlaySound(FargosSoundRegistry.WoFDeathrayTelegraph, Main.player[t].Center);
                            ai_State = -2f;
                            npc.ai[3] = (npc.Center - Main.player[t].Center).ToRotation();
                            if (npc.realLife != -1 && Main.npc[npc.realLife].velocity.X > 0)
                                npc.ai[3] += (float)Math.PI;

                            Vector2 speed = Vector2.UnitX.RotatedBy(npc.ai[3]);
                            if (FargoSoulsUtil.HostCheck)
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, speed, ModContent.ProjectileType<PhantasmalDeathrayWOFS>(), 0, 0f, Main.myPlayer, 0, npc.whoAmI);
                        }

                        npc.netUpdate = true;
                        NetSync(npc);
                    }
                    else if (ai_Timer > stopTime)
                    {
                        HasTelegraphedNormalLasers = false;

                        RepeatingAI = true;
                        npc.AI();
                        RepeatingAI = false;

                        npc.localAI[1] = -1f;
                        npc.localAI[2] = 0f;

                        npc.rotation = npc.ai[3];
                        return false;
                    }
                }
            }

            //dont fire during mouth's special attacks (this is at bottom to override others)
            if ((mouth.GetGlobalNPC<WallofFlesh>().InPhase2 && mouth.GetGlobalNPC<WallofFlesh>().WorldEvilAttackCycleTimer < 240 || mouth.GetGlobalNPC<WallofFlesh>().InDesperationPhase) && !WorldSavingSystem.MasochistModeReal)
            {
                npc.localAI[1] = -90f;
                npc.localAI[2] = 0f;

                HasTelegraphedNormalLasers = false;
            }

            if (npc.localAI[2] > 1) //has shot at least one laser
            {
                HasTelegraphedNormalLasers = false;
            }
            else if (npc.localAI[1] >= 0f && !HasTelegraphedNormalLasers && npc.HasValidTarget) //telegraph for imminent laser
            {
                HasTelegraphedNormalLasers = true;
                TelegraphingLasers = true;
                //if (FargoSoulsUtil.HostCheck)
                //Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, -22);
                //Projectile.scale = 18f * progress;

            }

            //if (NPC.FindFirstNPC(npc.type) == npc.whoAmI) FargoSoulsUtil.PrintAI(npc);

            return true;
        }

        public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
        {
            base.OnHitPlayer(npc, target, hurtInfo);

            target.AddBuff(BuffID.Burning, 300);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (npc.dontTakeDamage && !npc.IsABestiaryIconDummy)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);

                ArmorShaderData shader = GameShaders.Armor.GetShaderFromItemId(ItemID.PhaseDye);
                shader.Apply(npc, new Terraria.DataStructures.DrawData?());
            }           
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (npc.dontTakeDamage && !npc.IsABestiaryIconDummy)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);
            }
        }

        public override void LoadSprites(NPC npc, bool recolor)
        {
            base.LoadSprites(npc, recolor);

            LoadNPCSprite(recolor, npc.type);
        }
    }

    public class Hungry : EModeNPCBehaviour
    {
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchTypeRange(NPCID.TheHungry, NPCID.TheHungryII);

        public override void OnFirstTick(NPC npc)
        {
            base.OnFirstTick(npc);

            npc.buffImmune[BuffID.OnFire] = true;
        }

        public override void AI(NPC npc)
        {
            base.AI(npc);

            NPC wall = FargoSoulsUtil.NPCExists(EModeGlobalNPC.wallBoss, NPCID.WallofFlesh);
            if (npc.HasValidTarget && npc.Distance(Main.player[npc.target].Center) < 200 && wall != null
                && wall.GetGlobalNPC<WallofFlesh>().UseCorruptAttack && wall.GetGlobalNPC<WallofFlesh>().WorldEvilAttackCycleTimer < 240
                && !WorldSavingSystem.MasochistModeReal)
            {
                //snap away from player if too close during wof cursed flame wall
                npc.position += (Main.player[npc.target].position - Main.player[npc.target].oldPosition) / 3;

                Vector2 vel = Main.player[npc.target].Center - npc.Center;
                vel += 200f * Main.player[npc.target].SafeDirectionTo(npc.Center);
                npc.velocity = vel / 15;
            }
        }

        public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
        {
            base.OnHitPlayer(npc, target, hurtInfo);

            target.AddBuff(BuffID.OnFire, 300);
        }

        public override void LoadSprites(NPC npc, bool recolor)
        {
            base.LoadSprites(npc, recolor);

            LoadNPCSprite(recolor, npc.type);
        }
    }

    public class Leech : EModeNPCBehaviour
    {
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchTypeRange(NPCID.LeechBody, NPCID.LeechHead, NPCID.LeechTail);

        public override void OnFirstTick(NPC npc)
        {
            base.OnFirstTick(npc);

            npc.buffImmune[BuffID.OnFire] = true;
        }

        public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
        {
            base.OnHitPlayer(npc, target, hurtInfo);

            target.AddBuff(BuffID.OnFire, 300);
            target.AddBuff(BuffID.Bleeding, 300);
            target.AddBuff(BuffID.Rabies, 600);
        }

        public override void LoadSprites(NPC npc, bool recolor)
        {
            base.LoadSprites(npc, recolor);

            LoadNPCSprite(recolor, npc.type);
        }
    }
}
