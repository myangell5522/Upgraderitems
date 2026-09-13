using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Upgraderitems.Common.Configs;
using Upgraderitems.Common.Economy;

namespace Upgraderitems.Common.UI.Elements
{
	/// <summary>
	/// Full-window overlay for picking the item you are gambling for. Lists every loaded item of the
	/// matching category - vanilla and modded alike - with live search, sorting and per-item odds.
	/// </summary>
	public class UICatalog : UIElement
	{
		private const int CellWidth = 70;
		private const int CellHeight = 96;
		private const int CellGap = 6;
		private const int HeaderHeight = 82;
		private const int FooterHeight = 34;

		public UpgradeCategory Category = UpgradeCategory.None;
		public long StakeValue;
		public Action<int> OnPick;
		public Action OnClose;

		private readonly List<CatalogEntry> filtered = new();
		private string search = string.Empty;
		private bool searchFocused;
		private int sortMode;
		private int scrollRow;
		private UpgradeCategory builtCategory = (UpgradeCategory)(-1);
		private string builtSearch;
		private int builtSort = -1;

		private static readonly string[] SortKeys = { "ValueAsc", "ValueDesc", "Name" };

		public void Open(UpgradeCategory category, long stakeValue)
		{
			Category = category;
			StakeValue = stakeValue;
			scrollRow = 0;
			searchFocused = false;
			Rebuild();
		}

		public void Close()
		{
			searchFocused = false;
			Main.blockInput = false;
		}

		/// <summary>Releases the text field and hands control back to the spin window.</summary>
		public void Dismiss()
		{
			Close();
			OnClose?.Invoke();
		}

		private int Columns => Math.Max(1, (int)(GetDimensions().Width - 24) / (CellWidth + CellGap));

		private int VisibleRows => Math.Max(1, (int)(GetDimensions().Height - HeaderHeight - FooterHeight) / (CellHeight + CellGap));

		private int TotalRows => (filtered.Count + Columns - 1) / Columns;

		private void Rebuild()
		{
			filtered.Clear();

			IEnumerable<CatalogEntry> source = ItemCatalog.Get(Category);
			if (!string.IsNullOrWhiteSpace(search))
			{
				string needle = search.Trim();
				source = source.Where(e => e.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
			}

			source = sortMode switch
			{
				1 => source.OrderByDescending(e => e.Value),
				2 => source.OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase),
				_ => source.OrderBy(e => e.Value)
			};

			filtered.AddRange(source);

			builtCategory = Category;
			builtSearch = search;
			builtSort = sortMode;
			scrollRow = Math.Clamp(scrollRow, 0, Math.Max(0, TotalRows - VisibleRows));
		}

		public override void ScrollWheel(UIScrollWheelEvent evt)
		{
			base.ScrollWheel(evt);
			scrollRow = Math.Clamp(scrollRow - Math.Sign(evt.ScrollWheelValue) * 2, 0, Math.Max(0, TotalRows - VisibleRows));
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (builtCategory != Category || builtSearch != search || builtSort != sortMode)
				Rebuild();

			if (IsMouseHovering)
				Main.LocalPlayer.mouseInterface = true;

			if (searchFocused)
			{
				PlayerInput.WritingText = true;
				Main.instance.HandleIME();
				Main.blockInput = true;

				string typed = Main.GetInputText(search);
				if (typed != search)
				{
					search = typed;
					scrollRow = 0;
				}

				if (Main.inputTextEnter || Main.inputTextEscape)
					searchFocused = false;
			}
			else
			{
				Main.blockInput = false;
			}
		}

		private Rectangle SearchRect()
		{
			Rectangle bounds = GetDimensions().ToRectangle();
			return new Rectangle(bounds.X + 12, bounds.Y + 42, bounds.Width - 290, 30);
		}

		private Rectangle SortRect()
		{
			Rectangle bounds = GetDimensions().ToRectangle();
			return new Rectangle(bounds.Right - 266, bounds.Y + 42, 180, 30);
		}

		private Rectangle CloseRect()
		{
			Rectangle bounds = GetDimensions().ToRectangle();
			return new Rectangle(bounds.Right - 78, bounds.Y + 42, 66, 30);
		}

		private Rectangle CellRect(int index)
		{
			Rectangle bounds = GetDimensions().ToRectangle();
			int columns = Columns;
			int visualIndex = index - scrollRow * columns;
			if (visualIndex < 0)
				return Rectangle.Empty;

			int row = visualIndex / columns;
			int column = visualIndex % columns;

			int gridWidth = columns * CellWidth + (columns - 1) * CellGap;
			int originX = bounds.X + (bounds.Width - gridWidth) / 2;

			return new Rectangle(
				originX + column * (CellWidth + CellGap),
				bounds.Y + HeaderHeight + row * (CellHeight + CellGap),
				CellWidth,
				CellHeight);
		}

