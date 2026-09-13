using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Upgraderitems.Common.Configs;
using Upgraderitems.Common.Economy;
using Upgraderitems.Common.UI;

namespace Upgraderitems.Common.Global
{
	/// <summary>
	/// Appends the market value to the bottom of every tooltip and draws the animated money sprite
	/// right after the text.
	/// </summary>
	public class ValueTooltipGlobalItem : GlobalItem
	{
		public const string ValueLineName = "UpgraderValue";
		public const string StackLineName = "UpgraderStackValue";

		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
		{
			UpgraderClientConfig config = UpgraderClientConfig.Instance;
			if (config == null || !config.ShowValueInTooltip)
				return;

			long value = ItemValue.Get(item);
			if (value <= 0)
				return;

			string text = Language.GetTextValue("Mods.Upgraderitems.Tooltip.Value", ItemValue.Format(value));

			tooltips.Add(new TooltipLine(Mod, ValueLineName, text + IconPadding(text))
			{
				OverrideColor = ItemValue.TierColor(value)
			});

			if (config.ShowStackValueInTooltip && item.stack > 1)
			{
				string stackText = Language.GetTextValue("Mods.Upgraderitems.Tooltip.StackValue", ItemValue.Format(ItemValue.GetStack(item)));
				tooltips.Add(new TooltipLine(Mod, StackLineName, stackText)
				{
					OverrideColor = new Color(150, 160, 175)
				});
			}
		}

		/// <summary>
		/// The tooltip panel is sized from the text alone, so the sprite needs to reserve its own width
		/// with trailing spaces or it would hang outside the frame.
		/// </summary>
		private static string IconPadding(string text)
		{
			DynamicSpriteFont font = FontAssets.MouseText.Value;
			float spaceWidth = font.MeasureString(" ").X;
			if (spaceWidth <= 0f)
				return "        ";

			float iconWidth = UpgraderAssets.CoinFrameWidth * IconScale() + 8f;
			return new string(' ', (int)Math.Ceiling(iconWidth / spaceWidth));
		}

		private static float IconScale()
		{
			UpgraderClientConfig config = UpgraderClientConfig.Instance;
			return UpgraderAssets.CoinDrawScale * (config == null ? 1f : config.TooltipIconSize);
		}

		public override void PostDrawTooltipLine(Item item, DrawableTooltipLine line)
		{
			if (line.Mod != Mod.Name || line.Name != ValueLineName)
				return;

			UpgraderClientConfig config = UpgraderClientConfig.Instance;
			if (config == null)
				return;

			Texture2D texture = UpgraderAssets.Coin;
			if (texture == null)
				return;

			Rectangle source = config.AnimateTooltipIcon
				? UpgraderAssets.CoinFrame(Main.GameUpdateCount, config.TooltipIconFrameTicks)
				: UpgraderAssets.CoinFrame(0, 1);

			float scale = IconScale() * line.BaseScale.X;
			string trimmed = line.Text.TrimEnd();
			Vector2 textSize = line.Font.MeasureString(trimmed) * line.BaseScale;

			Vector2 position = new(
				line.X + textSize.X + 6f,
				line.Y + UpgraderStyle.InkCenterY(textSize.Y) - UpgraderAssets.CoinFrameHeight * scale * 0.5f);

			Main.spriteBatch.Draw(texture, position, source, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		}
	}
}
