using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Upgraderitems.Common.Configs;
using Upgraderitems.Common.Players;

namespace Upgraderitems.Common.UI
{
	/// <summary>
	/// The cash button that lives next to the inventory. Right-drag moves it and the position is saved
	/// with the character.
	/// </summary>
	public class UpgraderToggleButton : UIElement
	{
		public const int Size = 44;

		/// <summary>Margin and slot pitch vanilla lays the inventory grid out with.</summary>
		private const float GridMargin = 20f;

		private const float GridPitch = 56f * 0.85f;

		/// <summary>
		/// Vanilla parks the trash slot one row below the grid, under its last column. Sitting one cell
		/// to its left keeps the button on the grid's own rhythm instead of floating beside the panel.
		/// </summary>
		public static readonly Vector2 DefaultPosition = new(
			GridMargin + GridPitch * 8f,
			GridMargin + GridPitch * 5f);

		private float hoverProgress;
		private float pressProgress;
		private float appear;
		private bool dragging;
		private Vector2 dragOffset;
		private bool movedWhileDragging;

		public UpgraderToggleButton()
		{
			Width.Set(Size, 0f);
			Height.Set(Size, 0f);
		}

		private static UpgraderPlayer ModPlayer => Main.LocalPlayer.GetModPlayer<UpgraderPlayer>();

		public override void RightMouseDown(UIMouseEvent evt)
		{
			base.RightMouseDown(evt);
			dragging = true;
			movedWhileDragging = false;
			dragOffset = evt.MousePosition - GetDimensions().Position();
		}

		public override void RightMouseUp(UIMouseEvent evt)
		{
			base.RightMouseUp(evt);
			dragging = false;
		}

		public override void LeftClick(UIMouseEvent evt)
		{
			base.LeftClick(evt);
			pressProgress = 1f;

			if (UpgraderClientConfig.Instance?.PlaySounds ?? true)
				SoundEngine.PlaySound(SoundID.MenuOpen);

			ModContent.GetInstance<UpgraderUISystem>().Toggle();
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			appear = MathHelper.Lerp(appear, 1f, 0.16f);
			hoverProgress = MathHelper.Lerp(hoverProgress, IsMouseHovering ? 1f : 0f, 0.18f);
			pressProgress = MathHelper.Lerp(pressProgress, 0f, 0.12f);

			if (IsMouseHovering)
			{
				Main.LocalPlayer.mouseInterface = true;
				Main.hoverItemName = Language.GetTextValue("Mods.Upgraderitems.UI.ButtonTooltip");
			}

			UpdateDrag();
		}

		private void UpdateDrag()
		{
			if (!dragging)
				return;

			if (!Main.mouseRight)
			{
				dragging = false;
				return;
			}

			Vector2 wanted = Main.MouseScreen - dragOffset;
			wanted.X = MathHelper.Clamp(wanted.X, 0f, Main.screenWidth - Size);
			wanted.Y = MathHelper.Clamp(wanted.Y, 0f, Main.screenHeight - Size);

			Vector2 offset = wanted - DefaultPosition;
			if (Vector2.DistanceSquared(offset, ModPlayer.ButtonOffset) > 0.01f)
				movedWhileDragging = true;

			ModPlayer.ButtonOffset = offset;
			ApplyPosition();
		}

		public void ApplyPosition()
		{
			Vector2 position = DefaultPosition + ModPlayer.ButtonOffset;
			position.X = MathHelper.Clamp(position.X, 0f, Math.Max(0f, Main.screenWidth - Size));
			position.Y = MathHelper.Clamp(position.Y, 0f, Math.Max(0f, Main.screenHeight - Size));

			Left.Set(position.X, 0f);
			Top.Set(position.Y, 0f);
			Recalculate();
		}

		public void ResetAppear() => appear = 0f;

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			Rectangle rect = GetDimensions().ToRectangle();

			float ease = UpgraderStyle.EaseOutBack(appear);
			float scale = MathHelper.Lerp(0.5f, 1f, ease) * (1f + hoverProgress * 0.10f - pressProgress * 0.12f);
			float alpha = MathHelper.Clamp(appear * 1.4f, 0f, 1f);

			Vector2 center = rect.Center.ToVector2();
			int drawn = (int)(Size * scale);
			Rectangle destination = new((int)(center.X - drawn / 2f), (int)(center.Y - drawn / 2f), drawn, drawn);

			float previousOpacity = UpgraderStyle.Opacity;
			UpgraderStyle.Opacity = previousOpacity * alpha;

			bool open = ModContent.GetInstance<UpgraderUISystem>().Visible;

			Color back = Color.Lerp(UpgraderStyle.PanelBack, UpgraderStyle.PanelBorder, hoverProgress * 0.6f);
			Color border = open
				? UpgraderStyle.Accent
				: Color.Lerp(UpgraderStyle.PanelBorder, UpgraderStyle.Accent, hoverProgress);

			UpgraderStyle.DrawFrame(spriteBatch, destination, back, border);

			// Slow breathing halo so the button reads as interactive without being noisy.
			float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.2f);
			UpgraderStyle.OutlineRect(spriteBatch, destination,
				UpgraderStyle.Accent * (0.12f + pulse * 0.12f + hoverProgress * 0.35f), 2);

			Texture2D icon = UpgraderAssets.Cash;
			if (icon != null)
			{
				float bob = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 1.5f * (0.3f + hoverProgress);
				float iconScale = drawn / (float)Size * 1.75f;

				spriteBatch.Draw(icon, center + new Vector2(0f, bob), null,
					Color.White * UpgraderStyle.Opacity, 0f,
					new Vector2(icon.Width, icon.Height) * 0.5f, iconScale, SpriteEffects.None, 0f);
			}

			UpgraderStyle.Opacity = previousOpacity;

			if (dragging && movedWhileDragging)
				UpgraderStyle.DrawTextCentered(spriteBatch,
					Language.GetTextValue("Mods.Upgraderitems.UI.ButtonDragging"),
					new Vector2(center.X, rect.Bottom + 14), UpgraderStyle.TextDim, 0.75f);
		}
	}

	public class UpgraderButtonState : UIState
	{
		private UpgraderToggleButton button;

		public override void OnInitialize()
		{
			button = new UpgraderToggleButton();
			Append(button);
		}

		public void OnInventoryOpened() => button?.ResetAppear();

		public override void Update(GameTime gameTime)
		{
			button?.ApplyPosition();
			base.Update(gameTime);
		}
	}
}
