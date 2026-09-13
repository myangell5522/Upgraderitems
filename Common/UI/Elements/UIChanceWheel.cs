using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Localization;
using Terraria.UI;
using Upgraderitems.Common.Economy;

namespace Upgraderitems.Common.UI.Elements
{
	/// <summary>
	/// The centrepiece dial. The green sweep starting at 12 o'clock is the win zone; the needle spins and
	/// lands inside or outside it, so the animation is an honest readout of the roll rather than theatre.
	/// </summary>
	public class UIChanceWheel : UIElement
	{
		private const int TickCount = 60;

		public float Chance;
		public float NeedleAngle;
		public bool Spinning;
		public int ResultFlash;
		public bool LastResultWon;

		private float displayedChance;
		private float glow;

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			// Ease the number so switching targets feels like a dial settling, not a hard cut.
			displayedChance = MathHelper.Lerp(displayedChance, Chance, Spinning ? 0.5f : 0.18f);
			if (Math.Abs(displayedChance - Chance) < 0.0002f)
				displayedChance = Chance;

			float target = Spinning ? 1f : ResultFlash > 0 ? 1f : 0f;
			glow = MathHelper.Lerp(glow, target, 0.15f);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			Rectangle rect = GetDimensions().ToRectangle();
			Vector2 center = rect.Center.ToVector2();
			float radius = Math.Min(rect.Width, rect.Height) * 0.5f - 6f;

			float ringRadius = radius - 16f;
			float ringThickness = 14f;

			DrawTicks(spriteBatch, center, radius);

			// Track.
			UpgraderStyle.DrawArc(spriteBatch, center, ringRadius, ringThickness, 0f, MathHelper.TwoPi,
				new Color(28, 34, 62), 110);

			// Win zone.
			float sweep = MathHelper.TwoPi * MathHelper.Clamp(displayedChance, 0f, 1f);
			if (sweep > 0.001f)
			{
				Color zone = UpgradeEngine.ChanceColor(displayedChance);
				UpgraderStyle.DrawArc(spriteBatch, center, ringRadius, ringThickness, 0f, sweep, zone, 110);
				UpgraderStyle.DrawArc(spriteBatch, center, ringRadius, ringThickness * 0.35f, 0f, sweep,
					Color.Lerp(zone, Color.White, 0.45f) * (0.35f + glow * 0.4f), 110);
			}

			DrawHub(spriteBatch, center, ringRadius - ringThickness * 0.5f - 4f);
			DrawNeedle(spriteBatch, center, radius);
			DrawReadout(spriteBatch, center);
		}

		private void DrawTicks(SpriteBatch spriteBatch, Vector2 center, float radius)
		{
			for (int i = 0; i < TickCount; i++)
			{
				float angle = MathHelper.TwoPi * i / TickCount;
				bool major = i % 5 == 0;
				float length = major ? 11f : 6f;
				float thickness = major ? 3f : 2f;

				Vector2 direction = UpgraderStyle.AngleToOffset(angle);
				Vector2 outer = center + direction * radius;
				Vector2 inner = center + direction * (radius - length);

				Color color = major ? new Color(150, 168, 215) : new Color(88, 102, 148);
				UpgraderStyle.DrawLine(spriteBatch, inner, outer, color, thickness);
			}
		}

		private void DrawHub(SpriteBatch spriteBatch, Vector2 center, float radius)
			=> UpgraderStyle.DrawDisc(spriteBatch, center, radius, new Color(30, 38, 70), new Color(16, 20, 38));

		private void DrawNeedle(SpriteBatch spriteBatch, Vector2 center, float radius)
		{
			Vector2 direction = UpgraderStyle.AngleToOffset(NeedleAngle);
			Vector2 tip = center + direction * (radius + 4f);
			Vector2 tail = center + direction * (radius - 34f);

			Color needle = ResultFlash > 0
				? LastResultWon ? new Color(140, 255, 150) : new Color(255, 110, 110)
				: UpgraderStyle.Accent;

			UpgraderStyle.DrawLine(spriteBatch, tail, tip, needle * 0.5f, 8f);
			UpgraderStyle.DrawLine(spriteBatch, tail, tip, needle, 4f);

			// Arrow head.
			Vector2 perpendicular = new(-direction.Y, direction.X);
			Vector2 headBase = center + direction * (radius - 12f);
			UpgraderStyle.DrawLine(spriteBatch, headBase + perpendicular * 7f, tip, needle, 3f);
			UpgraderStyle.DrawLine(spriteBatch, headBase - perpendicular * 7f, tip, needle, 3f);
		}

		private void DrawReadout(SpriteBatch spriteBatch, Vector2 center)
		{
			string percent = (displayedChance * 100f).ToString("0.00", CultureInfo.InvariantCulture) + "%";
			Color color = Chance <= 0f ? new Color(200, 70, 70) : UpgradeEngine.ChanceColor(displayedChance);

			if (ResultFlash > 0)
			{
				percent = Language.GetTextValue(LastResultWon
					? "Mods.Upgraderitems.UI.Success"
					: "Mods.Upgraderitems.UI.Failure");
				color = LastResultWon ? new Color(150, 255, 160) : new Color(255, 100, 100);
			}

			float pulse = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.02f * glow;
			// Offsets are picked so the percentage and its caption straddle the hub's middle as a pair.
			UpgraderStyle.DrawTextCentered(spriteBatch, percent, center - new Vector2(0f, 12f), color, 1.35f * pulse);

			string label = Language.GetTextValue("Mods.Upgraderitems.UI.Chance");
			UpgraderStyle.DrawTextCentered(spriteBatch, label, center + new Vector2(0f, 14f), UpgraderStyle.TextDim, 0.85f);
		}
	}
}
