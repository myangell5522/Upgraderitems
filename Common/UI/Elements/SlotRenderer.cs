using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace Upgraderitems.Common.UI.Elements
{
	/// <summary>
	/// Thin wrapper over <see cref="ItemSlot"/> so every slot in the mod is drawn at an exact pixel size
	/// regardless of the player's inventory scale setting.
	/// </summary>
	public static class SlotRenderer
	{
		/// <summary>Vanilla slot sprites are 52x52 before scaling.</summary>
		public const float BaseSlotSize = 52f;

		public static void Draw(SpriteBatch sb, ref Item item, int context, Rectangle destination)
		{
			float previousScale = Main.inventoryScale;
			Main.inventoryScale = destination.Width / BaseSlotSize;

			ItemSlot.Draw(sb, ref item, context, new Vector2(destination.X, destination.Y));

			Main.inventoryScale = previousScale;
		}

		/// <summary>Draws just the item sprite, fitted into the rectangle. Used for catalog previews.</summary>
		public static void DrawItemOnly(SpriteBatch sb, Item item, Rectangle destination, float opacity = 1f)
		{
			if (item == null || item.IsAir)
				return;

			Main.instance.LoadItem(item.type);
			Texture2D texture = Terraria.GameContent.TextureAssets.Item[item.type].Value;
			Rectangle frame = Main.itemAnimations[item.type] != null
				? Main.itemAnimations[item.type].GetFrame(texture)
				: texture.Frame();

			float scale = 1f;
			float longest = System.Math.Max(frame.Width, frame.Height);
			if (longest > destination.Width)
				scale = destination.Width / longest;

			Vector2 center = destination.Center.ToVector2();
			sb.Draw(texture, center, frame, Color.White * opacity, 0f,
				frame.Size() * 0.5f, scale, SpriteEffects.None, 0f);
		}

		public static void ShowTooltipFor(Item item)
		{
			if (item == null || item.IsAir)
				return;

			Main.HoverItem = item.Clone();
			Main.hoverItemName = Main.HoverItem.Name;
		}
	}
}