		public override void LeftClick(UIMouseEvent evt)
		{
			base.LeftClick(evt);
			Point mouse = evt.MousePosition.ToPoint();

			if (CloseRect().Contains(mouse))
			{
				Dismiss();
				return;
			}

			if (SearchRect().Contains(mouse))
			{
				searchFocused = true;
				return;
			}

			searchFocused = false;

			if (SortRect().Contains(mouse))
			{
				sortMode = (sortMode + 1) % SortKeys.Length;
				scrollRow = 0;
				if (UpgraderClientConfig.Instance?.PlaySounds ?? true)
					SoundEngine.PlaySound(SoundID.MenuTick);
				return;
			}

			int first = scrollRow * Columns;
			int last = Math.Min(filtered.Count, first + Columns * VisibleRows);
			for (int i = first; i < last; i++)
			{
				if (!CellRect(i).Contains(mouse))
					continue;

				if (UpgraderClientConfig.Instance?.PlaySounds ?? true)
					SoundEngine.PlaySound(SoundID.Grab);

				OnPick?.Invoke(filtered[i].Type);

				// Picking is the whole point of the overlay, so it has to hand control straight back
				// to the spin window instead of leaving the grid sitting on top of it.
				Dismiss();
				return;
			}
		}

		public override void RightClick(UIMouseEvent evt)
		{
			base.RightClick(evt);
			Dismiss();
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			Rectangle bounds = GetDimensions().ToRectangle();
			UpgraderStyle.DrawFrame(spriteBatch, bounds, new Color(28, 36, 74), UpgraderStyle.PanelBorder);

			DrawHeader(spriteBatch, bounds);
			DrawGrid(spriteBatch);
			DrawFooter(spriteBatch, bounds);
		}

		private void DrawHeader(SpriteBatch spriteBatch, Rectangle bounds)
		{
			string title = Language.GetTextValue("Mods.Upgraderitems.UI.CatalogTitle", Category.DisplayName());
			UpgraderStyle.DrawText(spriteBatch, title, new Vector2(bounds.X + 14, bounds.Y + 12), UpgraderStyle.Accent, 1.05f);

			// Search field.
			Rectangle searchRect = SearchRect();
			UpgraderStyle.FillRect(spriteBatch, searchRect, new Color(14, 18, 38));
			UpgraderStyle.OutlineRect(spriteBatch, searchRect, searchFocused ? UpgraderStyle.Accent : UpgraderStyle.InnerBorder, 2);

			string shown = search;
			if (string.IsNullOrEmpty(shown) && !searchFocused)
				shown = Language.GetTextValue("Mods.Upgraderitems.UI.SearchHint");
			else if (searchFocused && Main.GameUpdateCount % 40 < 20)
				shown += "|";

			Color searchColor = string.IsNullOrEmpty(search) && !searchFocused ? UpgraderStyle.TextDim : UpgraderStyle.TextBright;
			UpgraderStyle.DrawText(spriteBatch, UpgraderStyle.Truncate(shown, searchRect.Width - 16, 0.9f),
				new Vector2(searchRect.X + 8, searchRect.Y + 5), searchColor, 0.9f);

			// Sort toggle.
			Rectangle sortRect = SortRect();
			bool sortHovered = sortRect.Contains(Main.MouseScreen.ToPoint());
			UpgraderStyle.FillRect(spriteBatch, sortRect, sortHovered ? UpgraderStyle.PanelBorder : UpgraderStyle.InnerBack);
			UpgraderStyle.OutlineRect(spriteBatch, sortRect, UpgraderStyle.InnerBorder, 2);
			UpgraderStyle.DrawTextCentered(spriteBatch,
				UpgraderStyle.Truncate(Language.GetTextValue("Mods.Upgraderitems.UI.Sort." + SortKeys[sortMode]), sortRect.Width - 12, 0.85f),
				sortRect.Center.ToVector2(), UpgraderStyle.TextBright, 0.85f);

			// Back to the spin window. Styled as a neutral step back rather than a red dismissal, since
			// nothing is lost by leaving the grid.
			Rectangle closeRect = CloseRect();
			bool closeHovered = closeRect.Contains(Main.MouseScreen.ToPoint());
			UpgraderStyle.FillRect(spriteBatch, closeRect, closeHovered ? UpgraderStyle.PanelBorder : UpgraderStyle.InnerBack);
			UpgraderStyle.OutlineRect(spriteBatch, closeRect, closeHovered ? UpgraderStyle.Accent : UpgraderStyle.InnerBorder, 2);
			UpgraderStyle.DrawTextCentered(spriteBatch, Language.GetTextValue("Mods.Upgraderitems.UI.Back"),
				closeRect.Center.ToVector2(), UpgraderStyle.TextBright, 0.85f);
		}

