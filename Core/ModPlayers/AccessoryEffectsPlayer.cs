using FargowiltasSouls.Content.Projectiles.BossWeapons;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Content.Projectiles.Souls;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Buffs;
using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using Luminance.Core.Graphics;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Content.UI.Elements;
using Microsoft.Xna.Framework.Graphics;

namespace FargowiltasSouls.Core.ModPlayers
{
    public partial class FargoSoulsPlayer
    {

        public void GoldKey(bool stunned)
        {
            if (!Player.HasBuff(ModContent.BuffType<GoldenStasisBuff>()) && !Player.HasBuff(ModContent.BuffType<GoldenStasisCDBuff>()))
            {
                if (stunned)
                    return;
                int duration = 300;

                if (ForceEffect<GoldEnchant>())
                {
                    duration *= 2;
                }

                Player.AddBuff(ModContent.BuffType<GoldenStasisBuff>(), duration);
                Player.AddBuff(ModContent.BuffType<GoldenStasisCDBuff>(), 3600);

                goldHP = Player.statLife;

                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/Accessories/GoldEncase"), Player.Center);
            }
            //cancel it early
            else
            {
                Player.ClearBuff(ModContent.BuffType<GoldenStasisBuff>());
            }
        }

        public void GoldUpdate()
        {
            Player.immune = true;
            Player.immuneTime = 90;
            Player.hurtCooldowns[0] = 90;
            Player.hurtCooldowns[1] = 90;
            Player.stealth = 1;

            //immune to DoT
            if (Player.statLife < goldHP)
                Player.statLife = goldHP;

            if (Player.ownedProjectileCounts[ModContent.ProjectileType<GoldShellProj>()] <= 0)
                Projectile.NewProjectile(Player.GetSource_Misc(""), Player.Center.X, Player.Center.Y, 0f, 0f, ModContent.ProjectileType<GoldShellProj>(), 0, 0, Main.myPlayer);
        }

        

        public bool CanJungleJump = false;
        public bool JungleJumping = false;
        public int savedRocketTime;


        private int GetNumSentries()
        {
            int count = 0;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];

                if (p.active && p.owner == Player.whoAmI && p.sentry)
                {
                    count++;
                }
            }

