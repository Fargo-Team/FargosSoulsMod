using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Core.ItemDropRules.Conditions;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Graphics.Capture;
using Terraria.GameContent.Achievements;
using FargowiltasSouls.Content.Items.Misc;

namespace FargowiltasSouls //lets everything access it without using
{
    public static partial class FargoSoulsUtil
    {
        [Obsolete("Use Luminance's Utilities.UniversalBindingFlags instead.", error: false)]
        public static readonly BindingFlags UniversalBindingFlags = LumUtils.UniversalBindingFlags;

        public static bool WorldIsExpertOrHarder() => Main.expertMode || (Main.GameModeInfo.IsJourneyMode && CreativePowerManager.Instance.GetPower<CreativePowers.DifficultySliderPower>().StrengthMultiplierToGiveNPCs >= 2);

        public static bool WorldIsMaster() => Main.masterMode || (Main.GameModeInfo.IsJourneyMode && CreativePowerManager.Instance.GetPower<CreativePowers.DifficultySliderPower>().StrengthMultiplierToGiveNPCs >= 3);

        public static bool HostCheck => Main.netMode != NetmodeID.MultiplayerClient;

        public static bool ActuallyClickingInGameplay(Player player) => !player.mouseInterface && !CaptureManager.Instance.Active;

        public static void AddDebuffFixedDuration(Player player, int buffID, int intendedTime, bool quiet = true)
        {
            if (WorldIsExpertOrHarder() && BuffID.Sets.LongerExpertDebuff[buffID])
            {
                float debuffTimeMultiplier = Main.GameModeInfo.DebuffTimeMultiplier;
                if (Main.GameModeInfo.IsJourneyMode)
                {
                    if (Main.masterMode)
                        debuffTimeMultiplier = Main.RegisteredGameModes[2].DebuffTimeMultiplier;
                    else if (Main.expertMode)
                        debuffTimeMultiplier = Main.RegisteredGameModes[1].DebuffTimeMultiplier;
                }
                player.AddBuff(buffID, (int)Math.Round(intendedTime / debuffTimeMultiplier, MidpointRounding.ToEven), quiet);
            }
            else
            {
                player.AddBuff(buffID, intendedTime, quiet);
            }
        }

        public static float ProjWorldDamage => Main.GameModeInfo.IsJourneyMode
            ? CreativePowerManager.Instance.GetPower<CreativePowers.DifficultySliderPower>().StrengthMultiplierToGiveNPCs
            : Main.GameModeInfo.EnemyDamageMultiplier;

        //npcDamageCalcsOffset because i wrote all the code around expert mode and my npcs do same contact damage in normal and expert
        public static int ScaledProjectileDamage(int npcDamage, float modifier = 1, int npcDamageCalculationsOffset = 2)
        {
            const float inherentHostileProjMultiplier = 2;
            float worldDamage = ProjWorldDamage;
            return (int)(modifier * npcDamage / inherentHostileProjMultiplier / Math.Max(npcDamageCalculationsOffset, worldDamage));
        }

        public static void AllCritEquals(Player player, float crit)
        {
            player.GetCritChance(DamageClass.Generic) = crit;

            player.GetCritChance(DamageClass.Melee) = 0;
            player.GetCritChance(DamageClass.Ranged) = 0;
            player.GetCritChance(DamageClass.Magic) = 0;
            player.GetCritChance(DamageClass.Summon) = 0;
        }

        public static int HighestDamageTypeScaling(Player player, int dmg)
        {
            List<float> types =
            [
                player.ActualClassDamage(DamageClass.Melee),
                player.ActualClassDamage(DamageClass.Ranged),
                player.ActualClassDamage(DamageClass.Magic),
                player.ActualClassDamage(DamageClass.Summon)
            ];

            return (int)(types.Max() * dmg);
        }

        public static float HighestCritChance(Player player)
        {
            List<float> types =
            [
                player.ActualClassCrit(DamageClass.Melee),
                player.ActualClassCrit(DamageClass.Ranged),
                player.ActualClassCrit(DamageClass.Magic),
                player.ActualClassCrit(DamageClass.Summon)
            ];

            return types.Max();
        }


        public static Projectile[] XWay(int num, IEntitySource spawnSource, Vector2 pos, int type, float speed, int damage, float knockback)
        {
            Projectile[] projs = new Projectile[num];
            double spread = 2 * Math.PI / num;
            for (int i = 0; i < num; i++)
                projs[i] = NewProjectileDirectSafe(spawnSource, pos, new Vector2(speed, speed).RotatedBy(spread * i), type, damage, knockback, Main.myPlayer);
            return projs;
        }

        public static Projectile NewProjectileDirectSafe(IEntitySource spawnSource, Vector2 pos, Vector2 vel, int type, int damage, float knockback, int owner = 255, float ai0 = 0f, float ai1 = 0f)
        {
            int p = Projectile.NewProjectile(spawnSource, pos, vel, type, damage, knockback, owner, ai0, ai1);
            return p < Main.maxProjectiles ? Main.projectile[p] : null;
        }

        public static int GetProjectileByIdentity(int player, float projectileIdentity, params int[] projectileType)
        {
            return GetProjectileByIdentity(player, (int)projectileIdentity, projectileType);
        }