		private void DrawGrid(SpriteBatch spriteBatch)
		{
			Point mouse = Main.MouseScreen.ToPoint();
			int first = scrollRow * Columns;
			int last = Math.Min(filtered.Count, first + Columns * VisibleRows);

			for (int i = first; i < last; i++)
			{
				CatalogEntry entry = filtered[i];
				Rectangle cell = CellRect(i);
				bool hovered = cell.Contains(mouse) && IsMouseHovering;

				UpgraderStyle.FillRect(spriteBatch, cell, hovered ? new Color(58, 74, 136) : new Color(20, 26, 54));
				UpgraderStyle.OutlineRect(spriteBatch, cell, hovered ? UpgraderStyle.Accent : new Color(52, 66, 118), 2);

				Rectangle iconRect = new(cell.X + 11, cell.Y + 6, 48, 44);
				if (ContentSamples.ItemsByType.TryGetValue(entry.Type, out Item sample))
				{
					SlotRenderer.DrawItemOnly(spriteBatch, sample, iconRect, UpgraderStyle.Opacity);
					if (hovered)
						SlotRenderer.ShowTooltipFor(sample);
				}

				UpgraderStyle.DrawTextCentered(spriteBatch, ItemValue.Format(entry.Value),
					new Vector2(cell.Center.X, cell.Y + 60), ItemValue.TierColor(entry.Value), 0.8f);

				float chance = UpgradeEngine.ComputeChance(StakeValue, entry.Value);

				UpgraderStyle.DrawTextCentered(spriteBatch,
					(chance * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%",
					new Vector2(cell.Center.X, cell.Y + 76), UpgradeEngine.ChanceColor(chance), 0.72f);

				DrawChanceBar(spriteBatch, new Rectangle(cell.X + 6, cell.Bottom - 10, cell.Width - 12, 5), chance);
			}

			if (filtered.Count == 0)
			{
				Rectangle bounds = GetDimensions().ToRectangle();
				UpgraderStyle.DrawTextCentered(spriteBatch, Language.GetTextValue("Mods.Upgraderitems.UI.NoResults"),
					new Vector2(bounds.Center.X, bounds.Center.Y), UpgraderStyle.TextDim, 1f);
			}
		}

		private static void DrawChanceBar(SpriteBatch spriteBatch, Rectangle rect, float chance)
		{
			UpgraderStyle.FillRect(spriteBatch, rect, new Color(12, 16, 32));
			int width = (int)(rect.Width * MathHelper.Clamp(chance, 0f, 1f));
			if (width > 0)
				UpgraderStyle.FillRect(spriteBatch, new Rectangle(rect.X, rect.Y, width, rect.Height), UpgradeEngine.ChanceColor(chance));
		}

		private void DrawFooter(SpriteBatch spriteBatch, Rectangle bounds)
		{
			int totalRows = TotalRows;
			string info = Language.GetTextValue("Mods.Upgraderitems.UI.CatalogFooter",
				filtered.Count, Math.Min(scrollRow + 1, Math.Max(totalRows, 1)), Math.Max(totalRows, 1));

			UpgraderStyle.DrawText(spriteBatch, info,
				new Vector2(bounds.X + 14, bounds.Bottom - 26), UpgraderStyle.TextDim, 0.8f);

			UpgraderStyle.DrawTextRight(spriteBatch, Language.GetTextValue("Mods.Upgraderitems.UI.CatalogHint"),
				new Vector2(bounds.Right - 24, bounds.Bottom - 17), UpgraderStyle.TextDim, 0.8f);

			if (totalRows <= VisibleRows)
				return;

			// Scrollbar.
			Rectangle track = new(bounds.Right - 18, bounds.Y + HeaderHeight, 6, bounds.Height - HeaderHeight - FooterHeight);
			UpgraderStyle.FillRect(spriteBatch, track, new Color(14, 18, 38));

			float ratio = VisibleRows / (float)totalRows;
			int handleHeight = Math.Max(24, (int)(track.Height * ratio));
			int handleY = track.Y + (int)((track.Height - handleHeight) * (scrollRow / (float)Math.Max(1, totalRows - VisibleRows)));
			UpgraderStyle.FillRect(spriteBatch, new Rectangle(track.X, handleY, track.Width, handleHeight), UpgraderStyle.PanelBorder);
		}
	}
}
