using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using Upgraderitems.Common.Configs;
using Upgraderitems.Common.Economy;

namespace Upgraderitems.Common.UI.Elements
{
	/// <summary>One of the two big slots: the item you stake on the left, the item you want on the right.</summary>
	public class UIUpgradeSlot : UIElement
	{
		public const int SlotSize = 68;

		public Func<Item> Provider = () => new Item();
		public Action Activated;
		public string EmptyTitle = string.Empty;
		public string EmptyHint = string.Empty;
		public bool Highlight;

		private readonly Item[] buffer = { new() };
		private float hoverProgress;
		private float popIn;
		private int lastShownType = ItemID.None;

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			hoverProgress = MathHelper.Lerp(hoverProgress, IsMouseHovering ? 1f : 0f, 0.2f);

			Item current = Provider() ?? new Item();
			if (current.type != lastShownType)
			{
				lastShownType = current.type;
				popIn = current.IsAir ? 1f : 0f;
			}

			popIn = MathHelper.Lerp(popIn, 1f, 0.2f);
		}

		public override void LeftClick(UIMouseEvent evt)
		{
			base.LeftClick(evt);
			if (UpgraderClientConfig.Instance?.PlaySounds ?? true)
				SoundEngine.PlaySound(SoundID.Grab);

			Activated?.Invoke();
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			Rectangle rect = GetDimensions().ToRectangle();

			Color border = Highlight
				? Color.Lerp(UpgraderStyle.AccentDark, UpgraderStyle.Accent, 0.4f + hoverProgress * 0.6f)
				: Color.Lerp(UpgraderStyle.InnerBorder, Color.White, hoverProgress * 0.3f);

			UpgraderStyle.DrawFrame(spriteBatch, rect, UpgraderStyle.InnerBack, border);
			DrawHexPattern(spriteBatch, rect);

			buffer[0] = Provider() ?? new Item();
			Item item = buffer[0];

			int slotSize = (int)(SlotSize * MathHelper.Lerp(0.7f, 1f, UpgraderStyle.EaseOutBack(popIn)));
			Rectangle slotRect = new(
				rect.X + (rect.Width - slotSize) / 2,
				rect.Y + 18,
				slotSize,
				slotSize);

			// InventoryItem keeps the slot the same blue as the surrounding Terraria panel; the chest
			// context renders brown and clashes.
			SlotRenderer.Draw(spriteBatch, ref buffer[0], ItemSlot.Context.InventoryItem, slotRect);

			if (item.IsAir)
			{
				DrawWrappedCentered(spriteBatch, EmptyTitle, new Vector2(rect.Center.X, rect.Y + SlotSize + 34),
					rect.Width - 16, UpgraderStyle.TextBright, 0.85f);
				DrawWrappedCentered(spriteBatch, EmptyHint, new Vector2(rect.Center.X, rect.Y + SlotSize + 78),
					rect.Width - 16, UpgraderStyle.TextDim, 0.75f);
			}
			else
			{
				long value = ItemValue.Get(item);
				string name = UpgraderStyle.Truncate(item.Name, rect.Width - 16, 0.8f);
				UpgraderStyle.DrawTextCentered(spriteBatch, name,
					new Vector2(rect.Center.X, rect.Y + SlotSize + 34), UpgraderStyle.TextBright, 0.8f);

				DrawValueBadge(spriteBatch, new Vector2(rect.Center.X, rect.Y + SlotSize + 62), value);
			}

			if (IsMouseHovering)
			{
				SlotRenderer.ShowTooltipFor(item);
				Main.LocalPlayer.mouseInterface = true;
			}
		}

		private static void DrawValueBadge(SpriteBatch spriteBatch, Vector2 center, long value)
		{
			const float textScale = 0.95f;
			const int paddingX = 7;
			const int gap = 4;

			const float iconWidth = UpgraderAssets.CoinDrawWidth;
			const float iconHeight = UpgraderAssets.CoinDrawHeight;
			const int badgeHeight = (int)iconHeight + 8;

			string text = ItemValue.Format(value);
			Vector2 textSize = UpgraderStyle.Measure(text, textScale);

			int badgeWidth = (int)(textSize.X + iconWidth) + gap + paddingX * 2;
			Rectangle badge = new(
				(int)(center.X - badgeWidth * 0.5f),
				(int)(center.Y - badgeHeight * 0.5f),
				badgeWidth,
				badgeHeight);

			UpgraderStyle.FillRect(spriteBatch, badge, new Color(18, 22, 44) * 0.85f);
			UpgraderStyle.OutlineRect(spriteBatch, badge, ItemValue.TierColor(value) * 0.6f, 1);

			int contentX = badge.X + paddingX;

			Texture2D coin = UpgraderAssets.Coin;
			if (coin != null)
			{
				spriteBatch.Draw(coin,
					new Vector2(contentX, badge.Center.Y - iconHeight * 0.5f),
					UpgraderAssets.CoinFrame(Main.GameUpdateCount, 9),
					Color.White * UpgraderStyle.Opacity, 0f, Vector2.Zero,
					UpgraderAssets.CoinDrawScale, SpriteEffects.None, 0f);
			}

			UpgraderStyle.DrawTextMiddle(spriteBatch, text,
				new Vector2(contentX + iconWidth + gap, badge.Center.Y),
				ItemValue.TierColor(value), textScale);
		}

		/// <summary>Faint honeycomb behind the slot, matching the reference mock-ups.</summary>
		private static void DrawHexPattern(SpriteBatch spriteBatch, Rectangle rect)
		{
			const int radius = 15;
			float horizontalStep = radius * 1.5f;
			float verticalStep = radius * 1.732f;

			Color color = new Color(120, 145, 210) * 0.09f;

			for (int column = 0; column * horizontalStep < rect.Width + radius; column++)
			{
				float x = rect.X + 6 + column * horizontalStep;
				float yOffset = column % 2 == 0 ? 0f : verticalStep * 0.5f;

				for (int row = 0; row * verticalStep + yOffset < rect.Height; row++)
				{
					Vector2 center = new(x, rect.Y + 6 + row * verticalStep + yOffset);
					DrawHexagon(spriteBatch, center, radius - 2, color, rect);
				}
			}
		}

		private static void DrawHexagon(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, Rectangle bounds)
		{
			Vector2 previous = Vector2.Zero;
			for (int i = 0; i <= 6; i++)
			{
				float angle = MathHelper.TwoPi * i / 6f;
				Vector2 point = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
				if (i > 0 && bounds.Contains(point.ToPoint()) && bounds.Contains(previous.ToPoint()))
					UpgraderStyle.DrawLine(spriteBatch, previous, point, color, 1.5f);

				previous = point;
			}
		}

		private static void DrawWrappedCentered(SpriteBatch spriteBatch, string text, Vector2 center,
			float maxWidth, Color color, float scale)
		{
			if (string.IsNullOrEmpty(text))
				return;

			string[] words = text.Split(' ');
			string line = string.Empty;
			int drawn = 0;

			foreach (string word in words)
			{
				string candidate = line.Length == 0 ? word : line + " " + word;
				if (UpgraderStyle.Measure(candidate, scale).X > maxWidth && line.Length > 0)
				{
					UpgraderStyle.DrawTextCentered(spriteBatch, line, center + new Vector2(0f, drawn * 20f), color, scale);
					drawn++;
					line = word;
				}
				else
				{
					line = candidate;
				}
			}

			if (line.Length > 0)
				UpgraderStyle.DrawTextCentered(spriteBatch, line, center + new Vector2(0f, drawn * 20f), color, scale);
		}
	}
}
