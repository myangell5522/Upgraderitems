using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.UI;
using Upgraderitems.Common.Economy;

namespace Upgraderitems.Common.UI.Elements
{
	/// <summary>
	/// The player's 50 inventory slots, laid out with the hotbar detached at the bottom like the mock-ups.
	/// Clicking an eligible item stakes it; items that cannot be upgraded are dimmed instead of hidden so
	/// the grid still reads as your inventory.
	/// </summary>
	public class UIInventoryGrid : UIElement
	{
		public const int Columns = 10;
		public const int MainRows = 4;
		public const int CellSize = 42;
		public const int Gap = 3;
		public const int HotbarGap = 14;

		public Action<int> OnSlotClicked;
		public Func<Item, bool> IsEligible = _ => true;

		public UIInventoryGrid()
		{
			Width.Set(Columns * CellSize + (Columns - 1) * Gap, 0f);
			Height.Set(MainRows * (CellSize + Gap) - Gap + HotbarGap + CellSize, 0f);
		}

		/// <summary>Row 0-3 map to inventory slots 10-49, the detached bottom row is the hotbar.</summary>
		private static int SlotIndexFor(int row, int column)
			=> row < MainRows ? 10 + row * Columns + column : column;

		private Rectangle CellRect(int row, int column)
		{
			Rectangle bounds = GetDimensions().ToRectangle();
			int y = bounds.Y + row * (CellSize + Gap);
			if (row >= MainRows)
				y += HotbarGap - Gap;

			return new Rectangle(bounds.X + column * (CellSize + Gap), y, CellSize, CellSize);
		}

		public override void LeftClick(UIMouseEvent evt)
		{
			base.LeftClick(evt);

			Point mouse = evt.MousePosition.ToPoint();
			Player player = Main.LocalPlayer;

			for (int row = 0; row <= MainRows; row++)
			{
				for (int column = 0; column < Columns; column++)
				{
					if (!CellRect(row, column).Contains(mouse))
						continue;

					int index = SlotIndexFor(row, column);

					// Holding an item on the cursor keeps the familiar pick-up / put-down behaviour.
					if (!Main.mouseItem.IsAir)
					{
						Utils.Swap(ref Main.mouseItem, ref player.inventory[index]);
						InventorySync.SyncSlot(player, index);
						return;
					}

					OnSlotClicked?.Invoke(index);
					return;
				}
			}
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			Player player = Main.LocalPlayer;
			Point mouse = Main.MouseScreen.ToPoint();

			for (int row = 0; row <= MainRows; row++)
			{
				for (int column = 0; column < Columns; column++)
				{
					int index = SlotIndexFor(row, column);
					Rectangle rect = CellRect(row, column);
					Item item = player.inventory[index];

					SlotRenderer.Draw(spriteBatch, ref player.inventory[index], ItemSlot.Context.InventoryItem, rect);

					bool eligible = !item.IsAir && IsEligible(item);
					if (!item.IsAir && !eligible)
						UpgraderStyle.FillRect(spriteBatch, rect, new Color(10, 12, 24) * 0.55f);

					if (eligible)
					{
						long value = ItemValue.Get(item);
						UpgraderStyle.OutlineRect(spriteBatch, rect, ItemValue.TierColor(value) * 0.55f, 2);
					}

					if (rect.Contains(mouse) && IsMouseHovering)
					{
						UpgraderStyle.FillRect(spriteBatch, rect, Color.White * 0.12f);
						SlotRenderer.ShowTooltipFor(item);
					}
				}
			}

			if (IsMouseHovering)
				Main.LocalPlayer.mouseInterface = true;
		}
	}
}