            return count;
        }

        //saved mount stats for squire
        //public Mount.MountData OriginalSquireMountData;
        public Mount.MountData BaseSquireMountData;

        public int BaseMountType = -1;


        //public float baseMountAccel;
        //public float baseMountRunSpeed;
        //public int baseMountJumpHeight;

        //        #endregion

        //        #region souls
        

        public bool extraCarpetDuration = true;



        //        #endregion

        #region maso acc

        public void OnLandingEffects()
        {
            

            //Player.justJumped use this tbh
            if (SlimyShieldFalling) //landing
            {
                if (Player.velocity.Y < 0f)
                    SlimyShieldFalling = false;

                if (Player.velocity.Y == 0f)
                {
                    SlimyShieldFalling = false;
                    if (Player.whoAmI == Main.myPlayer && Player.gravDir > 0)
                    {
                        if (SlimyShieldItem != null)
                        {
                            SoundEngine.PlaySound(SoundID.Item21 with { Volume = 0.5f }, Player.Center);
                            Vector2 mouse = Main.MouseWorld;
                            int damage = 8;
                            if (SupremeDeathbringerFairy)
                                damage = 16;
                            if (MasochistSoul)
                                damage = 80;
                            damage = (int)(damage * Player.ActualClassDamage(DamageClass.Melee));
                            for (int i = 0; i < 3; i++)
                            {
                                Vector2 spawn = new(mouse.X + Main.rand.Next(-200, 201), mouse.Y - Main.rand.Next(600, 901));
                                if (Collision.CanHitLine(mouse, 0, 0, spawn, 0, 0))
                                {
                                    Vector2 speed = mouse - spawn;
                                    speed.Normalize();
                                    speed *= 10f;
                                    int p = Projectile.NewProjectile(Player.GetSource_Accessory(SlimyShieldItem, "SlimyShield"), spawn, speed, ModContent.ProjectileType<SlimeBall>(), damage, 1f, Main.myPlayer);
                                    if (p.IsWithinBounds(Main.maxProjectiles))
                                    {
                                        Main.projectile[p].DamageType = DamageClass.Generic;
                                    }
                                }
                            }
                        }

                        if (GelicWingsItem != null && Player.HasEffect<GelicWingSpikes>())
                        {
                            int dam = 60; //deliberately no scaling
                            for (int j = -1; j <= 1; j += 2)
                            {
                                Vector2 baseVel = Vector2.UnitX.RotatedBy(MathHelper.ToRadians(-10 * j));
                                const int max = 8;
                                for (int i = 0; i < max; i++)
                                {
                                    Vector2 vel = Main.rand.NextFloat(5f, 10f) * j * baseVel.RotatedBy(-MathHelper.PiOver4 * 0.8f / max * i * j);
                                    Projectile.NewProjectile(Player.GetSource_Accessory(GelicWingsItem), Player.Bottom - Vector2.UnitY * 8, vel, ModContent.ProjectileType<GelicWingSpike>(), dam, 5f, Main.myPlayer);
                                }
                            }
                        }
                    }
                }
            }
            else if (Player.velocity.Y > 3f)
            {
                SlimyShieldFalling = true;
            }
        }


        public void DarkenedHeartAttack(Projectile projectile)
        {
            if (DarkenedHeartCD <= 0)
            {
                DarkenedHeartCD = 60;

                if (Player.HasEffect<DarkenedHeartEaters>() && (projectile == null || projectile.type != ProjectileID.TinyEater))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit1, Player.Center);
                    for (int index1 = 0; index1 < 20; ++index1)
                    {
                        int index2 = Dust.NewDust(Player.position, Player.width, Player.height, DustID.ScourgeOfTheCorruptor, 0.0f, 0.0f, 0, new Color(), 1f);
                        Dust dust = Main.dust[index2];
                        dust.scale *= 1.1f;
                        Main.dust[index2].noGravity = true;
                    }
                    for (int index1 = 0; index1 < 30; ++index1)
                    {
                        int index2 = Dust.NewDust(Player.position, Player.width, Player.height, DustID.ScourgeOfTheCorruptor, 0.0f, 0.0f, 0, new Color(), 1f);
                        Dust dust1 = Main.dust[index2];
                        dust1.velocity *= 2.5f;
                        Dust dust2 = Main.dust[index2];
                        dust2.scale *= 0.8f;
                        Main.dust[index2].noGravity = true;
                    }
                    int num = 2;
                    if (Main.rand.NextBool(3))
                        ++num;
                    if (Main.rand.NextBool(6))
                        ++num;
                    if (Main.rand.NextBool(9))
                        ++num;
                    int dam = PureHeart ? 35 : 13;
                    if (MasochistSoul)
                        dam *= 2;
                    for (int index = 0; index < num; ++index)
                    {
                        int p = Projectile.NewProjectile(Player.GetSource_Accessory(DarkenedHeartItem), Player.Center.X, Player.Center.Y, Main.rand.Next(-35, 36) * 0.02f * 10f,
                            Main.rand.Next(-35, 36) * 0.02f * 10f, ProjectileID.TinyEater, (int)(dam * Player.ActualClassDamage(DamageClass.Melee)), 1.75f, Player.whoAmI);
                        if (p.IsWithinBounds(Main.maxProjectiles))
                        {
                            Main.projectile[p].DamageType = DamageClass.Default;
                        }
                    }
                        
                }
            }
        }

        public void FrigidGemstoneKey()
        {
            if (FrigidGemstoneCD > 0)
                return;

            if (!Player.CheckMana(6, true))
                return;

            FrigidGemstoneCD = 10;
            Player.manaRegenDelay = Math.Max(Player.manaRegenDelay, 30);

            SoundEngine.PlaySound(SoundID.Item28, Player.Center);
            int damage = (int)(14 * Player.ActualClassDamage(DamageClass.Magic));
            if (!Main.hardMode)
                damage = 0;

            Projectile.NewProjectile(Player.GetSource_EffectItem<FrigidGemstoneKeyEffect>(), Player.Center, 12f * Player.SafeDirectionTo(Main.MouseWorld), ProjectileID.IceBlock, damage, 2f, Player.whoAmI, Player.tileTargetX, Player.tileTargetY);
        }


        public void SpecialDashKey(int type)
        {
            if (SpecialDashCD <= 0)
            {
                SpecialDashCD = LumUtils.SecondsToFrames(5);

                if (Player.whoAmI == Main.myPlayer)
                {
                    Player.RemoveAllGrapplingHooks();

                    /*Player.controlLeft = false;
                    Player.controlRight = false;
                    Player.controlJump = false;
                    Player.controlDown = false;*/
                    Player.controlUseItem = false;
                    Player.controlUseTile = false;
                    Player.controlHook = false; 
                    //Player.controlMount = false;

                    Player.itemAnimation = 0;
                    Player.itemTime = 0;
                    Player.reuseDelay = 0;

                    if (Player.HasEffect<BetsyDashEffect>() && type == 2)
                    {
                        Vector2 vel = Player.SafeDirectionTo(Main.MouseWorld) * 25;
                        Projectile.NewProjectile(Player.GetSource_Accessory(BetsysHeartItem), Player.Center, vel, ModContent.ProjectileType<Content.Projectiles.BetsyDash>(), (int)(100 * Player.ActualClassDamage(DamageClass.Melee)), 6f, Player.whoAmI);

                        Player.immune = true;
                        Player.immuneTime = Math.Max(Player.immuneTime, 2);
                        Player.hurtCooldowns[0] = Math.Max(Player.hurtCooldowns[0], 2);
                        Player.hurtCooldowns[1] = Math.Max(Player.hurtCooldowns[1], 2);

                        CooldownBarManager.Activate("SpecialDashCooldown", ModContent.Request<Texture2D>("FargowiltasSouls/Content/Items/Accessories/Masomode/BetsysHeart").Value, Color.OrangeRed, 
                            () => 1 - (float)SpecialDashCD / LumUtils.SecondsToFrames(5), activeFunction: () => BetsysHeartItem != null);
                    }
                    else if (Player.HasEffect<SpecialDashEffect>() && type == 0)
                    {
                        SpecialDashCD += LumUtils.SecondsToFrames(1);

                        Vector2 vel = Player.SafeDirectionTo(Main.MouseWorld) * 20;
                        Projectile.NewProjectile(Player.GetSource_Accessory(QueenStingerItem), Player.Center, vel, ModContent.ProjectileType<BeeDash>(), (int)(44 * Player.ActualClassDamage(DamageClass.Melee)), 6f, Player.whoAmI);

                        CooldownBarManager.Activate("SpecialDashCooldown", ModContent.Request<Texture2D>("FargowiltasSouls/Content/Items/Accessories/Masomode/QueenStinger").Value, Color.Yellow, 
                            () => 1 - (float)SpecialDashCD / LumUtils.SecondsToFrames(6), activeFunction: () => QueenStingerItem != null);
                    }

                    Player.AddBuff(ModContent.BuffType<BetsyDashBuff>(), 20);
                }
            }
        }

        public bool TryCleanseDebuffs()
        {
            bool cleansed = false;

            int max = Player.buffType.Length;
            for (int i = 0; i < max; i++)
            {
                int timeLeft = Player.buffTime[i];
                if (timeLeft <= 0)
                    continue;

                int buffType = Player.buffType[i];
                if (buffType <= 0)
                    continue;

                if (timeLeft > 5
                    && Main.debuff[buffType]
                    && !Main.buffNoTimeDisplay[buffType]
                    && !BuffID.Sets.NurseCannotRemoveDebuff[buffType])
                {
                    Player.DelBuff(i);

                    cleansed = true;

                    i--;
                    max--; //just in case, to prevent being stuck here forever
                }
            }

            return cleansed;
        }

        public void MagicalBulbKey()
        {
            if (Player.HasBuff(ModContent.BuffType<MagicalCleanseCDBuff>()))
                return;
            TryCleanseDebuffs();
            if (true)
            {
                int cdInSec = 40;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && !Main.npc[i].friendly && Main.npc[i].lifeMax > 5)
                    {
                        Main.npc[i].AddBuff(ModContent.BuffType<MagicalCurseBuff>(), LumUtils.SecondsToFrames(cdInSec + 5));
                    }
                }    

                Player.AddBuff(ModContent.BuffType<MagicalCleanseCDBuff>(), LumUtils.SecondsToFrames(cdInSec));

                SoundEngine.PlaySound(SoundID.Item4, Player.Center);

                for (int index1 = 0; index1 < 50; ++index1)
                {
                    int index2 = Dust.NewDust(Player.position, Player.width, Player.height, Main.rand.NextBool() ? 107 : 157, 0f, 0f, 0, new Color(), 3f);
                    Main.dust[index2].noGravity = true;
                    Main.dust[index2].velocity *= 8f;
                }
            }
        }

        public void BombKey()
        {
            if (MutantEyeItem != null && MutantEyeCD <= 0)
            {
                MutantEyeCD = 3600;

                if (!Main.dedServ && Main.LocalPlayer.active)
                    ScreenShakeSystem.StartShake(10, shakeStrengthDissipationIncrement: 10f / 30);

                const int invulTime = 90;
                Player.immune = true;
                Player.immuneTime = invulTime;
                Player.hurtCooldowns[0] = invulTime;
                Player.hurtCooldowns[1] = invulTime;

                SoundEngine.PlaySound(SoundID.Item84, Player.Center);

                const int max = 100; //make some indicator dusts
                for (int i = 0; i < max; i++)
                {
                    Vector2 vector6 = Vector2.UnitY * 30f;
                    vector6 = vector6.RotatedBy((i - (max / 2 - 1)) * 6.28318548f / max) + Main.LocalPlayer.Center;
                    Vector2 vector7 = vector6 - Main.LocalPlayer.Center;
                    int d = Dust.NewDust(vector6 + vector7, 0, 0, DustID.Vortex, 0f, 0f, 0, default, 3f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity = vector7;
                }

                for (int i = 0; i < 50; i++) //make some indicator dusts
                {
                    int d = Dust.NewDust(Player.position, Player.width, Player.height, DustID.Vortex, 0f, 0f, 0, default, 2.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 24f;
                }

                FargoSoulsUtil.ClearHostileProjectiles(1);

                void SpawnSphereRing(int ringMax, float speed, int damage2, float rotationModifier)
                {
                    float rotation = 2f * (float)Math.PI / ringMax;
                    Vector2 vel = Vector2.UnitY * speed;
                    int type = ModContent.ProjectileType<PhantasmalSphereRing>();
                    for (int i = 0; i < ringMax; i++)
                    {
                        vel = vel.RotatedBy(rotation);
                        Projectile.NewProjectile(Player.GetSource_Accessory(MutantEyeItem), Player.Center, vel, type, damage2, 3f, Main.myPlayer, rotationModifier * Player.direction, speed);
                    }
                }

                int damage = (int)(1700 * Player.ActualClassDamage(DamageClass.Magic));
                SpawnSphereRing(24, 12f, damage, -1f);
                SpawnSphereRing(24, 12f, damage, 1f);
            }
            else if (CirnoGraze)
            {
                Projectile cirnoBomb = Main.projectile.FirstOrDefault(p => p.active && p.friendly && p.owner == Player.whoAmI && p.type == ModContent.ProjectileType<CirnoBomb>());
                if (cirnoBomb != null)
                {
                    cirnoBomb.ai[0] = 1;
                    cirnoBomb.netUpdate = true;

                    CirnoGrazeCounter = 0;
                }
            }
        }

        public void DebuffInstallKey()
        {
            if (Player.HasEffect<AgitatingLensInstall>())
            {
				if (!Player.HasBuff(ModContent.BuffType<BerserkerInstallBuff>())
					&& !Player.HasBuff(ModContent.BuffType<BerserkerInstallCDBuff>()))
				{
					SoundEngine.PlaySound(SoundID.Item119, Player.Center);

					Player.AddBuff(ModContent.BuffType<BerserkerInstallBuff>(), LumUtils.SecondsToFrames(8f));

					for (int i = 0; i < 60; i++)
					{
						int index2 = Dust.NewDust(Player.position, Player.width, Player.height, DustID.RedTorch, 0f, 0f, 0, default, 3f);
						Main.dust[index2].noGravity = true;
						Main.dust[index2].velocity *= 9;
					}
				}
                return;
            }

			if (Player.HasEffect<FusedLensInstall>())
            {
                int buffType = ModContent.BuffType<TwinsInstallBuff>();
                if (Player.HasBuff(buffType))
                {
                    Player.ClearBuff(buffType);
                }
                else
                {
                    SoundEngine.PlaySound(SoundID.Item119, Player.Center);

                    Player.AddBuff(buffType, 2);

                    int max = 60;
                    for (int i = 0; i < max; i++)
                    {
                        float scale = 3f;
                        int index2 = Dust.NewDust(Player.position, Player.width, Player.height, Main.rand.NextBool() ? 90 : 89, 0f, 0f, 0, new Color(), scale);
                        Main.dust[index2].noGravity = true;
                        Main.dust[index2].velocity *= scale * 3;
                    }
                }
            }
        }

        public void AmmoCycleKey()
        {
            SoundEngine.PlaySound(SoundID.Unlock, Player.Center);

            for (int i = 54; i <= 56; i++)
            {
                int j = i + 1;

                (Player.inventory[j], Player.inventory[i]) = (Player.inventory[i], Player.inventory[j]);
            }
        }

        int fastFallCD;
        public void TryFastfallUpdate()
        {
            if (fastFallCD > 0)
                fastFallCD--;

            bool canFastFall = Player.HasEffect<LihzahrdGroundPound>() || Player.HasEffect<DeerclawpsDive>();
            if (!(Player.gravDir > 0 && Player.HasEffect<DiveEffect>() && canFastFall))
                return;

            bool keying = Player.controlDown && Player.releaseDown && Player.doubleTapCardinalTimer[0] > 0 && Player.doubleTapCardinalTimer[0] != 15;
            
            ModKeybind key = AccessoryEffectLoader.GetKeybind<DiveEffect>(this);
            if (key == null)
                return;
            if (key.GetAssignedKeys().Any())
                keying = key.JustPressed;

            if (fastFallCD <= 0 && !Player.mount.Active && !Player.controlJump && keying)
            {
                if (Player.velocity.Y != 0f)
                {
                    if (Player.velocity.Y < 15f)
                    {
                        Player.velocity.Y = 15f;
                    }

                    if (GroundPound <= 0)
                    {
                        GroundPound = 1;
                    }
                }
            }

            if (GroundPound > 0)
            {
                fastFallCD = 60;

                if (Player.velocity.Y == 0f && Player.controlDown)
                {
                    Vector2 vec = Collision.TileCollision(Player.position, 15f * Vector2.UnitY, Player.width, Player.height, true, true, (int)Player.gravDir);
                    if (vec != Vector2.Zero)
                    {
                        Player.position += vec;
                        Player.velocity.Y = 15f;
                    }
                }

                if (Player.velocity.Y < 0f || Player.mount.Active || (Player.controlJump && !Player.controlDown))
                {
                    GroundPound = 0;
                }
                else if (Player.velocity.Y == 0f && Player.oldVelocity.Y == 0f)
                {
                    int x = (int)Player.Center.X / 16;
                    int y = (int)(Player.position.Y + Player.height + 8) / 16;
                    if (/*GroundPound > 15 && */x >= 0 && x < Main.maxTilesX && y >= 0 && y < Main.maxTilesY
                        && Main.tile[x, y] != null && Main.tile[x, y].HasUnactuatedTile && Main.tileSolid[Main.tile[x, y].TileType])
                    {
                        GroundPound = 0;

                        if (Player.HasEffect<DeerclawpsDive>())
                        {
                            DeerclawpsDive.DeerclawpsLandingSpikes(Player, Player.Bottom);
                        }
                        if (Player.HasEffect<LihzahrdBoulders>())
                        {
                            if (!Main.dedServ)
                                ScreenShakeSystem.StartShake(10, shakeStrengthDissipationIncrement: 10f / 30);

                            if (Player.whoAmI == Main.myPlayer)
                            {
                                int baseDam = 500;
                                if (MasochistSoul)
                                    baseDam *= 3;

                                //explosion
                                Projectile.NewProjectile(Player.GetSource_Accessory(LihzahrdTreasureBoxItem), Player.Center, Vector2.Zero, ModContent.ProjectileType<MoonLordSunBlast>(), 0, 0f, Player.whoAmI);
                                int p = Projectile.NewProjectile(Player.GetSource_Accessory(LihzahrdTreasureBoxItem), Player.Center, Vector2.Zero, ModContent.ProjectileType<Explosion>(), (int)(baseDam * 2 * Player.ActualClassDamage(DamageClass.Melee)), 9f, Player.whoAmI);
                                if (p != Main.maxProjectiles)
                                    Main.projectile[p].DamageType = DamageClass.Melee;

                                //boulders
                                int dam = baseDam;
                                for (int i = -5; i <= 5; i += 2)
                                {
                                    Projectile.NewProjectile(Player.GetSource_Accessory(LihzahrdTreasureBoxItem), Player.Center, -10f * Vector2.UnitY.RotatedBy(MathHelper.PiOver2 / 6 * i),
                                        ModContent.ProjectileType<LihzahrdBoulderFriendly>(), (int)(dam * Player.ActualClassDamage(DamageClass.Melee)), 7.5f, Player.whoAmI);
                                }

                                //geysers
                                int baseDamage = (int)(baseDam / 2 * Player.ActualClassDamage(DamageClass.Melee));
                                if (MasochistSoul)
                                    baseDamage *= 3;
                                y -= 2;
                                for (int i = -3; i <= 3; i++)
                                {
                                    if (i == 0)
                                        continue;
                                    int tilePosX = x + 16 * i;
                                    int tilePosY = y;
                                    if (Main.tile[tilePosX, tilePosY] != null && tilePosX >= 0 && tilePosX < Main.maxTilesX)
                                    {
                                        while (Main.tile[tilePosX, tilePosY] != null && tilePosY >= 0 && tilePosY < Main.maxTilesY
                                            && !(Main.tile[tilePosX, tilePosY].HasUnactuatedTile && Main.tileSolid[Main.tile[tilePosX, tilePosY].TileType]))
                                        {
                                            tilePosY++;
                                        }
                                        Projectile.NewProjectile(Player.GetSource_Accessory(LihzahrdTreasureBoxItem), tilePosX * 16 + 8, tilePosY * 16 + 8, 0f, -8f, ModContent.ProjectileType<GeyserFriendly>(), baseDamage, 8f, Player.whoAmI);
                                    }
                                }
                            }
                        }
                    }

                }
                else
                {
                    if (Player.controlDown && Player.velocity.Y < 15f)
                    {
                        Player.velocity.Y = 15f;
                    }

                    Player.maxFallSpeed = 15f;
                    GroundPound++;

                    if (Player.HasEffect<LihzahrdGroundPound>())
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int d = Dust.NewDust(Player.position, Player.width, Player.height, DustID.Torch, Scale: 1.5f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].velocity *= 0.2f;
                        }
                    }
                    if (Player.HasEffect<DeerclawpsDive>())
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int d = Dust.NewDust(Player.position, Player.width, Player.height, LumpOfFlesh ? DustID.CrimsonTorch : DustID.IceTorch, Scale: 1.5f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].velocity *= 0.2f;
                        }
                    }
                }
            }
        }

        public void AbomWandUpdate()
        {
            if (AbominableWandRevived) //has been revived already
            {
                if (Player.statLife >= Player.statLifeMax2 && Player.statLife >= 400) //can revive again
                {
                    AbominableWandRevived = false;

                    SoundEngine.PlaySound(SoundID.Item28, Player.Center);

                    FargoSoulsUtil.DustRing(Player.Center, 50, 87, 8f, default, 2f);

                    for (int i = 0; i < 30; i++)
                    {
                        int d = Dust.NewDust(Player.position, Player.width, Player.height, DustID.GemTopaz, 0f, 0f, 0, default, 2.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].velocity *= 8f;
                    }
                }
                else //cannot currently revive
                {
                    Player.AddBuff(ModContent.BuffType<AbomCooldownBuff>(), 2);
                }
            }
        }

        public void DreadParryCounter()
        {
            SoundEngine.PlaySound(SoundID.Item119, Player.Center);

            if (Player.whoAmI == Main.myPlayer)
            {
                int type = ModContent.ProjectileType<Bloodshed>();
                const float speed = 12f;
                Projectile.NewProjectile(Player.GetSource_EffectItem<DreadShellEffect>(), Player.Center, Main.rand.NextVector2Circular(speed, speed), type, 0, 0f, Main.myPlayer, 1f);

                int projDamage = FargoSoulsUtil.HighestDamageTypeScaling(Player, 1000);

                const int max = 20;
                for (int i = 0; i < max; i++)
                {
                    void SharpTears(Vector2 pos, Vector2 vel)
                    {
                        int p = Projectile.NewProjectile(Player.GetSource_EffectItem<DreadShellEffect>(), pos, vel, ProjectileID.SharpTears, projDamage, 12f, Player.whoAmI, 0f, Main.rand.NextFloat(0.5f, 1f));
                        if (p != Main.maxProjectiles)
                        {
                            Main.projectile[p].DamageType = DamageClass.Default;
                            Main.projectile[p].usesLocalNPCImmunity = false;
                            Main.projectile[p].usesIDStaticNPCImmunity = true;
                            Main.projectile[p].idStaticNPCHitCooldown = 60;
                            Main.projectile[p].FargoSouls().noInteractionWithNPCImmunityFrames = true;
                            Main.projectile[p].FargoSouls().DeletionImmuneRank = 1;
                        }
                    }

                    SharpTears(Player.Center, 16f * Vector2.UnitX.RotatedBy(MathHelper.TwoPi / max * (i + Main.rand.NextFloat(-0.5f, 0.5f))));

                    for (int k = 0; k < 6; k++)
                    {
                        Vector2 spawnPos = Player.Bottom;
                        spawnPos.X += Main.rand.NextFloat(-256, 256);

                        bool foundTile = false;

                        for (int j = 0; j < 40; j++)
                        {
                            Tile tile = Framing.GetTileSafely(spawnPos);
                            if (tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]))
                            {
                                foundTile = true;
                                break;
                            }
                            spawnPos.Y += 16;
                        }

                        if (!foundTile)
                            spawnPos.Y = Player.Bottom.Y + Main.rand.NextFloat(-64, 64);

                        for (int j = 0; j < 40; j++)
                        {
                            Tile tile = Framing.GetTileSafely(spawnPos);
                            if (tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]))
                                spawnPos.Y -= 16;
                            else
                                break;
                        }

                        if (!Collision.SolidCollision(spawnPos, 0, 0))
                        {
                            spawnPos.Y += 16;
                            SharpTears(spawnPos, 16f * -Vector2.UnitY.RotatedByRandom(MathHelper.ToRadians(30)));
                            break;
                        }
                    }
                }
            }
        }

        public void PumpkingsCapeCounter(int damage)
        {
            SoundEngine.PlaySound(SoundID.Item62, Player.Center);

            if (Player.whoAmI == Main.myPlayer)
            {
                int heal = GetHealMultiplier(damage);
                Player.statLife += heal;
                if (Player.statLife > Player.statLifeMax2)
                    Player.statLife = Player.statLifeMax2;
                Player.HealEffect(heal);

                int counterDamage = damage;

                for (int i = 0; i < 30; i++)
                {
                    Vector2 vel = Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi);
                    vel *= Main.rand.NextFloat(12f, 24f);
                    int p = FargoSoulsUtil.NewSummonProjectile(Player.GetSource_EffectItem<PumpkingsCapeEffect>(), Player.Center, vel, ModContent.ProjectileType<SpookyScythe>(), counterDamage, 6f, Player.whoAmI);
                    if (p.IsWithinBounds(Main.maxProjectiles))
                    {
                        Main.projectile[p].DamageType = DamageClass.Generic;
                    }
                }
            }
        }

        public void DeerSinewDash(int dir)
        {
            float dashSpeed = 12f;
            DashCD = 60;
            Player.dashDelay = DashCD;
            if (IsDashingTimer < 15)
                IsDashingTimer = 15;
            Player.velocity.X = dir * dashSpeed;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.PlayerControls, number: Player.whoAmI);
        }
        public float DeerSinewCritNerf()
        {
            float ratio = Math.Min(Player.velocity.Length() / 16f, 1f);
            return MathHelper.Lerp(1f, 0.75f, ratio);
        }

        #endregion maso acc

        // TODO: rework this because we can only get final damage in hurtinfo hooks
        public void TryParryAttack(ref Player.HurtInfo hurtInfo)
        {
            bool silverEffect = Player.HasEffect<SilverEffect>();
            bool dreadEffect = Player.HasEffect<DreadShellEffect>();
            bool pumpkingEffect = Player.HasEffect<PumpkingsCapeEffect>();
            if (GuardRaised && shieldTimer > 0 && !Player.immune)
            {
                Player.immune = true;
                int invul = Player.longInvince ? 90 : 60;
                int extrashieldCD = 40;

                int damageBlockCap = 100;
                const int higherCap = 200;

                if (dreadEffect || pumpkingEffect)
                {
                    damageBlockCap = higherCap;

                    invul += 60;

                    extrashieldCD = LONG_SHIELD_COOLDOWN;
                }
                else if (silverEffect)
                {
                    extrashieldCD = BASE_SHIELD_COOLDOWN;
                }

                bool perfectParry = shieldHeldTime <= PERFECT_PARRY_WINDOW;

                if (silverEffect)
                {
                    if (perfectParry || ForceEffect<SilverEnchant>())
                    {
                        damageBlockCap = higherCap;
                        Player.AddBuff(BuffID.ParryDamageBuff, 300);

                        SoundEngine.PlaySound(SoundID.Item4, Player.Center);
                        /*
                        for (int i = 0; i < 50; i++)
                        {
                            int d = Dust.NewDust(Player.Center, 0, 0, DustID.GemDiamond, 0f, 0f, 0, default, 3f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].velocity *= 9f;
                        }
                        */

                        if (Player.HasEffect<TerraLightningEffect>())
                        {
                            TerraProcCD = 0;
                            int targetID = FargoSoulsUtil.FindClosestHostileNPC(Player.Center, 1000, true, true);
                            if (targetID.IsWithinBounds(Main.maxNPCs) && Main.npc[targetID] is NPC target && target.Alive())
                            {
                                TerraLightningEffect.LightningProc(Player, target, 7f);
                            }
                            
                        }
                    }
                    int sheet = perfectParry ? 1 : 0; // which parry vfx sprite sheet to use
                    Projectile.NewProjectile(Player.GetSource_Misc(""), Player.Center, Vector2.Zero, ModContent.ProjectileType<IronParry>(), 0, 0f, Main.myPlayer, sheet);
                }

                int damageBlocked = Math.Min(damageBlockCap, hurtInfo.Damage);
                var newDamage = hurtInfo.Damage - damageBlocked;
                if (newDamage < 1)
                {
                    hurtInfo.Null();
                }
                else
                {
                    hurtInfo.Damage = newDamage;
                }

                if (dreadEffect && perfectParry)
                {
                    DreadParryCounter();
                }

                if (pumpkingEffect && perfectParry)
                {
                    PumpkingsCapeCounter(damageBlocked);
                }

                Player.immuneTime = invul;
                Player.hurtCooldowns[0] = invul;
                Player.hurtCooldowns[1] = invul;
                ParryDebuffImmuneTime = invul;
                shieldCD = invul + extrashieldCD;

                CooldownBarManager.Activate("ParryCooldown", ModContent.Request<Texture2D>("FargowiltasSouls/Content/Items/Accessories/Enchantments/SilverEnchant").Value, Color.Gray, 
                    () => 1 - shieldCD / (float)(invul + extrashieldCD), activeFunction: () => Player.HasEffect<SilverEffect>() || Player.HasEffect<DreadShellEffect>() || Player.HasEffect<PumpkingsCapeEffect>());

                foreach (int debuff in FargowiltasSouls.DebuffIDs) //immune to all debuffs
                {
                    if (!Player.HasBuff(debuff))
                        Player.buffImmune[debuff] = true;
                }
            }
        }

        public const int BASE_PARRY_WINDOW = 20;
        public const int BASE_SHIELD_COOLDOWN = 100;
        public const int HARD_PARRY_WINDOW = 10;
        public const int LONG_SHIELD_COOLDOWN = 360;
        public const int PERFECT_PARRY_WINDOW = 10;

        public static int ShieldCooldown(Player player) => player.HasEffect<DreadShellEffect>() || player.HasEffect<PumpkingsCapeEffect>() ? LONG_SHIELD_COOLDOWN : BASE_SHIELD_COOLDOWN;

        void RaisedShieldEffects()
        {
            bool silverEffect = Player.HasEffect<SilverEffect>();
            bool dreadEffect = Player.HasEffect<DreadShellEffect>();
            bool pumpkingEffect = Player.HasEffect<PumpkingsCapeEffect>();
            if (dreadEffect)
            {
                if (!MasochistSoul)
                    DreadShellVulnerabilityTimer = 60;
            }

            if (pumpkingEffect) //strong aura effect
            {
                const float distance = 300f;
                for (int i = 0; i < Main.maxNPCs; i++)
                    if (Main.npc[i].active && !Main.npc[i].friendly && Main.npc[i].Distance(Player.Center) < distance)
                        Main.npc[i].AddBuff(ModContent.BuffType<RottingBuff>(), 600);

                for (int i = 0; i < 20; i++)
                {
                    Vector2 offset = new();
                    double angle = Main.rand.NextDouble() * 2d * Math.PI;
                    offset.X += (float)(Math.Sin(angle) * distance);
                    offset.Y += (float)(Math.Cos(angle) * distance);
                    Dust dust = Main.dust[Dust.NewDust(Player.Center + offset - new Vector2(4, 4), 0, 0, DustID.Ice_Pink, 0, 0, 100, Color.White, 1f)];
                    dust.velocity = Player.velocity;
                    if (Main.rand.NextBool(3))
                        dust.velocity += Vector2.Normalize(offset) * -5f;
                    dust.noGravity = true;
                }
            }

            if ((dreadEffect || pumpkingEffect) && !silverEffect)
            {
                Player.velocity.X *= 0.85f;
                if (Player.velocity.Y < 0)
                    Player.velocity.Y *= 0.85f;
            }

            int cooldown = ShieldCooldown(Player);

            if (shieldCD < cooldown)
                shieldCD = cooldown;
        }

        public void UpdateShield()
        {
            bool silverEffect = Player.HasEffect<SilverEffect>();
            bool dreadEffect = Player.HasEffect<DreadShellEffect>();
            bool pumpkingEffect = Player.HasEffect<PumpkingsCapeEffect>();
            GuardRaised = false;
            ModKeybind key = AccessoryEffectLoader.GetKeybind<ParryEffect>(this);

            //no need when player has brand of inferno
            if ((!silverEffect && !dreadEffect && !pumpkingEffect) || key == null ||
                Player.inventory[Player.selectedItem].type == ItemID.DD2SquireDemonSword || Player.inventory[Player.selectedItem].type == ItemID.BouncingShield)
            {
                shieldTimer = 0;
                shieldHeldTime = 0;
                wasHoldingShield = false;
                return;
            }
            bool holdingKey = PlayerInput.Triggers.Current.MouseRight && Player.controlUseTile && Player.releaseUseItem && !Player.tileInteractionHappened && !Player.controlUseItem;
            if (key.GetAssignedKeys().Any())
                holdingKey = key.Current;
            bool preventRightClick = Main.HoveringOverAnNPC || Main.SmartInteractShowingGenuine;
            if (key != null)
                preventRightClick = false;
            Player.shieldRaised = Player.selectedItem != 58 && FargoSoulsUtil.ActuallyClickingInGameplay(Player)
                && !preventRightClick && Player.itemAnimation == 0 && Player.itemTime == 0 && Player.reuseDelay == 0 && holdingKey;

            if (Player.shieldRaised)
            {
                GuardRaised = true;
                shieldHeldTime++;

                for (int i = 3; i < 8 + Player.extraAccessorySlots; i++)
                {
                    if (Player.shield == -1 && Player.armor[i].shieldSlot != -1)
                        Player.shield = Player.armor[i].shieldSlot;
                }

                if (shieldTimer > 0)
                    shieldTimer--;

                if (!wasHoldingShield)
                {
                    wasHoldingShield = true;

                    if (shieldCD == 0) //if cooldown over, enable parry
                    {
                        shieldTimer = silverEffect ? BASE_PARRY_WINDOW : HARD_PARRY_WINDOW;
                    }

                    Player.itemAnimation = 0;
                    Player.itemTime = 0;
                    Player.reuseDelay = 0;
                }
                else //doing this so that on the first tick, these things DO NOT run
                {
                    RaisedShieldEffects();
                }

                if (shieldTimer == 1) //parry window over
                {
                    SoundEngine.PlaySound(SoundID.Item27, Player.Center);

                    List<int> dusts = [];
                    if (dreadEffect)
                        dusts.Add(DustID.LifeDrain);
                    if (pumpkingEffect)
                        dusts.Add(87);
                    if (silverEffect)
                        dusts.Add(66);

                    if (dusts.Count > 0)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            int d = Dust.NewDust(Player.position, Player.width, Player.height, Main.rand.Next(dusts), 0, 0, 0, default, 1.5f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].velocity *= 3f;
                        }
                    }
                }
            }
            else
            {
                shieldTimer = 0;
                shieldHeldTime = 0;

                if (wasHoldingShield)
                {
                    wasHoldingShield = false;

                    Player.shield_parry_cooldown = 0; //prevent that annoying tick noise
                }

                if (shieldCD == 1) //cooldown over
                {
                    SoundEngine.PlaySound(SoundID.Item28, Player.Center); //make a sound for refresh

                    List<int> dusts = [];
                    if (dreadEffect)
                        dusts.Add(DustID.LifeDrain);
                    if (pumpkingEffect)
                        dusts.Add(87);
                    if (silverEffect)
                        dusts.Add(66);
                    
                    if (dusts.Count > 0)
                    {
                        for (int i = 0; i < 30; i++)
                        {
                            int d = Dust.NewDust(Player.position, Player.width, Player.height, Main.rand.Next(dusts), 0, 0, 0, default, 2.5f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].velocity *= 6f;
                        }
                    }
                }

                if (shieldCD > 0)
                    shieldCD--;
            }

            //Main.NewText($"{ironShieldCD}, {ironShieldTimer}");
        }

        
    }
}
