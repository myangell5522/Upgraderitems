using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Upgraderitems.Common.Players
{
	/// <summary>
	/// Holds everything that must survive a save/quit: the item currently sitting in the upgrader,
	/// the chosen target, lifetime gambling stats and the player's custom button position.
	/// </summary>
	public class UpgraderPlayer : ModPlayer
	{
		public const int HistoryLength = 10;

		public Item StakedItem = new();
		public int TargetType = ItemID.None;

		public int Wins;
		public int Losses;
		public int Streak;
		public int BestStreak;
		public long TotalValueWon;
		public long TotalValueLost;

		/// <summary>Most recent results, newest last. True = win.</summary>
		public readonly List<bool> History = new();

		public Vector2 ButtonOffset = Vector2.Zero;
		public int Cooldown;

		public override void ResetEffects()
		{
			if (Cooldown > 0)
				Cooldown--;
		}

		public void RecordResult(bool won, long stakeValue, long rewardValue)
		{
			if (won)
			{
				Wins++;
				Streak++;
				if (Streak > BestStreak)
					BestStreak = Streak;
				TotalValueWon += rewardValue - stakeValue;
			}
			else
			{
				Losses++;
				Streak = 0;
				TotalValueLost += stakeValue;
			}

			History.Add(won);
			while (History.Count > HistoryLength)
				History.RemoveAt(0);
		}

		public override void SaveData(TagCompound tag)
		{
			if (!StakedItem.IsAir)
				tag["staked"] = ItemIO.Save(StakedItem);

			if (TargetType > ItemID.None)
				tag["targetName"] = FullNameOf(TargetType);

			tag["wins"] = Wins;
			tag["losses"] = Losses;
			tag["bestStreak"] = BestStreak;
			tag["streak"] = Streak;
			tag["valueWon"] = TotalValueWon;
			tag["valueLost"] = TotalValueLost;
			tag["buttonX"] = ButtonOffset.X;
			tag["buttonY"] = ButtonOffset.Y;

			if (History.Count > 0)
				tag["history"] = History.Select(b => (byte)(b ? 1 : 0)).ToList();
		}

		public override void LoadData(TagCompound tag)
		{
			StakedItem = tag.TryGet("staked", out TagCompound stakedTag) ? ItemIO.Load(stakedTag) : new Item();
			TargetType = tag.TryGet("targetName", out string targetName) ? TypeOfFullName(targetName) : ItemID.None;

			Wins = tag.GetInt("wins");
			Losses = tag.GetInt("losses");
			BestStreak = tag.GetInt("bestStreak");
			Streak = tag.GetInt("streak");
			TotalValueWon = tag.GetLong("valueWon");
			TotalValueLost = tag.GetLong("valueLost");
			ButtonOffset = new Vector2(tag.GetFloat("buttonX"), tag.GetFloat("buttonY"));

			History.Clear();
			if (tag.TryGet("history", out List<byte> history))
				History.AddRange(history.Select(b => b != 0));
		}

		/// <summary>
		/// Item ids shift when the mod list changes, so the target is persisted by name instead.
		/// </summary>
		private static string FullNameOf(int type)
		{
			if (type < ItemID.Count)
				return "Terraria:" + ItemID.Search.GetName(type);

			ModItem modItem = ItemLoader.GetItem(type);
			return modItem == null ? string.Empty : modItem.Mod.Name + ":" + modItem.Name;
		}

		private static int TypeOfFullName(string fullName)
		{
			if (string.IsNullOrEmpty(fullName))
				return ItemID.None;

			string[] parts = fullName.Split(':', 2);
			if (parts.Length != 2)
				return ItemID.None;

			if (parts[0] == "Terraria")
				return ItemID.Search.TryGetId(parts[1], out int vanillaType) ? vanillaType : ItemID.None;

			return ModContent.TryFind(parts[0], parts[1], out ModItem modItem) ? modItem.Type : ItemID.None;
		}
	}
}
