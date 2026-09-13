using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Upgraderitems.Common.Economy
{
	public readonly struct CatalogEntry
	{
		public readonly int Type;
		public readonly long Value;
		public readonly string Name;

		public CatalogEntry(int type, long value, string name)
		{
			Type = type;
			Value = value;
			Name = name;
		}
	}

	/// <summary>
	/// The pool of items the upgrader is allowed to hand out, split by category and sorted by value.
	/// Built once after all mods have loaded, so modded items are included automatically.
	/// </summary>
	public class ItemCatalog : ModSystem
	{
		private static readonly Dictionary<UpgradeCategory, List<CatalogEntry>> Pools = new();

		public override void PostSetupContent()
		{
			ItemValue.ClearCache();
			Build();
		}

		public override void Unload() => Pools.Clear();

		public static IReadOnlyList<CatalogEntry> Get(UpgradeCategory category)
		{
			if (Pools.Count == 0)
				Build();

			return Pools.TryGetValue(category, out List<CatalogEntry> list) ? list : Array.Empty<CatalogEntry>();
		}

		private static void Build()
		{
			Pools.Clear();
			foreach (UpgradeCategory category in Enum.GetValues<UpgradeCategory>())
				Pools[category] = new List<CatalogEntry>();

			foreach (KeyValuePair<int, Item> pair in ContentSamples.ItemsByType)
			{
				Item sample = pair.Value;
				if (!IsObtainable(sample))
					continue;

				UpgradeCategory category = UpgradeCategoryUtils.Classify(sample);
				if (category == UpgradeCategory.None)
					continue;

				Pools[category].Add(new CatalogEntry(sample.type, ItemValue.Get(sample.type), sample.Name));
			}

			foreach (List<CatalogEntry> pool in Pools.Values)
				pool.Sort((a, b) => a.Value != b.Value ? a.Value.CompareTo(b.Value) : string.CompareOrdinal(a.Name, b.Name));
		}

		private static bool IsObtainable(Item item)
		{
			if (item == null || item.type <= ItemID.None || item.IsAir)
				return false;
			if (string.IsNullOrWhiteSpace(item.Name))
				return false;
			if (item.type < ItemID.Count && ItemID.Sets.Deprecated[item.type])
				return false;
			if (item.rare == ItemRarityID.Quest)
				return false;

			return true;
		}

		/// <summary>Closest entry to the requested value; used by the x2 / x4 / x8 shortcut buttons.</summary>
		public static int FindClosestToValue(UpgradeCategory category, long targetValue, int excludeType = -1)
		{
			IReadOnlyList<CatalogEntry> pool = Get(category);
			if (pool.Count == 0)
				return ItemID.None;

			int best = ItemID.None;
			long bestDistance = long.MaxValue;
			foreach (CatalogEntry entry in pool)
			{
				if (entry.Type == excludeType)
					continue;

				long distance = Math.Abs(entry.Value - targetValue);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = entry.Type;
				}
			}

			return best;
		}

		public static int PickRandom(UpgradeCategory category, long minValue, long maxValue)
		{
			List<CatalogEntry> matches = Get(category)
				.Where(e => e.Value >= minValue && e.Value <= maxValue)
				.ToList();

			if (matches.Count == 0)
				return ItemID.None;

			return matches[Main.rand.Next(matches.Count)].Type;
		}
	}
}