        public static int GetProjectileByIdentity(int player, int projectileIdentity, params int[] projectileType)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].identity == projectileIdentity && Main.projectile[i].owner == player
                    && (projectileType.Length == 0 || projectileType.Contains(Main.projectile[i].type)))
                {
                    return i;
                }
            }
            return -1;
        }

        public static bool IsSummonDamage(Projectile projectile, bool includeMinionShot = true, bool includeWhips = true)
        {
            if (!includeWhips && ProjectileID.Sets.IsAWhip[projectile.type])
                return false;

            if (!includeMinionShot && (ProjectileID.Sets.MinionShot[projectile.type] || ProjectileID.Sets.SentryShot[projectile.type]))
                return false;

            return projectile.CountsAsClass(DamageClass.Summon) || projectile.minion || projectile.sentry || projectile.minionSlots > 0 || ProjectileID.Sets.MinionSacrificable[projectile.type]
                || (includeMinionShot && (ProjectileID.Sets.MinionShot[projectile.type] || ProjectileID.Sets.SentryShot[projectile.type]))
                || (includeWhips && ProjectileID.Sets.IsAWhip[projectile.type]);
        }

        public static bool CanDeleteProjectile(Projectile projectile, int deletionRank = 0, bool clearSummonProjs = false)
        {
            if (!projectile.active)
                return false;
            if (projectile.damage <= 0)
                return false;
            if (projectile.FargoSouls().DeletionImmuneRank > deletionRank)
                return false;
            if (projectile.friendly)
            {
                if (projectile.whoAmI == Main.player[projectile.owner].heldProj)
                    return false;
                if (IsSummonDamage(projectile, false) && !clearSummonProjs)
                    return false;
            }
            return true;
        }

        public static Player PlayerExists(int whoAmI)
        {
            return whoAmI > -1 && whoAmI < Main.maxPlayers && Main.player[whoAmI].active && !Main.player[whoAmI].dead && !Main.player[whoAmI].ghost ? Main.player[whoAmI] : null;
        }

        public static Player PlayerExists(float whoAmI)
        {
            return PlayerExists((int)whoAmI);
        }

        public static Projectile ProjectileExists(int whoAmI, params int[] types)
        {
            return whoAmI > -1 && whoAmI < Main.maxProjectiles && Main.projectile[whoAmI].active && (types.Length == 0 || types.Contains(Main.projectile[whoAmI].type)) ? Main.projectile[whoAmI] : null;
        }

        public static Projectile ProjectileExists(float whoAmI, params int[] types)
        {
            return ProjectileExists((int)whoAmI, types);
        }

        public static NPC NPCExists(int whoAmI, params int[] types)
        {
            return whoAmI > -1 && whoAmI < Main.maxNPCs && Main.npc[whoAmI].active && (types.Length == 0 || types.Contains(Main.npc[whoAmI].type)) ? Main.npc[whoAmI] : null;
        }

        public static NPC NPCExists(float whoAmI, params int[] types)
        {
            return NPCExists((int)whoAmI, types);
        }

        public static bool OtherBossAlive(int npcID)
        {
            if (npcID > -1 && npcID < Main.maxNPCs)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].boss && i != npcID)
                        return true;
                }
            }
            return false;
        }

        public static bool BossIsAlive(ref int bossID, int bossType)
        {
            if (bossID != -1)
            {
                if (Main.npc[bossID].active && Main.npc[bossID].type == bossType)
                {
                    return true;
                }
                else
                {
                    bossID = -1;
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static void ClearFriendlyProjectiles(int deletionRank = 0, int bossNpc = -1, bool clearSummonProjs = false)
        {
            ClearProjectiles(false, true, deletionRank, bossNpc, clearSummonProjs);
        }

        public static void ClearHostileProjectiles(int deletionRank = 0, int bossNpc = -1)
        {
            ClearProjectiles(true, false, deletionRank, bossNpc);
        }

        public static void ClearAllProjectiles(int deletionRank = 0, int bossNpc = -1, bool clearSummonProjs = false)
        {
            ClearProjectiles(true, true, deletionRank, bossNpc, clearSummonProjs);
        }

        private static void ClearProjectiles(bool clearHostile, bool clearFriendly, int deletionRank = 0, int bossNpc = -1, bool clearSummonProjs = false)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (OtherBossAlive(bossNpc))
                clearHostile = false;

            for (int j = 0; j < 2; j++) //do twice to wipe out projectiles spawned by projectiles
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile projectile = Main.projectile[i];
                    if (projectile.active && ((projectile.hostile && clearHostile) || (projectile.friendly && clearFriendly)) && CanDeleteProjectile(projectile, deletionRank, clearSummonProjs))
                    {
                        projectile.Kill();
                    }
                }
            }
        }

        public static void ReplaceItem(this Player player, Item itemToReplace, int itemIDtoReplaceWith)
        {
            bool foundSlot = false;
            for (int i = 0; i < player.inventory.Length; i++)
            {
                if (player.inventory[i] == itemToReplace)
                {
                    Item newItem = new(itemIDtoReplaceWith, itemToReplace.stack, itemToReplace.prefix);
                    newItem.active = true;
                    newItem.favorited = itemToReplace.favorited;
                    player.inventory[i] = newItem;
                    itemToReplace.active = false;
                    foundSlot = true;
                    break;
                }
            }
            if (!foundSlot) //didn't find item in normal inventory, drop instead
            {
                Item.NewItem(player.GetSource_ItemUse(itemToReplace), player.Center, itemIDtoReplaceWith, prefixGiven: itemToReplace.prefix);
            }
        }
        public static bool NPCInAnyTiles(NPC npc, bool platforms)
        {
            //WHERE'S TJHE FKC IJNGI METHOD FOR HTIS? ITS NOT COLLISION.SOLKIDCOLLIOSOM ITS NOPT COLLISON.SOLDITILES I HATE 1.4 IHATE TMODLAOREI I HATE THIS FUSPTID FUCKIGN GNAME SOFU KIGN MCUCH FUCK FUCK FUCK
            bool isInTiles = false;
            for (int x = 0; x < npc.width; x += 16)
            {
                for (float y = 0; y < npc.height; y += 16)
                {
                    Tile tile = Framing.GetTileSafely((int)(npc.position.X + x) / 16, (int)(npc.position.Y + y) / 16);
                    if ((tile.HasUnactuatedTile && platforms) && (Main.tileSolid[tile.TileType] || (Main.tileSolidTop[tile.TileType] && platforms)))
                    {
                        isInTiles = true;
                        break;
                    }
                }
            }
            return isInTiles;
        }

        public static void PrintAI(NPC npc)
        {
            Main.NewText($"{npc.whoAmI} ai: {npc.ai[0]} {npc.ai[1]} {npc.ai[2]} {npc.ai[3]}, local: {npc.localAI[0]} {npc.localAI[1]} {npc.localAI[2]} {npc.localAI[3]}");
        }

        public static void PrintAI(Projectile projectile)
        {
            Main.NewText($"{projectile.whoAmI} ai: {projectile.ai[0]} {projectile.ai[1]} {projectile.ai[2]}, local: {projectile.localAI[0]} {projectile.localAI[1]}");
        }

        public static void GrossVanillaDodgeDust(Entity entity)
        {
            for (int index1 = 0; index1 < 100; ++index1)
            {
                int index2 = Dust.NewDust(entity.position, entity.width, entity.height, DustID.Smoke, 0.0f, 0.0f, 100, new Color(), 2f);
                Main.dust[index2].position.X += Main.rand.Next(-20, 21);
                Main.dust[index2].position.Y += Main.rand.Next(-20, 21);
                Dust dust = Main.dust[index2];
                dust.velocity *= 0.4f;
                Main.dust[index2].scale *= 1f + Main.rand.Next(40) * 0.01f;
                if (Main.rand.NextBool())
                {
                    Main.dust[index2].scale *= 1f + Main.rand.Next(40) * 0.01f;
                    Main.dust[index2].noGravity = true;
                }
            }

            int index3 = Gore.NewGore(entity.GetSource_FromThis(), new Vector2(entity.Center.X - 24, entity.Center.Y - 24), new Vector2(), Main.rand.Next(61, 64), 1f);
            Main.gore[index3].scale = 1.5f;
            Main.gore[index3].velocity.X = Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[index3].velocity.Y = Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[index3].velocity *= 0.4f;

            int index4 = Gore.NewGore(entity.GetSource_FromThis(), new Vector2(entity.Center.X - 24, entity.Center.Y - 24), new Vector2(), Main.rand.Next(61, 64), 1f);
            Main.gore[index4].scale = 1.5f;
            Main.gore[index4].velocity.X = 1.5f + Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[index4].velocity.Y = 1.5f + Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[index4].velocity *= 0.4f;

            int index5 = Gore.NewGore(entity.GetSource_FromThis(), new Vector2(entity.Center.X - 24, entity.Center.Y - 24), new Vector2(), Main.rand.Next(61, 64), 1f);
            Main.gore[index5].scale = 1.5f;
            Main.gore[index5].velocity.X = -1.5f - Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[index5].velocity.Y = 1.5f + Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[index5].velocity *= 0.4f;

            int index6 = Gore.NewGore(entity.GetSource_FromThis(), new Vector2(entity.Center.X - 24, entity.Center.Y - 24), new Vector2(), Main.rand.Next(61, 64), 1f);
            Main.gore[index6].scale = 1.5f;
            Main.gore[index6].velocity.X = 1.5f - Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[index6].velocity.Y = -1.5f + Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[index6].velocity *= 0.4f;

            int index7 = Gore.NewGore(entity.GetSource_FromThis(), new Vector2(entity.Center.X - 24, entity.Center.Y - 24), new Vector2(), Main.rand.Next(61, 64), 1f);
            Main.gore[index7].scale = 1.5f;
            Main.gore[index7].velocity.X = -1.5f - Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[index7].velocity.Y = -1.5f + Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[index7].velocity *= 0.4f;
        }

        public static int FindClosestHostileNPC(Vector2 location, float detectionRange, bool lineCheck = false, bool prioritizeBoss = false)
        {
            NPC closestNpc = null;

            void FindClosest(IEnumerable<NPC> npcs)
            {
                float range = detectionRange;
                foreach (NPC n in npcs)
                {
                    if (n.CanBeChasedBy() && n.Distance(location) < range
                        && (!lineCheck || Collision.CanHitLine(location, 0, 0, n.Center, 0, 0)))
                    {
                        range = n.Distance(location);
                        closestNpc = n;
                    }
                }
            }

            if (prioritizeBoss)
                FindClosest(Main.npc.Where(n => n.boss));
            if (closestNpc == null)
                FindClosest(Main.npc);

            return closestNpc == null ? -1 : closestNpc.whoAmI;
        }

        public static int FindClosestHostileNPCPrioritizingMinionFocus(Projectile projectile, float detectionRange, bool lineCheck = false, Vector2 center = default, bool prioritizeBoss = false)
        {
            if (center == default)
                center = projectile.Center;

            NPC minionAttackTargetNpc = projectile.OwnerMinionAttackTargetNPC;
            if (minionAttackTargetNpc != null && minionAttackTargetNpc.CanBeChasedBy() && minionAttackTargetNpc.Distance(center) < detectionRange
                && (!lineCheck || Collision.CanHitLine(center, 0, 0, minionAttackTargetNpc.Center, 0, 0)))
            {
                return minionAttackTargetNpc.whoAmI;
            }
            return FindClosestHostileNPC(center, detectionRange, lineCheck, prioritizeBoss);
        }

        public static void DustRing(Vector2 location, int max, int dust, float speed, Color color = default, float scale = 1f, bool noLight = false)
        {
            for (int i = 0; i < max; i++)
            {
                Vector2 velocity = speed * Vector2.UnitY.RotatedBy(MathHelper.TwoPi / max * i);
                int d = Dust.NewDust(location, 0, 0, dust, newColor: color);
                Main.dust[d].noLight = noLight;
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = velocity;
                Main.dust[d].scale = scale;
            }
        }

        public static void PrintText(string text)
        {
            PrintText(text, Color.White);
        }

        public static void PrintLocalization(string localizationKey, Color color)
        {
            PrintText(Language.GetTextValue(localizationKey), color);
        }

        public static void PrintLocalization(string localizationKey, int r, int g, int b) => PrintLocalization(localizationKey, new Color(r, g, b));

        public static void PrintLocalization(string localizationKey, Color color, params object[] args) => PrintText(Language.GetTextValue(localizationKey, args), color);

        public static void PrintText(string text, Color color)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(text, color);
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color);
            }
        }

        public static void PrintText(string text, int r, int g, int b) => PrintText(text, new Color(r, g, b));

        public static Vector2 ClosestPointInHitbox(Rectangle hitboxOfTarget, Vector2 desiredLocation)
        {
            Vector2 offset = desiredLocation - hitboxOfTarget.Center.ToVector2();
            offset.X = Math.Min(Math.Abs(offset.X), hitboxOfTarget.Width / 2) * Math.Sign(offset.X);
            offset.Y = Math.Min(Math.Abs(offset.Y), hitboxOfTarget.Height / 2) * Math.Sign(offset.Y);
            return hitboxOfTarget.Center.ToVector2() + offset;
        }

        public static Vector2 ClosestPointInHitbox(Entity entity, Vector2 desiredLocation)
        {
            return ClosestPointInHitbox(entity.Hitbox, desiredLocation);
        }

        public static float RotationDifference(Vector2 from, Vector2 to) => (float)Math.Atan2(to.Y * from.X - to.X * from.Y, from.X * to.X + from.Y * to.Y);
        public static Vector2 PredictiveAim(Vector2 startingPosition, Vector2 targetPosition, Vector2 targetVelocity, float shootSpeed, int iterations = 4)
        {
            float previousTimeToReachDestination = 0f;
            Vector2 currentTargetPosition = targetPosition;
            for (int i = 0; i < iterations; i++)
            {
                float timeToReachDestination = Vector2.Distance(startingPosition, currentTargetPosition) / shootSpeed;
                currentTargetPosition += targetVelocity * (timeToReachDestination - previousTimeToReachDestination);
                previousTimeToReachDestination = timeToReachDestination;
            }
            return (currentTargetPosition - startingPosition).SafeNormalize(Vector2.UnitY) * shootSpeed;
        }

        public static void HeartDust(Vector2 position, float rotationOffset = MathHelper.PiOver2, Vector2 addedVel = default, float spreadModifier = 1f, float scaleModifier = 1f)
        {
            for (float j = 0; j < MathHelper.TwoPi; j += MathHelper.ToRadians(360 / 60))
            {
                Vector2 dustPos = new(
                    16f * (float)Math.Pow(Math.Sin(j), 3),
                    13 * (float)Math.Cos(j) - 5 * (float)Math.Cos(2 * j) - 2 * (float)Math.Cos(3 * j) - (float)Math.Cos(4 * j));
                dustPos.Y *= -1;
                dustPos = dustPos.RotatedBy(rotationOffset - MathHelper.PiOver2);

                int d = Dust.NewDust(position, 0, 0, DustID.GemAmethyst, 0);
                Main.dust[d].velocity = dustPos * 0.25f * spreadModifier + addedVel;
                Main.dust[d].scale = 2f * scaleModifier;
                Main.dust[d].noGravity = true;
            }
        }

        public static int NewSummonProjectile(IEntitySource source, Vector2 spawn, Vector2 velocity, int type, int rawBaseDamage, float knockback, int owner = 255, float ai0 = 0, float ai1 = 0)
        {
            int p = Projectile.NewProjectile(source, spawn, velocity, type, rawBaseDamage, knockback, owner, ai0, ai1);
            if (p != Main.maxProjectiles)
            {
                Main.projectile[p].originalDamage = rawBaseDamage;
                Main.projectile[p].ContinuouslyUpdateDamageStats = true;
            }
            return p;
        }

        public static int NewNPCEasy(IEntitySource source, Vector2 spawnPos, int type, int start = 0, float ai0 = 0, float ai1 = 0, float ai2 = 0, float ai3 = 0, int target = 255, Vector2 velocity = default)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return Main.maxNPCs;

            int n = NPC.NewNPC(source, (int)spawnPos.X, (int)spawnPos.Y, type, start, ai0, ai1, ai2, ai3, target);
            if (n != Main.maxNPCs)
            {
                if (velocity != default)
                {
                    Main.npc[n].velocity = velocity;
                }

                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
            }

            return n;
        }

        public static void AuraDust(Entity entity, float distance, int dustid, Color color = default, bool reverse = false, float dustScaleModifier = 1f)
        {
            const int baseDistance = 500;
            const int baseMax = 20;

            int dustMax = (int)(distance / baseDistance * baseMax);
            if (dustMax < 10)
                dustMax = 10;
            if (dustMax > 40)
                dustMax = 40;

            float dustScale = distance / baseDistance;
            if (dustScale < 0.75f)
                dustScale = 0.75f;
            if (dustScale > 2f)
                dustScale = 2f;

            for (int i = 0; i < dustMax; i++)
            {
                Vector2 spawnPos = entity.Center + Main.rand.NextVector2CircularEdge(distance, distance);
                Vector2 offset = spawnPos - Main.LocalPlayer.Center;
                if (Math.Abs(offset.X) > Main.screenWidth * 0.6f || Math.Abs(offset.Y) > Main.screenHeight * 0.6f) //dont spawn dust if its pointless
                    continue;
                Dust dust = Main.dust[Dust.NewDust(spawnPos, 0, 0, dustid, 0, 0, 100, Color.White)];
                dust.scale = dustScale * dustScaleModifier;
                dust.velocity = entity.velocity;
                if (Main.rand.NextBool(3))
                {
                    dust.velocity += Vector2.Normalize(entity.Center - dust.position) * Main.rand.NextFloat(5f) * (reverse ? -1f : 1f);
                    dust.position += dust.velocity * 5f;
                }
                dust.noGravity = true;
                if (color != default)
                    dust.color = color;
            }
        }
        /// <summary>
        /// Makes an aura out of particles, same form as old dust auras.
        /// Different particle types:
        /// 0/default: circular bloom "ball"
        /// 1: small sparkle
        /// </summary>
        public static void AuraParticles(Entity entity, float distance, Color color = default, bool reverse = false, float scaleModifier = 1f, int particleType = 0)
        {
            const int baseDistance = 500;
            const int baseMax = 20;

            int pMax = (int)(distance / baseDistance * baseMax);
            if (pMax < 10)
                pMax = 10;
            if (pMax > 40)
                pMax = 40;

            float scale = distance / baseDistance;
            if (scale < 0.75f)
                scale = 0.75f;
            if (scale > 2f)
                scale = 2f;

            for (int i = 0; i < pMax; i++)
            {
                Vector2 spawnPos = entity.Center + Main.rand.NextVector2CircularEdge(distance, distance);
                Vector2 offset = spawnPos - Main.LocalPlayer.Center;
                if (Math.Abs(offset.X) > Main.screenWidth * 0.6f || Math.Abs(offset.Y) > Main.screenHeight * 0.6f) //dont spawn dust if its pointless
                    continue;
                Particle p;
                switch (particleType)
                {
                    case 1:
                        {
                            p = new SmallSparkle(
                            worldPosition: spawnPos,
                            velocity: entity.velocity,
                            drawColor: color != default ? color : Color.White,
                            scale: scale * scaleModifier,
                            lifetime: 30,
                            rotation: Main.rand.NextFloat(MathHelper.TwoPi),
                            rotationSpeed: Main.rand.NextFloat(-MathHelper.Pi / 32, MathHelper.Pi / 32)
                            );
                            break;
                        }
                    default:
                        {
                            p = new ExpandingBloomParticle(
                            position: spawnPos,
                            velocity: entity.velocity,
                            drawColor: color != default ? color : Color.White,
                            startScale: Vector2.One * scale * scaleModifier,
                            endScale: Vector2.One * 0,
                            lifetime: 30
                            );
                            break;
                        }
                }
                p.Spawn();
                if (Main.rand.NextBool(3))
                {
                    p.Velocity += Vector2.Normalize(entity.Center - p.Position) * Main.rand.NextFloat(5f) * (reverse ? -1f : 1f);
                    p.Position += p.Velocity * 5f;
                }
            }
        }

        public static bool OnSpawnEnchCanAffectProjectile(Projectile projectile, bool allowMinions)
        {
            if (!allowMinions && (projectile.minion || projectile.sentry || projectile.minionSlots > 0))
                return false;

            return projectile.friendly
                && projectile.damage > 0
                && !projectile.hostile
                && !projectile.npcProj
                && !projectile.trap;
            //&& (projectile.DamageType != DamageClass.Default || ProjectileID.Sets.MinionShot[projectile.type]);
        }

        public static void SpawnBossNetcoded(Player player, int bossType, bool obeyLocalPlayerCheck = true)
        {
            if (player.whoAmI == Main.myPlayer || !obeyLocalPlayerCheck)
            {
                // If the player using the item is the client
                // (explicitely excluded serverside here)
                SoundEngine.PlaySound(SoundID.Roar, player.position);

                if (FargoSoulsUtil.HostCheck)
                {
                    // If the player is not in multiplayer, spawn directly
                    NPC.SpawnOnPlayer(player.whoAmI, bossType);
                }
                else
                {
                    // If the player is in multiplayer, request a spawn
                    // This will only work if NPCID.Sets.MPAllowedEnemies[type] is true, set in NPC code
                    NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: bossType);
                }
            }
        }

        public static bool IsProjSourceItemUseReal(Projectile proj, IEntitySource source)
        {
            return source is EntitySource_ItemUse parent && parent.Item.type == Main.player[proj.owner].HeldItem.type;
        }

        public static bool AprilFools => DateTime.Today.Month == 4 && DateTime.Today.Day <= 1;
        public static string TryAprilFoolsTexture => AprilFools ? "_April" : "";

        public static void ScreenshakeRumble(float strength)
        {
            if (ScreenShakeSystem.OverallShakeIntensity < strength)
            {
                ScreenShakeSystem.SetUniversalRumble(strength, MathF.Tau, null, 0.2f);
            }
        }

        public static Vector2 EyePosition(this Player player)
        {
            // Taken from player.Yoraiz0rEye().
            int num = 0;
            num += player.bodyFrame.Y / 56;
            if (num >= Main.OffsetsPlayerHeadgear.Length)
                num = 0;

            Vector2 vector = Main.OffsetsPlayerHeadgear[num];
            vector *= player.Directions;
            Vector2 vector2 = new Vector2(player.width / 2, player.height / 2) + vector + (player.MountedCenter - player.Center);
            player.sitting.GetSittingOffsetInfo(player, out var posOffset, out var seatAdjustment);
            vector2 += posOffset + new Vector2(0f, seatAdjustment);
            if (player.face == 19)
                vector2.Y -= 5f * player.gravDir;

            if (player.head == 276)
                vector2.X += 2.5f * (float)player.direction;

            if (player.mount.Active && player.mount.Type == 52)
            {
                vector2.X += 14f * (float)player.direction;
                vector2.Y -= 2f * player.gravDir;
            }

            // this is added to adjust to exact eye position
            //vector2.X += 1f * player.direction;
            vector2.Y -= 2f * player.gravDir;

            float y = -11.5f * player.gravDir;
            Vector2 vector3 = new Vector2(3 * player.direction - ((player.direction == 1) ? 1 : 0), y) + Vector2.UnitY * player.gfxOffY + vector2;
            Vector2 vector4 = new Vector2(3 * player.shadowDirection[1] - ((player.direction == 1) ? 1 : 0), y) + vector2;
            Vector2 vector5 = Vector2.Zero;
            if (player.mount.Active && player.mount.Cart)
            {
                int num2 = Math.Sign(player.velocity.X);
                if (num2 == 0)
                    num2 = player.direction;

                vector5 = new Vector2(MathHelper.Lerp(0f, -8f, player.fullRotation / ((float)Math.PI / 4f)), MathHelper.Lerp(0f, 2f, Math.Abs(player.fullRotation / ((float)Math.PI / 4f)))).RotatedBy(player.fullRotation);
                if (num2 == Math.Sign(player.fullRotation))
                    vector5 *= MathHelper.Lerp(1f, 0.6f, Math.Abs(player.fullRotation / ((float)Math.PI / 4f)));
            }

            if (player.fullRotation != 0f)
            {
                vector3 = vector3.RotatedBy(player.fullRotation, player.fullRotationOrigin);
                //vector4 = vector4.RotatedBy(player.fullRotation, player.fullRotationOrigin);
            }

            //float num3 = 0f;
            Vector2 vector6 = player.position + vector3 + vector5;
            return vector6;
        }

        public static void TileExplosion(Vector2 compareSpot, int radius)
        {
            Point tileCenter = compareSpot.ToTileCoordinates();
            int minI = (int)tileCenter.X - radius;
            int maxI = (int)tileCenter.X + radius;
            int minJ = (int)tileCenter.Y - radius;
            int maxJ = (int)tileCenter.Y + radius;

            Projectile sampleBomb = ContentSamples.ProjectilesByType[ProjectileID.Bomb];
            bool wallSplode = sampleBomb.ShouldWallExplode(compareSpot, radius, minI, maxI, minJ, maxJ);
            sampleBomb.ExplodeTiles(compareSpot, radius, minI, maxI, minJ, maxJ, wallSplode);
        }

        public static Vector2 SmartAccel(Vector2 position, Vector2 destination, Vector2 velocity, float accel, float decel)
        {
            Vector2 dif = destination - position;

            if (dif == Vector2.Zero)
                return Vector2.Zero;

            if (velocity == Vector2.Zero)
                velocity = Vector2.UnitX * 0.1f;

            // Project velocity onto difference
            Vector2 a = velocity;
            Vector2 b = dif.SafeNormalize(Vector2.Zero);
            float scalarProj = Vector2.Dot(a, b);
            Vector2 vProj = b * scalarProj;
            Vector2 vOrth = velocity - vProj;

            // towards target
            Vector2 vProjN = vProj.SafeNormalize(Vector2.Zero);
            if (scalarProj > 0)
                velocity += vProjN * (float)SmartAccel1D(dif.Length(), vProj.Length(), accel, decel);
            else
                velocity -= vProjN * decel;
            // perpendicular to target
            velocity -= Math.Min(decel, vOrth.Length()) * vOrth.SafeNormalize(Vector2.Zero);
            if (velocity == Vector2.Zero)
                velocity = b * accel;
            return velocity;
        }

        public static double SmartAccel1D(double s, double v, double a, double d)
        {
            // Deceleration should be negative, acceleration positive, and distance positive.
            s = Math.Abs(s);
            a = Math.Abs(a);
            d = -Math.Abs(d);

            // The root part of the linear acceleration formula solved for t.
            // If the root is real, there's 2 solutions for t, which means we're overshooting. Our only option is to decelerate fully.
            // If the root is 0, there's 1 solution for t, which means we perfectly match the target by decelerating.
            // If the root is imaginary, we would undershoot by decelerating now.

            double root = Math.Abs(v * v / (d * d)) + 2 * s / d;
            if (root >= 0)
                return d;

            // We're undershooting.
            // If full acceleration would cause us to overshoot next frame, only accelerate enough to perfectly match deceleration time with arrival time.
            double root2 = -2 * s / d;
            if (root2 <= 0)
                return a;
            double accelFraction = (Math.Sqrt(root2) * -d - v) / a;
            if (accelFraction > 0 && accelFraction < 1)
                return accelFraction * a;
            return a;

        }

        #region npcloot

        public static bool LockEarlyBirdDrop(NPCLoot npcLoot, IItemDropRule rule)
        {
            EModeEarlyBirdLockDropCondition lockCondition = new();
            IItemDropRule conditionalRule = new LeadingConditionRule(lockCondition);
            conditionalRule.OnSuccess(rule);
            npcLoot.Add(conditionalRule);
            return true;
        }

        public static void AddEarlyBirdDrop(NPCLoot npcLoot, IItemDropRule rule)
        {
            EModeEarlyBirdRewardDropCondition dropCondition = new();
            IItemDropRule conditionalRule = new LeadingConditionRule(dropCondition);
            conditionalRule.OnSuccess(rule);
            npcLoot.Add(conditionalRule);
        }

        public static void EModeDrop(NPCLoot npcLoot, IItemDropRule rule)
        {
            EModeDropCondition dropCondition = new();
            IItemDropRule conditionalRule = new LeadingConditionRule(dropCondition);
            conditionalRule.OnSuccess(rule);
            npcLoot.Add(conditionalRule);
        }

        public static IItemDropRule BossBagDropCustom(int itemType, int amount = 1)
        {
            return new DropLocalPerClientAndResetsNPCMoneyTo0(itemType, 1, amount, amount, null);
        }

        #endregion npcloot

        /// ALL below From BaseDrawing meme, only used in golem Gib?? prob destroy, update

        #region basedrawing


        /*
         * Draws the given texture using the override color.
         * Uses a Entity for width, height, position, rotation, and sprite direction.
         */
        public static void DrawTexture(object sb, Texture2D texture, int shader, Entity codable, Color? overrideColor = null, bool drawCentered = false, Vector2 overrideOrigin = default)
        {
            DrawTexture(sb, texture, shader, codable, 1, overrideColor, drawCentered, overrideOrigin);
        }

        /*
         * Draws the given texture using the override color.
         * Uses a Entity for width, height, position, rotation, and sprite direction.
         */
        public static void DrawTexture(object sb, Texture2D texture, int shader, Entity codable, int framecountX, Color? overrideColor = null, bool drawCentered = false, Vector2 overrideOrigin = default)
        {
            Color lightColor = overrideColor != null ? (Color)overrideColor : codable is Item item ? item.GetAlpha(GetLightColor(codable.Center)) : codable is NPC nPC6 ? GetNPCColor(nPC6, codable.Center, false) : codable is Projectile projectile ? projectile.GetAlpha(GetLightColor(codable.Center)) : GetLightColor(codable.Center);
            int frameCount = codable is Item ? 1 : codable is NPC nPC ? Main.npcFrameCount[nPC.type] : 1;
            Rectangle frame = codable is NPC nPC1 ? nPC1.frame : new Rectangle(0, 0, texture.Width, texture.Height);
            float scale = codable is Item item1 ? item1.scale : codable is NPC nPC5 ? nPC5.scale : ((Projectile)codable).scale;
            float rotation = codable is Item ? 0 : codable is NPC nPC4 ? nPC4.rotation : ((Projectile)codable).rotation;
            int spriteDirection = codable is Item ? 1 : codable is NPC nPC2 ? nPC2.spriteDirection : ((Projectile)codable).spriteDirection;
            float offsetY = codable is NPC nPC3 ? nPC3.gfxOffY : 0f;
            DrawTexture(sb, texture, shader, codable.position + new Vector2(0f, offsetY), codable.width, codable.height, scale, rotation, spriteDirection, frameCount, framecountX, frame, lightColor, drawCentered, overrideOrigin);
        }

        public static void DrawTexture(object sb, Texture2D texture, int shader, Vector2 position, int width, int height, float scale, float rotation, int direction, int framecount, Rectangle frame, Color? overrideColor = null, bool drawCentered = false, Vector2 overrideOrigin = default)
        {
            DrawTexture(sb, texture, shader, position, width, height, scale, rotation, direction, framecount, 1, frame, overrideColor, drawCentered, overrideOrigin);
        }

        /*
         * Draws the given texture using lighting nearby, or the overriden color given.
         */
        public static void DrawTexture(object sb, Texture2D texture, int shader, Vector2 position, int width, int height, float scale, float rotation, int direction, int framecount, int framecountX, Rectangle frame, Color? overrideColor = null, bool drawCentered = false, Vector2 overrideOrigin = default)
        {
            Vector2 origin = overrideOrigin != default ? overrideOrigin : new Vector2(frame.Width / framecountX / 2, texture.Height / framecount / 2);
            Color lightColor = overrideColor != null ? (Color)overrideColor : GetLightColor(position + new Vector2(width * 0.5f, height * 0.5f));
            if (sb is List<DrawData> list)
            {
                DrawData dd = new(texture, GetDrawPosition(position, origin, width, height, texture.Width, texture.Height, framecount, framecountX, scale, drawCentered), frame, lightColor, rotation, origin, scale, direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0)
                {
                    shader = shader
                };
                list.Add(dd);
            }
            else if (sb is SpriteBatch batch)
            {
                bool applyDye = shader > 0;
                if (applyDye)
                {
                    batch.End();
                    batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                    GameShaders.Armor.ApplySecondary(shader, Main.player[Main.myPlayer], null);
                }
                batch.Draw(texture, GetDrawPosition(position, origin, width, height, texture.Width, texture.Height, framecount, framecountX, scale, drawCentered), frame, lightColor, rotation, origin, scale, direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
                if (applyDye)
                {
                    batch.End();
                    batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                }
            }
        }

        public static Color GetNPCColor(NPC npc, Vector2? position = null, bool effects = true, float shadowOverride = 0f)
        {
            return npc.GetAlpha(BuffEffects(npc, GetLightColor(position != null ? (Vector2)position : npc.Center), shadowOverride != 0f ? shadowOverride : 0f, effects, npc.poisoned, npc.onFire, npc.onFire2, Main.player[Main.myPlayer].detectCreature, false, false, false, npc.venom, npc.midas, npc.ichor, npc.onFrostBurn, false, false, npc.dripping, npc.drippingSlime, npc.loveStruck, npc.stinky));
        }

        /*
         * Convenience method for getting lighting color using an npc or projectile position.
         */
        public static Color GetLightColor(Vector2 position)
        {
            return Lighting.GetColor((int)(position.X / 16f), (int)(position.Y / 16f));
        }

        /*
         * Returns the draw position of a texture for npcs and projectiles.
         */
        public static Vector2 GetDrawPosition(Vector2 position, Vector2 origin, int width, int height, int texWidth, int texHeight, int framecount, float scale, bool drawCentered = false)
        {
            return GetDrawPosition(position, origin, width, height, texWidth, texHeight, framecount, 1, scale, drawCentered);
        }

        /*
         * Returns the draw position of a texture for npcs and projectiles.
         */
        public static Vector2 GetDrawPosition(Vector2 position, Vector2 origin, int width, int height, int texWidth, int texHeight, int framecount, int framecountX, float scale, bool drawCentered = false)
        {
            Vector2 screenPos = new((int)Main.screenPosition.X, (int)Main.screenPosition.Y);
            if (drawCentered)
            {
                Vector2 texHalf = new(texWidth / framecountX / 2, texHeight / framecount / 2);
                return position + new Vector2(width / 2, height / 2) - (texHalf * scale) + (origin * scale) - screenPos;
            }
            return position - screenPos + new Vector2(width / 2, height) - (new Vector2(texWidth / framecountX / 2, texHeight / framecount) * scale) + (origin * scale) + new Vector2(0f, 5f);
        }

        /*
         * Applies buff coloring and spawns effects based on if the effect is true or false. 
         */
        public static Color BuffEffects(Entity codable, Color lightColor, float shadow = 0f, bool effects = true, bool poisoned = false, bool onFire = false, bool onFire2 = false, bool hunter = false, bool noItems = false, bool blind = false, bool bleed = false, bool venom = false, bool midas = false, bool ichor = false, bool onFrostBurn = false, bool burned = false, bool honey = false, bool dripping = false, bool drippingSlime = false, bool loveStruck = false, bool stinky = false)
        {
            float cr = 1f; float cg = 1f; float cb = 1f; float ca = 1f;
            if (effects && honey && Main.rand.NextBool(30))
            {
                int dustID = Dust.NewDust(codable.position, codable.width, codable.height, DustID.Honey, 0f, 0f, 150, default, 1f);
                Main.dust[dustID].velocity.Y = 0.3f;
                Main.dust[dustID].velocity.X *= 0.1f;
                Main.dust[dustID].scale += Main.rand.Next(3, 4) * 0.1f;
                Main.dust[dustID].alpha = 100;
                Main.dust[dustID].noGravity = true;
                Main.dust[dustID].velocity += codable.velocity * 0.1f;
                //if (codable is Player) Main.playerDrawDust.Add(dustID);
            }
            if (poisoned)
            {
                if (effects && Main.rand.NextBool(30))
                {
                    int dustID = Dust.NewDust(codable.position, codable.width, codable.height, DustID.Poisoned, 0f, 0f, 120, default, 0.2f);
                    Main.dust[dustID].noGravity = true;
                    Main.dust[dustID].fadeIn = 1.9f;
                    //if (codable is Player) Main.playerDrawDust.Add(dustID);
                }
                cr *= 0.65f;
                cb *= 0.75f;
            }
            if (venom)
            {
                if (effects && Main.rand.NextBool(10))
                {
                    int dustID = Dust.NewDust(codable.position, codable.width, codable.height, DustID.Venom, 0f, 0f, 100, default, 0.5f);
                    Main.dust[dustID].noGravity = true;
                    Main.dust[dustID].fadeIn = 1.5f;
                    //if (codable is Player) Main.playerDrawDust.Add(dustID);
                }
                cg *= 0.45f;
                cr *= 0.75f;
            }
            if (midas)
            {
                cb *= 0.3f;
                cr *= 0.85f;
            }
            if (ichor)
            {
                if (codable is NPC) { lightColor = new Color(255, 255, 0, 255); } else { cb = 0f; }
            }
            if (burned)
            {
                if (effects)
                {
                    int dustID = Dust.NewDust(new Vector2(codable.position.X - 2f, codable.position.Y - 2f), codable.width + 4, codable.height + 4, DustID.Torch, codable.velocity.X * 0.4f, codable.velocity.Y * 0.4f, 100, default, 2f);
                    Main.dust[dustID].noGravity = true;
                    Main.dust[dustID].velocity *= 1.8f;
                    Main.dust[dustID].velocity.Y -= 0.75f;
                    //if (codable is Player) Main.playerDrawDust.Add(dustID);
                }
                if (codable is Player)
                {
                    cr = 1f;
                    cb *= 0.6f;
                    cg *= 0.7f;
                }
            }
            if (onFrostBurn)
            {
                if (effects)
                {
                    if (Main.rand.Next(4) < 3)
                    {
                        int dustID = Dust.NewDust(new Vector2(codable.position.X - 2f, codable.position.Y - 2f), codable.width + 4, codable.height + 4, DustID.IceTorch, codable.velocity.X * 0.4f, codable.velocity.Y * 0.4f, 100, default, 3.5f);
                        Main.dust[dustID].noGravity = true;
                        Main.dust[dustID].velocity *= 1.8f;
                        Main.dust[dustID].velocity.Y -= 0.5f;
                        if (Main.rand.NextBool(4))
                        {
                            Main.dust[dustID].noGravity = false;
                            Main.dust[dustID].scale *= 0.5f;
                        }
                        //if (codable is Player) Main.playerDrawDust.Add(dustID);
                    }
                    Lighting.AddLight((int)(codable.position.X / 16f), (int)(codable.position.Y / 16f + 1f), 0.1f, 0.6f, 1f);
                }
                if (codable is Player)
                {
                    cr *= 0.5f;
                    cg *= 0.7f;
                }
            }
            if (onFire)
            {
                if (effects)
                {
                    if (!Main.rand.NextBool(4))
                    {
                        int dustID = Dust.NewDust(codable.position - new Vector2(2f, 2f), codable.width + 4, codable.height + 4, DustID.Torch, codable.velocity.X * 0.4f, codable.velocity.Y * 0.4f, 100, default, 3.5f);
                        Main.dust[dustID].noGravity = true;
                        Main.dust[dustID].velocity *= 1.8f;
                        Main.dust[dustID].velocity.Y -= 0.5f;
                        if (Main.rand.NextBool(4))
                        {
                            Main.dust[dustID].noGravity = false;
                            Main.dust[dustID].scale *= 0.5f;
                        }
                        //if (codable is Player) Main.playerDrawDust.Add(dustID);
                    }
                    Lighting.AddLight((int)(codable.position.X / 16f), (int)(codable.position.Y / 16f + 1f), 1f, 0.3f, 0.1f);
                }
                if (codable is Player)
                {
                    cb *= 0.6f;
                    cg *= 0.7f;
                }
            }
            if (dripping && shadow == 0f && !Main.rand.NextBool(4))
            {
                Vector2 position = codable.position;
                position.X -= 2f; position.Y -= 2f;
                if (Main.rand.NextBool())
                {
                    int dustID = Dust.NewDust(position, codable.width + 4, codable.height + 2, DustID.Wet, 0f, 0f, 50, default, 0.8f);
                    if (Main.rand.NextBool()) Main.dust[dustID].alpha += 25;
                    if (Main.rand.NextBool()) Main.dust[dustID].alpha += 25;
                    Main.dust[dustID].noLight = true;
                    Main.dust[dustID].velocity *= 0.2f;
                    Main.dust[dustID].velocity.Y += 0.2f;
                    Main.dust[dustID].velocity += codable.velocity;
                    //if (codable is Player) Main.playerDrawDust.Add(dustID);
                }
                else
                {
                    int dustID = Dust.NewDust(position, codable.width + 8, codable.height + 8, DustID.Wet, 0f, 0f, 50, default, 1.1f);
                    if (Main.rand.NextBool()) Main.dust[dustID].alpha += 25;
                    if (Main.rand.NextBool()) Main.dust[dustID].alpha += 25;
                    Main.dust[dustID].noLight = true;
                    Main.dust[dustID].noGravity = true;
                    Main.dust[dustID].velocity *= 0.2f;
                    Main.dust[dustID].velocity.Y += 1f;
                    Main.dust[dustID].velocity += codable.velocity;
                    //if (codable is Player) Main.playerDrawDust.Add(dustID);
                }
            }
            if (drippingSlime && shadow == 0f)
            {
                int alpha = 175;
                Color newColor = new(0, 80, 255, 100);
                if (!Main.rand.NextBool(4))
                {
                    if (Main.rand.NextBool())
                    {
                        Vector2 position2 = codable.position;
                        position2.X -= 2f; position2.Y -= 2f;
                        int dustID = Dust.NewDust(position2, codable.width + 4, codable.height + 2, DustID.TintableDust, 0f, 0f, alpha, newColor, 1.4f);
                        if (Main.rand.NextBool()) Main.dust[dustID].alpha += 25;
                        if (Main.rand.NextBool()) Main.dust[dustID].alpha += 25;
                        Main.dust[dustID].noLight = true;
                        Main.dust[dustID].velocity *= 0.2f;
                        Main.dust[dustID].velocity.Y += 0.2f;
                        Main.dust[dustID].velocity += codable.velocity;
                        //if (codable is Player) Main.playerDrawDust.Add(dustID);
                    }
                }
                cr *= 0.8f;
                cg *= 0.8f;
            }
            if (onFire2)
            {
                if (effects)
                {
                    if (!Main.rand.NextBool(4))
                    {
                        int dustID = Dust.NewDust(codable.position - new Vector2(2f, 2f), codable.width + 4, codable.height + 4, DustID.CursedTorch, codable.velocity.X * 0.4f, codable.velocity.Y * 0.4f, 100, default, 3.5f);
                        Main.dust[dustID].noGravity = true;
                        Main.dust[dustID].velocity *= 1.8f;
                        Main.dust[dustID].velocity.Y -= 0.5f;
                        if (Main.rand.NextBool(4))
                        {
                            Main.dust[dustID].noGravity = false;
                            Main.dust[dustID].scale *= 0.5f;
                        }
                        //if (codable is Player) Main.playerDrawDust.Add(dustID);
                    }
                    Lighting.AddLight((int)(codable.position.X / 16f), (int)(codable.position.Y / 16f + 1f), 1f, 0.3f, 0.1f);
                }
                if (codable is Player)
                {
                    cb *= 0.6f;
                    cg *= 0.7f;
                }
            }
            if (noItems)
            {
                cr *= 0.65f;
                cg *= 0.8f;
            }
            if (blind)
            {
                cr *= 0.7f;
                cg *= 0.65f;
            }
            if (bleed)
            {
                bool dead = codable is Player player ? player.dead : codable is NPC nPC && nPC.life <= 0;
                if (effects && !dead && Main.rand.NextBool(30))
                {
                    int dustID = Dust.NewDust(codable.position, codable.width, codable.height, DustID.Blood, 0f, 0f, 0, default, 1f);
                    Main.dust[dustID].velocity.Y += 0.5f;
                    Main.dust[dustID].velocity *= 0.25f;
                    //if (codable is Player) Main.playerDrawDust.Add(dustID);
                }
                cg *= 0.9f;
                cb *= 0.9f;
            }
            if (loveStruck && effects && shadow == 0f && Main.instance.IsActive && !Main.gamePaused && Main.rand.NextBool(5))
            {
                Vector2 value = new(Main.rand.Next(-10, 11), Main.rand.Next(-10, 11));
                value.Normalize();
                value.X *= 0.66f;
                int goreID = Gore.NewGore(codable.GetSource_FromThis(), codable.position + new Vector2(Main.rand.Next(codable.width + 1), Main.rand.Next(codable.height + 1)), value * Main.rand.Next(3, 6) * 0.33f, 331, Main.rand.Next(40, 121) * 0.01f);
                Main.gore[goreID].sticky = false;
                Main.gore[goreID].velocity *= 0.4f;
                Main.gore[goreID].velocity.Y -= 0.6f;
                //if (codable is Player) Main.playerDrawGore.Add(goreID);
            }
            if (stinky && shadow == 0f)
            {
                cr *= 0.7f;
                cb *= 0.55f;
                if (effects && Main.rand.NextBool(5) && Main.instance.IsActive && !Main.gamePaused)
                {
                    Vector2 value2 = new(Main.rand.Next(-10, 11), Main.rand.Next(-10, 11));
                    value2.Normalize(); value2.X *= 0.66f; value2.Y = Math.Abs(value2.Y);
                    Vector2 vector = value2 * Main.rand.Next(3, 5) * 0.25f;
                    int dustID = Dust.NewDust(codable.position, codable.width, codable.height, DustID.FartInAJar, vector.X, vector.Y * 0.5f, 100, default, 1.5f);
                    Main.dust[dustID].velocity *= 0.1f;
                    Main.dust[dustID].velocity.Y -= 0.5f;
                    //if (codable is Player) Main.playerDrawDust.Add(dustID);
                }
            }
            lightColor.R = (byte)(lightColor.R * cr);
            lightColor.G = (byte)(lightColor.G * cg);
            lightColor.B = (byte)(lightColor.B * cb);
            lightColor.A = (byte)(lightColor.A * ca);

            if (hunter && (codable is not NPC || ((NPC)codable).lifeMax > 1))
            {
                if (effects && !Main.gamePaused && Main.instance.IsActive && Main.rand.NextBool(50))
                {
                    int dustID = Dust.NewDust(codable.position, codable.width, codable.height, DustID.MagicMirror, 0f, 0f, 150, default, 0.8f);
                    Main.dust[dustID].velocity *= 0.1f;
                    Main.dust[dustID].noLight = true;
                    //if (codable is Player) Main.playerDrawDust.Add(dustID);
                }
                byte colorR = 50, colorG = 255, colorB = 50;
                if (codable is NPC nPC && !(nPC.friendly || nPC.catchItem > 0 || (nPC.damage == 0 && nPC.lifeMax == 5)))
                {
                    colorR = 255; colorG = 50;
                }
                if (codable is not NPC && lightColor.R < 150) { lightColor.A = Main.mouseTextColor; }
                if (lightColor.R < colorR) { lightColor.R = colorR; }
                if (lightColor.G < colorG) { lightColor.G = colorG; }
                if (lightColor.B < colorB) { lightColor.B = colorB; }
            }
            return lightColor;
        }

        #endregion

        #region Easings
        public static float SineInOut(float value) => (0f - (MathF.Cos((value * MathF.PI)) - 1f)) / 2f;
        #endregion
    }
}
