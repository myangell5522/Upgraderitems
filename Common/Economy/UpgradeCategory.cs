using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace Upgraderitems.Common.Economy
{
	public enum UpgradeCategory
	{
		None,
		Weapon,
		Armor,
		Accessory
	}

	public static class UpgradeCategoryUtils
	{
		/// <summary>
		/// Works off raw <see cref="Item"/> fields only, so modded content is classified without any
		/// cross-mod calls.
		/// </summary>
		public static UpgradeCategory Classify(Item item)
		{
			if (item == null || item.IsAir || item.type <= ItemID.None)
				return UpgradeCategory.None;

			// Placeable furniture (monoliths, statues, torches...) is never gear, even when a mod marks it
			// as an accessory so it can be worn.
			if (item.createTile > TileID.Dirt || item.createWall >= 0)
				return UpgradeCategory.None;

			if (item.accessory)
				return UpgradeCategory.Accessory;

			if (item.defense > 0 && (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0))
				return UpgradeCategory.Armor;

			// maxStack == 1 keeps thrown consumables, ammo and potions out of the weapon pool.
			if (item.damage > 0 && item.useStyle != ItemUseStyleID.None && item.ammo == AmmoID.None
				&& item.maxStack == 1 && !item.consumable)
				return UpgradeCategory.Weapon;

			return UpgradeCategory.None;
		}

		public static string DisplayName(this UpgradeCategory category)
			=> Language.GetTextValue($"Mods.Upgraderitems.Category.{category}");
	}
}
