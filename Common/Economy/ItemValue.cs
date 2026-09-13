using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Upgraderitems.Common.Economy
{
	/// <summary>
	/// Assigns every item - vanilla or modded - a single "market value" number used as the currency of the
	/// upgrader. The score is derived purely from public <see cref="Item"/> stat fields, which is what makes
	/// mod support automatic: any weapon, armour piece or accessory registered by any mod gets a value.
	///
	/// The score mixes five independent signals so that no single stat can be gamed:
	///   1. effective DPS      (damage x attack speed x crit)
	///   2. defense
	///   3. tool power         (pickaxe / axe / hammer)
	///   4. rarity tier        (the game's own progression hint)
	///   5. vendor price       (a hand-tuned number the devs already balanced)
	/// </summary>
	public static class ItemValue
	{
		private static readonly Dictionary<int, long> BaseValueByType = new();

		public const long MinValue = 1;
		public const long MaxValue = 100_000_000;

		public static void ClearCache() => BaseValueByType.Clear();

		/// <summary>Value of a pristine, unprefixed item of this type.</summary>
		public static long Get(int type)
		{
			if (type <= ItemID.None)
				return 0;

			if (BaseValueByType.TryGetValue(type, out long cached))
				return cached;

			long value = MinValue;
			if (ContentSamples.ItemsByType.TryGetValue(type, out Item sample) && sample != null)
				value = Compute(sample);

			BaseValueByType[type] = value;
			return value;
		}

		/// <summary>Value of a concrete item instance, prefix included.</summary>
		public static long Get(Item item)
		{
			if (item == null || item.IsAir)
				return 0;

			// Unmodified items hit the per-type cache; reforged ones are recomputed so the player can see
			// that a Legendary sword really is worth more than an unforged one.
			if (item.prefix == 0)
				return Get(item.type);

			return Compute(item);
		}

		/// <summary>Total value of a stack.</summary>
		public static long GetStack(Item item)
			=> item == null || item.IsAir ? 0 : Get(item) * Math.Max(item.stack, 1);

		private static long Compute(Item item)
		{
			double score = 0;

			bool isTool = item.pick > 0 || item.axe > 0 || item.hammer > 0;

			// --- 1. offense -------------------------------------------------------------------------
			if (item.damage > 0 && !item.accessory)
			{
				// useAnimation drives the real swing rate; clamp it so 1-frame oddities cannot explode.
				int animation = item.useAnimation > 0 ? item.useAnimation : item.useTime;
				double attacksPerSecond = 60.0 / Math.Clamp(animation, 2, 240);
				attacksPerSecond = Math.Min(attacksPerSecond, 15.0);

				double critMultiplier = 1.0 + Math.Clamp(item.crit + 4, 0, 100) / 100.0;
				double dps = item.damage * attacksPerSecond * critMultiplier;

				if (item.consumable)
					dps *= 0.35;
				if (item.ammo != AmmoID.None)
					dps *= 0.5;
				// A pickaxe swinging 12 times a second is not a 12-hits-per-second weapon.
				if (isTool)
					dps *= 0.25;

				score += Math.Pow(dps, 1.12) * 0.9;
			}

			// --- 2. defense -------------------------------------------------------------------------
			if (item.defense > 0)
				score += Math.Pow(item.defense, 1.8) * 2.5;

			// --- 3. tools ---------------------------------------------------------------------------
			int toolPower = Math.Max(item.pick, Math.Max(item.axe * 5, item.hammer));
			if (toolPower > 0)
				score += Math.Pow(toolPower, 1.45) * 0.5;

			// --- 4. rarity --------------------------------------------------------------------------
			// Rarity is the game's own progression ladder, so it carries the most weight; the stat terms
			// above then order items *within* a tier. Without this, a fast low-damage gun outranks a
			// slow endgame sword purely on paper DPS.
			int rarity = EffectiveRarity(item.rare);
			if (rarity > 0)
				score += Math.Pow(rarity, 2.1) * 26.0;

			// --- 5. vendor price --------------------------------------------------------------------
			if (item.value > 0)
				score += Math.Sqrt(item.value / 5.0) * 2.0;

			// --- category baselines -----------------------------------------------------------------
			// Accessories have no stat the engine exposes, so rarity and price do all the work here.
			if (item.accessory)
				score += 80.0 + rarity * 25.0;

			// Wooden armour has 1 defense and no vendor price, which would otherwise score a 2 and make
			// the bottom of the armour ladder unplayable.
			if (item.defense > 0 && (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0))
				score += 30.0 + rarity * 15.0;

			if (item.healLife > 0)
				score += item.healLife * 1.5;
			if (item.healMana > 0)
				score += item.healMana * 0.4;

			// --- multipliers ------------------------------------------------------------------------
			double multiplier = 1.0;
			if (item.expert || item.rare == ItemRarityID.Expert)
				multiplier *= 1.25;
			if (item.master || item.rare == ItemRarityID.Master)
				multiplier *= 1.4;
			if (item.prefix > 0)
				multiplier *= PrefixMultiplier(item);

			double result = Math.Round(score * multiplier);
			return (long)Math.Clamp(result, MinValue, MaxValue);
		}

		/// <summary>
		/// A reforge already changed damage/crit/defense on the instance, so this only adds the extra
		/// desirability of the prefix itself. Vanilla encodes exactly that in the item's price ratio.
		/// </summary>
		private static double PrefixMultiplier(Item item)
		{
			if (!ContentSamples.ItemsByType.TryGetValue(item.type, out Item sample) || sample == null || sample.value <= 0)
				return 1.0;

			double ratio = item.value / (double)sample.value;
			return Math.Clamp(1.0 + (ratio - 1.0) * 0.5, 0.8, 1.6);
		}

		/// <summary>
		/// Maps the rarity id - including the negative special rarities and modded rarities above the
		/// vanilla range - onto a linear 0..14 progression scale.
		/// </summary>
		public static int EffectiveRarity(int rare)
		{
			switch (rare)
			{
				case ItemRarityID.Master:
					return 12;
				case ItemRarityID.Expert:
					return 11;
				case ItemRarityID.Quest:
					return 11;
			}

			if (rare < 0)
				return 0;

			// Modded rarities sit above ItemRarityID.Purple; flatten them so a mod cannot claim tier 500.
			if (rare > ItemRarityID.Purple)
				return ItemRarityID.Purple + Math.Min(rare - ItemRarityID.Purple, 3);

			return rare;
		}

		/// <summary>1234567 -> "1.23M". Keeps slot labels readable.</summary>
		public static string Format(long value)
		{
			if (value >= 1_000_000_000)
				return (value / 1_000_000_000.0).ToString("0.##", CultureInfo.InvariantCulture) + "B";
			if (value >= 1_000_000)
				return (value / 1_000_000.0).ToString("0.##", CultureInfo.InvariantCulture) + "M";
			if (value >= 10_000)
				return (value / 1_000.0).ToString("0.#", CultureInfo.InvariantCulture) + "K";
			return value.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Quick visual tier read. Thresholds are set against the vanilla spread, where a fresh
		/// character sits near 50 and endgame gear lands around 5000.
		/// </summary>
		public static Color TierColor(long value)
		{
			if (value >= 4000)
				return new Color(255, 90, 90);
			if (value >= 2200)
				return new Color(255, 160, 60);
			if (value >= 1000)
				return new Color(255, 215, 90);
			if (value >= 400)
				return new Color(160, 220, 120);
			if (value >= 120)
				return new Color(120, 200, 235);
			return new Color(190, 200, 215);
		}
	}
}
