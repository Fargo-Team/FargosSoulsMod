using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.BossBars;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Items.BossBags;
using FargowiltasSouls.Content.Items.Materials;
using FargowiltasSouls.Content.Items.Pets;
using FargowiltasSouls.Content.Items.Placables.Relics;
using FargowiltasSouls.Content.Items.Placables.Trophies;
using FargowiltasSouls.Content.Items.Summons;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.ItemDropRules.Conditions;
using FargowiltasSouls.Core.Systems;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Creative;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.Bosses.AbomBoss
{
    [AutoloadBossHead]
    public class AbomBoss : ModNPC
    {
        public bool playerInvulTriggered;
        private bool droppedSummon = false;
        public int ritualProj, ringProj, spriteProj, ritualProjMaso, ritualProjFTW;

        string TownNPCName;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Abominationn");

            Main.npcFrameCount[NPC.type] = 4;
            NPCID.Sets.NoMultiplayerSmoothingByType[NPC.type] = true;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.MustAlwaysDraw[Type] = true;

            NPCID.Sets.BossBestiaryPriority.Add(NPC.type);
            NPC.AddDebuffImmunities(
            [
                BuffID.Confused,
                    BuffID.Chilled,
                    BuffID.OnFire,
                    BuffID.Suffocation,
                    ModContent.BuffType<LethargicBuff>(),
                    ModContent.BuffType<ClippedWingsBuff>(),
                    ModContent.BuffType<MutantNibbleBuff>(),
                    ModContent.BuffType<OceanicMaulBuff>(),
                    ModContent.BuffType<LightningRodBuff>()
            ]);
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange([
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                new FlavorTextBestiaryInfoElement($"Mods.FargowiltasSouls.Bestiary.{Name}")
            ]);
        }

        /*public override bool Autoload(ref string name)
        {
            return false;
        }*/

        public override void SetDefaults()
        {
            NPC.width = 120;
            NPC.height = 120;
            if (Main.getGoodWorld)
            {
                NPC.width = Player.defaultWidth;
                NPC.height = Player.defaultHeight;
            }
            NPC.damage = 240;
            NPC.defense = 80;
            NPC.lifeMax = 680000;
            if (Main.expertMode) //compensate universe core
                NPC.lifeMax *= 2;
            if (WorldSavingSystem.MasochistModeReal) //he's tanky enough
                NPC.lifeMax = (int)(NPC.lifeMax * 0.9f);
            NPC.value = Item.buyPrice(12);
            NPC.HitSound = SoundID.NPCHit57;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.npcSlots = 50f;
            NPC.knockBackResist = 0f;
            NPC.boss = true;
            NPC.lavaImmune = true;
            NPC.aiStyle = -1;
            NPC.netAlways = true;
            NPC.timeLeft = NPC.activeTime * 30;
            NPC.BossBar = ModContent.GetInstance<AbominationnBossBar>();

            Music = MusicID.OtherworldlyPlantera;
            bool foundMod = ModLoader.TryGetMod("FargowiltasMusic", out Mod musicMod);
            if (foundMod)
            {
                if (musicMod.Version >= Version.Parse("0.1.5"))
                    Music = MusicLoader.GetMusicSlot(musicMod, "Assets/Music/Laevateinn_P1");
                else
                    Music = MusicLoader.GetMusicSlot(musicMod, "Assets/Music/Stigma");
            }
            SceneEffectPriority = SceneEffectPriority.BossMedium;
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            NPC.damage = (int)(NPC.damage * 0.5f);
            NPC.lifeMax = (int)(NPC.lifeMax /** 0.5f*/ * balance);
        }

        public override bool CanHitPlayer(Player target, ref int CooldownSlot)
        {
            CooldownSlot = 1;
            return NPC.Distance(FargoSoulsUtil.ClosestPointInHitbox(target, NPC.Center)) < Player.defaultHeight && NPC.ai[0] != 0 && NPC.ai[0] != 10 && NPC.ai[0] != 18;
        }

        public override bool CanHitNPC(NPC target)
        {
            if (target.type == ModContent.Find<ModNPC>("Fargowiltas", "Deviantt").Type
                || target.type == ModContent.Find<ModNPC>("Fargowiltas", "Abominationn").Type
                || target.type == ModContent.Find<ModNPC>("Fargowiltas", "Mutant").Type)
                return false;

            return base.CanHitNPC(target);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.localAI[0]);
            writer.Write(NPC.localAI[1]);
            writer.Write(NPC.localAI[2]);
            writer.Write(NPC.localAI[3]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.localAI[0] = reader.ReadSingle();
            NPC.localAI[1] = reader.ReadSingle();
            NPC.localAI[2] = reader.ReadSingle();
            NPC.localAI[3] = reader.ReadSingle();
        }

        public override void OnSpawn(IEntitySource source)
        {
            int[] rituals = [ModContent.ProjectileType<AbomRitual>(), ModContent.ProjectileType<AbomRitualMaso>(), ModContent.ProjectileType<AbomRitualFTW>(), ModContent.ProjectileType<AbomRitual2>()];
            for (int i = 0; i <  Main.projectile.Length; i++)
            {
                if (Main.projectile[i] != null && Main.projectile[i].active && rituals.Contains(Main.projectile[i].type))
                {
                    Main.projectile[i].Kill();
                }
            }
            if (ModContent.TryFind("Fargowiltas", "Abominationn", out ModNPC modNPC))
            {
                int n = NPC.FindFirstNPC(modNPC.Type);
                if (n != -1 && n != Main.maxNPCs)
                {
                    NPC.Bottom = Main.npc[n].Bottom;
                    TownNPCName = Main.npc[n].GivenName;

                    Main.npc[n].life = 0;
                    Main.npc[n].active = false;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                }
            }
        }

        public override bool PreAI()
        {
            EModeGlobalNPC.abomBoss = NPC.whoAmI;

            if (NPC.localAI[3] == 0)
            {
                NPC.TargetClosest();
                if (NPC.timeLeft < 30)
                    NPC.timeLeft = 30;
                if (NPC.Distance(Main.player[NPC.target].Center) < 1500)
                {
                    NPC.localAI[3] = 1;
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    NPC.localAI[0] = Main.rand.Next(3); //start on a random strong attack
                    NPC.localAI[1] = Main.rand.Next(2); //start on a random super
                }
            }
            else if (NPC.localAI[3] == 1)
            {
                EModeGlobalNPC.Aura(NPC, 2000f, true, -1, default, ModContent.BuffType<GodEaterBuff>());
            }

            if (FargoSoulsUtil.HostCheck)
            {
                if (WorldSavingSystem.EternityMode && NPC.localAI[3] == 2 && FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<AbomRitual>()) == null)
                    ritualProj = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<AbomRitual>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 0f, NPC.whoAmI);

                if (WorldSavingSystem.MasochistModeReal && NPC.localAI[3] > 0 && FargoSoulsUtil.ProjectileExists(ritualProjMaso, ModContent.ProjectileType<AbomRitualMaso>()) == null)
                    ritualProjMaso = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<AbomRitualMaso>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 0f, NPC.whoAmI);

                if (Main.getGoodWorld && NPC.localAI[3] == 2 && FargoSoulsUtil.ProjectileExists(ritualProjFTW, ModContent.ProjectileType<AbomRitualFTW>()) == null)
                    ritualProjFTW = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<AbomRitualFTW>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 0f, NPC.whoAmI);

                if (FargoSoulsUtil.ProjectileExists(ringProj, ModContent.ProjectileType<AbomRitual2>()) == null)
                    ringProj = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<AbomRitual2>(), 0, 0f, Main.myPlayer, 0f, NPC.whoAmI);

                if (FargoSoulsUtil.ProjectileExists(spriteProj, ModContent.ProjectileType<AbomBossProjectile>()) == null)
                {
                    if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        int number = 0;
                        for (int index = 999; index >= 0; --index)
                        {
                            if (!Main.projectile[index].active)
                            {
                                number = index;
                                break;
                            }
                        }
                        if (number >= 0)
                        {
                            if (Main.netMode == NetmodeID.SinglePlayer)
                            {
                                Projectile projectile = Main.projectile[number];
                                projectile.SetDefaults(ModContent.ProjectileType<AbomBossProjectile>());
                                projectile.Center = NPC.Center;
                                projectile.owner = Main.myPlayer;
                                projectile.velocity.X = 0;
                                projectile.velocity.Y = 0;
                                projectile.damage = 0;
                                projectile.knockBack = 0f;
                                projectile.identity = number;
                                projectile.gfxOffY = 0f;
                                projectile.stepSpeed = 1f;
                                projectile.ai[1] = NPC.whoAmI;

                                spriteProj = number;
                            }
                        }
                    }
                    else //server
                    {
                        spriteProj = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<AbomBossProjectile>(), 0, 0f, Main.myPlayer, 0, NPC.whoAmI);
                    }
                }
            }

            if (Main.LocalPlayer.active && NPC.Distance(Main.LocalPlayer.Center) < 3000f)
            {
                if (WorldSavingSystem.EternityMode)
                    Main.LocalPlayer.AddBuff(ModContent.BuffType<AbomPresenceBuff>(), 2);

                if (NPC.life == 1 && WorldSavingSystem.MasochistModeReal)
                {
                    Main.LocalPlayer.AddBuff(ModContent.BuffType<TimeStopCDBuff>(), 2);
                    Main.LocalPlayer.AddBuff(ModContent.BuffType<GoldenStasisCDBuff>(), 2);
                }
            }

            if (NPC.localAI[3] == 2) //in phase 2
            {
                Music = MusicID.OtherworldlyPlantera;
                bool foundMod = ModLoader.TryGetMod("FargowiltasMusic", out Mod musicMod);
                if (foundMod)
                {
                    if (musicMod.Version >= Version.Parse("0.1.5"))
                        Music = MusicLoader.GetMusicSlot(musicMod, "Assets/Music/Laevateinn_P2");
                    else
                        Music = MusicLoader.GetMusicSlot(musicMod, "Assets/Music/Stigma");
                }

                //because this breaks the background???
                if (Main.GameModeInfo.IsJourneyMode && CreativePowerManager.Instance.GetPower<CreativePowers.FreezeTime>().Enabled)
                    CreativePowerManager.Instance.GetPower<CreativePowers.FreezeTime>().SetPowerInfo(false);

                if (!SkyManager.Instance["FargowiltasSouls:AbomBoss"].IsActive())
                    SkyManager.Instance.Activate("FargowiltasSouls:AbomBoss");

                Main.dayTime = false;
                Main.time = 16200; //midnight

                Main.raining = false; //disable rain
                Main.rainTime = 0;
                Main.maxRaining = 0;
            }

            return base.PreAI();
        }

        public override void AI()
        {
            Player player = Main.player[NPC.target];
            NPC.direction = NPC.spriteDirection = NPC.Center.X < player.Center.X ? 1 : -1;
            Vector2 targetPos;
            float speedModifier;

             // perma laevaetinn debug
             /*
            if (NPC.localAI[3] > 1 && NPC.ai[0] == 0)
            {
                NPC.dontTakeDamage = false;
                NPC.ai[0] = 15;
            }
           */

            switch ((int)NPC.ai[0])
            {
                case -4: //ACTUALLY dead
                    NPC.velocity *= 0.9f;
                    NPC.dontTakeDamage = true;
                    for (int i = 0; i < 5; i++)
                    {
                        int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemTopaz, 0f, 0f, 0, default, 2.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].velocity *= 12f;
                    }
                    if (++NPC.ai[1] > 180)
                    {
                        if (FargoSoulsUtil.HostCheck)
                        {
                            int trollSpeedUp = WorldSavingSystem.MasochistModeReal ? 2 : 1;
                            int max = WorldSavingSystem.MasochistModeReal ? 120 : 30;
                            for (int i = 0; i < max; i++)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                                    trollSpeedUp * Vector2.UnitX.RotatedBy(Main.rand.NextDouble() * Math.PI) * Main.rand.NextFloat(30f),
                                    ModContent.ProjectileType<AbomDeathScythe>(),
                                    FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 10),
                                    0f, Main.myPlayer);
                            }

                            if (ModContent.TryFind("Fargowiltas", "Abominationn", out ModNPC modNPC) && !NPC.AnyNPCs(modNPC.Type))
                            {
                                int n = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, modNPC.Type);
                                if (n != Main.maxNPCs)
                                {
                                    Main.npc[n].homeless = true;
                                    if (TownNPCName != default)
                                        Main.npc[n].GivenName = TownNPCName;
                                    if (Main.netMode == NetmodeID.Server)
                                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                                }
                            }

                            Main.eclipse = false;
                            NetMessage.SendData(MessageID.WorldData);
                        }
                        NPC.life = 0;
                        NPC.dontTakeDamage = false;
                        NPC.checkDead();
                    }
                    break;

                case -3: //pause to let arena recenter, then proceed
                    if (!AliveCheck(player))
                        break;
                    NPC.velocity *= 0.9f;
                    NPC.dontTakeDamage = true;
                    if (++NPC.ai[1] > 120)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0] = 15;
                        NPC.ai[1] = 0;
                    }
                    break;

                case -2: //dead, begin last stand
                    if (!AliveCheck(player))
                        break;
                    NPC.velocity *= 0.9f;
                    NPC.dontTakeDamage = true;
                    for (int i = 0; i < 5; i++)
                    {
                        int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemTopaz, 0f, 0f, 0, default, 2.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].velocity *= 12f;
                    }
                    if (++NPC.ai[1] > 180)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0] = 9;
                        NPC.ai[1] = 0;
                    }
                    break;

                case -1: //phase 2 transition
                    NPC.velocity *= 0.9f;
                    NPC.dontTakeDamage = true;
                    if (NPC.buffType[0] != 0)
                        NPC.DelBuff(0);

                    if (++NPC.ai[1] > 120)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemTopaz, 0f, 0f, 0, default, 1.5f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].velocity *= 4f;
                        }
                        NPC.localAI[3] = 2; //this marks p2
                        if (WorldSavingSystem.MasochistModeReal)
                        {
                            int heal = (int)(NPC.lifeMax / 90 * Main.rand.NextFloat(1f, 1.5f));
                            NPC.life += heal;
                            if (NPC.life > NPC.lifeMax)
                                NPC.life = NPC.lifeMax;
                            CombatText.NewText(NPC.Hitbox, CombatText.HealLife, heal);
                        }
                        if (NPC.ai[1] > 210)
                        {
                            NPC.ai[0]++;
                            NPC.ai[1] = 0;
                            NPC.ai[2] = 0;
                            NPC.ai[3] = 0;
                            NPC.netUpdate = true;
                        }
                    }
                    else if (NPC.ai[1] == 120)
                    {
                        FargoSoulsUtil.ClearFriendlyProjectiles(1);
                        if (FargoSoulsUtil.HostCheck && FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<AbomRitual>()) == null)
                        {
                            ritualProj = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<AbomRitual>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 0f, NPC.whoAmI);
                        }
                        SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    }
                    break;

                case 0: //track player, throw scythes (makes 4way using orig vel in p1, 8way targeting you in p2)
                    if (!AliveCheck(player) || Phase2Check())
                        break;
                    NPC.dontTakeDamage = false;

                    if (NPC.localAI[2] == 0) //store rotation offset
                    {
                        NPC.localAI[2] = player.SafeDirectionTo(NPC.Center).ToRotation()
                            + MathHelper.ToRadians(WorldSavingSystem.EternityMode ? 90 : 70) * Main.rand.NextFloat(-1, 1);
                        NPC.netUpdate = true;
                    }

                    targetPos = player.Center;
                    targetPos += 500 * NPC.localAI[2].ToRotationVector2();
                    if (NPC.Distance(targetPos) > 16)
                    {
                        NPC.position += (player.position - player.oldPosition) / 3;

                        speedModifier = NPC.localAI[3] > 0 ? 1f : 2f;
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
                        if (NPC.localAI[3] > 0)
                        {
                            if (Math.Abs(NPC.velocity.X) > 24)
                                NPC.velocity.X = 24 * Math.Sign(NPC.velocity.X);
                            if (Math.Abs(NPC.velocity.Y) > 24)
                                NPC.velocity.Y = 24 * Math.Sign(NPC.velocity.Y);
                        }
                    }

                    if (NPC.localAI[3] > 0) //in range, fight has begun
                    {
                        NPC.ai[1]++;

                        if (NPC.ai[3] == 0)
                        {
                            NPC.ai[3] = 1;
                            if (WorldSavingSystem.MasochistModeReal) //phase 2 saucers
                            {
                                int max = NPC.localAI[3] > 1 ? 6 : 2;
                                for (int i = 0; i < max; i++)
                                {
                                    float ai2 = i * MathHelper.TwoPi / max; //rotation offset
                                    FargoSoulsUtil.NewNPCEasy(NPC.GetSource_FromAI(), NPC.Center, ModContent.NPCType<AbomSaucer>(), 0, NPC.whoAmI, 0, ai2);
                                }
                            }
                        }
                    }

                    if (NPC.ai[1] == 120 - AbomStyxGazer.TelegraphTime)
                    {
                        if (NPC.ai[2] < (WorldSavingSystem.MasochistModeReal ? 7 : 5) && FargoSoulsUtil.HostCheck)
                        {
                            //float rotation = MathHelper.Pi * 1f * (NPC.Center.X < player.Center.X ? 1 : -1);
                            float rotation = MathHelper.Pi * 1f * AbomStyxGazer.Direction;
                            AbomStyxGazer.Direction *= -1;
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.DirectionTo(player.Center).RotatedBy(rotation * 0.6f),
                                ModContent.ProjectileType<AbomStyxGazer>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, rotation / 60 * 2);
                        }
                    }

                    if (NPC.ai[1] > 120)
                    {
                        NPC.netUpdate = true;
                        //NPC.TargetClosest();
                        NPC.ai[1] = WorldSavingSystem.MasochistModeReal ? 45 : 30;
                        NPC.localAI[2] = 0;
                        if (++NPC.ai[2] > (WorldSavingSystem.MasochistModeReal ? 7 : 5))
                        {
                            NPC.ai[0]++;
                            NPC.ai[1] = 0;
                            NPC.ai[2] = 0;
                            NPC.velocity = NPC.SafeDirectionTo(player.Center) * 2f;
                        }
                        else if (FargoSoulsUtil.HostCheck)
                        {
                            float ai0 = NPC.Distance(player.Center) / 30 * 2f;
                            float ai1 = NPC.localAI[3] > 1 ? 1f : 0f;
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.SafeDirectionTo(player.Center) * 30f, ModContent.ProjectileType<AbomScytheSplit>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, ai0, ai1);
                        }
                    }
                    /*else if (NPC.ai[1] == 90)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), NPC.Center, NPC.SafeDirectionTo(player.Center + player.velocity * 30) * 30f, ModContent.ProjectileType<AbomScythe>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                    }*/
                    break;

                case 1: //flaming scythe spread (shoots out further in p2)
                    {
                        if (!AliveCheck(player) || Phase2Check())
                            break;
                        NPC.velocity = NPC.SafeDirectionTo(player.Center);
                        NPC.velocity *= NPC.localAI[3] > 1 && WorldSavingSystem.EternityMode ? 2f : 6f;

                        int max = NPC.localAI[3] > 1 ? 7 : 6;
                        if (WorldSavingSystem.MasochistModeReal)
                            max++;

                        /*if (NPC.ai[1] == 50 && NPC.ai[2] != 4 && NPC.localAI[3] > 1)
                        {
                            if (FargoSoulsUtil.HostCheck)
                            {
                                for (int i = 0; i < max; i++)
                                {
                                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 0, MathHelper.TwoPi / max * (i + 0.5f));
                                    if (p != Main.maxProjectiles)
                                    {
                                        Main.projectile[p].localAI[1] = NPC.whoAmI;
                                        if (Main.netMode == NetmodeID.Server)
                                            NetMessage.SendData(MessageID.SyncProjectile, number: p);
                                    }
                                }
                            }
                        }*/
                        if (--NPC.ai[1] < 0)
                        {
                            if (++NPC.ai[2] > 4)
                            {
                                NPC.ai[0]++;
                                NPC.ai[1] = 0;
                                NPC.ai[2] = 0;
                                //NPC.TargetClosest();
                            }
                            else
                            {
                                NPC.ai[1] = 80;

                                float baseDelay = NPC.localAI[3] > 1 ? WorldSavingSystem.MasochistModeReal ? 60 : 90 : 20;
                                float extendedDelay = NPC.localAI[3] > 1 ? 90 : 40;
                                float speed = NPC.localAI[3] > 1 ? 20 : 10;
                                float offset = NPC.ai[2] % 2 == 0 ? 0 : 0.5f;
                                
                                if (FargoSoulsUtil.HostCheck && NPC.HasPlayerTarget)
                                {
                                    for (int i = 0; i < max; i++)
                                    {
                                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.SafeDirectionTo(player.Center).RotatedBy(MathHelper.TwoPi / max * (i + offset)) * speed, ModContent.ProjectileType<AbomScytheFlaming>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, baseDelay, baseDelay + extendedDelay, ai2: NPC.target);
                                    }
                                        
                                }
                                SoundEngine.PlaySound(SoundID.ForceRoarPitched, NPC.Center);
                            }
                            NPC.netUpdate = true;
                            break;
                        }
                    }
                    break;

                case 2: //pause and then initiate dash
                    if (!AliveCheck(player) || Phase2Check())
                        break;

                    NPC.velocity *= 0.9f;
                    if (WorldSavingSystem.MasochistModeReal && NPC.localAI[3] <= 1)
                        NPC.velocity *= 0.8f;

                    if (NPC.ai[2] == 0) //first dash only
                    {
                        if (NPC.localAI[3] > 1) //emode modified tells
                        {
                            if (NPC.ai[1] == 30 && WorldSavingSystem.EternityMode)
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<GlowRingHollow>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 3, NPC.whoAmI);
                            else if (NPC.ai[1] == 210)
                            {
                                if (FargoSoulsUtil.HostCheck)
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Souls.IronParry>(), 0, 0f, Main.myPlayer);
                                NPC.netUpdate = true;
                            }
                        }
                        else if (NPC.ai[1] == 0) //basic tell
                        {
                            if (FargoSoulsUtil.HostCheck)
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Souls.IronParry>(), 0, 0f, Main.myPlayer);
                        }
                    }
                    /*else
                    {
                        NPC.velocity *= 0.9f;
                    }*/
                    int windup = 30;
                    if (WorldSavingSystem.MasochistModeReal && NPC.localAI[3] <= 1)
                        windup = NPC.ai[2] == 0 ? 30 : 10;
                    if (++NPC.ai[1] > (NPC.ai[2] == 0 && NPC.localAI[3] > 1 && WorldSavingSystem.EternityMode ? 240 : windup)) //delay on first entry here
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                        NPC.ai[3] = 0;
                        if (++NPC.ai[2] > 5)
                        {
                            NPC.ai[0]++; //go to next attack after dashes
                            NPC.ai[2] = 0;
                        }
                        else
                        {
                            NPC.velocity = NPC.SafeDirectionTo(player.Center + player.velocity) * 30f;
                            if (NPC.localAI[3] > 1)
                            {
                                if (WorldSavingSystem.EternityMode)
                                    NPC.velocity *= 1.2f;

                                const int ring = 128;
                                for (int index1 = 0; index1 < ring; ++index1)
                                {
                                    Vector2 vector2 = (-Vector2.UnitY.RotatedBy(index1 * 3.14159274101257 * 2 / ring) * new Vector2(8f, 16f)).RotatedBy(NPC.velocity.ToRotation());
                                    int index2 = Dust.NewDust(NPC.Center, 0, 0, DustID.GemTopaz, 0.0f, 0.0f, 0, new Color(), 1f);
                                    Main.dust[index2].scale = 3f;
                                    Main.dust[index2].noGravity = true;
                                    Main.dust[index2].position = NPC.Center;
                                    Main.dust[index2].velocity = Vector2.Zero;
                                    //Main.dust[index2].velocity = 5f * Vector2.Normalize(NPC.Center - NPC.velocity * 3f - Main.dust[index2].position);
                                    Main.dust[index2].velocity += vector2 * 1.5f + NPC.velocity * 0.5f;
                                }

                                if (FargoSoulsUtil.HostCheck)
                                {
                                    float rotation = MathHelper.Pi * 1.5f * (NPC.ai[2] % 2 == 0 ? 1 : -1);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(-rotation / 2),
                                        ModContent.ProjectileType<AbomStyxGazerDash>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, rotation / 60 * 2);
                                }
                            }
                        }
                    }
                    break;

                case 3: //while dashing (p2 makes side scythes)
                    if (Phase2Check())
                        break;

                    NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);

                    ClearFrozen();

                    if (NPC.localAI[3] > 1)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            int d = Dust.NewDust(NPC.Center - NPC.velocity * Main.rand.NextFloat(), 0, 0, DustID.GemTopaz, 0f, 0f, 0, new Color());
                            Main.dust[d].scale = 1f + 4f * (1f - NPC.ai[1] / 30f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].velocity *= 0.1f;
                        }
                    }

                    if (++NPC.ai[3] > 5)
                    {
                        NPC.ai[3] = 0;
                        if (FargoSoulsUtil.HostCheck)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity), ModContent.ProjectileType<AbomPhoenix>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0, Main.myPlayer);
                            if (NPC.localAI[3] > 1)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(Math.PI / 2), ModContent.ProjectileType<AbomPhoenix>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0, Main.myPlayer);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(NPC.velocity).RotatedBy(-Math.PI / 2), ModContent.ProjectileType<AbomPhoenix>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0, Main.myPlayer);
                            }
                        }
                    }
                    if (++NPC.ai[1] > 30)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0]--;
                        NPC.ai[1] = 0;
                        NPC.ai[3] = 0;
                    }
                    break;

                case 4: //choose the next attack
                    if (!AliveCheck(player))
                        break;
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    NPC.netUpdate = true;
                    //NPC.TargetClosest();
                    NPC.ai[0] += ++NPC.localAI[0];
                    if (NPC.localAI[0] >= 3) //reset p1 hard option counter
                        NPC.localAI[0] = 0;
                    break;

                case 5: //modified mutant scythe 8way
                    if (!AliveCheck(player) || Phase2Check())
                        break;

                    NPC.velocity = NPC.SafeDirectionTo(player.Center) * 3f;

                    if (++NPC.ai[1] > (NPC.localAI[3] > 1 ? 75 : 90))
                    {
                        NPC.ai[1] = 0;
                        if (++NPC.ai[2] > 3)
                        {
                            NPC.ai[0] = 8;
                            NPC.ai[2] = 0;
                            //NPC.TargetClosest();
                        }
                        else
                        {
                            if (FargoSoulsUtil.HostCheck) //aim at player in p2
                            {
                                float baseRot = NPC.localAI[3] > 1 ? NPC.SafeDirectionTo(player.Center).ToRotation() : 0;
                                float baseSpeed = 1000f;
                                if (NPC.localAI[3] > 1 && NPC.Distance(player.Center) > baseSpeed / 2)
                                    baseSpeed = NPC.Distance(player.Center);
                                baseSpeed /= 90f;

                                for (int i = 0; i < 4; i++)
                                {
                                    Vector2 straightSpeed = new Vector2(baseSpeed, 0).RotatedBy(baseRot + Math.PI / 2 * i);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, straightSpeed, ModContent.ProjectileType<AbomSickleSplit1>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, straightSpeed, ModContent.ProjectileType<GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 1, straightSpeed.ToRotation() + MathHelper.PiOver2);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, straightSpeed, ModContent.ProjectileType<GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 1, straightSpeed.ToRotation() - MathHelper.PiOver2);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, straightSpeed, ModContent.ProjectileType<GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 1, straightSpeed.ToRotation() + MathHelper.PiOver4);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, straightSpeed, ModContent.ProjectileType<GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 1, straightSpeed.ToRotation() + MathHelper.PiOver4 + MathHelper.Pi);

                                    Vector2 diagonalSpeed = new Vector2(baseSpeed, baseSpeed).RotatedBy(baseRot + Math.PI / 2 * i);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, diagonalSpeed, ModContent.ProjectileType<AbomSickleSplit1>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI);

                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, diagonalSpeed, ModContent.ProjectileType<GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 1, diagonalSpeed.ToRotation() + MathHelper.PiOver2);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, diagonalSpeed, ModContent.ProjectileType<GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 1, diagonalSpeed.ToRotation() - MathHelper.PiOver2);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 1, baseRot + MathHelper.TwoPi / 4 * i);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 1, baseRot + MathHelper.TwoPi / 4 * (i + 0.5f));
                                }
                            }
                            SoundEngine.PlaySound(SoundID.ForceRoarPitched, NPC.Center);
                        }
                        NPC.netUpdate = true;
                        break;
                    }
                    break;

                case 6: //flocko swarm (p2 shoots ice waves horizontally after)
                    if (Phase2Check())
                        break;
                    NPC.velocity *= 0.99f;
                    if (NPC.ai[2] == 0)
                    {

                        NPC.ai[2] = 1;
                        if (FargoSoulsUtil.HostCheck)
                        {
                            for (int i = -3; i <= 3; i++) //make flockos
                            {
                                if (i == 0) //dont shoot one straight up
                                    continue;
                                Vector2 speed = new(Main.rand.NextFloat(40f), Main.rand.NextFloat(-20f, 20f));
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, speed, ModContent.ProjectileType<AbomFlocko>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, 360 / 3 * i);
                            }

                            if (NPC.localAI[3] > 1) //prepare ice waves
                            {
                                Vector2 speed = new(Main.rand.NextFloat(40f), Main.rand.NextFloat(-20f, 20f));
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, speed, ModContent.ProjectileType<AbomFlocko2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target, -1);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -speed, ModContent.ProjectileType<AbomFlocko2>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target, 1);
                            }

                            float offset = 420;
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2CircularEdge(20, 20), ModContent.ProjectileType<AbomFlocko3>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, offset);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2CircularEdge(20, 20), ModContent.ProjectileType<AbomFlocko3>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, -offset);

                            if (!WorldSavingSystem.MasochistModeReal)
                            {
                                for (int i = -1; i <= 1; i += 2)
                                {
                                    for (int j = -1; j <= 1; j += 2)
                                    {
                                        int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + 3000 * i * Vector2.UnitX, Vector2.UnitY * j, ModContent.ProjectileType<GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 5, 220 * i);
                                        if (p != Main.maxProjectiles)
                                        {
                                            Main.projectile[p].localAI[1] = NPC.whoAmI;
                                            if (Main.netMode == NetmodeID.Server)
                                                NetMessage.SendData(MessageID.SyncProjectile, number: p);
                                        }
                                    }
                                }
                            }
                        }

                        SoundEngine.PlaySound(SoundID.Item27, NPC.Center);
                        for (int index1 = 0; index1 < 30; ++index1)
                        {
                            int index2 = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Snow, 0.0f, 0.0f, 0, new Color(), 1f);
                            Main.dust[index2].noGravity = true;
                            Main.dust[index2].noLight = true;
                            Main.dust[index2].velocity *= 5f;
                        }
                    }
                    /*if (NPC.ai[1] > 150 && NPC.ai[1] % 4 == 0) //rain down along the exact borders
                    {
                        if (FargoSoulsUtil.HostCheck)
                        {
                            Vector2 spawnPos = NPC.Center - Vector2.UnitY * 1100;
                            for (int i = -1; i <= 1; i += 2)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos + Main.rand.NextFloat(300, 450) * Vector2.UnitX * i, Vector2.UnitY * 8f * Main.rand.NextFloat(1f, 4f),
                                    ModContent.ProjectileType<AbomFrostShard>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                            }
                        }
                    }*/
                    if (++NPC.ai[1] > 420)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0] = 8;
                        NPC.ai[1] = 0;
                    }
                    break;

                case 7: //saucer laser spam with rockets (p2 does two spams)
                    if (Phase2Check())
                        break;
                    NPC.velocity *= 0.99f;
                    if (NPC.ai[1] == 0)
                    {
                        if (FargoSoulsUtil.HostCheck)
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, NPC.whoAmI, -4);
                    }
                    if (++NPC.ai[1] > 420)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0] = 8;
                        NPC.ai[1] = 0;
                        NPC.ai[3] = 0;
                    }
                    else if (NPC.ai[1] > 60) //spam lasers, lerp aim
                    {
                        if (NPC.localAI[3] > 1) //p2 use a different lerp instead
                        {
                            NPC.ai[3] = MathHelper.Lerp(NPC.ai[3], 1f, 0.05f);
                        }
                        else //p1 lerps slowly at you
                        {
                            float targetRot = NPC.SafeDirectionTo(player.Center).ToRotation();
                            while (targetRot < -(float)Math.PI)
                                targetRot += 2f * (float)Math.PI;
                            while (targetRot > (float)Math.PI)
                                targetRot -= 2f * (float)Math.PI;
                            NPC.ai[3] = NPC.ai[3].AngleLerp(targetRot, 0.05f);
                        }

                        if (++NPC.ai[2] > 1) //spam lasers
                        {
                            NPC.ai[2] = 0;
                            SoundEngine.PlaySound(SoundID.Item12, NPC.Center);
                            if (FargoSoulsUtil.HostCheck)
                            {
                                if (NPC.localAI[3] > 1) //p2 shoots to either side of you
                                {
                                    float angleOffset = MathHelper.Lerp(180, 20, NPC.ai[3]);

                                    for (int i = -1; i <= 1; i += 2)
                                    {
                                        Vector2 speed = 16f * NPC.SafeDirectionTo(player.Center).RotatedBy((Main.rand.NextDouble() - 0.5) * 0.785398185253143 / 3.0);
                                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, speed.RotatedBy(MathHelper.ToRadians(angleOffset * i)), ModContent.ProjectileType<AbomLaser>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                                    }
                                }
                                else //p1 shoots directly
                                {
                                    for (int i = 0; i < 2; i++)
                                    {
                                        Vector2 speed = 16f * NPC.ai[3].ToRotationVector2().RotatedBy((Main.rand.NextDouble() - 0.5) * 0.785398185253143 / 2.0);
                                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, speed, ModContent.ProjectileType<AbomLaser>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                                    }
                                }
                            }
                        }

                        if (++NPC.localAI[2] > 60) //shoot rockets
                        {
                            NPC.localAI[2] = 0;

                            int max = WorldSavingSystem.EternityMode ? 5 : 3;
                            for (int i = 0; i < max; i++)
                            {
                                Vector2 vel = NPC.SafeDirectionTo(player.Center).RotatedBy(MathHelper.TwoPi / max * i);
                                vel *= NPC.localAI[3] > 1 ? 5 : 8;
                                vel *= Main.rand.NextFloat(0.9f, 1.1f);
                                vel = vel.RotatedByRandom(MathHelper.TwoPi / max / 3);

                                if (FargoSoulsUtil.HostCheck)
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<AbomRocket>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.target, Main.rand.Next(25, 36));
                            }
                        }
                    }
                    else
                    {
                        if (NPC.localAI[3] > 1)
                        {
                            NPC.ai[3] = 0;
                        }
                        else
                        {
                            NPC.ai[3] = NPC.DirectionFrom(player.Center).ToRotation() - 0.001f;
                            while (NPC.ai[3] < -(float)Math.PI)
                                NPC.ai[3] += 2f * (float)Math.PI;
                            while (NPC.ai[3] > (float)Math.PI)
                                NPC.ai[3] -= 2f * (float)Math.PI;
                        }

                        SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                        //make warning dust
                        for (int i = 0; i < 5; i++)
                        {
                            int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemTopaz, 0f, 0f, 0, default, 1.5f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].velocity *= 4f;
                        }
                    }
                    break;

                case 8: //return to beginning in p1, proceed in p2
                    if (!AliveCheck(player) || Phase2Check())
                        break;
                    NPC.velocity *= 0.9f;
                    NPC.localAI[2] = 0;
                    if (++NPC.ai[1] > 120)
                    {
                        SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                        NPC.netUpdate = true;
                        NPC.ai[1] = 0;
                        NPC.ai[2] = 0;
                        NPC.ai[3] = 0;
                        //NPC.TargetClosest();
                        if (NPC.localAI[3] > 1 && WorldSavingSystem.EternityMode) //if in maso p2, do super attacks
                        {
                            if (NPC.localAI[1] == 0)
                            {
                                NPC.localAI[1] = 1;
                                NPC.ai[0] = 15;
                            }
                            else
                            {
                                NPC.localAI[1] = 0;
                                NPC.ai[0]++;
                            }
                        }
                        else //still in p1
                        {
                            NPC.ai[0] = 0;
                        }
                    }
                    break;

                case 9: //beginning of scythe rows and deathray rain
                    if (NPC.ai[1] == 0 && !AliveCheck(player))
                        break;

                    NPC.velocity = Vector2.Zero;
                    NPC.localAI[2] = 0;

                    if (NPC.ai[1] < 60)
                        FancyFireballs((int)NPC.ai[1]);

                    if (++NPC.ai[1] == 1)
                    {
                        SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                        NPC.ai[3] = NPC.SafeDirectionTo(player.Center).ToRotation();
                        if (FargoSoulsUtil.HostCheck)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.ai[3].ToRotationVector2(), ModContent.ProjectileType<AbomDeathraySmall>(), 0, 0f, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -NPC.ai[3].ToRotationVector2(), ModContent.ProjectileType<AbomDeathraySmall>(), 0, 0f, Main.myPlayer);
                        }
                    }
                    else if (NPC.ai[1] == 61)
                    {
                        const int max = 12;
                        const float gap = 1200 / max;
                        for (int j = -1; j <= 1; j += 2)
                        {
                            Vector2 dustVel = NPC.ai[3].ToRotationVector2() * j * 3f;

                            for (int i = 0; i < 20; i++)
                            {
                                int dust = Dust.NewDust(NPC.Center, 0, 0, DustID.Smoke, dustVel.X, dustVel.Y, 0, default, 3f);
                                Main.dust[dust].velocity *= 1.4f;
                            }

                            for (int i = 1; i <= max + 2; i++)
                            {
                                float speed = i * j * gap / 30;
                                float ai1 = i % 2 == 0 ? -1 : 1;

                                Vector2 vel = speed * NPC.ai[3].ToRotationVector2();

                                for (int k = 0; k < 3; k++)
                                {
                                    int d = Dust.NewDust(NPC.Center, 0, 0, DustID.PurpleCrystalShard, vel.X, vel.Y, Scale: 3f);
                                    Main.dust[d].velocity *= 1.5f;
                                    Main.dust[d].noGravity = true;
                                }

                                if (FargoSoulsUtil.HostCheck)
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<AbomScytheSpin>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, NPC.whoAmI, ai1);
                            }
                        }
                    }
                    else if (NPC.ai[1] > 61 + 420)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                        NPC.ai[3] = 0;
                    }
                    break;

                case 10: //prepare deathray rain
                    if (NPC.ai[1] < 90 && !AliveCheck(player))
                        break;

                    ClearFrozen();

                    /*for (int i = 0; i < 5; i++) //make warning dust
                    {
                        int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, 87, 0f, 0f, 0, default(Color), 1.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].velocity *= 4f;
                    }*/

                    if (NPC.ai[2] == 0 && NPC.ai[3] == 0) //target one side of arena
                    {
                        NPC.ai[2] = NPC.Center.X + (player.Center.X < NPC.Center.X ? -1400 : 1400);
                    }

                    if (NPC.localAI[2] == 0) //direction to dash in next
                    {
                        NPC.localAI[2] = NPC.ai[2] > NPC.Center.X ? -1 : 1;
                    }

                    if (NPC.ai[1] > 90)
                    {
                        FancyFireballs((int)NPC.ai[1] - 90);
                    }
                    else
                    {
                        NPC.ai[3] = player.Center.Y - 300;
                    }

                    targetPos = new Vector2(NPC.ai[2], NPC.ai[3]);
                    Movement(targetPos, 1.4f);

                    if (++NPC.ai[1] > 150)
                    {
                        SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                        NPC.netUpdate = true;
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                        NPC.ai[2] = NPC.localAI[2];
                        NPC.ai[3] = 0;
                        NPC.localAI[2] = 0;
                    }
                    break;

                case 11: //dash and make deathrays
                    NPC.velocity.X = NPC.ai[2] * 18f;
                    MovementY(player.Center.Y - 250, Math.Abs(player.Center.Y - NPC.Center.Y) < 200 ? 2f : 0.7f);
                    NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);

                    ClearFrozen();

                    if (++NPC.ai[3] > 5)
                    {
                        NPC.ai[3] = 0;

                        SoundEngine.PlaySound(SoundID.Item12, NPC.Center);

                        float timeLeft = 2400 / Math.Abs(NPC.velocity.X) * 2 - NPC.ai[1] + 120;
                        if (NPC.ai[1] <= 15)
                        {
                            timeLeft = 0;
                        }
                        else
                        {
                            if (NPC.localAI[2] != 0)
                                timeLeft = 0;
                            if (++NPC.localAI[2] > 2)
                                NPC.localAI[2] = 0;
                        }

                        if (FargoSoulsUtil.HostCheck)
                        {
                            Vector2 vel1 = Vector2.UnitY.RotatedBy(MathHelper.ToRadians(20) * (Main.rand.NextDouble() - 0.5));
                            Vector2 vel2 = -Vector2.UnitY.RotatedBy(MathHelper.ToRadians(20) * (Main.rand.NextDouble() - 0.5));
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel1, ModContent.ProjectileType<AbomDeathrayMark>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, timeLeft);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel2, ModContent.ProjectileType<AbomDeathrayMark>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, timeLeft);
                        }
                    }
                    if (++NPC.ai[1] > 2400 / Math.Abs(NPC.velocity.X))
                    {
                        NPC.netUpdate = true;
                        NPC.velocity.X = NPC.ai[2] * 18f;
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                        //NPC.ai[2] = 0; //will be reused shortly
                        NPC.ai[3] = 0;
                    }
                    break;

                case 12: //prepare for next deathrain
                    if (NPC.ai[1] < 150 && !AliveCheck(player))
                        break;

                    ClearFrozen();

                    NPC.velocity.Y = 0f;

                    /*for (int i = 0; i < 5; i++) //make warning dust
                    {
                        int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, 87, 0f, 0f, 0, default(Color), 1.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].velocity *= 4f;
                    }*/

                    NPC.velocity *= 0.947f;
                    NPC.ai[3] += NPC.velocity.Length();

                    if (NPC.ai[1] > 150)
                        FancyFireballs((int)NPC.ai[1] - 150);

                    if (++NPC.ai[1] > 210)
                    {
                        SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                        NPC.netUpdate = true;
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                        NPC.ai[3] = 0;
                    }
                    break;

                case 13: //second deathray dash
                    NPC.velocity.X = NPC.ai[2] * -18f;
                    MovementY(player.Center.Y - 250, Math.Abs(player.Center.Y - NPC.Center.Y) < 200 ? 2f : 0.7f);
                    NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);

                    ClearFrozen();

                    if (++NPC.ai[3] > 5)
                    {
                        NPC.ai[3] = 0;

                        SoundEngine.PlaySound(SoundID.Item12, NPC.Center);

                        float timeLeft = 2400 / Math.Abs(NPC.velocity.X) * 2 - NPC.ai[1] + 120;
                        if (NPC.ai[1] <= 15)
                        {
                            timeLeft = 0;
                        }
                        else
                        {
                            if (NPC.localAI[2] != 0)
                                timeLeft = 0;
                            if (++NPC.localAI[2] > 2)
                                NPC.localAI[2] = 0;
                        }

                        if (FargoSoulsUtil.HostCheck)
                        {
                            Vector2 vel1 = Vector2.UnitY.RotatedBy(MathHelper.ToRadians(20) * (Main.rand.NextDouble() - 0.5));
                            Vector2 vel2 = -Vector2.UnitY.RotatedBy(MathHelper.ToRadians(20) * (Main.rand.NextDouble() - 0.5));
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel1, ModContent.ProjectileType<AbomDeathrayMark>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, timeLeft);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel2, ModContent.ProjectileType<AbomDeathrayMark>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, timeLeft);
                        }
                    }
                    if (++NPC.ai[1] > 2400 / Math.Abs(NPC.velocity.X))
                    {
                        NPC.netUpdate = true;
                        NPC.velocity.X = NPC.ai[2] * -18f;
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                        NPC.ai[2] = 0;
                        NPC.ai[3] = 0;
                    }
                    break;

                case 14: //pause before looping back to first attack
                    if (!AliveCheck(player))
                        break;
                    NPC.velocity *= 0.9f;
                    if (++NPC.ai[1] > 60)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0] = NPC.dontTakeDamage ? -3 : 0;
                        NPC.ai[1] = 0;
                    }
                    break;

                case 15: //beginning of laevateinn, pause and then sworddash
                    NPC.velocity *= 0.9f;

                    void FancyFireballs(int repeats)
                    {
                        float modifier = 0;
                        for (int i = 0; i < repeats; i++)
                            modifier = MathHelper.Lerp(modifier, 1f, 0.08f);

                        float distance = 1400 * (1f - modifier);
                        float rotation = MathHelper.TwoPi * modifier;
                        const int max = 4;
                        for (int i = 0; i < max; i++)
                        {
                                int d = Dust.NewDust(NPC.Center + distance * Vector2.UnitX.RotatedBy(rotation + MathHelper.TwoPi / max * i), 0, 0, DustID.PurpleCrystalShard, NPC.velocity.X * 0.3f, NPC.velocity.Y * 0.3f, newColor: Color.White);
                                Main.dust[d].noGravity = true;
                                Main.dust[d].scale = 6f - 4f * modifier;
                        }
                    }

                    ClearFrozen();

                    if (NPC.ai[1] < 60)
                        FancyFireballs((int)NPC.ai[1]);

                    if (NPC.ai[1] == 0 && NPC.ai[2] != 2 && FargoSoulsUtil.HostCheck)
                    {
                        float ai1 = NPC.ai[2] == 1 ? -1 : 1;
                        ai1 *= MathHelper.ToRadians(270) / 120 * -1 * 60; //spawning offset of sword below
                        int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 3, ai1);
                        if (p != Main.maxProjectiles)
                        {
                            Main.projectile[p].localAI[1] = NPC.whoAmI;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncProjectile, number: p);
                        }
                    }
                    if (++NPC.ai[1] > 90)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                        NPC.velocity = NPC.SafeDirectionTo(player.Center) * 3f;
                    }
                    else if (NPC.ai[1] == 60 && FargoSoulsUtil.HostCheck)
                    {
                        NPC.netUpdate = true;
                        NPC.velocity = Vector2.Zero;

                        //SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                        float ai0 = NPC.ai[2] == 1 ? -1 : 1;
                        ai0 *= MathHelper.ToRadians(270) / 120;
                        Vector2 vel = NPC.SafeDirectionTo(player.Center).RotatedBy(-ai0 * 60);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, NPC.whoAmI, ai2: 1);
                        if (WorldSavingSystem.MasochistModeReal)
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -vel, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, NPC.whoAmI, ai2: 1);
                    }
                    break;

                case 16: //while dashing
                    NPC.direction = NPC.spriteDirection = Math.Sign(NPC.velocity.X);
                    if (++NPC.ai[1] > 120)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                    }
                    break;

                case 17: //wait for scythes to clear
                    if (!AliveCheck(player))
                        break;

                    ClearFrozen();

                    targetPos = player.Center + player.SafeDirectionTo(NPC.Center) * 500;
                    if (NPC.Distance(targetPos) > 50)
                        Movement(targetPos, 0.7f);
                    if (++NPC.ai[1] > 60) // || (NPC.dontTakeDamage && NPC.ai[1] > 30))
                    {
                        NPC.netUpdate = true;
                        if (++NPC.ai[2] < 2)
                        {
                            NPC.ai[0] -= 2;
                        }
                        else
                        {
                            NPC.ai[0]++;
                            NPC.ai[2] = 0;
                        }
                        NPC.ai[1] = 0;
                    }
                    break;

                case 18: //beginning of vertical dive
                    {
                        if (NPC.ai[1] < 90 && !AliveCheck(player))
                            break;

                        ClearFrozen();

                        /*for (int i = 0; i < 5; i++) //make warning dust
                        {
                            int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, 87, 0f, 0f, 0, default(Color), 1.5f);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].velocity *= 4f;
                        }*/

                        if (NPC.ai[2] == 0 && NPC.ai[3] == 0) //target one side of arena
                        {
                            NPC.netUpdate = true;
                            NPC.ai[2] = player.Center.X;
                            NPC.ai[3] = player.Center.Y;
                            if (FargoSoulsUtil.ProjectileExists(ritualProj, ModContent.ProjectileType<AbomRitual>()) != null)
                            {
                                NPC.ai[2] = Main.projectile[ritualProj].Center.X;
                                NPC.ai[3] = Main.projectile[ritualProj].Center.Y;
                            }

                            Vector2 offset;
                            offset.X = Math.Sign(player.Center.X - NPC.ai[2]);
                            offset.Y = Math.Sign(player.Center.Y - NPC.ai[3]);
                            NPC.localAI[2] = offset.ToRotation();
                        }

                        Vector2 actualTargetPositionOffset = (float)Math.Sqrt(2 * 1200 * 1200) * NPC.localAI[2].ToRotationVector2();
                        actualTargetPositionOffset.Y -= 450 * Math.Sign(actualTargetPositionOffset.Y);

                        targetPos = new Vector2(NPC.ai[2], NPC.ai[3]) + actualTargetPositionOffset;
                        Movement(targetPos, 1f);

                        if (NPC.ai[1] == 0 && FargoSoulsUtil.HostCheck)
                        {
                            float horizontalModifier = Math.Sign(NPC.ai[2] - targetPos.X);
                            float verticalModifier = Math.Sign(NPC.ai[3] - targetPos.Y);

                            float startRotation = horizontalModifier > 0 ? MathHelper.ToRadians(0.1f) * -verticalModifier : MathHelper.Pi - MathHelper.ToRadians(0.1f) * -verticalModifier;
                            float ai1 = horizontalModifier > 0 ? MathHelper.Pi : 0;
                            int p = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, startRotation.ToRotationVector2(), ModContent.ProjectileType<GlowLine>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 4, ai1);
                            if (p != Main.maxProjectiles)
                            {
                                Main.projectile[p].localAI[1] = NPC.whoAmI;
                                if (Main.netMode == NetmodeID.Server)
                                    NetMessage.SendData(MessageID.SyncProjectile, number: p);
                            }
                        }

                        if (NPC.ai[1] > 90)
                            FancyFireballs((int)NPC.ai[1] - 90);

                        if (++NPC.ai[1] > 150)
                        {
                            NPC.netUpdate = true;
                            NPC.velocity = Vector2.Zero;
                            NPC.ai[0]++;
                            NPC.ai[1] = 0;
                        }
                        /*else if (NPC.ai[1] == 180 || (NPC.dontTakeDamage && NPC.ai[1] == 120))
                        {
                            SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                            if (FargoSoulsUtil.HostCheck)
                                Projectile.NewProjectile(npc.GetSource_FromThis(), NPC.Center, Vector2.UnitX * NPC.localAI[2], ModContent.ProjectileType<AbomDeathraySmall2>(), 0, 0f, Main.myPlayer, 0f, NPC.whoAmI);
                        }*/
                    }
                    break;

                case 19: //prepare to dash
                    NPC.direction = NPC.spriteDirection = Math.Sign(NPC.ai[2] - NPC.Center.X);

                    ClearFrozen();
                    int SpinTime = WorldSavingSystem.MasochistModeReal ? 100 : 60;
                    if (NPC.ai[1] == 0)
                    {
                        if (FargoSoulsUtil.HostCheck)
                        {
                            float horizontalModifier = Math.Sign(NPC.ai[2] - NPC.Center.X);
                            float verticalModifier = Math.Sign(NPC.ai[3] - NPC.Center.Y);

                            float ai0 = horizontalModifier * MathHelper.Pi / SpinTime * verticalModifier;
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitX * -horizontalModifier, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, NPC.whoAmI);
                            if (WorldSavingSystem.MasochistModeReal)
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, -Vector2.UnitX * -horizontalModifier, ModContent.ProjectileType<AbomSword>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage, 4f * 3 / 8), 0f, Main.myPlayer, ai0, NPC.whoAmI);
                        }
                    }

                    if (++NPC.ai[1] > SpinTime)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;

                        NPC.velocity.X = 0f;//(player.Center.X - NPC.Center.X) / 90 / 4;
                        NPC.velocity.Y = 24 * Math.Sign(NPC.ai[3] - NPC.Center.Y);
                    }
                    break;

                case 20: //while dashing down

                    ClearFrozen();

                    NPC.velocity.Y *= 0.97f;
                    NPC.position += NPC.velocity;
                    NPC.direction = NPC.spriteDirection = Math.Sign(NPC.ai[2] - NPC.Center.X);
                    if (++NPC.ai[1] > 90)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                    }
                    break;

                case 21: //wait for scythes to clear
                    if (!AliveCheck(player))
                        break;
                    NPC.localAI[2] = 0;
                    targetPos = player.Center;
                    targetPos.X += 500 * (NPC.Center.X < targetPos.X ? -1 : 1);
                    if (NPC.Distance(targetPos) > 50)
                        Movement(targetPos, 0.7f);
                    if (++NPC.ai[1] > 60)
                    {
                        NPC.netUpdate = true;
                        NPC.ai[0] = NPC.dontTakeDamage ? -4 : 0;
                        NPC.ai[1] = 0;
                        NPC.ai[2] = 0;
                        NPC.ai[3] = 0;
                    }
                    break;

                default:
                    Main.NewText("UH OH, STINKY");
                    NPC.netUpdate = true;
                    NPC.ai[0] = 0;
                    goto case 0;
            }

            void ClearFrozen()
            {
                if (NPC.HasBuff<FrozenBuff>())
                {
                    int frozen = NPC.FindBuffIndex(ModContent.BuffType<FrozenBuff>());
                    NPC.DelBuff(frozen);
                }
            }

            if (NPC.ai[0] >= 9 && NPC.dontTakeDamage)
            {
                for (int i = 0; i < 5; i++)
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemTopaz, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 4f;
                }
            }

            if (player.immune || player.hurtCooldowns[0] != 0 || player.hurtCooldowns[1] != 0)
                playerInvulTriggered = true;

            //drop summon
            if (WorldSavingSystem.EternityMode && NPC.downedMoonlord && !WorldSavingSystem.DownedAbom && FargoSoulsUtil.HostCheck && NPC.HasPlayerTarget && !droppedSummon)
            {
                Item.NewItem(NPC.GetSource_Loot(), player.Hitbox, ModContent.ItemType<AbomsCurse>());
                droppedSummon = true;
            }
        }

        private bool AliveCheck(Player player)
        {
            if ((!player.active || player.dead || Vector2.Distance(NPC.Center, player.Center) > 5000f) && NPC.localAI[3] > 0)
            {
                NPC.TargetClosest();
                player = Main.player[NPC.target];
                if (!player.active || player.dead || Vector2.Distance(NPC.Center, player.Center) > 5000f)
                {
                    if (NPC.timeLeft > 30)
                        NPC.timeLeft = 30;
                    NPC.velocity.Y -= 1f;
                    if (NPC.timeLeft == 1)
                    {
                        if (NPC.position.Y < 0)
                            NPC.position.Y = 0;
                        if (FargoSoulsUtil.HostCheck)
                        {
                            FargoSoulsUtil.ClearHostileProjectiles(2, NPC.whoAmI);

                            if (ModContent.TryFind("Fargowiltas", "Abominationn", out ModNPC modNPC) && !NPC.AnyNPCs(modNPC.Type))
                            {
                                int n = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, modNPC.Type);
                                if (n != Main.maxNPCs)
                                {
                                    Main.npc[n].homeless = true;
                                    if (TownNPCName != default)
                                        Main.npc[n].GivenName = TownNPCName;
                                    if (Main.netMode == NetmodeID.Server)
                                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                                }
                            }

                            Main.eclipse = false;
                            NetMessage.SendData(MessageID.WorldData);
                        }
                    }
                    return false;
                }
            }

            if (NPC.timeLeft < 600)
                NPC.timeLeft = 600;

            if (player.Center.Y / 16f > Main.worldSurface)
            {
                NPC.velocity.X *= 0.95f;
                NPC.velocity.Y -= 1f;
                if (NPC.velocity.Y < -32f)
                    NPC.velocity.Y = -32f;
                return false;
            }

            return true;
        }

        private bool Phase2Check()
        {
            if (NPC.localAI[3] > 1)
                return false;

            if (NPC.life < NPC.lifeMax * (WorldSavingSystem.EternityMode ? 0.66 : 0.50) && Main.expertMode)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    NPC.ai[0] = -1;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                    NPC.ai[3] = 0;
                    NPC.netUpdate = true;
                    FargoSoulsUtil.ClearHostileProjectiles(2, NPC.whoAmI);
                }
                return true;
            }
            return false;
        }

        private void Movement(Vector2 targetPos, float speedModifier, bool fastX = true)
        {
            if (Math.Abs(NPC.Center.X - targetPos.X) > 5)
            {
                if (NPC.Center.X < targetPos.X)
                {
                    NPC.velocity.X += speedModifier;
                    if (NPC.velocity.X < 0)
                        NPC.velocity.X += speedModifier * (fastX ? 2 : 1);
                }
                else
                {
                    NPC.velocity.X -= speedModifier;
                    if (NPC.velocity.X > 0)
                        NPC.velocity.X -= speedModifier * (fastX ? 2 : 1);
                }
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
            if (Math.Abs(NPC.velocity.X) > 24)
                NPC.velocity.X = 24 * Math.Sign(NPC.velocity.X);
            if (Math.Abs(NPC.velocity.Y) > 24)
                NPC.velocity.Y = 24 * Math.Sign(NPC.velocity.Y);
        }

        private void MovementY(float targetY, float speedModifier)
        {
            if (NPC.Center.Y < targetY)
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
            if (Math.Abs(NPC.velocity.Y) > 24)
                NPC.velocity.Y = 24 * Math.Sign(NPC.velocity.Y);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (WorldSavingSystem.EternityMode)
            {
                //target.AddBuff(ModContent.BuffType<MutantNibble>(), 300);
                target.AddBuff(ModContent.BuffType<AbomFangBuff>(), 300);
                //target.AddBuff(ModContent.BuffType<Unstable>(), 240);
                //target.AddBuff(ModContent.BuffType<BerserkedBuff>(), 120);
            }
            target.AddBuff(BuffID.Bleeding, 600);
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 3; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemTopaz, 0f, 0f, 0, default, 1f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 3f;
            }
        }

        //public override bool StrikeNPC(ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit)
        //{
        //    if (NPC.localAI[3] > 1 && Main.expertMode)
        //        modifiers.FinalDamage /= 2;

        //    return true;
        //}

        public override bool CheckDead()
        {
            if (NPC.ai[0] == -4 && NPC.ai[1] >= 180)
                return true;

            NPC.life = 1;
            NPC.active = true;
            if (NPC.localAI[3] < 2)
            {
                NPC.localAI[3] = 2;
                /*if (FargoSoulsUtil.HostCheck && Main.expertMode)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<AbomRitual>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 0f, NPC.whoAmI);
                }*/
            }
            if (FargoSoulsUtil.HostCheck && NPC.ai[0] > -2)
            {
                NPC.ai[0] = WorldSavingSystem.MasochistModeReal ? -2 : -4;
                NPC.ai[1] = 0;
                NPC.ai[2] = 0;
                NPC.ai[3] = 0;
                NPC.localAI[2] = 0;
                NPC.dontTakeDamage = true;
                NPC.netUpdate = true;
                FargoSoulsUtil.ClearHostileProjectiles(2, NPC.whoAmI);
            }
            return false;
        }

        public override void OnKill()
        {
            base.OnKill();

            if (!playerInvulTriggered && WorldSavingSystem.EternityMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.Hitbox, ModContent.ItemType<BrokenHilt>());
            }

            NPC.SetEventFlagCleared(ref WorldSavingSystem.downedAbom, -1);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            base.ModifyNPCLoot(npcLoot);

            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<AbomEnergy>(), 1, 10, 20));
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<AbomBag>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AbomTrophy>(), 10));

            npcLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<AbomRelic>()));
            npcLoot.Add(ItemDropRule.MasterModeDropOnAllPlayers(ModContent.ItemType<BabyScythe>(), 4));

            LeadingConditionRule emodeRule = new(new EModeDropCondition());
            emodeRule.OnSuccess(FargoSoulsUtil.BossBagDropCustom(ModContent.ItemType<AbominableWand>()));
            npcLoot.Add(emodeRule);
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void FindFrame(int frameHeight)
        {
            if (++NPC.frameCounter > 6)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= 4 * frameHeight)
                    NPC.frame.Y = 0;
            }
        }

        public override void BossHeadSpriteEffects(ref SpriteEffects spriteEffects)
        {
            //spriteEffects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.IsABestiaryIconDummy)
            {
                Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
                Rectangle rectangle = NPC.frame;
                Vector2 origin2 = rectangle.Size() / 2f;

                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                Main.EntitySpriteDraw(texture2D13, NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), NPC.GetAlpha(drawColor), NPC.rotation, origin2, NPC.scale, effects, 0);
                return false;
            }
                
            /*Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
            Rectangle rectangle = NPC.frame;
            Vector2 origin2 = rectangle.Size() / 2f;

            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Main.EntitySpriteDraw(texture2D13, NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), NPC.GetAlpha(drawColor), NPC.rotation, origin2, NPC.scale, effects, 0);*/

            if (NPC.localAI[3] == 1) // draw aura
            {
                Color outerColor = Color.DarkOrange;
                outerColor.A = 0;

                Color darkColor = outerColor;
                Color mediumColor = Color.Lerp(outerColor, Color.White, 0.75f);
                Color lightColor2 = Color.Lerp(outerColor, Color.White, 0.5f);

                Vector2 auraPos = NPC.Center;
                float radius = 2000f;
                var target = Main.LocalPlayer;
                var blackTile = TextureAssets.MagicPixel;
                var diagonalNoise = FargosTextureRegistry.WavyNoise;
                if (!blackTile.IsLoaded || !diagonalNoise.IsLoaded)
                    return false;
                var maxOpacity = NPC.Opacity;

                ManagedShader borderShader = ShaderManager.GetShader("FargowiltasSouls.MutantP1Aura");
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

                spriteBatch.GraphicsDevice.Textures[1] = diagonalNoise.Value;

                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, borderShader.WrappedEffect, Main.GameViewMatrix.TransformationMatrix);
                Rectangle rekt = new(Main.screenWidth / 2, Main.screenHeight / 2, Main.screenWidth, Main.screenHeight);
                spriteBatch.Draw(blackTile.Value, rekt, null, default, 0f, blackTile.Value.Size() * 0.5f, 0, 0f);
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
            return false;
        }
    }
}
