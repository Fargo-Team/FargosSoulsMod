using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Items.Placables;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ItemDropRules.Conditions;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasSouls.Core.Globals
{
    public partial class EModeGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        //masochist doom
        //public bool[] masoBool = new bool[4];
        //private int Stop = 0;

        public bool BeetleOffenseAura;
        public bool BeetleDefenseAura;
        public bool BeetleUtilAura;
        public int BeetleTimer;

        public bool PaladinsShield;
        public bool isWaterEnemy;

        //public List<int> auraDebuffs = new List<int>();
#pragma warning disable CA2211
        public static int slimeBoss = -1;
        public static int eyeBoss = -1;
        public static int eaterBoss = -1;
        public static int brainBoss = -1;
        public static int beeBoss = -1;
        public static int skeleBoss = -1;
        public static int deerBoss = -1;
        public static int wallBoss = -1;
        public static int retiBoss = -1;
        public static int spazBoss = -1;
        public static int destroyBoss = -1;
        public static int primeBoss = -1;
        public static int queenSlimeBoss = -1;
        public static int empressBoss = -1;
        public static int betsyBoss = -1;
        public static int fishBoss = -1;
        public static int cultBoss = -1;
        public static int moonBoss = -1;
        public static int guardBoss = -1;
        public static int fishBossEX = -1;
        public static bool spawnFishronEX;
        public static int deviBoss = -1;
        public static int abomBoss = -1;
        public static int mutantBoss = -1;
        public static int championBoss = -1;

        public static int eaterTimer;
        //public static int eaterResist;
#pragma warning restore CA2211

        public override void ResetEffects(NPC npc)
        {
            PaladinsShield = false;

            if (BeetleTimer > 0 && --BeetleTimer <= 0)
            {
                BeetleDefenseAura = false;
                BeetleOffenseAura = false;
                BeetleUtilAura = false;
            }
        }

        public override void SetDefaults(NPC npc)
        {
            if (!WorldSavingSystem.EternityMode) return;

            npc.value = (int)(npc.value * 1.3);
            if (!npc.boss && !npc.townNPC && !npc.CountsAsACritter && npc.life > 10 && !Main.masterMode && !LumUtils.AnyBosses())
            {
                npc.lifeMax = (int)Math.Round(npc.lifeMax * 1.1f);
            }

            //VERY old masomode boss scaling numbers, leaving here in case we ever want to do the funny again
            // +2.5% hp each kill 
            // +1.25% damage each kill
            // max of 4x hp and 2.5x damage
            //pre hm get 8x and 5x
        }

        public static void RefreshBuff(NPC npc, int buffId, int duration = 120)
        {
            //Only apply buff if required
            int index = npc.FindBuffIndex(buffId);

            if (index == -1 || npc.buffTime[index] < 30)
            {
                npc.AddBuff(buffId, duration);
            }
        }

        public override bool PreAI(NPC npc)
        {
            if (!WorldSavingSystem.EternityMode)
                return base.PreAI(npc);

            //in pre-hm, enemies glow slightly at night
            if (!Main.dayTime && !Main.hardMode && Main.player.Any(p => p.Alive() && p.FargoSouls().BoxofGizmos))
            {
                int x = (int)npc.Center.X / 16;
                int y = (int)npc.Center.Y / 16;
                if (y < Main.worldSurface && y > 0 && x > 0 && x < Main.maxTilesX)
                {
                    Tile tile = Framing.GetTileSafely(x, y);
                    if (tile != null && tile.WallType == 0)
                    {
                        Lighting.AddLight(npc.Center, 0.5f, 0.5f, 0.5f);
                    }
                }
            }

            /*if (Stop > 0)
            {
                Stop--;
                npc.position = npc.oldPosition;
                npc.frameCounter = 0;
            }*/

            if (!npc.dontTakeDamage)
            {
                /*
                bool boss = npc.boss || npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail;
                if (npc.position.Y / 16 < Main.worldSurface * 0.35f && !boss) //enemy in space
                    npc.AddBuff(BuffID.Suffocation, 2, true);
                else if (npc.position.Y / 16 > Main.maxTilesY - 200 && !boss && !Main.remixWorld) //enemy in hell
                {
                    //because of funny bug where town npcs fall forever in mp, including into hell
                    if (FargoSoulsUtil.HostCheck)
                        npc.AddBuff(BuffID.OnFire, 2);
                }
                */
                Vector2 tileCenter = npc.Center;
                tileCenter.X /= 16;
                tileCenter.Y /= 16;
                Tile currentTile = Framing.GetTileSafely((int)tileCenter.X, (int)tileCenter.Y);

                if (Main.raining && (npc.position.Y / 16 < Main.worldSurface))
                {
                    if (currentTile.WallType == WallID.None)
                    {
                        RefreshBuff(npc, BuffID.Wet, 240);
                    }
                }

                if (npc.wet && !npc.noTileCollide && !isWaterEnemy && npc.HasPlayerTarget)
                {
                    RefreshBuff(npc, ModContent.BuffType<LethargicBuff>());
                    if (Main.player[npc.target].ZoneCorrupt)
                        RefreshBuff(npc, BuffID.CursedInferno);
                    if (Main.player[npc.target].ZoneCrimson)
                        RefreshBuff(npc, BuffID.Ichor);
                    if (Main.player[npc.target].ZoneHallow)
                        RefreshBuff(npc, ModContent.BuffType<SmiteBuff>());
                    if (Main.player[npc.target].ZoneJungle)
                        RefreshBuff(npc, BuffID.Poisoned);
                }



                //if (!npc.boss && !npc.friendly && Main.SceneMetrics.EnoughTilesForSnow)
                //{
                //    npc.AddBuff(ModContent.BuffType<FrozenBuff>(), 3600);
                //}
            }

            

            return true;
        }

        public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
        {
            if (WorldSavingSystem.EternityMode)
            {
                //switch (npc.type)
                //{
                //    case NPCID.EaterofWorldsHead:
                //    case NPCID.EaterofWorldsBody:
                //    case NPCID.EaterofWorldsTail:
                //        target.AddBuff(BuffID.CursedInferno, 180);
                //        target.AddBuff(ModContent.BuffType<Rotting>(), 600);
                //        break;

                //    default:
                //        break;
                //}

                if (BeetleUtilAura)
                {
                    target.FargoSouls().AddBuffNoStack(BuffID.Frozen, 30);
                }
            }
        }
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (WorldSavingSystem.EternityMode)
            {
                spawnRate = (int)(spawnRate * 0.9);
                maxSpawns = (int)(maxSpawns * 1.2f);

                if ((player.ZoneTowerSolar && NPC.ShieldStrengthTowerSolar == 0)
                    || (player.ZoneTowerVortex && NPC.ShieldStrengthTowerVortex == 0)
                    || (player.ZoneTowerNebula && NPC.ShieldStrengthTowerNebula == 0)
                    || (player.ZoneTowerStardust && NPC.ShieldStrengthTowerStardust == 0))
                {
                    maxSpawns = 0;
                }
            }
        }
        public FieldInfo modBiomeFlags = typeof(Player).GetField("modBiomeFlags", LumUtils.UniversalBindingFlags);
        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            var playerModBiomeFlags = modBiomeFlags.GetValue(spawnInfo.Player) as BitArray;
            if (playerModBiomeFlags.Cast<bool>().Contains(true))
                return;
            //layers
            int x = spawnInfo.SpawnTileX;
            int y = spawnInfo.SpawnTileY;
            bool cavern = y >= Main.maxTilesY * 0.4f && y <= Main.maxTilesY * 0.8f;
            bool underground = y > Main.worldSurface && y <= Main.maxTilesY * 0.4f;
            bool surface = y < Main.worldSurface && !spawnInfo.Sky;
            bool wideUnderground = cavern || underground;
            bool underworld = spawnInfo.Player.ZoneUnderworldHeight;
            bool sky = spawnInfo.Sky;

            //times
            bool night = !Main.dayTime;
            bool day = Main.dayTime;

            //biomes
            bool noBiome = FargowiltasSouls.NoBiomeNormalSpawn(spawnInfo);
            bool ocean = spawnInfo.Player.ZoneBeach;
            bool dungeon = spawnInfo.Player.ZoneDungeon;
            bool spiderCave = spawnInfo.SpiderCave;
            bool mushroom = spawnInfo.Player.ZoneGlowshroom;
            bool jungle = spawnInfo.Player.ZoneJungle;
            bool granite = spawnInfo.Granite;
            bool marble = spawnInfo.Marble;
            bool corruption = spawnInfo.Player.ZoneCorrupt;
            bool crimson = spawnInfo.Player.ZoneCrimson;
            bool snow = spawnInfo.Player.ZoneSnow;
            bool hallow = spawnInfo.Player.ZoneHallow;
            bool desert = spawnInfo.Player.ZoneDesert;

            bool nebulaTower = spawnInfo.Player.ZoneTowerNebula;
            bool vortexTower = spawnInfo.Player.ZoneTowerVortex;
            bool stardustTower = spawnInfo.Player.ZoneTowerStardust;
            bool solarTower = spawnInfo.Player.ZoneTowerSolar;

            //events
            bool oldOnesArmy = DD2Event.Ongoing && spawnInfo.Player.ZoneOldOneArmy;
            bool frostMoon = surface && night && Main.snowMoon;
            bool pumpkinMoon = surface && night && Main.pumpkinMoon;
            bool solarEclipse = surface && day && Main.eclipse;
            bool lunarEvents = NPC.LunarApocalypseIsUp && (nebulaTower || vortexTower || stardustTower || solarTower);
            //bool monsterMadhouse = MMWorld.MMArmy;
            bool noEvent = Main.invasionType == 0 && !oldOnesArmy && !frostMoon && !pumpkinMoon && !solarEclipse && !lunarEvents;

            //no work?
            //is lava on screen
            //bool nearLava = Collision.LavaCollision(spawnInfo.Player.position, spawnInfo.SpawnTileX, spawnInfo.SpawnTileY);
            bool noInvasion = FargowiltasSouls.NoInvasion(spawnInfo);
            bool normalSpawn = !spawnInfo.PlayerInTown && noInvasion && !oldOnesArmy && noEvent;

            bool bossCanSpawn = WorldSavingSystem.MasochistModeReal && !spawnInfo.Player.HasEffect<SinisterIconEffect>() && !LumUtils.AnyBosses();

            //MASOCHIST MODE
            if (WorldSavingSystem.EternityMode)
            {
                //all the pre hardmode
                if (!Main.hardMode)
                {
                    //mutually exclusive world layers
                    if (surface)
                    {
                        if (night && normalSpawn)
                        {
                            if (noBiome)
                            {
                                pool[NPCID.CorruptBunny] = NPC.downedBoss1 ? .02f : .01f;
                                pool[NPCID.CrimsonBunny] = NPC.downedBoss1 ? .02f : .01f;
                            }

                            if (snow)
                            {
                                pool[NPCID.CorruptPenguin] = NPC.downedBoss1 ? .04f : .02f;
                                pool[NPCID.CrimsonPenguin] = NPC.downedBoss1 ? .04f : .02f;
                            }

                            if (ocean || Main.raining)
                            {
                                pool[NPCID.CorruptGoldfish] = NPC.downedBoss1 ? .04f : .02f;
                                pool[NPCID.CrimsonGoldfish] = NPC.downedBoss1 ? .04f : .02f;
                            }

                            if (NPC.downedBoss1)
                            {
                                if (jungle)
                                    pool[NPCID.DoctorBones] = .05f;

                                if (NPC.downedBoss3 && !NPC.downedMechBoss2 && bossCanSpawn)
                                    pool[NPCID.EyeofCthulhu] = Main.bloodMoon ? .0004f : .0002f;
                            }
                        }

                        /*
                        if (normalSpawn && WorldSavingSystem.DownedAnyBoss)
                        {
                            if (snow)
                                pool[NPCID.IceGolem] = .005f;

                            if (desert)
                                pool[NPCID.SandElemental] = .005f;
                        }
                        */
                        if (Main.slimeRain && NPC.downedBoss2 && bossCanSpawn)
                            pool[NPCID.KingSlime] = 0.004f;

                        if (day && NPC.downedGolemBoss && (noBiome || dungeon))
                            pool[NPCID.CultistArcherWhite] = .01f;
                        float scoutRate = 0.07f;
                        int xFromSpawn = Math.Abs(x - Main.spawnTileX);
                        bool goblinCondition = xFromSpawn > Main.maxTilesX / 3 || Main.remixWorld;
                        if ((!NPC.savedGoblin && goblinCondition) || (pool.TryGetValue(NPCID.GoblinScout, out float value) && value < scoutRate))
                        {
                            pool[NPCID.GoblinScout] = scoutRate;
                        }
                            

                    }
                    else if (wideUnderground)
                    {
                        //if (deepUnderground && !jungle && !snow)
                        //{
                        //    pool[NPCID.FireImp] = .01f;
                        //    pool[NPCID.LavaSlime] = .01f;
                        //}

                        if (marble && NPC.downedBoss2)
                        {
                            pool[NPCID.Medusa] = .04f;
                        }

                        if (granite)
                        {
                            pool[NPCID.GraniteFlyer] = .1f;
                            pool[NPCID.GraniteGolem] = .1f;
                        }

                        if (cavern)
                        {
                            if (noBiome && NPC.downedBoss3)
                                pool[NPCID.DarkCaster] = .02f;
                            if (noBiome && (!pool.ContainsKey(NPCID.RockGolem) || pool[NPCID.RockGolem] < 0.01f))
                                pool[NPCID.RockGolem] = 0.01f;
                                
                        }

                        if (NPC.downedGoblins && !NPC.savedGoblin && !NPC.AnyNPCs(NPCID.BoundGoblin))
                            pool[NPCID.BoundGoblin] = .5f;

                        if (spiderCave && !NPC.savedStylist && !NPC.AnyNPCs(NPCID.WebbedStylist))
                            pool[NPCID.WebbedStylist] = .5f;
                    }
                    else if (underworld)
                    {
                        pool[NPCID.LeechHead] = .02f;
                        pool[NPCID.BlazingWheel] = .05f;
                        //if (!FargoSoulsUtil.BossIsAlive(ref wallBoss, NPCID.WallofFlesh))
                        //pool[NPCID.RedDevil] = .025f;
                    }
                    else if (sky)
                    {
                        if (normalSpawn)
                        {
                            pool[NPCID.AngryNimbus] = .02f;

                            if (WorldSavingSystem.DownedAnyBoss)
                                pool[NPCID.WyvernHead] = .005f;
                        }
                    }

                    //height-independent biomes
                    if (corruption)
                    {
                        if (NPC.downedBoss2)
                        {
                            pool[NPCID.SeekerHead] = .005f;
                            if (normalSpawn && NPC.downedBoss3 && !underworld && bossCanSpawn)
                                pool[NPCID.EaterofWorldsHead] = .0002f;
                        }
                    }

                    if (crimson)
                    {
                        if (NPC.downedBoss2)
                        {
                            pool[NPCID.IchorSticker] = .005f;
                            if (normalSpawn && NPC.downedBoss3 && !underworld && bossCanSpawn)
                                pool[NPCID.BrainofCthulhu] = .0002f;
                        }
                    }

                    if (jungle)
                    {
                        if (WorldSavingSystem.MasochistModeReal && Main.getGoodWorld && normalSpawn)
                            pool[NPCID.Parrot] = .01f;
                    }

                    if (mushroom)
                    {
                        pool[NPCID.FungiBulb] = .02f;
                        pool[NPCID.MushiLadybug] = .02f;
                        pool[NPCID.ZombieMushroom] = .02f;
                        pool[NPCID.ZombieMushroomHat] = .02f;
                        pool[NPCID.AnomuraFungus] = .02f;
                    }

                    if (ocean)
                    {
                        pool[NPCID.PigronCorruption] = .002f;
                        pool[NPCID.PigronCrimson] = .002f;
                        pool[NPCID.PigronHallow] = .002f;
                    }

                    if (!surface && normalSpawn)
                    {
                        pool[NPCID.Mimic] = .002f;
                        if (desert && NPC.downedBoss2)
                            pool[NPCID.DuneSplicerHead] = .002f;
                    }
                }
                else //all the hardmode
                {
                    //mutually exclusive world layers
                    if (surface && !lunarEvents)
                    {
                        if (day)
                        {
                            if (normalSpawn)
                            {
                                if (noBiome && bossCanSpawn)
                                    pool[NPCID.KingSlime] = Main.slimeRain ? .0004f : .0002f;

                                if (NPC.downedGolemBoss && (noBiome || dungeon))
                                    pool[NPCID.CultistArcherWhite] = .01f;
                            }
                        }
                        else //night
                        {
                            if (Main.bloodMoon)
                            {
                                pool[NPCID.ChatteringTeethBomb] = .1f;
                                /*if (!sinisterIcon && !NPC.downedMechBoss2 && !LumUtils.AnyBosses())
                                    pool[NPCID.EyeofCthulhu] = .004f;

                                if (NPC.downedPlantBoss)
                                {
                                    if (bossCanSpawn)
                                    {
                                        pool[NPCID.Retinazer] = .002f;
                                        pool[NPCID.Spazmatism] = .002f;
                                        pool[NPCID.TheDestroyer] = .002f;
                                        pool[NPCID.SkeletronPrime] = .002f;
                                    }
                                }*/
                            }

                            if (noInvasion && !oldOnesArmy && bossCanSpawn)
                            {
                                pool[NPCID.Clown] = 0.01f;
                                if (!pool.ContainsKey(NPCID.Werewolf) || pool[NPCID.Werewolf] < 0.005f)
                                    pool[NPCID.Werewolf] = 0.005f;
                            }
                                

                            if (normalSpawn)
                            {
                                if (NPC.downedBoss1)
                                {
                                    if (noBiome)
                                    {
                                        pool[NPCID.CorruptBunny] = .05f;
                                        pool[NPCID.CrimsonBunny] = .05f;
                                    }

                                    if (snow)
                                    {
                                        pool[NPCID.CorruptPenguin] = .05f;
                                        pool[NPCID.CrimsonPenguin] = .05f;
                                    }

                                    if (ocean || Main.raining)
                                    {
                                        pool[NPCID.CorruptGoldfish] = .05f;
                                        pool[NPCID.CrimsonGoldfish] = .05f;
                                    }
                                }

                                if (bossCanSpawn && !NPC.downedMechBoss2)
                                    pool[NPCID.EyeofCthulhu] = .001f;

                                if (NPC.downedMechBossAny)
                                    pool[NPCID.Probe] = 0.01f;

                                if (NPC.downedPlantBoss) //GODLUL
                                {
                                    if (bossCanSpawn)
                                    {
                                        pool[NPCID.Retinazer] = .0001f;
                                        pool[NPCID.Spazmatism] = .0001f;
                                        pool[NPCID.TheDestroyer] = .0001f;
                                        pool[NPCID.SkeletronPrime] = .0001f;
                                    }

                                    //if (!spawnInfo.player.FargoSouls().SkullCharm)
                                    pool[NPCID.SkeletonSniper] = .005f;
                                    pool[NPCID.SkeletonCommando] = .005f;
                                    pool[NPCID.TacticalSkeleton] = .005f;
                                }
                            }

                            if (NPC.downedMechBossAny && noInvasion)
                            {
                                #region night pumpkin moon, frost moon
                                if (noBiome)
                                {
                                    pool[NPCID.Scarecrow1] = .01f;
                                    pool[NPCID.Scarecrow2] = .01f;
                                    pool[NPCID.Scarecrow3] = .01f;
                                    pool[NPCID.Scarecrow4] = .01f;
                                    pool[NPCID.Scarecrow5] = .01f;
                                    pool[NPCID.Scarecrow6] = .01f;
                                    pool[NPCID.Scarecrow7] = .01f;
                                    pool[NPCID.Scarecrow8] = .01f;
                                    pool[NPCID.Scarecrow9] = .01f;
                                    pool[NPCID.Scarecrow10] = .01f;

                                    if (NPC.downedHalloweenKing && bossCanSpawn)
                                    {
                                        //pool[NPCID.HeadlessHorseman] = .01f;
                                        pool[NPCID.Pumpking] = .0025f;
                                    }
                                }
                                else //in some biome
                                {
                                    if (hallow)
                                    {
                                        pool[NPCID.PresentMimic] = .01f;
                                    }
                                    else if (crimson || corruption)
                                    {
                                        pool[NPCID.Splinterling] = .05f;

                                        if (NPC.downedHalloweenTree && bossCanSpawn)
                                        {
                                            pool[NPCID.MourningWood] = .0025f;
                                        }
                                    }

                                    if (snow)
                                    {
                                        pool[NPCID.ZombieElf] = .02f;
                                        pool[NPCID.ZombieElfBeard] = .02f;
                                        pool[NPCID.ZombieElfGirl] = .02f;
                                        pool[NPCID.Yeti] = .01f;

                                        pool[NPCID.ElfArcher] = .05f;
                                        pool[NPCID.ElfCopter] = .01f;

                                        if (NPC.downedChristmasTree && bossCanSpawn)
                                        {
                                            pool[NPCID.Everscream] = .0025f;
                                        }

                                        if (NPC.downedChristmasSantank && bossCanSpawn)
                                        {
                                            pool[NPCID.SantaNK1] = .0025f;
                                        }
                                    }
                                }
                                #endregion
                            }
                        }

                        if (hallow)
                        {
                            if (!Main.raining)
                                pool[NPCID.RainbowSlime] = .001f;
                            pool[NPCID.GingerbreadMan] = .05f;
                        }

                        if (snow && noInvasion)
                        {
                            if (!Main.raining && !spawnInfo.PlayerInTown)
                                pool[NPCID.IceGolem] = .01f;
                            /*pool[NPCID.SnowBalla] = .04f;
                            pool[NPCID.MisterStabby] = .04f;
                            pool[NPCID.SnowmanGangsta] = .04f;*/
                        }

                        if (ocean)
                        {
                            if (night)
                            {
                                pool[NPCID.CreatureFromTheDeep] = .02f;
                            }

                            pool[NPCID.PigronCorruption] = .01f;
                            pool[NPCID.PigronCrimson] = .01f;
                            pool[NPCID.PigronHallow] = .01f;
                            if (NPC.downedFishron && bossCanSpawn)
                                pool[NPCID.DukeFishron] = .0002f;
                        }
                        else if (desert)
                        {
                            pool[NPCID.DesertBeast] = .05f;
                        }

                        if (NPC.downedMechBossAny && Main.raining)
                        {
                            pool[NPCID.LightningBug] = .1f;
                        }

                        if (corruption)
                        {
                            pool[NPCID.SeekerHead] = .1f;
                        }

                        if (crimson)
                        {
                            pool[NPCID.IchorSticker] = .1f;
                        }
                    }
                    else if (wideUnderground)
                    {
                        if (desert && !corruption && !crimson)
                        {
                            pool[NPCID.DesertDjinn] = .05f;
                        }

                        //if (deepUnderground && !jungle && !snow)
                        //{
                        //    pool[NPCID.FireImp] = .02f;
                        //    pool[NPCID.LavaSlime] = .02f;
                        //}

                        if (cavern)
                        {
                            if (noBiome && NPC.downedBoss3)
                                pool[NPCID.DarkCaster] = .05f;
                        }

                        if (!NPC.savedWizard && !NPC.AnyNPCs(NPCID.BoundWizard))
                            pool[NPCID.BoundWizard] = .5f;

                        if (dungeon && night && normalSpawn && bossCanSpawn)
                            pool[NPCID.SkeletronHead] = .00005f;

                        if (NPC.downedMechBossAny)
                        {
                            if (snow && !Main.dayTime) //frost moon underground
                            {
                                if (underground)
                                    pool[NPCID.Nutcracker] = .05f;

                                if (cavern)
                                {
                                    pool[NPCID.Krampus] = .025f;
                                    if (NPC.downedChristmasIceQueen && bossCanSpawn)
                                        pool[NPCID.IceQueen] = .0025f;
                                }
                            }
                        }

                        if (NPC.downedAncientCultist && dungeon && bossCanSpawn)
                            pool[NPCID.CultistBoss] = 0.00002f;

                        if (spawnInfo.Player.ZoneUndergroundDesert)
                        {
                            if (!hallow && !corruption && !crimson)
                            {
                                pool[NPCID.SandShark] = .1f;
                            }
                            else
                            {
                                if (hallow)
                                    pool[NPCID.SandsharkHallow] = .1f;
                                if (corruption)
                                    pool[NPCID.SandsharkCorrupt] = .1f;
                                if (crimson)
                                    pool[NPCID.SandsharkCrimson] = .1f;
                            }
                        }
                    }
                    else if (underworld)
                    {
                        pool[NPCID.LeechHead] = .025f;
                        pool[NPCID.BoneSerpentHead] = .025f;
                        pool[NPCID.BlazingWheel] = .05f;

                        if (bossCanSpawn && !FargoSoulsUtil.BossIsAlive(ref wallBoss, NPCID.WallofFlesh))
                            pool[NPCID.TheHungryII] = .03f;

                        if (NPC.downedMechBossAny)
                        {
                            pool[NPCID.BlazingWheel] = .05f;
                        }

                        if (NPC.downedPlantBoss)// && !spawnInfo.player.FargoSouls().SkullCharm)
                        {
                            pool[NPCID.DiabolistRed] = .001f;
                            pool[NPCID.DiabolistWhite] = .001f;
                            pool[NPCID.Necromancer] = .001f;
                            pool[NPCID.NecromancerArmored] = .001f;
                            pool[NPCID.RaggedCaster] = .001f;
                            pool[NPCID.RaggedCasterOpenCoat] = .001f;
                        }

                        if (WorldSavingSystem.DownedBetsy && bossCanSpawn)
                            pool[NPCID.DD2Betsy] = .0002f;
                    }
                    else if (sky)
                    {
                        if (normalSpawn)
                        {
                            pool[NPCID.AngryNimbus] = .05f;
                            pool[NPCID.MartianSaucerCore] = 0.001f;

                            if (NPC.downedGolemBoss)
                            {
                                pool[NPCID.SolarCrawltipedeHead] = .03f;
                                pool[NPCID.VortexHornetQueen] = .03f;
                                pool[NPCID.NebulaBrain] = .03f;
                                pool[NPCID.StardustJellyfishBig] = .03f;
                                pool[NPCID.AncientCultistSquidhead] = .03f;
                                pool[NPCID.CultistDragonHead] = .03f;
                            }
                            else if (NPC.downedMechBossAny)
                            {
                                pool[NPCID.SolarCrawltipedeHead] = .001f;
                                pool[NPCID.VortexHornetQueen] = .001f;
                                pool[NPCID.NebulaBrain] = .001f;
                                pool[NPCID.StardustJellyfishBig] = .001f;
                            }

                            if (NPC.downedMoonlord && bossCanSpawn)
                            {
                                pool[NPCID.MoonLordCore] = 0.0002f;
                            }
                        }
                    }

                    //height-independent biomes
                    if (corruption)
                    {
                        if (normalSpawn && bossCanSpawn)
                            pool[NPCID.EaterofWorldsHead] = .0002f;
                    }

                    if (crimson)
                    {
                        if (normalSpawn && bossCanSpawn)
                            pool[NPCID.BrainofCthulhu] = .0002f;
                    }

                    if (jungle)
                    {
                        if (normalSpawn && bossCanSpawn)
                            pool[NPCID.QueenBee] = .0001f;

                        if (normalSpawn)
                            pool[NPCID.Parrot] = .01f;

                        if (!surface)
                        {
                            pool[NPCID.BigMimicJungle] = .0015f;

                            if (NPC.downedGolemBoss && bossCanSpawn)
                                pool[NPCID.Plantera] = .00005f;
                        }
                    }

                    if (spawnInfo.Lihzahrd && spawnInfo.SpawnTileType == TileID.LihzahrdBrick)
                    {
                        pool[NPCID.BlazingWheel] = .1f;
                        pool[NPCID.SpikeBall] = .1f;

                        if (NPC.downedPlantBoss)// && !spawnInfo.player.FargoSouls().SkullCharm)
                        {
                            const float rate = .01f;
                            pool[NPCID.BigMimicJungle] = rate;

                            pool[NPCID.DiabolistRed] = rate / 2;
                            pool[NPCID.DiabolistWhite] = rate / 2;
                            pool[NPCID.Necromancer] = rate / 2;
                            pool[NPCID.NecromancerArmored] = rate / 2;
                            pool[NPCID.RaggedCaster] = rate / 2;
                            pool[NPCID.RaggedCasterOpenCoat] = rate / 2;
                        }
                    }

                    if (ocean && spawnInfo.Water)
                    {
                        pool[NPCID.AnglerFish] = .1f;
                    }
                }
                // irrespective of hardmode
                if (snow && !surface)
                {
                    pool[NPCID.SnowFlinx] = .05f;
                }
            }

            /*if (monsterMadhouse)
            {
                pool.Clear();
                if (MMWorld.MMPoints >= 0) //Goblin Army
                {

                }
                if (MMWorld.MMPoints >= 90 && MMWorld.MMPoints < 270) //OOA 1
                {

                }
                if (MMWorld.MMPoints >= 180) //Pirates
                {

                }
                if (MMWorld.MMPoints >= 270 && MMWorld.MMPoints < 630) //OOA2
                {

                }
                if (MMWorld.MMPoints >= 360) //Eclipse
                {

                }
                if (MMWorld.MMPoints >= 450) //Pumpkin Moon
                {

                }
                if (MMWorld.MMPoints >= 540) //Frost Moon
                {

                }
                if (MMWorld.MMPoints >= 540) //Martians
                {

                }
                if (MMWorld.MMPoints >= 630) //OOA3
                {

                }
                if (MMWorld.MMPoints >= 720) //Lunar Events
                {

                }
            }*/
        }

        public override void OnKill(NPC npc)
        {
            base.OnKill(npc);

            if (npc.type == NPCID.Painter && WorldSavingSystem.DownedMutant && NPC.AnyNPCs(ModContent.NPCType<MutantBoss>()))
                Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, ModContent.ItemType<ScremPainting>());
        }
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            #region tim's concoction drops

            void TimsConcoctionDrop(IItemDropRule rule)
            {
                TimsConcoctionDropCondition dropCondition = new();
                IItemDropRule conditionalRule = new LeadingConditionRule(dropCondition);
                conditionalRule.OnSuccess(rule);
                npcLoot.Add(conditionalRule);
            }
            
            switch (npc.type)
            {
                case NPCID.BlueSlime:
                    TimsConcoctionDrop(ItemDropRule.Common(npc.netID == NPCID.Pinky ? ItemID.TeleportationPotion : ItemID.RecallPotion));
                    break;

                case NPCID.DemonEye:
                case NPCID.DemonEye2:
                case NPCID.DemonEyeOwl:
                case NPCID.DemonEyeSpaceship:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.NightOwlPotion));
                    break;

                case NPCID.MossHornet:
                case NPCID.Hornet:
                case NPCID.HornetFatty:
                case NPCID.HornetHoney:
                case NPCID.HornetLeafy:
                case NPCID.HornetSpikey:
                case NPCID.HornetStingy:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.RagePotion, 1, 1, 2));
                    break;

                case NPCID.Bee:
                case NPCID.BeeSmall:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.WrathPotion, 2));
                    break;

                case NPCID.GoblinPeon:
                case NPCID.GoblinThief:
                case NPCID.GoblinWarrior:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.BattlePotion));
                    break;

                case NPCID.JungleBat:
                case NPCID.IceBat:
                case NPCID.Vampire:
                case NPCID.VampireBat:
                case NPCID.GiantFlyingFox:
                case NPCID.Hellbat:
                case NPCID.Lavabat:
                case NPCID.IlluminantBat:
                case NPCID.CaveBat:
                case NPCID.GiantBat:
                case NPCID.SporeBat:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.SonarPotion));
                    break;

                case NPCID.AngryBones:
                case NPCID.AngryBonesBig:
                case NPCID.AngryBonesBigHelmet:
                case NPCID.AngryBonesBigMuscle:
                case NPCID.HellArmoredBones:
                case NPCID.HellArmoredBonesMace:
                case NPCID.HellArmoredBonesSpikeShield:
                case NPCID.HellArmoredBonesSword:
                case NPCID.RustyArmoredBonesAxe:
                case NPCID.RustyArmoredBonesFlail:
                case NPCID.RustyArmoredBonesSword:
                case NPCID.RustyArmoredBonesSwordNoArmor:
                case NPCID.BlueArmoredBones:
                case NPCID.BlueArmoredBonesMace:
                case NPCID.BlueArmoredBonesNoPants:
                case NPCID.BlueArmoredBonesSword:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.TitanPotion));
                    break;

                case NPCID.DarkCaster:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.PotionOfReturn, 1, 1, 6));
                    break;

                case NPCID.GiantShelly:
                case NPCID.GiantShelly2:
                case NPCID.GiantTortoise:
                case NPCID.IceTortoise:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.ThornsPotion, 1, 1, 6));
                    break;

                case NPCID.Zombie:
                case NPCID.BaldZombie:
                case NPCID.FemaleZombie:
                case NPCID.PincushionZombie:
                case NPCID.SlimedZombie:
                case NPCID.TwiggyZombie:
                case NPCID.ZombiePixie:
                case NPCID.ZombieSuperman:
                case NPCID.ZombieSweater:
                case NPCID.ZombieXmas:
                case NPCID.SwampZombie:
                case NPCID.SmallSwampZombie:
                case NPCID.BigSwampZombie:
                case NPCID.ZombieDoctor:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.StinkPotion));
                    break;

                case NPCID.Antlion:
                case NPCID.WalkingAntlion:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.BuilderPotion, 1, 1, 3));
                    break;

                case NPCID.WallCreeper:
                case NPCID.WallCreeperWall:
                case NPCID.BlackRecluse:
                case NPCID.BlackRecluseWall:
                case NPCID.JungleCreeper:
                case NPCID.JungleCreeperWall:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.TrapsightPotion, 1, 1, 3));
                    break;

                case NPCID.FireImp:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.InfernoPotion, 1, 1, 3));
                    break;

                case NPCID.Harpy:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.CalmingPotion, 1, 1, 3));
                    break;

                case NPCID.Crab:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.CratePotion, 1, 1, 3));
                    break;

                case NPCID.FlyingFish:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.FishingPotion, 1, 1, 3));
                    break;

                case NPCID.IceSlime:
                case NPCID.SpikedIceSlime:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.WaterWalkingPotion, 1, 1, 3));
                    break;

                case NPCID.Piranha:
                case NPCID.Arapaima:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.GillsPotion, 1, 1, 3));
                    break;

                case NPCID.BloodZombie:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.RegenerationPotion, 1, 1, 3));
                    break;

                case NPCID.LavaSlime:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.WarmthPotion, 1, 1, 3));
                    break;

                case NPCID.UmbrellaSlime:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.FeatherfallPotion, 1, 1, 3));
                    break;

                case NPCID.Drippler:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.HeartreachPotion, 1, 1, 3));
                    break;

                case NPCID.GiantWormHead:
                case NPCID.DiggerHead:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.WormholePotion, 1, 1, 3));
                    break;

                case NPCID.GreekSkeleton:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.AmmoReservationPotion, 1, 1, 3));
                    break;

                case NPCID.GraniteFlyer:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.IronskinPotion, 1, 1, 3));
                    break;

                case NPCID.GraniteGolem:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.EndurancePotion, 1, 3, 5));
                    break;

                case NPCID.Shark:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.FlipperPotion, 1, 1, 3));
                    break;

                case NPCID.GoblinArcher:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.ArcheryPotion, 1, 1, 3));
                    break;

                case NPCID.GoblinSorcerer:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.ManaRegenerationPotion, 1, 1, 3));
                    break;

                case NPCID.PinkJellyfish:
                case NPCID.BlueJellyfish:
                case NPCID.GreenJellyfish:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.ShinePotion, 1, 1, 3));
                    break;

                case NPCID.Salamander:
                case NPCID.Salamander2:
                case NPCID.Salamander3:
                case NPCID.Salamander4:
                case NPCID.Salamander5:
                case NPCID.Salamander6:
                case NPCID.Salamander7:
                case NPCID.Salamander8:
                case NPCID.Salamander9:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.InvisibilityPotion, 1, 1, 3));
                    break;

                case NPCID.MotherSlime:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.SummoningPotion, 1, 1, 6));
                    break;

                case NPCID.Nymph:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.LovePotion, 1, 1, 6));
                    break;

                case NPCID.Tumbleweed:
                case NPCID.DesertBeast:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.SwiftnessPotion, 1, 1, 6));
                    break;

                case NPCID.TombCrawlerHead:
                case NPCID.DuneSplicerHead:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.HunterPotion, 1, 1, 6));
                    break;

                case NPCID.DoctorBones:
                case NPCID.Mimic:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.SpelunkerPotion, 1, 1, 6));
                    break;

                case NPCID.UndeadMiner:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.MiningPotion, 1, 1, 6));
                    break;

                case NPCID.Tim:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.MagicPowerPotion, 1, 1, 6));
                    break;

                case NPCID.BoneSerpentHead:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.ObsidianSkinPotion, 1, 1, 12));
                    break;


                case NPCID.Gnome:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.LuckPotionLesser, 1, 1, 6));
                    break;
                case NPCID.DungeonSlime:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.LuckPotion, 1, 1, 6));
                    break;
                case NPCID.Clown:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.LuckPotionGreater, 1, 1, 6));
                    break;


                case NPCID.ChaosElemental:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.TeleportationPotion, 1, 1, 3));
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.PotionOfReturn, 1, 1, 6));
                    break;

                case NPCID.RainbowSlime:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.RegenerationPotion, 1, 1, 3));
                    break;

                case NPCID.RuneWizard:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.MagicPowerPotion, 1, 1, 6));
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.ManaRegenerationPotion, 1, 1, 6));
                    break;

                case NPCID.GoblinSummoner:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.SummoningPotion, 1, 1, 12));
                    break;

                case NPCID.PirateCaptain:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.AmmoReservationPotion, 1, 1, 12));
                    break;

                case NPCID.WyvernHead:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.GravitationPotion, 1, 1, 12));
                    break;

                case NPCID.BigMimicJungle:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.RedPotion));
                    goto case NPCID.BigMimicCorruption;
                case NPCID.BigMimicCorruption:
                case NPCID.BigMimicCrimson:
                case NPCID.BigMimicHallow:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.LifeforcePotion, 1, 1, 12));
                    break;

                case NPCID.ManEater:
                case NPCID.Nutcracker:
                case NPCID.Parrot:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.GenderChangePotion, 1, 1, 3));
                    break;
                case NPCID.CorruptBunny:
                case NPCID.CrimsonBunny:
                case NPCID.CorruptGoldfish:
                case NPCID.CrimsonGoldfish:
                case NPCID.CorruptPenguin:
                case NPCID.CrimsonPenguin:
                    TimsConcoctionDrop(ItemDropRule.Common(ItemID.BiomeSightPotion, 1, 1, 3));
                    break;
                default: break;
            }
            #endregion
            //if (npc.ModNPC == null || npc.ModNPC.Mod is FargowiltasSouls) //not for other mods
            //{
            int allowedRecursionDepth = 10;
            void AddDrop(IItemDropRule dropRule)
            {
                if (npc.type == NPCID.Retinazer || npc.type == NPCID.Spazmatism)
                {
                    LeadingConditionRule noTwin = new(new Conditions.MissingTwin());
                    noTwin.OnSuccess(dropRule);
                    npcLoot.Add(noTwin);
                }
                else if (npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsTail)
                {
                    LeadingConditionRule lastEater = new(new Conditions.LegacyHack_IsABoss());
                    lastEater.OnSuccess(dropRule);
                    npcLoot.Add(lastEater);
                }
                else
                {
                    npcLoot.Add(dropRule);
                }
            }

            void CheckMasterDropRule(IItemDropRule dropRule)
            {
                if (--allowedRecursionDepth > 0)
                {
                    if (dropRule != null && dropRule.ChainedRules != null)
                    {
                        foreach (IItemDropRuleChainAttempt chain in dropRule.ChainedRules)
                        {
                            if (chain != null && chain.RuleToChain != null)
                                CheckMasterDropRule(chain.RuleToChain);
                        }
                    }


                    if (dropRule is DropBasedOnMasterMode dropBasedOnMasterMode)
                    {
                        if (dropBasedOnMasterMode != null && dropBasedOnMasterMode.ruleForMasterMode != null)
                            CheckMasterDropRule(dropBasedOnMasterMode.ruleForMasterMode);
                        //if (dropBasedOnMasterMode.ruleForMasterMode is CommonDrop masterDrop)
                        //{
                        //    IItemDropRule emodeDropRule = ItemDropRule.ByCondition(
                        //        new EModeNotMasterDropCondition(),
                        //        masterDrop.itemId,
                        //        masterDrop.chanceDenominator,
                        //        masterDrop.amountDroppedMinimum,
                        //        masterDrop.amountDroppedMaximum,
                        //        masterDrop.chanceNumerator
                        //    );
                        //    npcLoot.Add(emodeDropRule);
                        //}
                    }
                }
                allowedRecursionDepth++;

                //if (dropRule is CommonDrop drop)
                //{
                if (dropRule is ItemDropWithConditionRule itemDropWithCondition && itemDropWithCondition.condition is Conditions.IsMasterMode)
                {
                    IItemDropRule emodeDropRule = ItemDropRule.ByCondition(
                        new EModeNotMasterDropCondition(),
                        itemDropWithCondition.itemId,
                        itemDropWithCondition.chanceDenominator,
                        itemDropWithCondition.amountDroppedMinimum,
                        itemDropWithCondition.amountDroppedMaximum,
                        itemDropWithCondition.chanceNumerator
                    );
                    //itemDropWithCondition.OnFailedConditions(emodeDropRule, true);
                    AddDrop(emodeDropRule);
                }
                else if (dropRule is DropPerPlayerOnThePlayer dropPerPlayer && dropPerPlayer.condition is Conditions.IsMasterMode)
                {
                    IItemDropRule emodeDropRule = ItemDropRule.ByCondition(
                        new EModeNotMasterDropCondition(),
                        dropPerPlayer.itemId,
                        dropPerPlayer.chanceDenominator,
                        dropPerPlayer.amountDroppedMinimum,
                        dropPerPlayer.amountDroppedMaximum,
                        dropPerPlayer.chanceNumerator
                    );
                    //dropPerPlayer.OnFailedConditions(emodeDropRule, true);
                    AddDrop(emodeDropRule);
                }
                //}
            }

            foreach (IItemDropRule rule in npcLoot.Get())
            {
                CheckMasterDropRule(rule);
            }
            //}
        }

        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            if (WorldSavingSystem.EternityMode && BeetleOffenseAura)
            {
                modifiers.FinalDamage *= 1.25f;
            }
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            //ModifyHitByEither(npc, player, ref damage, ref knockback, ref crit);

            if (WorldSavingSystem.EternityMode)
            {
                if (NPCID.Sets.CountsAsCritter[npc.type]) //npc.catchItem != 0 && npc.lifeMax == 5)
                    player.AddBuff(ModContent.BuffType<GuiltyBuff>(), 300);
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[projectile.owner];

            //ModifyHitByEither(npc, player, ref damage, ref knockback, ref crit);

            if (WorldSavingSystem.EternityMode)
            {
                if (NPCID.Sets.CountsAsCritter[npc.type] /*npc.catchItem != 0 && npc.lifeMax == 5*/ && projectile.friendly && !projectile.hostile && projectile.type != ProjectileID.FallingStar)
                    player.AddBuff(ModContent.BuffType<GuiltyBuff>(), 300);
            }
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (WorldSavingSystem.EternityMode)
            {
                if (BeetleDefenseAura)
                    modifiers.FinalDamage *= 0.75f;

                if (PaladinsShield)
                    modifiers.FinalDamage *= 0.5f;

                if (WorldSavingSystem.MasochistModeReal && ((npc.boss || LumUtils.AnyBosses()) && FargoSoulsGlobalNPC.boss.IsWithinBounds(Main.maxNPCs) && npc.Distance(Main.npc[FargoSoulsGlobalNPC.boss].Center) < 3000))
                    modifiers.FinalDamage *= 0.9f;
            }

            //normal damage calc
            base.ModifyIncomingHit(npc, ref modifiers);
        }

        //make aura enemies display them one day(tm)
        /*public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Color drawColor)
        {
            for (int i = 0; i < auraDebuffs.Count; i++)
            {
                Texture2D buffIcon = Main.buffTexture[auraDebuffs[i]];
                Color buffColor = drawColor * 0.5f;
                Vector2 drawPos = npc.Top;
                drawPos.Y -= 32f;
                float mid = auraDebuffs.Count / 2f - 0.5f;
                drawPos.X -= 32f * (i - mid);
                Main.EntitySpriteDraw(buffIcon, drawPos - Main.screenPosition + new Vector2(0f, npc.gfxOffY), buffIcon.Bounds, buffColor, 0, buffIcon.Bounds.Size() / 2, 1f, SpriteEffects.None, 0);
            }
        }*/

        public static bool StealFromInventory(Player target, ref Item item)
        {
            if (target.FargoSouls().StealingCooldown <= 0 && !item.IsAir)
            {
                target.FargoSouls().StealingCooldown = 900; //trust me, keep these separate
                target.AddBuff(ModContent.BuffType<ThiefCDBuff>(), 900);


                //int i = Item.NewItem(target.GetSource_DropAsItem("Stolen"), (int)target.position.X, (int)target.position.Y, target.width, target.height, item.type, item.stack, false, -1, false, false);
                //Vector2 position = Main.item[i].position;

                //Main.item[i] = item.Clone();
                //Main.item[i].whoAmI = i;
                //Main.item[i].position = position;
                //Main.item[i].stack = item.stack;

                //Main.item[i].velocity.X = Main.rand.Next(-20, 21) * 0.2f;
                //Main.item[i].velocity.Y = Main.rand.Next(-20, 1) * 0.2f;
                //Main.item[i].noGrabDelay = 100;
                //Main.item[i].newAndShiny = false;

                //if (Main.netMode == NetmodeID.MultiplayerClient)
                //    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, i, 1f);

                //item.TurnToAir();

                target.DropItem(target.GetSource_DropAsItem("Stolen"), target.Center, ref item);

                return true;
            }
            else
            {
                return false;
            }
        }

        public static void Horde(NPC npc, int size)
        {

            if (npc == null || !npc.active)
            {
                return;
            }
            int repeatTries = 50;

            for (int i = 0; i < size; i++)
            {
                Vector2 pos = new(npc.Center.X + Main.rand.NextFloat(-2f, 2f) * npc.width, npc.Center.Y);

                if (Collision.SolidCollision(pos, npc.width, npc.height))
                {
                    if (repeatTries > 0) //retry up to the max attempts
                    {
                        repeatTries -= 1;
                        i -= 1;
                    }
                    continue;
                }

                if (FargoSoulsUtil.HostCheck)
                {
                    int j = NPC.NewNPC(npc.GetSource_FromAI(), (int)pos.X + npc.width / 2, (int)pos.Y + npc.height / 2, npc.type);
                    if (j != Main.maxNPCs)
                    {
                        NPC newNPC = Main.npc[j];
                        if (newNPC != null && newNPC.active && newNPC.type == npc.type) //super mega safeguard check
                        {
                            newNPC.velocity = Vector2.UnitX.RotatedByRandom(2 * Math.PI) * 5f;
                            newNPC.FargoSouls().CanHordeSplit = false;
                            /*
                            if (newNPC.TryGetGlobalNPC(out EModeNPCBehaviour globalNPC))
                            {
                                globalNPC.FirstTick = false;
                            }
                            */
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, j);
                        }
                    }
                }
            }
        }

        //for backwards compat
        public static void Aura(NPC npc, float distance, int buff, bool reverse = false, int dustid = DustID.GoldFlame, Color color = default)
        {
            Aura(npc, distance, reverse, dustid, color, buff);
        }

        public static void Aura(NPC npc, float distance, bool reverse = false, int dustid = -1, Color color = default, params int[] buffs)
        {
            Player p = Main.LocalPlayer;

            //if (FargowiltasSouls.Instance.MasomodeEXLoaded) distance *= reverse ? 0.5f : 2f;
            if (dustid != -1)
                FargoSoulsUtil.AuraDust(npc, distance, dustid, color, reverse);

            if (buffs.Length == 0 || buffs[0] < 0)
                return;

            //works because buffs are client side anyway :ech:
            float range = npc.Distance(p.Center);
            if (p.Alive() && (reverse ? range > distance && range < Math.Max(3000f, distance * 2) : range < distance))
            {
                foreach (int buff in buffs)
                {
                    FargoSoulsUtil.AddDebuffFixedDuration(p, buff, 2);
                }
            }
        }

        /*private void Shoot(NPC npc, int delay, float distance, int speed, int proj, int dmg, float kb, bool hostile = false, int dustID = -1)
        {
            int t = npc.HasPlayerTarget ? npc.target : npc.FindClosestPlayer();
            if (t == -1)
                return;

            Player player = Main.player[t];
            //npc facing player target or if already started attack
            if (player.active && !player.dead && npc.direction == (Math.Sign(player.position.X - npc.position.X)) || Stop > 0)
            {
                //start the pause
                if (delay != 0 && Stop == 0 && npc.Distance(player.Center) < distance)
                {
                    Stop = delay;

                    //dust ring
                    if (dustID != -1)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            Vector2 vector6 = Vector2.UnitY * 5f;
                            vector6 = vector6.RotatedBy((i - (20 / 2 - 1)) * 6.28318548f / 20) + npc.Center;
                            Vector2 vector7 = vector6 - npc.Center;
                            int d = Dust.NewDust(vector6 + vector7, 0, 0, dustID);
                            Main.dust[d].noGravity = true;
                            Main.dust[d].velocity = vector7;
                            Main.dust[d].scale = 1.5f;
                        }
                    }

                }
                //half way through start attack
                else if (delay == 0 || Stop == delay / 2)
                {
                    Vector2 velocity = Vector2.Zero;

                    if (npc.Distance(player.Center) < distance || delay != 0)
                    {
                        velocity = Vector2.Normalize(player.Center - npc.Center) * speed;
                    }

                    if (velocity != Vector2.Zero)
                    {
                        int p = Projectile.NewProjectile(npc.Center, velocity, proj, dmg, kb, Main.myPlayer);
                        if (p < 1000)
                        {
                            if (hostile)
                            {
                                Main.projectile[p].friendly = false;
                                Main.projectile[p].hostile = true;
                            }
                        }

                        Counter[0] = 0;
                    } 
                }
            }
        }*/
        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            bool ret = base.CanHitPlayer(npc, target, ref cooldownSlot);
            if (!WorldSavingSystem.EternityMode)
                return ret;
            if (npc.type is NPCID.Sharkron or NPCID.Sharkron2)
            {
                int halfwidth = npc.width / 2;
                Vector2 dir = npc.velocity.SafeNormalize(Vector2.Zero);
                if (!Collision.CheckAABBvLineCollision(target.position, target.Size, npc.Center - dir * halfwidth, npc.Center + dir * halfwidth))
                    return false;
            }
            return ret;
        }
        public static void CustomReflect(NPC npc, int dustID, int ratio = 1)
        {
            float distance = 2f * 16;

            Main.projectile.Where(x => x.active && x.friendly && !FargoSoulsUtil.IsSummonDamage(x, false)).ToList().ForEach(x =>
            {
                if (Vector2.Distance(x.Center, npc.Center) <= distance)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        int dustId = Dust.NewDust(new Vector2(x.position.X, x.position.Y + 2f), x.width, x.height + 5, dustID, x.velocity.X * 0.2f, x.velocity.Y * 0.2f, 100, default, 1.5f);
                        Main.dust[dustId].noGravity = true;
                    }

                    // Set ownership
                    x.hostile = true;
                    x.friendly = false;
                    x.owner = Main.myPlayer;
                    x.damage /= ratio;

                    // Turn around
                    x.velocity *= -1f;

                    // Flip sprite
                    if (x.Center.X > npc.Center.X * 0.5f)
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
                }
            });
        }
    }
}
