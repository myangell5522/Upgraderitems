using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using Upgraderitems.Common.Configs;

namespace Upgraderitems.Common.UI.Elements
{
	public enum ButtonStyle
	{
		Primary,
		Secondary,
		Danger
	}

	/// <summary>Flat Terraria-styled text button with a hover glow that eases in and out.</summary>
	public class UIAccentButton : UIElement
	{
		public string Text = string.Empty;
		public string Tooltip = string.Empty;
		public ButtonStyle Style = ButtonStyle.Secondary;
		public float TextScale = 1f;
		public Color? TextColorOverride;

		/// <summary>Sweeps a highlight across the label. Reserved for the one button that matters.</summary>
		public bool Gleam;

		public Func<bool> IsEnabled = () => true;
		public Func<bool> IsSelected = () => false;

		private float hoverProgress;

		public UIAccentButton(string text, ButtonStyle style = ButtonStyle.Secondary)
		{
			Text = text;
			Style = style;
		}

		public override void MouseOver(UIMouseEvent evt)
		{
			base.MouseOver(evt);
			if (IsEnabled() && (UpgraderClientConfig.Instance?.PlaySounds ?? true))
				SoundEngine.PlaySound(SoundID.MenuTick);
		}

		public override void LeftClick(UIMouseEvent evt)
		{
			if (!IsEnabled())
				return;

			if (UpgraderClientConfig.Instance?.PlaySounds ?? true)
				SoundEngine.PlaySound(Style == ButtonStyle.Primary ? SoundID.Coins : SoundID.MenuTick);

			base.LeftClick(evt);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			bool hovered = IsMouseHovering && IsEnabled();
			hoverProgress = MathHelper.Lerp(hoverProgress, hovered ? 1f : 0f, 0.22f);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			Rectangle rect = GetDimensions().ToRectangle();
			bool enabled = IsEnabled();
			bool selected = IsSelected();

			Color back;
			Color border;
			Color text;

			// Where the sweeping highlight lands. Dark letters on the gold bar only need to warm up a
			// little; going all the way to white would wash them into the background.
			Color gleam;

			switch (Style)
			{
				case ButtonStyle.Primary:
					back = Color.Lerp(new Color(150, 110, 25), UpgraderStyle.Accent, 0.35f + hoverProgress * 0.4f);
					border = Color.Lerp(UpgraderStyle.AccentDark, Color.White, hoverProgress * 0.5f);
					text = new Color(30, 24, 8);
					gleam = new Color(126, 88, 26);
					break;
				case ButtonStyle.Danger:
					back = Color.Lerp(new Color(120, 40, 45), new Color(190, 65, 70), hoverProgress);
					border = new Color(230, 110, 110);
					text = UpgraderStyle.TextBright;
					gleam = Color.White;
					break;
				default:
					back = Color.Lerp(UpgraderStyle.InnerBack, UpgraderStyle.PanelBorder, hoverProgress * 0.55f);
					border = Color.Lerp(UpgraderStyle.InnerBorder, Color.White, hoverProgress * 0.35f);
					text = UpgraderStyle.TextBright;
					gleam = Color.White;
					break;
			}

			if (selected)
			{
				back = Color.Lerp(back, UpgraderStyle.Accent, 0.35f);
				border = UpgraderStyle.Accent;
			}

			if (!enabled)
			{
				back = Color.Lerp(back, new Color(30, 34, 52), 0.7f);
				border = new Color(60, 68, 96);
				text = new Color(120, 126, 145);
			}

			UpgraderStyle.FillRect(spriteBatch, rect, back);
			UpgraderStyle.OutlineRect(spriteBatch, rect, border, 2);

			// A soft inner highlight strip sells the raised, clickable look.
			if (enabled)
			{
				Rectangle highlight = new(rect.X + 2, rect.Y + 2, rect.Width - 4, Math.Max(2, rect.Height / 6));
				UpgraderStyle.FillRect(spriteBatch, highlight, Color.White * (0.06f + hoverProgress * 0.08f));
			}

			Color textColor = TextColorOverride ?? text;
			Vector2 textCenter = rect.Center.ToVector2();

			if (Gleam && enabled)
			{
				UpgraderStyle.DrawGleamTextCentered(spriteBatch, Text, textCenter, textColor, gleam, TextScale);
			}
			else
			{
				UpgraderStyle.DrawTextCentered(spriteBatch, Text, textCenter, textColor, TextScale);
			}

			if (IsMouseHovering && !string.IsNullOrEmpty(Tooltip))
				Main.hoverItemName = Tooltip;
		}
	}
}
