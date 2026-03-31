using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Content.Items.Armor;
using FargowiltasSouls.Content.Items.Pets;
using FargowiltasSouls.Content.Items.Placables.Relics;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.ItemDropRules;
using FargowiltasSouls.Core.Systems;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.Bosses.Champions.Spirit
{
    [AutoloadBossHead]
    public class SpiritChampion : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Champion of Spirit");
            //DisplayName.AddTranslation((int)GameCulture.CultureName.Chinese, "魂灵英灵");
            Main.npcFrameCount[NPC.type] = 2;
            NPCID.Sets.TrailCacheLength[NPC.type] = 6;
            NPCID.Sets.TrailingMode[NPC.type] = 1;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.NoMultiplayerSmoothingByType[NPC.type] = true;
            NPCID.Sets.MustAlwaysDraw[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(NPC.type);

            NPC.AddDebuffImmunities(
            [
                BuffID.Confused,
                BuffID.Chilled,
                BuffID.OnFire,
                BuffID.Suffocation,
                ModContent.BuffType<LethargicBuff>(),
                ModContent.BuffType<ClippedWingsBuff>()
            ]);

            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                CustomTexturePath = $"FargowiltasSouls/Content/Bosses/Champions/Spirit/{Name}_Still",
                Position = new Vector2(4, 0),
                PortraitScale = 0.5f,
                PortraitPositionXOverride = 0
            });
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange([
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundDesert,
                new FlavorTextBestiaryInfoElement($"Mods.FargowiltasSouls.Bestiary.{Name}")
            ]);
        }

        public override Color? GetAlpha(Color drawColor)
        {
            if (NPC.IsABestiaryIconDummy)
            {
                // This is required because we have NPC.alpha = 255, in the bestiary it would look transparent
                return NPC.GetBestiaryEntryColor();
            }
            return base.GetAlpha(drawColor);
        }

        private bool doPredictiveSandnado;
        public const float auraDistance = 1200;

        public int GrabCooldown;

        public override void SetDefaults()
        {
            NPC.width = 120;
            NPC.height = 150;
            NPC.damage = 125;
            NPC.defense = 40;
            NPC.lifeMax = 630000;
            NPC.HitSound = SoundID.NPCHit54;
            NPC.DeathSound = SoundID.NPCDeath52;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.aiStyle = -1;
            NPC.value = Item.buyPrice(6, 50);
            NPC.boss = true;

            Music = ModLoader.TryGetMod("FargowiltasMusic", out Mod musicMod)
                ? MusicLoader.GetMusicSlot(musicMod, "Assets/Music/Champions") : MusicID.OtherworldlyBoss1;
            SceneEffectPriority = SceneEffectPriority.BossLow;

            NPC.dontTakeDamage = true;
            NPC.alpha = 255;
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            //NPC.damage = (int)(NPC.damage * 0.5f);
            NPC.lifeMax = (int)(NPC.lifeMax * balance);
        }

        public override bool CanHitPlayer(Player target, ref int CooldownSlot)
        {
            CooldownSlot = 1;
            return true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.localAI[0]);
            writer.Write(NPC.localAI[1]);
            writer.Write(NPC.localAI[2]);
            writer.Write(NPC.localAI[3]);

            writer.Write(GrabCooldown);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.localAI[0] = reader.ReadSingle();
            NPC.localAI[1] = reader.ReadSingle();
            NPC.localAI[2] = reader.ReadSingle();
            NPC.localAI[3] = reader.ReadSingle();

            GrabCooldown = reader.ReadInt32();
        }

        public override void AI()
        {
            EModeGlobalNPC.championBoss = NPC.whoAmI;

            if (GrabCooldown > 0)
                GrabCooldown--;

            if (NPC.localAI[3] == 0) //spawn friends
            {
                NPC.TargetClosest(false);

                if (NPC.ai[2] == 1)
                {
                    NPC.velocity = Vector2.Zero;
                    NPC.noTileCollide = true;
                    NPC.noGravity = true;
                    NPC.alpha = 0;

                    if (NPC.ai[1] < 120)
                        NPC.ai[1] = 120;

                    if (NPC.ai[1] == 180)
                    {
                        SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                        if (FargoSoulsUtil.HostCheck)
                        {
                            int n = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<SpiritChampionHand>(), NPC.whoAmI, 0f, NPC.whoAmI, -1f, -1f, NPC.target);
                            if (n != Main.maxNPCs)
                            {
                                Main.npc[n].velocity.X = Main.rand.NextFloat(-24f, 24f);
                                Main.npc[n].velocity.Y = Main.rand.NextFloat(-24f, 24f);
                                if (Main.netMode == NetmodeID.Server)
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                            }
                            n = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<SpiritChampionHand>(), NPC.whoAmI, 0f, NPC.whoAmI, -1f, 1f, NPC.target);
                            if (n != Main.maxNPCs)
                            {
                                Main.npc[n].velocity.X = Main.rand.NextFloat(-24f, 24f);
                                Main.npc[n].velocity.Y = Main.rand.NextFloat(-24f, 24f);
                                if (Main.netMode == NetmodeID.Server)
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                            }
                            n = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<SpiritChampionHand>(), NPC.whoAmI, 0f, NPC.whoAmI, 1f, -1f, NPC.target);
                            if (n != Main.maxNPCs)
                            {
                                Main.npc[n].velocity.X = Main.rand.NextFloat(-24f, 24f);
                                Main.npc[n].velocity.Y = Main.rand.NextFloat(-24f, 24f);
                                if (Main.netMode == NetmodeID.Server)
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                            }
                            n = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<SpiritChampionHand>(), NPC.whoAmI, 0f, NPC.whoAmI, 1f, 1f, NPC.target);
                            if (n != Main.maxNPCs)
                            {
                                Main.npc[n].velocity.X = Main.rand.NextFloat(-24f, 24f);
                                Main.npc[n].velocity.Y = Main.rand.NextFloat(-24f, 24f);
                                if (Main.netMode == NetmodeID.Server)
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                            }
                        }
                    }

                    if (++NPC.ai[1] > 300)
                    {
                        if (FargoSoulsUtil.HostCheck && WorldSavingSystem.MasochistModeReal) // extra hand that always grabs you
                        {

                            int n2 = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<SpiritChampionHand>(), NPC.whoAmI, 3f, NPC.whoAmI, 1f, 1f, NPC.target);
                            if (n2 != Main.maxNPCs)
                            {
                                Main.npc[n2].velocity.X = Main.rand.NextFloat(-24f, 24f);
                                Main.npc[n2].velocity.Y = Main.rand.NextFloat(-24f, 24f);
                                if (Main.netMode == NetmodeID.Server)
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n2);
                            }
                        }
                        NPC.ai[1] = 0;
                        NPC.ai[2] = 0;
                        NPC.ai[3] = 0;
                        NPC.localAI[3] = 1;
                        NPC.netUpdate = true;
                    }
                    return;
                }

                if (NPC.ai[3] == 0 && NPC.HasValidTarget)
                {
                    NPC.ai[3] = 1;
                    NPC.Center = Main.player[NPC.target].Center - Vector2.UnitY * 500;
                    NPC.ai[2] = Main.player[NPC.target].Center.Y - NPC.height / 2; //fall this distance, accounting for my height
                    NPC.netUpdate = true;
                }

                NPC.alpha -= 10;
                if (NPC.alpha < 0)
                {
                    NPC.alpha = 0;
                    NPC.noGravity = false;
                }

                if (NPC.Center.Y > NPC.ai[2])
                {
                    NPC.noTileCollide = false;
                }

                if (++NPC.ai[1] > 300 || NPC.velocity.Y == 0 && NPC.ai[1] > 30)
                {
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 1;

                    SoundEngine.PlaySound(SoundID.Item14, NPC.Center);

                    for (int k = -2; k <= 2; k++) //explosions
                    {
                        Vector2 dustPos = NPC.Center;
                        int width = NPC.width / 5;
                        dustPos.X += width * k;
                        dustPos.Y += NPC.height / 2;

                        for (int i = 0; i < 20; i++)
                        {
                            int dust = Dust.NewDust(dustPos - new Vector2(16, 16), 32, 32, DustID.Smoke, 0f, 0f, 100, default, 2f);
                            //Main.dust[dust].velocity *= 1.4f;
                        }

                        /*for (int i = 0; i < 20; i++)
                        {
                            int dust = Dust.NewDust(dustPos - new Vector2(16, 16), 32, 32, 6, 0f, 0f, 100, default(Color), 3.5f);
                            Main.dust[dust].noGravity = true;
                            Main.dust[dust].velocity *= 7f;
                            dust = Dust.NewDust(dustPos - new Vector2(16, 16), 32, 32, 6, 0f, 0f, 100, default(Color), 1.5f);
                            Main.dust[dust].velocity *= 3f;
                        }*/

                        float scaleFactor9 = 0.5f;
                        for (int j = 0; j < 4; j++)
                        {
                            int gore = Gore.NewGore(NPC.GetSource_FromThis(), dustPos, default, Main.rand.Next(61, 64));
                            Main.gore[gore].velocity *= scaleFactor9;
                            //Main.gore[gore].velocity.X += 1f;
                            //Main.gore[gore].velocity.Y += 1f;
                        }
                    }
                }

                return;
            }

            Player player = Main.player[NPC.target];
            Vector2 targetPos;

            if (NPC.HasValidTarget && NPC.Distance(player.Center) < 2500 && (Framing.GetTileSafely(player.Center).WallType != WallID.None || player.ZoneUndergroundDesert))
                NPC.timeLeft = 600;

            NPC.dontTakeDamage = false;

            switch ((int)NPC.ai[0])
            {
                case -4: //final float
                    NPC.dontTakeDamage = true;
                    goto case 0;

                case -3: //final you think you're safe
                    NPC.dontTakeDamage = true;

                    if (NPC.localAI[2] == 0)
                        NPC.localAI[2] = 1;

                    if (!player.active || player.dead || Vector2.Distance(NPC.Center, player.Center) > 2500f
                        || Framing.GetTileSafely(player.Center).WallType == WallID.None && !player.ZoneUndergroundDesert) //despawn code
                    {
                        NPC.TargetClosest(false);
                        if (NPC.timeLeft > 30)
                            NPC.timeLeft = 30;

                        NPC.noTileCollide = true;
                        NPC.noGravity = true;
                        NPC.velocity.Y += 1f;

                        return;
                    }

                    targetPos = new Vector2(NPC.localAI[0], NPC.localAI[1]);
                    if (NPC.Distance(targetPos) > 25)
                        Movement(targetPos, 0.8f, 24f);

                    if (NPC.ai[1] == 0) //respawn dead hands
                    {
                        int size = WorldSavingSystem.MasochistModeReal ? 5 : 4;
                        bool[] foundHand = new bool[size];

                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<SpiritChampionHand>() && Main.npc[i].ai[1] == NPC.whoAmI)
                            {
                                if (!foundHand[0])
                                    foundHand[0] = Main.npc[i].ai[2] == -1f && Main.npc[i].ai[3] == -1f;
                                if (!foundHand[1])
                                    foundHand[1] = Main.npc[i].ai[2] == -1f && Main.npc[i].ai[3] == 1f;
                                if (!foundHand[2])
                                    foundHand[2] = Main.npc[i].ai[2] == 1f && Main.npc[i].ai[3] == -1f;
                                if (!foundHand[3])
                                    foundHand[3] = Main.npc[i].ai[2] == 1f && Main.npc[i].ai[3] == 1f;
                                if (WorldSavingSystem.MasochistModeReal && !foundHand[4])
                                    foundHand[4] = Main.npc[i].ai[0] == 3f || Main.npc[i].ai[3] == 1f;
                            }
                        }

                        if (FargoSoulsUtil.HostCheck) //if hands somehow disappear
                        {
                            if (!foundHand[0])
                            {
                                int n = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<SpiritChampionHand>(), NPC.whoAmI, 0f, NPC.whoAmI, -1f, -1f, NPC.target);
                                if (n != Main.maxNPCs)
                                {
                                    Main.npc[n].velocity.X = Main.rand.NextFloat(-24f, 24f);
                                    Main.npc[n].velocity.Y = Main.rand.NextFloat(-24f, 24f);
                                    if (Main.netMode == NetmodeID.Server)
                                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                                }
                            }
                            if (!foundHand[1])
                            {
                                int n = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<SpiritChampionHand>(), NPC.whoAmI, 0f, NPC.whoAmI, -1f, 1f, NPC.target);
                                if (n != Main.maxNPCs)
                                {
                                    Main.npc[n].velocity.X = Main.rand.NextFloat(-24f, 24f);
                                    Main.npc[n].velocity.Y = Main.rand.NextFloat(-24f, 24f);
                                    if (Main.netMode == NetmodeID.Server)
                                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                                }
                            }
                            if (!foundHand[2])
                            {
                                int n = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<SpiritChampionHand>(), NPC.whoAmI, 0f, NPC.whoAmI, 1f, -1f, NPC.target);
                                if (n != Main.maxNPCs)
                                {
                                    Main.npc[n].velocity.X = Main.rand.NextFloat(-24f, 24f);
                                    Main.npc[n].velocity.Y = Main.rand.NextFloat(-24f, 24f);
                                    if (Main.netMode == NetmodeID.Server)
                                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                                }
                            }
                            if (!foundHand[3])
                            {
                                int n = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<SpiritChampionHand>(), NPC.whoAmI, 0f, NPC.whoAmI, 1f, 1f, NPC.target);
                                if (n != Main.maxNPCs)
                                {
                                    Main.npc[n].velocity.X = Main.rand.NextFloat(-24f, 24f);
                                    Main.npc[n].velocity.Y = Main.rand.NextFloat(-24f, 24f);
                                    if (Main.netMode == NetmodeID.Server)
                                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                                }
                            }
                            if (WorldSavingSystem.MasochistModeReal && !foundHand[4])
                            {
                                int n = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<SpiritChampionHand>(), NPC.whoAmI, 3f, NPC.whoAmI, 1f, 1f, NPC.target);
                                if (n != Main.maxNPCs)
                                {
                                    Main.npc[n].velocity.X = Main.rand.NextFloat(-24f, 24f);
                                    Main.npc[n].velocity.Y = Main.rand.NextFloat(-24f, 24f);
                                    if (Main.netMode == NetmodeID.Server)
                                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                                }
                            }
                        }
                    }
                    else if (NPC.ai[1] == 120)
                    {
                        SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                        for (int i = 0; i < Main.maxNPCs; i++) //update ai
                        {
                            if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<SpiritChampionHand>() && Main.npc[i].ai[1] == NPC.whoAmI && Main.npc[i].ai[0] != 3f)
                            {
                                Main.npc[i].ai[0] = 1f;
                                Main.npc[i].netUpdate = true;
                            }
                        }

                        if (FargoSoulsUtil.HostCheck) //spawn super hand
                        {

                            int n2 = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<SpiritChampionHand>(), NPC.whoAmI, 3f, NPC.whoAmI, 1f, 1f, NPC.target);
                            if (n2 != Main.maxNPCs)
                            {
                                Main.npc[n2].velocity.X = Main.rand.NextFloat(-24f, 24f);
                                Main.npc[n2].velocity.Y = Main.rand.NextFloat(-24f, 24f);
                                if (Main.netMode == NetmodeID.Server)
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n2);
                            }
                        }
                    }

                    if (++NPC.ai[2] > 85) //bone spray
                    {
                        NPC.ai[2] = 0;

                        if (FargoSoulsUtil.HostCheck)
                        {
                            SoundEngine.PlaySound(SoundID.Item2, NPC.Center);
                            int crosses = WorldSavingSystem.MasochistModeReal ? 12 : 3;
                            for (int i = 0; i < crosses; i++)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.position.X + Main.rand.Next(NPC.width), NPC.position.Y + Main.rand.Next(NPC.height),
                                    Main.rand.NextFloat(-8f, 8f), Main.rand.NextFloat(-8f, 8f), ModContent.ProjectileType<SpiritCrossBone>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                            }
                        }
                    }

                    if (++NPC.ai[3] > 110)
                    {
                        NPC.ai[3] = 0;
                        if (FargoSoulsUtil.HostCheck) //sandnado
                        {
                            Vector2 target = player.Center;
                            target.Y -= 100;
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), target, Vector2.Zero, ProjectileID.SandnadoHostileMark, 0, 0f, Main.myPlayer);

                            int length = (int)NPC.Distance(target) / 10;
                            Vector2 offset = NPC.SafeDirectionTo(target) * 10f;
                            for (int i = 0; i < length; i++) //dust warning line for sandnado
                            {
                                int d = Dust.NewDust(NPC.Center + offset * i, 0, 0, DustID.Sandnado, 0f, 0f, 0, new Color());
                                Main.dust[d].noLight = true;
                                Main.dust[d].scale = 1.25f;
                            }
                        }
                    }

                    if (++NPC.ai[1] > 600)
                    {
                        NPC.dontTakeDamage = false;
                        NPC.netUpdate = true;
                        NPC.ai[0] = 0;
                        NPC.ai[1] = 0;
                        NPC.ai[2] = 0;
                        NPC.ai[3] = 0;
                        NPC.localAI[3] = 2; //can die now
                    }
                    break;

                case -1:
                    targetPos = new Vector2(NPC.localAI[0], NPC.localAI[1]);
                    if (NPC.Distance(targetPos) > 25)
                        Movement(targetPos, 0.8f, 24f);

                    if (++NPC.ai[1] > 360)
                    {
                        NPC.TargetClosest();
                        NPC.ai[0] = 4;
                        NPC.ai[1] = 0;
                        NPC.ai[2] = 0;
                        NPC.ai[3] = 0;
                        NPC.netUpdate = true;

                        if (NPC.Hitbox.Intersects(player.Hitbox))
                        {
                            player.velocity.X = player.Center.X < NPC.Center.X ? -15f : 15f;
                            player.velocity.Y = -10f;
                            SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                        }
                    }
                    break;

                case 0: //float to player
                    if (!player.active || player.dead || Vector2.Distance(NPC.Center, player.Center) > 2500f
                        || Framing.GetTileSafely(player.Center).WallType == WallID.None && !player.ZoneUndergroundDesert) //despawn code
                    {
                        NPC.TargetClosest(false);
                        if (NPC.timeLeft > 30)
                            NPC.timeLeft = 30;

                        NPC.noTileCollide = true;
                        NPC.noGravity = true;
                        NPC.velocity.Y += 1f;

                        return;
                    }

                    if (NPC.ai[1] == 0)
                    {
                        targetPos = player.Center;
                        NPC.velocity = (targetPos - NPC.Center) / 75;

                        NPC.localAI[0] = targetPos.X;
                        NPC.localAI[1] = targetPos.Y;
                    }

                    if (++NPC.ai[1] > 75)
                    {
                        NPC.TargetClosest();
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                        NPC.ai[2] = 0;
                        NPC.ai[3] = 0;
                        NPC.netUpdate = true;
                    }
                    break;

                case 1: //cross bone/sandnado
                    if (NPC.localAI[2] == 0)
                        NPC.localAI[2] = 1;

                    targetPos = new Vector2(NPC.localAI[0], NPC.localAI[1]);
                    if (NPC.Distance(targetPos) > 25)
                        Movement(targetPos, 0.8f, 24f);

                    if (++NPC.ai[2] > 45)
                    {
                        NPC.ai[2] = 0;

                        if (FargoSoulsUtil.HostCheck)
                        {
                            if (NPC.ai[1] < 180) //cross bones
                            {
                                SoundEngine.PlaySound(SoundID.Item2, NPC.Center);
                                int crosses = WorldSavingSystem.MasochistModeReal ? 12 : 3;
                                for (int i = 0; i < crosses; i++)
                                {
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.position.X + Main.rand.Next(NPC.width), NPC.position.Y + Main.rand.Next(NPC.height),
                                        Main.rand.NextFloat(-8f, 8f), Main.rand.NextFloat(-8f, 8f), ModContent.ProjectileType<SpiritCrossBone>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                                }
                            }
                            else //sandnado
                            {
                                doPredictiveSandnado = !doPredictiveSandnado;

                                Vector2 target = player.Center;
                                if (doPredictiveSandnado && NPC.life < NPC.lifeMax * 0.66)
                                    target += player.velocity * 30f; //alternate between predictive and direct aim
                                target.Y -= 100;
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), target, Vector2.Zero, ProjectileID.SandnadoHostileMark, 0, 0f, Main.myPlayer);

                                int length = (int)NPC.Distance(target) / 10;
                                Vector2 offset = NPC.SafeDirectionTo(target) * 10f;
                                for (int i = 0; i < length; i++) //dust warning line for sandnado
                                {
                                    int d = Dust.NewDust(NPC.Center + offset * i, 0, 0, DustID.Sandnado, 0f, 0f, 0, new Color());
                                    Main.dust[d].noLight = true;
                                    Main.dust[d].scale = 1.25f;
                                }
                            }
                        }
                    }

                    if (++NPC.ai[1] > 400)
                    {
                        NPC.TargetClosest();
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                        NPC.ai[2] = 0;
                        NPC.ai[3] = 0;
                        NPC.netUpdate = true;
                    }
                    break;

                case 2:
                    goto case 0;

                case 3: //grab
                    targetPos = new Vector2(NPC.localAI[0], NPC.localAI[1]);
                    if (NPC.Distance(targetPos) > 25)
                        Movement(targetPos, 0.8f, 24f);

                    if (++NPC.ai[2] == 30)
                    {
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<SpiritChampionHand>() && Main.npc[i].ai[1] == NPC.whoAmI && Main.npc[i].ai[0] != 3f)
                            {
                                Main.npc[i].ai[0] = 1f;
                                Main.npc[i].netUpdate = true;
                            }
                        }
                    }

                    if (NPC.life < NPC.lifeMax * 0.66)
                    {
                        if (++NPC.ai[3] > 55) //homing spectre bolts
                        {
                            NPC.ai[3] = 0;
                            if (FargoSoulsUtil.HostCheck)
                            {
                                const int max = 5;
                                for (int i = 0; i < max; i++)
                                {
                                    Vector2 speed = Main.rand.NextFloat(1, 2) * Vector2.UnitX.RotatedByRandom(Math.PI * 2);
                                    float ai1 = 60 + Main.rand.Next(30);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, speed, ModContent.ProjectileType<SpiritSpirit>(),
                                        FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, NPC.whoAmI, ai1);
                                }
                            }
                        }
                    }

                    if (++NPC.ai[1] > 360)
                    {
                        NPC.TargetClosest();
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                        NPC.ai[2] = 0;
                        NPC.ai[3] = 0;
                        NPC.netUpdate = true;
                    }
                    break;

                case 4:
                    goto case 0;

                case 5: //swords
                    targetPos = new Vector2(NPC.localAI[0], NPC.localAI[1]);
                    if (NPC.Distance(targetPos) > 25)
                        Movement(targetPos, 0.8f, 24f);

                    if (++NPC.ai[2] > 80)
                    {
                        NPC.ai[2] = 0;

                        SoundEngine.PlaySound(SoundID.Item92, NPC.Center);

                        if (FargoSoulsUtil.HostCheck)
                        {
                            for (int i = 0; i < 15; i++) //sword burst
                            {
                                float speed = Main.rand.NextFloat(4f, 8f);
                                Vector2 velocity = speed * Vector2.UnitX.RotatedBy(Main.rand.NextDouble() * 2 * Math.PI);
                                float ai1 = speed / Main.rand.NextFloat(60f, 120f);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<SpiritSword>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, 0f, ai1);
                            }

                            if (NPC.life < NPC.lifeMax * 0.66)
                            {
                                const int max = 12; //hand ring
                                for (int i = 0; i < max; i++)
                                {
                                    Vector2 vel = NPC.SafeDirectionTo(player.Center).RotatedBy(Math.PI * 2 / max * i);
                                    float ai0 = 1.04f;
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<SpiritHand>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, ai0);
                                }
                            }
                        }
                    }

                    if (++NPC.ai[1] > 300)
                    {
                        NPC.TargetClosest();
                        NPC.ai[0]++;
                        NPC.ai[1] = 0;
                        NPC.ai[2] = 0;
                        NPC.ai[3] = 0;
                        NPC.netUpdate = true;
                    }
                    break;

                case 6:
                    goto case 0;

                case 7: //skip this number, staying on even number allows hands to remain drawn close
                    NPC.ai[0]++;
                    break;

                case 8: //shadow hands, reflect, mummy spirits
                    {
                        targetPos = new Vector2(NPC.localAI[0], NPC.localAI[1]);
                        if (NPC.Distance(targetPos) > 25)
                            Movement(targetPos, 0.8f, 24f);

                        /* lol!
                        const float distance = 150;

                        for (int i = 0; i < 20; i++)
                        {
                            Vector2 offset = new();
                            double angle = Main.rand.NextDouble() * 2d * Math.PI;
                            offset.X += (float)(Math.Sin(angle) * distance);
                            offset.Y += (float)(Math.Cos(angle) * distance);
                            Dust dust = Main.dust[Dust.NewDust(
                                NPC.Center + offset - new Vector2(4, 4), 0, 0,
                                DustID.GemTopaz, 0, 0, 100, Color.White, 1f
                                )];
                            dust.velocity = NPC.velocity;
                            //if (Main.rand.NextBool(3)) dust.velocity += Vector2.Normalize(offset) * -5f;
                            dust.noGravity = true;
                        }

                        if (NPC.ai[1] > 80)
                        {
                            Main.projectile.Where(x => x.active && x.friendly && !FargoSoulsUtil.IsSummonDamage(x, false)).ToList().ForEach(x => //reflect projectiles
                            {
                                if (Vector2.Distance(x.Center, NPC.Center) <= distance)
                                {
                                    for (int i = 0; i < 5; i++)
                                    {
                                        int dustId = Dust.NewDust(x.position, x.width, x.height, DustID.GemTopaz,
                                            x.velocity.X * 0.2f, x.velocity.Y * 0.2f, 100, default, 1.5f);
                                        Main.dust[dustId].noGravity = true;
                                    }

                                    // Set ownership
                                    x.hostile = true;
                                    x.friendly = false;
                                    x.owner = Main.myPlayer;
                                    x.damage /= 4;

                                    // Turn around
                                    x.velocity *= -1f;

                                    // Flip sprite
                                    if (x.Center.X > NPC.Center.X * 0.5f)
                                    {
                                        x.direction = 1;
                                        x.spriteDirection = 1;
                                    }
                                    else
                                    {
                                        x.direction = -1;
                                        x.spriteDirection = -1;
                                    }

                                    //x.netUpdate = true;

                                    if (x.owner == Main.myPlayer)
                                        Projectile.NewProjectile(NPC.GetSource_FromThis(), x.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Souls.IronParry>(), 0, 0f, Main.myPlayer);
                                }
                            });
                        }

                        */
                        if (NPC.ai[1] == 0)
                        {
                            SoundEngine.PlaySound(SoundID.ForceRoarPitched, NPC.Center);
                            //if (FargoSoulsUtil.HostCheck)
                            //    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.GlowRing>(), 0, 0f, Main.myPlayer, NPC.whoAmI, -6);
                        }

                        if (++NPC.ai[3] > 10) //spirits
                        {
                            NPC.ai[3] = 0;

                            SoundEngine.PlaySound(SoundID.Item8, NPC.Center);

                            if (FargoSoulsUtil.HostCheck) //vanilla code from desert spirits idfk
                            {
                                Point tileCoordinates1 = NPC.Center.ToTileCoordinates();
                                Point tileCoordinates2 = Main.player[NPC.target].Center.ToTileCoordinates();
                                Vector2 vector2 = Main.player[NPC.target].Center - NPC.Center;
                                int num1 = 6;
                                int num2 = 6;
                                int num3 = 0;
                                int num4 = 2;
                                int num5 = 0;
                                bool flag1 = false;
                                if (vector2.Length() > 2000)
                                    flag1 = true;
                                while (!flag1 && num5 < 50)
                                {
                                    ++num5;
                                    int index1 = Main.rand.Next(tileCoordinates2.X - num1, tileCoordinates2.X + num1 + 1);
                                    int index2 = Main.rand.Next(tileCoordinates2.Y - num1, tileCoordinates2.Y + num1 + 1);
                                    if ((index2 < tileCoordinates2.Y - num3 || index2 > tileCoordinates2.Y + num3 || index1 < tileCoordinates2.X - num3 || index1 > tileCoordinates2.X + num3) && (index2 < tileCoordinates1.Y - num2 || index2 > tileCoordinates1.Y + num2 || index1 < tileCoordinates1.X - num2 || index1 > tileCoordinates1.X + num2) && !Main.tile[index1, index2].HasUnactuatedTile)
                                    {
                                        bool flag2 = true;
                                        if (flag2 && Main.tile[index1, index2].LiquidType == LiquidID.Lava && Main.tile[index1, index2].LiquidAmount > 0)
                                            flag2 = false;
                                        if (flag2 && Collision.SolidTiles(index1 - num4, index1 + num4, index2 - num4, index2 + num4))
                                            flag2 = false;
                                        if (flag2)
                                        {
                                            Projectile.NewProjectile(NPC.GetSource_FromThis(), index1 * 16 + 8, index2 * 16 + 8, 0, 0f,
                                                ProjectileID.DesertDjinnCurse, 0, 1f, Main.myPlayer, NPC.target, 0);
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (++NPC.ai[2] > 70) //hands
                        {
                            NPC.ai[2] = 0;

                            if (FargoSoulsUtil.HostCheck)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    Vector2 vel = NPC.SafeDirectionTo(player.Center).RotatedBy(Math.PI / 6 * (Main.rand.NextDouble() - 0.5));
                                    float ai0 = Main.rand.NextFloat(1.04f, 1.06f);
                                    float ai1 = Main.rand.NextFloat(0.025f);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<SpiritHand>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer, ai0, ai1);
                                }
                            }
                        }

                        if (NPC.ai[1] % 30 == 0 && FargoSoulsUtil.HostCheck && NPC.life < NPC.lifeMax * 0.66)
                        {
                            SoundEngine.PlaySound(SoundID.Item2, NPC.Center);
                            int crossbones = WorldSavingSystem.MasochistModeReal ? 3 : 2;
                            for (int i = 0; i < crossbones; i++)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.position.X + Main.rand.Next(NPC.width), NPC.position.Y + Main.rand.Next(NPC.height),
                                    Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-8f, 0f), ModContent.ProjectileType<SpiritCrossBone>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.position.X + Main.rand.Next(NPC.width), NPC.position.Y + Main.rand.Next(NPC.height),
                                    Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(8f, 0f), ModContent.ProjectileType<SpiritCrossBoneReverse>(), FargoSoulsUtil.ScaledProjectileDamage(NPC.defDamage), 0f, Main.myPlayer);
                            }
                        }

                        if (++NPC.ai[1] > 360)
                        {
                            NPC.TargetClosest();
                            NPC.ai[0]++;
                            NPC.ai[1] = 0;
                            NPC.ai[2] = 0;
                            NPC.ai[3] = 0;
                            NPC.netUpdate = true;
                        }
                    }
                    break;

                case 9: //skip this number, get back to usual behaviour
                    NPC.ai[0]++;
                    break;

                /*case 10:
                    goto case 0;*/

                default:
                    NPC.ai[0] = 0;
                    goto case 0;
            }

            if (NPC.localAI[2] != 0 && WorldSavingSystem.EternityMode) //aura
            {
                float range = NPC.Distance(player.Center);
                if (range > auraDistance && range < 3000)
                {
                    if (++NPC.localAI[2] > 60)
                    {
                        NPC.localAI[2] = 1;
                        NPC.netUpdate = true;

                        SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                        if (FargoSoulsUtil.HostCheck) //spawn super hand
                        {

                            int n2 = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<SpiritChampionHand>(), NPC.whoAmI, 4f, NPC.whoAmI, 1f, 1f, NPC.target);
                            if (n2 != Main.maxNPCs)
                            {
                                Main.npc[n2].velocity.X = Main.rand.NextFloat(-24f, 24f);
                                Main.npc[n2].velocity.Y = Main.rand.NextFloat(-24f, 24f);
                                if (Main.netMode == NetmodeID.Server)
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n2);
                            }
                        }
                    }
                }
                /*
                for (int i = 0; i < 20; i++) //dust
                {
                    int d = Dust.NewDust(NPC.Center + auraDistance * Vector2.UnitX.RotatedBy(Math.PI * 2 * Main.rand.NextDouble()), 0, 0, DustID.GemTopaz);
                    Main.dust[d].velocity = NPC.velocity;
                    Main.dust[d].noGravity = true;
                    Main.dust[d].scale++;
                }
                */
            }
        }

        public override bool CheckDead()
        {
            if (NPC.localAI[3] != 2f && WorldSavingSystem.EternityMode)
            {
                NPC.active = true;
                NPC.life = 1;

                if (FargoSoulsUtil.HostCheck)
                {
                    NPC.TargetClosest(false);
                    NPC.ai[0] = -4f;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = 0;
                    NPC.ai[3] = 0;
                    NPC.dontTakeDamage = true;
                    NPC.netUpdate = true;
                }

                return false;
            }
            return true;
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

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frame.Y = (int)NPC.ai[0] switch
            {
                //eyes closed
                -4 or 0 or 2 or 4 or 6 or 8 => frameHeight,
                //eyes open
                _ => 0,
            };
            if (NPC.localAI[3] == 0)
            {
                if (NPC.ai[2] == 1 && NPC.ai[1] > 180)
                    NPC.frame.Y = 0;
                else
                    NPC.frame.Y = frameHeight;
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0 && NPC.localAI[3] == 2)
            {
                for (int i = 1; i <= 5; i++)
                {
                    Vector2 pos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                    if (!Main.dedServ)
                        Gore.NewGore(NPC.GetSource_FromThis(), pos, NPC.velocity, ModContent.Find<ModGore>(Mod.Name, $"SpiritGore{i}").Type, NPC.scale);
                }
            }
        }

        public override void OnKill()
        {
            NPC.SetEventFlagCleared(ref WorldSavingSystem.DownedBoss[(int)WorldSavingSystem.Downed.SpiritChampion], -1);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SpiritMask>(), 7));

            npcLoot.Add(new ChampionEnchDropRule(BaseForce.EnchantsIn<SpiritForce>()));

            npcLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<SpiritChampionRelic>()));
            npcLoot.Add(ItemDropRule.MasterModeDropOnAllPlayers(ModContent.ItemType<AccursedRags>(), 4));
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

            Main.EntitySpriteDraw(texture2D13, NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), NPC.GetAlpha(drawColor), NPC.rotation, origin2, NPC.scale, effects, 0);

            if (NPC.localAI[2] != 0 && WorldSavingSystem.EternityMode) //aura
            {
                Color outerColor = Color.LightYellow;
                outerColor.A = 0;

                Color darkColor = outerColor;
                Color mediumColor = Color.Lerp(outerColor, Color.White, 0.75f);
                Color lightColor2 = Color.Lerp(outerColor, Color.White, 0.5f);

                Vector2 auraPos = NPC.Center;
                float radius = auraDistance;
                var target = Main.LocalPlayer;
                var blackTile = TextureAssets.MagicPixel;
                var diagonalNoise = FargosTextureRegistry.SandyNoise;
                if (!blackTile.IsLoaded || !diagonalNoise.IsLoaded)
                    return false;
                var maxOpacity = NPC.Opacity;

                ManagedShader borderShader = ShaderManager.GetShader("FargowiltasSouls.SpiritAuraShader");
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
