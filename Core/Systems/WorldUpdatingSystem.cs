using FargowiltasSouls.Content.Bosses.CursedCoffin;
using FargowiltasSouls.Content.WorldGeneration;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using static FargowiltasSouls.Core.Systems.WorldSavingSystem;

namespace FargowiltasSouls.Core.Systems
{
    public class WorldUpdatingSystem : ModSystem
    {
        public static int rainCD;
        public static int IceGolemTimer;
        public static int SandElementalTimer;
        public static bool SeenIceGolemMessage;
        public static bool SeenSandElementalMessage;

        public override void PreUpdateNPCs() => SwarmActive = FargowiltasSouls.MutantMod is Mod fargo && (bool)fargo.Call("SwarmActive");

        public override void PostUpdateWorld()
        {
            //NPC.LunarShieldPowerMax = NPC.downedMoonlord ? 50 : 100;

            if (!downedBoss[(int)Downed.CursedCoffin] && !Main.hardMode)
            {
                bool noCoffin = !NPC.AnyNPCs(ModContent.NPCType<CursedCoffinInactive>()) && !NPC.AnyNPCs(ModContent.NPCType<CursedCoffin>());

                if (noCoffin || !ShiftingSandEvent)
                {
                    Vector2 coffinArenaCenter = CoffinArena.FightCenter;
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player player = Main.player[i];
                        if (player != null && player.Alive())
                        {
                            float xDif = MathF.Abs(player.Center.X - coffinArenaCenter.X);
                            if (noCoffin && xDif < 2500 && Math.Abs(player.Center.Y - coffinArenaCenter.Y) < 2500)
                                NPC.NewNPC(NPC.GetSource_NaturalSpawn(), (int)coffinArenaCenter.X, (int)coffinArenaCenter.Y, ModContent.NPCType<CursedCoffinInactive>());
                            if (!ShiftingSandEvent && player.Center.Y < coffinArenaCenter.Y && xDif < CoffinArena.VectorWidth / 2)
                            {
                                ShiftingSandEvent = true;
                                FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.Message.{Name}.ShiftingSands", Color.Goldenrod);
                                if (Main.netMode == NetmodeID.Server)
                                    NetMessage.SendData(MessageID.WorldData);
                                ScreenShakeSystem.StartShake(5f, shakeStrengthDissipationIncrement: 5f / 200);
                                
                                for (int x = -10; x < 10; x++)
                                {
                                    for (int y = 0; y < 50; y++)
                                    {
                                        Tile t = Main.tile[x + (int)(player.Center.X / 16f), y + (int)(player.Center.Y / 16f)];
                                        if (t.HasTile && t.TileType == TileID.Sand)
                                        {
                                            for (int d = 0; d < 3; d++)
                                            {
                                                Dust.NewDust(player.Center + Vector2.UnitX * (x * 16 - 8) + Vector2.UnitY * (y * 16 - 8), 16, 16, DustID.Sand);
                                            }
                                            break;
                                        }
                                    }
                                }
                                
                            }
                    }
                    }
                }
            }


            if (!PlacedMutantStatue && (Main.zenithWorld || Main.remixWorld))
            {
                int positionX = Main.spawnTileX; //offset by dimensions of statue
                int positionY = Main.spawnTileY;
                int checkUp = -30;
                int checkDown = 10;
                bool placed = false;
                for (int offsetX = -50; offsetX <= 50; offsetX++)
                {
                    for (int offsetY = checkUp; offsetY <= checkDown; offsetY++)
                    {
                        if (WorldGenSystem.TryPlacingStatue(positionX + offsetX, positionY + offsetY))
                        {
                            placed = true;
                            PlacedMutantStatue = true;
                            break;
                        }
                    }

                    if (placed)
                        break;
                }
            }

