using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Upgraderitems.Common.Configs;
using Upgraderitems.Common.Players;

namespace Upgraderitems.Common.Economy
{
	public enum UpgradeBlockReason
	{
		Ready,
		NoStake,
		NoTarget,
		CategoryMismatch,
		SameItem,
		Favorited,
		Cooldown,
		MultiplayerDisabled
	}

	public static class UpgradeEngine
	{
		/// <summary>
		/// Fair odds are stake/target; the house edge shaves a slice off so the pool slowly drains,
		/// which is what keeps the mechanic from being an infinite item generator.
		/// </summary>
		public static float ComputeChance(long stakeValue, long targetValue)
		{
			UpgraderServerConfig config = UpgraderServerConfig.Instance;
			if (stakeValue <= 0 || targetValue <= 0)
				return 0f;

			double raw = (double)stakeValue / targetValue * config.HouseEdge;
			return (float)Math.Clamp(raw, config.MinChance, config.MaxChance);
		}

		/// <summary>Target value that would produce exactly <paramref name="chance"/> for this stake.</summary>
		public static long TargetValueForChance(long stakeValue, float chance)
		{
			UpgraderServerConfig config = UpgraderServerConfig.Instance;
			chance = Math.Clamp(chance, config.MinChance, config.MaxChance);
			return (long)Math.Max(1, stakeValue * config.HouseEdge / chance);
		}

		public static UpgradeBlockReason Validate(Player player, Item stake, int targetType)
		{
			UpgraderServerConfig config = UpgraderServerConfig.Instance;

			if (Main.netMode != NetmodeID.SinglePlayer && !config.AllowInMultiplayer)
				return UpgradeBlockReason.MultiplayerDisabled;

			if (stake == null || stake.IsAir)
				return UpgradeBlockReason.NoStake;

			if (config.ProtectFavoritedItems && stake.favorited)
				return UpgradeBlockReason.Favorited;

			if (targetType <= ItemID.None)
				return UpgradeBlockReason.NoTarget;

			if (targetType == stake.type)
				return UpgradeBlockReason.SameItem;

			if (config.StrictCategories)
			{
				UpgradeCategory stakeCategory = UpgradeCategoryUtils.Classify(stake);
				UpgradeCategory targetCategory = ContentSamples.ItemsByType.TryGetValue(targetType, out Item sample)
					? UpgradeCategoryUtils.Classify(sample)
					: UpgradeCategory.None;

				if (stakeCategory == UpgradeCategory.None || stakeCategory != targetCategory)
					return UpgradeBlockReason.CategoryMismatch;
			}

			if (player.GetModPlayer<UpgraderPlayer>().Cooldown > 0)
				return UpgradeBlockReason.Cooldown;

			return UpgradeBlockReason.Ready;
		}

		public static string BlockMessage(UpgradeBlockReason reason)
			=> Language.GetTextValue($"Mods.Upgraderitems.Blocked.{reason}");

		/// <summary>
		/// Applies an already-rolled outcome. The roll happens up front so the wheel animation can land
		/// on the real result instead of faking it.
		/// </summary>
		public static void ApplyResult(Player player, bool won, int targetType, Item stake)
		{
			UpgraderServerConfig config = UpgraderServerConfig.Instance;
			UpgraderPlayer modPlayer = player.GetModPlayer<UpgraderPlayer>();

			long stakeValue = ItemValue.Get(stake);
			long rewardValue = ItemValue.Get(targetType);

			IEntitySource source = player.GetSource_Misc("Upgraderitems:Upgrade");

			if (won)
			{
				player.QuickSpawnItem(source, targetType);
			}
			else if (config.FailureRefund > 0f)
			{
				// Refund is paid in real coins based on the item's sell price, not the abstract value.
				long copper = (long)(stake.value / 5.0 * config.FailureRefund);
				SpawnCoins(player, source, copper);
			}

			modPlayer.Cooldown = config.CooldownTicks;
			modPlayer.RecordResult(won, stakeValue, rewardValue);
		}

		private static void SpawnCoins(Player player, IEntitySource source, long copper)
		{
			if (copper <= 0)
				return;

			int[] coins = Utils.CoinsSplit(copper);
			for (int tier = 0; tier < coins.Length; tier++)
			{
				if (coins[tier] <= 0)
					continue;

				int type = tier switch
				{
					0 => ItemID.CopperCoin,
					1 => ItemID.SilverCoin,
					2 => ItemID.GoldCoin,
					_ => ItemID.PlatinumCoin
				};

				player.QuickSpawnItem(source, type, coins[tier]);
			}
		}

		public static Color ChanceColor(float chance)
		{
			if (chance >= 0.60f)
				return new Color(120, 235, 120);
			if (chance >= 0.35f)
				return new Color(240, 225, 110);
			if (chance >= 0.15f)
				return new Color(250, 165, 70);
			return new Color(245, 95, 95);
		}
	}
}
