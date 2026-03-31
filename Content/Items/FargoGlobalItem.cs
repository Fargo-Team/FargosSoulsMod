using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Souls;
using FargowiltasSouls.Content.Items.Placables;
using FargowiltasSouls.Content.Items.Weapons.BossDrops;
using FargowiltasSouls.Content.Items.Weapons.Challengers;
using FargowiltasSouls.Content.Projectiles.Souls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;

//using FargowiltasSouls.Content.Buffs.Souls;
//using FargowiltasSouls.Content.Projectiles.Critters;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.Items
{
    public class FargoGlobalItem : GlobalItem
    {
        public override void SetDefaults(Item item)
        {
            if (item.type is ItemID.Acorn or ItemID.GemTreeAmberSeed or ItemID.GemTreeAmethystSeed or ItemID.GemTreeDiamondSeed or ItemID.GemTreeEmeraldSeed or ItemID.GemTreeRubySeed or ItemID.GemTreeSapphireSeed or ItemID.GemTreeTopazSeed)
            {
                item.ammo = ItemID.Acorn;
                item.notAmmo = true;
            }

            if (item.type == ItemID.Bone)
            {
                item.ammo = ItemID.Bone;
                item.notAmmo = true;
            }
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (player.manaCost <= 0f) player.manaCost = 0f;
        }

        public override void ModifyManaCost(Item item, Player player, ref float reduce, ref float mult)
        {
        }

        public override void GrabRange(Item item, Player player, ref int grabRange)
        {
            FargoSoulsPlayer p = player.FargoSouls();
            if (player.whoAmI == Main.myPlayer && player.HasEffect<IronEffect>())
            {
                if (player.HasEffect<IronEquippedEffect>() || (item.type != ItemID.CopperCoin && item.type != ItemID.SilverCoin && item.type != ItemID.GoldCoin && item.type != ItemID.PlatinumCoin && item.type != ItemID.CandyApple && item.type != ItemID.SoulCake &&
                    item.type != ItemID.Star && item.type != ItemID.CandyCane && item.type != ItemID.SugarPlum && item.type != ItemID.Heart && item.type != ItemID.ManaCloakStar))
                {
                    int rangeBonus = 160;
                    if (p.ForceEffect<IronEnchant>())
                        rangeBonus = 320;
                    if (p.TerrariaSoul)
                        rangeBonus = 640;

                    grabRange += rangeBonus;
                }
            }
        }

        public override bool OnPickup(Item item, Player player)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();

            if (player.whoAmI == Main.myPlayer && player.HasEffect<GoldToPiggy>())
                modPlayer.GoldEnchMoveCoins = true;
            
            if (ItemID.Sets.IsAPickup[item.type] || item.type == ItemID.ManaCloakStar) //only vanilla pickup that doesnt set IsAPickup
            {
                OnRetrievePickup(player);
            }
            else
                IronPickupEffect.OnPickup(player);

            return base.OnPickup(item, player);
        }
        public static void OnRetrievePickup(Player player)
        {
            PearlwoodEffect.OnPickup(player);
            IronPickupEffect.OnPickup(player);
        }

        public override void PickAmmo(Item weapon, Item ammo, Player player, ref int type, ref float speed, ref StatModifier damage, ref float knockback)
        {
            //if (weapon.CountsAsClass(DamageClass.Ranged) && player.FargoSouls().Jammed)
                //type = ProjectileID.ConfettiGun;

            switch (ammo.type)
            {
                case ItemID.GemTreeAmethystSeed:
                    damage.Flat += 1;
                    break;
                case ItemID.GemTreeTopazSeed:
                    damage.Flat += 2; 
                    break;
                case ItemID.GemTreeSapphireSeed:
                    damage.Flat += 3;
                    break;
                case ItemID.GemTreeEmeraldSeed:
                    damage.Flat += 4;
                    break;
                case ItemID.GemTreeRubySeed:
                    damage.Flat += 5;
                    break;
                case ItemID.GemTreeAmberSeed:
                    damage.Flat += 6;
                    break;
                case ItemID.GemTreeDiamondSeed:
                    damage.Flat += 7;
                    break;
            }

            //coin gun is broken as fucking shit codingwise so i'm fixing it
            if (weapon.type == ItemID.CoinGun)
            {
                if (ammo.type == ItemID.CopperCoin || ammo.type == ModContent.Find<ModItem>("Fargowiltas", "CopperCoinBag").Type)
                {
                    type = ProjectileID.CopperCoin;
                }
                if (ammo.type == ItemID.SilverCoin || ammo.type == ModContent.Find<ModItem>("Fargowiltas", "SilverCoinBag").Type)
                {
                    type = ProjectileID.SilverCoin;
                }
                if (ammo.type == ItemID.GoldCoin || ammo.type == ModContent.Find<ModItem>("Fargowiltas", "GoldCoinBag").Type)
                {
                    type = ProjectileID.GoldCoin;
                }
                if (ammo.type == ItemID.PlatinumCoin || ammo.type == ModContent.Find<ModItem>("Fargowiltas", "PlatinumCoinBag").Type)
                {
                    type = ProjectileID.PlatinumCoin;
                }
            }
        }

        public override void OnConsumeItem(Item item, Player player)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();

            if (item.healLife > 0 && item.potion && player.HasBuff(BuffID.PotionSickness))
            {
                if (player.HasEffect<ShroomiteHealEffect>())
                {
                    if (item.type == ItemID.Mushroom)
                    {
                        player.AddBuff(ModContent.BuffType<MushroomPowerBuff>(), LumUtils.SecondsToFrames(20f));
                    }
                }
                if (player.HasEffect<HallowEffect>())
                {
                    int hallowIndex = ModContent.GetInstance<HallowEffect>().Index;
                    // Hallow needs to disabled so it doesn't set GetHealLife to 0
                    player.AccessoryEffects().ActiveEffects[hallowIndex] = false;
                    float mult = modPlayer.ForceEffect<HallowEnchant>() ? 1.7f : 1.4f;
                    modPlayer.HallowHealTotal = player.GetHealLife(item) * mult;
                    modPlayer.HallowHealTime = 600;
                    player.AccessoryEffects().ActiveEffects[hallowIndex] = true;
                    HallowEffect.HealRepel(player);
                }
                modPlayer.StatLifePrevious += modPlayer.GetHealMultiplier(item.healLife);
            }
        }

        public override bool ConsumeItem(Item item, Player player)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();

            //if (item.makeNPC > 0 && (p.WoodForce || p.WizardEnchant) && Main.rand.NextBool())
            //{
            //    return false;
            //}

            if (modPlayer.BuilderMode && (item.createTile > TileID.Dirt || item.createWall > 0))
                return false;

            return base.ConsumeItem(item, player);
        }

        public static List<int> TungstenAlwaysAffects =
        [
            ItemID.TerraBlade,
            ItemID.NightsEdge,
            ItemID.TrueNightsEdge,
            ItemID.Excalibur,
            ItemID.TrueExcalibur,
            //ItemID.PiercingStarlight,
            ItemID.TheHorsemansBlade,
            ModContent.ItemType<TheBaronsTusk>(),
            ItemID.LucyTheAxe,
            ModContent.ItemType<SlimeKingsSlasher>(),
            ItemID.TheAxe
        ];
        public override void ModifyItemScale(Item item, Player player, ref float scale)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();

            if (!item.IsAir && ((item.IsWeapon() && !item.noMelee) || TungstenAlwaysAffects.Contains(item.type)))
            {
                if (player.HasEffect<TungstenEffect>())
                {
                    scale *= TungstenEffect.TungstenIncreaseWeaponSize(modPlayer);
                    if (item.type == ItemID.TheAxe && player.name.ToLower().Contains("gonk"))
                        scale *= 2.5f;
                }
                if (modPlayer.Atrophied)
                    scale *= 0.5f;
            }
        }

        public override void ModifyWeaponKnockback(Item item, Player player, ref StatModifier knockback)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();

            if (modPlayer.UniverseSoul || modPlayer.Eternity)
                knockback *= 2;
        }

        public override bool? CanAutoReuseItem(Item item, Player player)
        {
            if (item.ModItem != null)
            {
                if (item.ModItem.CanAutoReuseItem(player) != null)
                    return item.ModItem.CanAutoReuseItem(player);
            }

            FargoSoulsPlayer modPlayer = player.FargoSouls();

            if (modPlayer.Berserked)
            {
                return true;
            }
            if (modPlayer.BoxofGizmos)
            {
                if (item.DamageType == DamageClass.Default && item.damage <= 0 && item.fishingPole <= 0)
                {
                    return true;
                }
            }
            return base.CanAutoReuseItem(item, player);
        }
        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            if (player.HasEffect<TikiEffect>())
            {
                if (item.shoot > ProjectileID.None && ProjectileID.Sets.IsAWhip[item.shoot] && item.DamageType.CountsAsClass(DamageClass.SummonMeleeSpeed))
                {
                    damage /= player.ActualClassDamage(DamageClass.SummonMeleeSpeed);
                    List<float> types =
                        [
                            player.ActualClassDamage(DamageClass.Melee),
                            player.ActualClassDamage(DamageClass.Ranged),
                            player.ActualClassDamage(DamageClass.Magic),
                            player.ActualClassDamage(DamageClass.Summon)
                        ];
                    damage *= types.Max();
                }
            }
        }
        public override void ModifyWeaponCrit(Item item, Player player, ref float crit)
        {
            if (player.HasEffect<TikiEffect>())
            {
                if (item.shoot > ProjectileID.None && ProjectileID.Sets.IsAWhip[item.shoot] && item.DamageType.CountsAsClass(DamageClass.SummonMeleeSpeed))
                {
                    crit /= player.ActualClassCrit(DamageClass.SummonMeleeSpeed);
                    crit *= FargoSoulsUtil.HighestCritChance(player);
                }
            }
        }
        public override bool CanUseItem(Item item, Player player)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();

            if (modPlayer.NoUsingItems > 0)
                return false;

            if (PyramidGenSystem.ArenaItemPrevention(item, player))
                return false;
            //if (modPlayer.AdamantiteEnchantActive && modPlayer.AdamantiteCD == 0)
            //{
            //// ??? tm
            //}

            //dont use hotkeys in stasis
            if (player.HasBuff(ModContent.BuffType<GoldenStasisBuff>()))
            {
                if (item.type == ItemID.RodofDiscord)
                    player.ClearBuff(ModContent.BuffType<GoldenStasisBuff>());
                else
                    return false;
            }

            if (modPlayer.BuilderMode && (item.createTile != -1 || item.createWall != -1) && item.type != ItemID.PlatinumCoin && item.type != ItemID.GoldCoin)
            {
                item.useTime = 1;
                item.useAnimation = 1;
            }

            if (item.IsWeapon() && player.HasAmmo(item) && !(item.mana > 0 && player.statMana < item.mana) //non weapons and weapons with no ammo begone
                && item.type != ItemID.ExplosiveBunny && item.type != ItemID.Cannonball
                && item.useTime > 0 && item.createTile == -1 && item.createWall == -1 && item.ammo == AmmoID.None)
            {
                modPlayer.TryAdditionalAttacks(item.damage, item.DamageType);
                player.AccessoryEffects().TryAdditionalAttacks(item.damage, item.DamageType);
            }

            //            //critter attack timer
            //            if (modPlayer.WoodEnchant && player.altFunctionUse == ItemAlternativeFunctionID.ActivatedAndUsed && item.makeNPC > 0)
            //            {
            //                if (modPlayer.CritterAttackTimer == 0)
            //                {
            //                    Vector2 vel = Vector2.Normalize(Main.MouseWorld - player.Center);
            //                    float damageMultiplier = player.GetDamage(DamageClass.Summon);

            //                    int type = -1;
            //                    int damage = 0;
            //                    int attackCooldown = 0;

            //                    switch (item.type)
            //                    {
            //                        //case ItemID.Bunny:
            //                        //    type = ProjectileID.ExplosiveBunny;
            //                        //    damage = 10;
            //                        //    attackCooldown = 10;
            //                        //    break;

            //                        case ItemID.Bird:
            //                            type = ModContent.ProjectileType<BirdProj>();
            //                            damage = 15;
            //                            attackCooldown = 15;
            //                            break;

            //                        case ItemID.BlueJay:
            //                            type = ModContent.ProjectileType<BlueJayProj>();
            //                            damage = 10;
            //                            attackCooldown = 10;
            //                            break;

            //                        case ItemID.Cardinal:
            //                            type = ModContent.ProjectileType<CardinalProj>();
            //                            damage = 20;
            //                            attackCooldown = 20;
            //                            break;
            //                    }

            //                    if (type != -1)
            //                    {
            //                        Projectile.NewProjectile(player.Center, vel * 2f, type, damage, 2, player.whoAmI);
            //                        modPlayer.CritterAttackTimer = attackCooldown;
            //                    }


            //                }





            //                return false;
            //            }

            if (item.type == ItemID.RodofDiscord && player.chaosState)
            {
                player.FargoSouls().WasHurtBySomething = true; //with abom rebirth, die to chaos state
            }
            /*
            if (item.type == ItemID.PotionOfReturn && LumUtils.AnyBosses() && WorldSavingSystem.EternityMode)
            {
                return false;
            }
            */
            if (item.IsWeaponWithDamageClass() && item == player.HeldItem)
            {
                player.FargoSouls().WeaponUseTimer = 2 + (int)Math.Round((Math.Max(item.useTime, item.useAnimation) + item.reuseDelay) / player.FargoSouls().AttackSpeed);
            }
            return true;
        }
        public override bool? UseItem(Item item, Player player)
        {
            if (item.type == ItemID.RodofDiscord)
            {
                player.ClearBuff(ModContent.BuffType<GoldenStasisBuff>());

               // if (player.FargoSouls().CrystalEnchantActive)
               //     player.AddBuff(ModContent.BuffType<FirstStrikeBuff>(), 60);
            }
            return base.UseItem(item, player);
        }

        //        public override bool AltFunctionUse(Item item, Player player)
        //        {
        //            FargoSoulsPlayer modPlayer = player.FargoSouls();

        //            if (modPlayer.WoodEnchant)
        //            {
        //                switch (item.type)
        //                {
        //                    case ItemID.Bunny:
        //                    case ItemID.Bird:
        //                    case ItemID.BlueJay:
        //                    case ItemID.Cardinal:
        //                        return true;

        //                }
        //            }



        //            return base.AltFunctionUse(item, player);
        //        }

        //        public override bool NewPreReforge(Item item)
        //        {
        //            /*if (Main.player[item.owner].FargoSouls().SecurityWallet)
        //            {
        //                switch(item.prefix)
        //                {
        //                    case PrefixID.Warding:  if (SoulConfig.Instance.walletToggles.Warding)  return false; break;
        //                    case PrefixID.Violent:  if (SoulConfig.Instance.walletToggles.Violent)  return false; break;
        //                    case PrefixID.Quick:    if (SoulConfig.Instance.walletToggles.Quick)    return false; break;
        //                    case PrefixID.Lucky:    if (SoulConfig.Instance.walletToggles.Lucky)    return false; break;
        //                    case PrefixID.Menacing: if (SoulConfig.Instance.walletToggles.Menacing) return false; break;
        //                    case PrefixID.Legendary:if (SoulConfig.Instance.walletToggles.Legendary)return false; break;
        //                    case PrefixID.Unreal:   if (SoulConfig.Instance.walletToggles.Unreal)   return false; break;
        //                    case PrefixID.Mythical: if (SoulConfig.Instance.walletToggles.Mythical) return false; break;
        //                    case PrefixID.Godly:    if (SoulConfig.Instance.walletToggles.Godly)    return false; break;
        //                    case PrefixID.Demonic:  if (SoulConfig.Instance.walletToggles.Demonic)  return false; break;
        //                    case PrefixID.Ruthless: if (SoulConfig.Instance.walletToggles.Ruthless) return false; break;
        //                    case PrefixID.Light:    if (SoulConfig.Instance.walletToggles.Light)    return false; break;
        //                    case PrefixID.Deadly:   if (SoulConfig.Instance.walletToggles.Deadly)   return false; break;
        //                    case PrefixID.Rapid:    if (SoulConfig.Instance.walletToggles.Rapid)    return false; break;
        //                    default: break;
        //                }
        //            }*/
        //            return true;
        //        }

        public override void ModifyShootStats(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();

            float velocityMult = 1f;
            if (modPlayer.Eternity)
                velocityMult += 1f;
            if (modPlayer.UniverseSoul)
                velocityMult += 0.5f;
            if (modPlayer.RangedSoul && item.CountsAsClass(DamageClass.Ranged))
                velocityMult += 0.2f;
            if (modPlayer.RangedEssence  && item.CountsAsClass(DamageClass.Ranged))
                velocityMult += 0.1f;
            velocity *= velocityMult;

        }

        public override bool ReforgePrice(Item item, ref int reforgePrice, ref bool canApplyDiscount)
        {
            if (Main.LocalPlayer.HasEffect<IronPassiveEffect>() && canApplyDiscount)
                reforgePrice = (int)(reforgePrice * 1.5);
            if (Main.LocalPlayer.FargoSouls().SecurityWallet && canApplyDiscount)
                reforgePrice /= 2;
            return base.ReforgePrice(item, ref reforgePrice, ref canApplyDiscount);
        }

        public override bool WingUpdate(int wings, Player player, bool inUse)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            if (player.HasEffect<JungleJump>() && inUse)
            {
                modPlayer.CanJungleJump = false;

                //spwn cloud
                if (modPlayer.JungleCD == 0)
                {

                    int tier = 1;
                    if (modPlayer.ChlorophyteEnchantActive)
                        tier++;
                    bool jungleForceEffect = modPlayer.ForceEffect<JungleEnchant>();
                    if (jungleForceEffect)
                        tier++;

                    modPlayer.JungleCD = 18 - tier * tier;
                    int dmg = 12 * tier * tier - 5;

                    SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.5f }, player.Center);
                    foreach (Projectile p in FargoSoulsUtil.XWay(10, player.GetSource_EffectItem<JungleJump>(), new Vector2(player.Center.X, player.Center.Y + player.height / 2), ProjectileID.SporeCloud, 3f, FargoSoulsUtil.HighestDamageTypeScaling(player, dmg), 0))
                    {
                        p.extraUpdates += 1;
                    }

                    modPlayer.JungleCD = 24;
                }
            }

            if (player.HasEffect<BeeEffect>() && inUse)
            {
                bool lifeForce = modPlayer.LifeForceActive;
                if (modPlayer.BeeCD == 0)
                {
                    int force = lifeForce ? 1 : 0;
                    int damage = player.ForceEffect<BeeEffect>() ? 88 : 22; //22
                    if (lifeForce)
                        damage = 222;
                    Projectile.NewProjectile(player.GetSource_Accessory(player.EffectItem<BeeEffect>()), player.Center, Vector2.Zero, ModContent.ProjectileType<BeeFlower>(), damage, 0.5f, player.whoAmI, ai2: force);
                    int cd = 50;
                    if ((lifeForce))
                        cd = 150;
                    modPlayer.BeeCD = cd;
                }
                if (modPlayer.BeeCD > 0)
                    modPlayer.BeeCD--;
            }

            return base.WingUpdate(wings, player, inUse);
        }

        public override void VerticalWingSpeeds(Item item, Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            base.VerticalWingSpeeds(item, player, ref ascentWhenFalling, ref ascentWhenRising, ref maxCanAscendMultiplier, ref maxAscentMultiplier, ref constantAscend);

            //Main.NewText($"vertical: {ascentWhenFalling} {ascentWhenRising} {maxCanAscendMultiplier} {maxAscentMultiplier} {constantAscend}");
        }

        public override void HorizontalWingSpeeds(Item item, Player player, ref float speed, ref float acceleration)
        {
            base.HorizontalWingSpeeds(item, player, ref speed, ref acceleration);

            //Main.NewText($"horiz: {speed} {acceleration}");
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.PiercingStarlight)
                tooltips.Add(new TooltipLine(Mod, "StarlightTungsten", Language.GetTextValue("Mods.FargowiltasSouls.Items.Extra.StarlightTungsten")));

            if (item.potion || item.healLife > 0)
            {
                bool hallow = Main.LocalPlayer.HasEffect<HallowEffect>();
                bool shroomite = Main.LocalPlayer.HasEffect<ShroomiteHealEffect>() && item.type == ItemID.Mushroom;
                if (hallow || (shroomite))
                {
                    foreach (var tooltip in tooltips)
                    {
                        if (tooltip.Name == "HealLife")
                        {
                            if (hallow)
                            {
                                tooltip.Text = "[i:FargowiltasSouls/HallowEnchant] " + tooltip.Text;
                                tooltip.Text += $" {Language.GetTextValue("Mods.FargowiltasSouls.Items.HallowEnchant.OverTime")}";
                            }
                            if (shroomite)
                            {
                                tooltip.Text = "[i:FargowiltasSouls/ShroomiteEnchant] " + tooltip.Text;
                                tooltip.Text += $" {Language.GetTextValue("Mods.FargowiltasSouls.Items.ShroomiteEnchant.AndMushroomPower")}";
                            }
                        }
                    }
                }
            }
            
            if (FargoSoulsUtil.AprilFools)
            {
                foreach (var tooltip in tooltips)
                {
                    tooltip.Text = tooltip.Text.Replace("Enchantment", "Enhancement");
                    tooltip.Text = tooltip.Text.Replace("Mutant", "Fargo");
                }
            }
        }

        static int infiniteLoopHackFix;
        public static bool Reforging = false;
        public override void PreReforge(Item item)
        {
            Reforging = true;
        }
        public override bool AllowPrefix(Item item, int pre)
        {
            if (!Main.gameMenu && Main.LocalPlayer.active && Main.LocalPlayer.HasEffect<IronPassiveEffect>() && Reforging)
            {
                List<int> allowed = [PrefixID.Legendary, PrefixID.Legendary2, PrefixID.Mythical, PrefixID.Unreal, PrefixID.Godly, PrefixID.Demonic, PrefixID.Ruthless];
                if (item.accessory)
                    allowed.AddRange([PrefixID.Violent, PrefixID.Quick, PrefixID.Menacing, PrefixID.Warding, PrefixID.Lucky, PrefixID.Arcane]);
                if (item.pick > 0 || item.axe > 0 || item.hammer > 0)
                    allowed.Add(PrefixID.Light);
                allowed.Remove(item.prefix);
                if (pre < 85 && !allowed.Contains(pre))
                {
                    if (++infiniteLoopHackFix < 2000)
                        return false;
                }
            }
            infiniteLoopHackFix = 0;

            return base.AllowPrefix(item, pre);
        }
        public override void PostReforge(Item item)
        {
            Reforging = false;
        }
    }
}