            if (ShouldBeEternityMode)
            {
                if (EternityMode && !FargoSoulsUtil.WorldIsExpertOrHarder())
                {
                    EternityMode = false;
                    FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.Message.{Name}.EternityWrongDifficulty", new Color(175, 75, 255));
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.WorldData);
                    if (!Main.dedServ)
                        SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/DifficultyDeactivate"), Main.LocalPlayer.Center);
                }
                else if (!EternityMode && FargoSoulsUtil.WorldIsExpertOrHarder() && !LumUtils.AnyBosses())
                {
                    EternityMode = true;
                    FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.Message.{Name}.EternityOn", new Color(175, 75, 255));
                    if (Main.masterMode && !CanPlayMaso)
                        FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.Message.{Name}.EternityMasterWarning", new Color(255, 255, 0));
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.WorldData);
                    if (!Main.dedServ)
                        SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/DifficultyEmode") with { Volume = 0.5f }, Main.LocalPlayer.Center);
                }
            }
            else if (EternityMode)
            {
                EternityMode = false;
                FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.Message.{Name}.EternityOff", new Color(175, 75, 255));
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.WorldData);
                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/DifficultyDeactivate"), Main.LocalPlayer.Center);
            }

            if (EternityMode)
            {
                //NPC.LunarShieldPowerMax = 25;

                if (/*Main.raining || Sandstorm.Happening || */Main.bloodMoon)
                {
                    if (!HaveForcedAbomFromGoblins && !DownedAnyBoss //pre boss, disable some events
                        && ModContent.TryFind("Fargowiltas", "Abominationn", out ModNPC abom) && !NPC.AnyNPCs(abom.Type))
                    {
                        /*
                        Main.raining = false;
                        Main.rainTime = 0;
                        Main.maxRaining = 0;
                        Sandstorm.Happening = false;
                        Sandstorm.TimeLeft = 0;
                        if (Main.bloodMoon)
                        */
                        FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.Message.{Name}.BloodMoonCancel", new Color(175, 75, 255));
                        Main.bloodMoon = false;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.WorldData);
                    }
                }

                if (!MasochistModeReal && EternityMode && CanActuallyPlayMaso && !LumUtils.AnyBosses())
                {
                    MasochistModeReal = true;
                    FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.Message.{Name}.MasochistOn{(Main.zenithWorld ? "Zenith" : "")}", new Color(51, 255, 191, 0));
                    if (Main.getGoodWorld)
                        FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.Message.{Name}.MasochistFTWWarning", new Color(51, 255, 191, 0));
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.WorldData);
                    if (!Main.dedServ)
                        SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/DifficultyMaso") with { Volume = 0.5f }, Main.LocalPlayer.Center);
                }
            }

            if (MasochistModeReal && !(EternityMode && CanActuallyPlayMaso))
            {
                MasochistModeReal = false;
                FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.Message.{Name}.MasochistOff", new Color(51, 255, 191, 0));
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.WorldData);
                if (!Main.dedServ)
                    SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Assets/Sounds/DifficultyDeactivate"), Main.LocalPlayer.Center);
            }

            if (rainCD > 0)
            {
                rainCD--;
            }

            if (EternityMode)
                PostUpdateWorld_Eternity();

            #region commented

            //right when day starts
            /*if(/*Main.time == 0 && Main.dayTime && !Main.eclipse && WorldSavingSystem.masochistMode)
            {
                    SoundEngine.PlaySound(SoundID.Roar, player.Center);

                    if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        Main.eclipse = true;
                        //Main.NewText(Lang.misc[20], 50, 255, 130, false);
                    }
                    else
                    {
                        //NetMessage.SendData(61, -1, -1, "", player.whoAmI, -6f, 0f, 0f, 0, 0, 0);
                    }


            }*/

            // if (this.itemTime == 0 && this.itemAnimation > 0 && item.type == 361 && Main.CanStartInvasion(1, true))
            // {
            // this.itemTime = item.useTime;
            // SoundEngine.PlaySound(SoundID.Roar, this.Center);
            // if (FargoSoulsUtil.HostCheck)
            // {
            // if (Main.invasionType == 0)
            // {
            // Main.invasionDelay = 0;
            // Main.StartInvasion(1);
            // }
            // }
            // else
            // {
            // NetMessage.SendData(61, -1, -1, "", this.whoAmI, -1f, 0f, 0f, 0, 0, 0);
            // }
            // }
            // if (this.itemTime == 0 && this.itemAnimation > 0 && item.type == 602 && Main.CanStartInvasion(2, true))
            // {
            // this.itemTime = item.useTime;
            // SoundEngine.PlaySound(SoundID.Roar, this.Center);
            // if (FargoSoulsUtil.HostCheck)
            // {
            // if (Main.invasionType == 0)
            // {
            // Main.invasionDelay = 0;
            // Main.StartInvasion(2);
            // }
            // }
            // else
            // {
            // NetMessage.SendData(61, -1, -1, "", this.whoAmI, -2f, 0f, 0f, 0, 0, 0);
            // }
            // }
            // if (this.itemTime == 0 && this.itemAnimation > 0 && item.type == 1315 && Main.CanStartInvasion(3, true))
            // {
            // this.itemTime = item.useTime;
            // SoundEngine.PlaySound(SoundID.Roar, this.Center);
            // if (FargoSoulsUtil.HostCheck)
            // {
            // if (Main.invasionType == 0)
            // {
            // Main.invasionDelay = 0;
            // Main.StartInvasion(3);
            // }
            // }
            // else
            // {
            // NetMessage.SendData(61, -1, -1, "", this.whoAmI, -3f, 0f, 0f, 0, 0, 0);
            // }
            // }
            // if (this.itemTime == 0 && this.itemAnimation > 0 && item.type == 1844 && !Main.dayTime && !Main.pumpkinMoon && !Main.snowMoon && !DD2Event.Ongoing)
            // {
            // this.itemTime = item.useTime;
            // SoundEngine.PlaySound(SoundID.Roar, this.Center);
            // if (FargoSoulsUtil.HostCheck)
            // {
            // Main.NewText(Lang.misc[31], 50, 255, 130, false);
            // Main.startPumpkinMoon();
            // }
            // else
            // {
            // NetMessage.SendData(61, -1, -1, "", this.whoAmI, -4f, 0f, 0f, 0, 0, 0);
            // }
            // }

            // if (this.itemTime == 0 && this.itemAnimation > 0 && item.type == 3601 && NPC.downedGolemBoss && Main.hardMode && !NPC.AnyDanger() && !NPC.AnyoneNearCultists())
            // {
            // SoundEngine.PlaySound(SoundID.Roar, this.Center);
            // this.itemTime = item.useTime;
            // if (Main.netMode == NetmodeID.SinglePlayer)
            // {
            // WorldGen.StartImpendingDoom();
            // }
            // else
            // {
            // NetMessage.SendData(61, -1, -1, "", this.whoAmI, -8f, 0f, 0f, 0, 0, 0);
            // }
            // }
            // if (this.itemTime == 0 && this.itemAnimation > 0 && item.type == 1958 && !Main.dayTime && !Main.pumpkinMoon && !Main.snowMoon && !DD2Event.Ongoing)
            // {
            // this.itemTime = item.useTime;
            // SoundEngine.PlaySound(SoundID.Roar, this.Center);
            // if (FargoSoulsUtil.HostCheck)
            // {
            // Main.NewText(Lang.misc[34], 50, 255, 130, false);
            // Main.startSnowMoon();
            // }
            // else
            // {
            // NetMessage.SendData(61, -1, -1, "", this.whoAmI, -5f, 0f, 0f, 0, 0, 0);
            // }
            // }

            #endregion
        }

        public void PostUpdateWorld_Eternity()
        {
            // ice golem and sand elemental early spawn
            if (!Main.hardMode && DownedAnyBoss && !LumUtils.AnyBosses())
            {
                int baseCooldown = LumUtils.SecondsToFrames(40);
                int postSpawnCooldown = LumUtils.MinutesToFrames(5);
                int messageDelay = LumUtils.SecondsToFrames(10);
                bool sandstorm = Sandstorm.Happening;
                bool blizzard = Main.IsItRaining;
                Player desertPlayer = null;
                Player snowPlayer = null;
                if (sandstorm)
                    desertPlayer = Main.player.FirstOrDefault(p => p.Alive() && p.ZoneDesert && p.ZoneOverworldHeight, null);
                if (blizzard)
                    snowPlayer = Main.player.FirstOrDefault(p => p.Alive() && p.ZoneSnow && p.ZoneOverworldHeight, null);
                
                if (sandstorm && desertPlayer != null)
                {
                    SandElementalTimer++;
                    if (SandElementalTimer == baseCooldown - messageDelay && !SeenSandElementalMessage)
                    {
                        FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.Message.{Name}.MightyFoe", Color.Goldenrod);
                        SeenSandElementalMessage = true;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.WorldData);
                    }
                    if (SandElementalTimer >= baseCooldown)
                    {
                        NPC.SpawnOnPlayer(desertPlayer.whoAmI, NPCID.SandElemental);
                        SandElementalTimer = -postSpawnCooldown;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.WorldData);
                    }
                }
                else
                {
                    if (SandElementalTimer > 0)
                        SandElementalTimer--;
                    if (SandElementalTimer < 0)
                        SandElementalTimer++;
                    if (SandElementalTimer <= 0 && SeenSandElementalMessage)
                    {
                        SeenSandElementalMessage = false;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.WorldData);
                    }
                }
                if (blizzard && snowPlayer != null)
                {
                    IceGolemTimer++;
                    if (IceGolemTimer == baseCooldown - messageDelay && !SeenIceGolemMessage)
                    {
                        FargoSoulsUtil.PrintLocalization($"Mods.{Mod.Name}.Message.{Name}.MightyFoe", Color.DarkCyan);
                        SeenIceGolemMessage = true;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.WorldData);
                    }
                    if (IceGolemTimer >= baseCooldown)
                    {
                        NPC.SpawnOnPlayer(snowPlayer.whoAmI, NPCID.IceGolem);
                        IceGolemTimer = -postSpawnCooldown;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.WorldData);
                    }
                }
                else
                {
                    if (IceGolemTimer > 0)
                        IceGolemTimer--;
                    if (IceGolemTimer < 0)
                        IceGolemTimer++;
                    if (IceGolemTimer <= 0 && SeenIceGolemMessage)
                    {
                        SeenIceGolemMessage = false;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.WorldData);
                    }
                }
            }
        }
        public static bool CanActuallyPlayMaso => (FargoSoulsUtil.WorldIsMaster() && CanPlayMaso) || Main.zenithWorld;

        public override void ModifyLightingBrightness(ref float scale)
        {
            /*
            if (Main.LocalPlayer.FargoSouls().Illuminated)
            {
                if (scale < 1.08f)
                    scale += 0.08f;
            }
            */
        }
    }
}
