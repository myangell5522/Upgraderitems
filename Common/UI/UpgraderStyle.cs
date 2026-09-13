using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;

namespace Upgraderitems.Common.UI
{
	/// <summary>
	/// Palette and drawing primitives shared by every part of the upgrader window. Colours follow the
	/// vanilla inventory panels so the window does not look bolted on.
	/// </summary>
	public static class UpgraderStyle
	{
		// Vanilla UI blues.
		public static readonly Color PanelBack = new(63, 82, 151);
		public static readonly Color PanelBorder = new(89, 116, 213);
		public static readonly Color InnerBack = new(37, 48, 94);
		public static readonly Color InnerBorder = new(72, 94, 168);

		public static readonly Color Accent = new(255, 201, 78);
		public static readonly Color AccentDark = new(178, 130, 34);
		public static readonly Color TextBright = new(238, 242, 255);
		public static readonly Color TextDim = new(158, 170, 200);

		/// <summary>Fades the whole window in and out during the open/close animation.</summary>
		public static float Opacity = 1f;

		private static Asset<Texture2D> panelBackground;
		private static Asset<Texture2D> panelBorder;
		private static Texture2D pixel;

		public static Color Fade(Color color) => color * Opacity;

		/// <summary>
		/// A guaranteed 1x1 white texture. The shared vanilla pixel is not 1x1, and every rotated draw
		/// here multiplies its size into the scale vector, which turns short lines into screen-long rays.
		/// </summary>
		public static Texture2D Pixel
		{
			get
			{
				if (pixel == null || pixel.IsDisposed)
				{
					pixel = new Texture2D(Main.instance.GraphicsDevice, 1, 1);
					pixel.SetData(new[] { Color.White });
				}

				return pixel;
			}
		}

		public static void Unload()
		{
			panelBackground = null;
			panelBorder = null;
			pixel?.Dispose();
			pixel = null;
		}

		public static void FillRect(SpriteBatch sb, Rectangle rect, Color color)
			=> sb.Draw(Pixel, rect, Fade(color));

		public static void OutlineRect(SpriteBatch sb, Rectangle rect, Color color, int thickness = 1)
		{
			FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
			FillRect(sb, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
			FillRect(sb, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
			FillRect(sb, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
		}

		/// <summary>Vanilla nine-sliced panel, same textures the inventory uses.</summary>
		public static void DrawFrame(SpriteBatch sb, Rectangle rect, Color? back = null, Color? border = null)
		{
			panelBackground ??= Main.Assets.Request<Texture2D>("Images/UI/PanelBackground", AssetRequestMode.ImmediateLoad);
			panelBorder ??= Main.Assets.Request<Texture2D>("Images/UI/PanelBorder", AssetRequestMode.ImmediateLoad);

			DrawNineSlice(sb, panelBackground.Value, rect, Fade(back ?? PanelBack));
			DrawNineSlice(sb, panelBorder.Value, rect, Fade(border ?? PanelBorder));
		}

		public static void DrawNineSlice(SpriteBatch sb, Texture2D texture, Rectangle dest, Color color)
		{
			if (texture == null)
				return;

			int corner = Math.Max(1, Math.Min(12, Math.Min(texture.Width, texture.Height) / 2));
			int tw = texture.Width;
			int th = texture.Height;

			int innerW = Math.Max(0, dest.Width - corner * 2);
			int innerH = Math.Max(0, dest.Height - corner * 2);
			int srcInnerW = Math.Max(1, tw - corner * 2);
			int srcInnerH = Math.Max(1, th - corner * 2);

			sb.Draw(texture, new Rectangle(dest.X, dest.Y, corner, corner), new Rectangle(0, 0, corner, corner), color);
			sb.Draw(texture, new Rectangle(dest.Right - corner, dest.Y, corner, corner), new Rectangle(tw - corner, 0, corner, corner), color);
			sb.Draw(texture, new Rectangle(dest.X, dest.Bottom - corner, corner, corner), new Rectangle(0, th - corner, corner, corner), color);
			sb.Draw(texture, new Rectangle(dest.Right - corner, dest.Bottom - corner, corner, corner), new Rectangle(tw - corner, th - corner, corner, corner), color);

			sb.Draw(texture, new Rectangle(dest.X + corner, dest.Y, innerW, corner), new Rectangle(corner, 0, srcInnerW, corner), color);
			sb.Draw(texture, new Rectangle(dest.X + corner, dest.Bottom - corner, innerW, corner), new Rectangle(corner, th - corner, srcInnerW, corner), color);
			sb.Draw(texture, new Rectangle(dest.X, dest.Y + corner, corner, innerH), new Rectangle(0, corner, corner, srcInnerH), color);
			sb.Draw(texture, new Rectangle(dest.Right - corner, dest.Y + corner, corner, innerH), new Rectangle(tw - corner, corner, corner, srcInnerH), color);

			sb.Draw(texture, new Rectangle(dest.X + corner, dest.Y + corner, innerW, innerH), new Rectangle(corner, corner, srcInnerW, srcInnerH), color);
		}

		/// <summary>Straight line of arbitrary angle, built from a stretched pixel.</summary>
		public static void DrawLine(SpriteBatch sb, Vector2 from, Vector2 to, Color color, float thickness)
		{
			Vector2 delta = to - from;
			float length = delta.Length();
			if (length < 0.01f)
				return;

			sb.Draw(Pixel, from, null, Fade(color), delta.ToRotation(),
				new Vector2(0f, 0.5f), new Vector2(length, thickness), SpriteEffects.None, 0f);
		}

		/// <summary>
		/// Ring arc drawn as a fan of short segments. Angles are in radians, 0 = straight up, clockwise.
		/// </summary>
		public static void DrawArc(SpriteBatch sb, Vector2 center, float radius, float thickness,
			float startAngle, float sweep, Color color, int segments = 96)
		{
			if (sweep <= 0f)
				return;

			segments = Math.Max(2, (int)(segments * Math.Min(1f, sweep / MathHelper.TwoPi) + 2));
			float step = sweep / segments;
			float segmentLength = radius * step * 1.15f + 1.5f;

			for (int i = 0; i < segments; i++)
			{
				float angle = startAngle + step * (i + 0.5f);
				Vector2 point = center + AngleToOffset(angle) * radius;

				// After rotating by `angle` the sprite's local X axis runs along the tangent and its
				// local Y axis along the radius, so length goes first and thickness second.
				sb.Draw(Pixel, point, null, Fade(color), angle,
					new Vector2(0.5f, 0.5f), new Vector2(segmentLength, thickness), SpriteEffects.None, 0f);
			}
		}

		/// <summary>Filled disc drawn as horizontal scanlines - one sprite per row instead of per arc.</summary>
		public static void DrawDisc(SpriteBatch sb, Vector2 center, float radius, Color inner, Color outer)
		{
			int radiusInt = (int)radius;
			for (int y = -radiusInt; y <= radiusInt; y++)
			{
				float halfWidth = (float)Math.Sqrt(Math.Max(0f, radius * radius - y * y));
				if (halfWidth < 0.5f)
					continue;

				float distance = Math.Abs(y) / radius;
				Rectangle row = new((int)(center.X - halfWidth), (int)center.Y + y, (int)(halfWidth * 2f), 1);
				FillRect(sb, row, Color.Lerp(inner, outer, distance));
			}
		}

		/// <summary>0 radians points up; the angle grows clockwise like a clock face.</summary>
		public static Vector2 AngleToOffset(float angle)
			=> new((float)Math.Sin(angle), -(float)Math.Cos(angle));

		public static DynamicSpriteFont Font => FontAssets.MouseText.Value;

		public static Vector2 Measure(string text, float scale = 1f)
			=> Font.MeasureString(text) * scale;

		/// <summary>
		/// Distance from a draw position down to the optical middle of the glyphs. MouseText measures the
		/// whole line box, but its lower half is descender and leading the letters never reach, so cap
		/// height ends up in the top half and centring on the measured height reads noticeably high.
		/// </summary>
		public static float InkCenterY(float measuredHeight) => measuredHeight * 0.25f;

		public static void DrawText(SpriteBatch sb, string text, Vector2 position, Color color, float scale = 1f)
			=> Utils.DrawBorderString(sb, text, position, Fade(color), scale);

		/// <summary>Draws text whose left edge and optical middle land on <paramref name="leftMiddle"/>.</summary>
		public static void DrawTextMiddle(SpriteBatch sb, string text, Vector2 leftMiddle, Color color, float scale = 1f)
		{
			Vector2 size = Measure(text, scale);
			Utils.DrawBorderString(sb, text, new Vector2(leftMiddle.X, leftMiddle.Y - InkCenterY(size.Y)), Fade(color), scale);
		}

		public static void DrawTextCentered(SpriteBatch sb, string text, Vector2 center, Color color, float scale = 1f)
		{
			Vector2 size = Measure(text, scale);
			Utils.DrawBorderString(sb, text, new Vector2(center.X - size.X * 0.5f, center.Y - InkCenterY(size.Y)), Fade(color), scale);
		}

		/// <summary>
		/// Centred text with a highlight that sweeps across it once every few seconds and stays dark in
		/// between, so the label catches the eye without pulsing at the player the whole time.
		/// </summary>
		public static void DrawGleamTextCentered(SpriteBatch sb, string text, Vector2 center, Color color,
			Color gleam, float scale = 1f)
		{
			if (string.IsNullOrEmpty(text))
				return;

			// Glyphs are placed one at a time so each can carry its own colour, so the advance has to be
			// summed the same way or the label would not sit centred on what actually gets drawn.
			float width = 0f;
			foreach (char character in text)
				width += Measure(character.ToString(), scale).X;

			Vector2 pen = new(center.X - width * 0.5f, center.Y - InkCenterY(Measure(text, scale).Y));

			const float cycle = 4f;
			// Runs past the end of the string so most of the cycle is a quiet gap.
			float head = Main.GlobalTimeWrappedHourly % cycle / cycle * 2.4f - 0.25f;

			for (int i = 0; i < text.Length; i++)
			{
				float position = text.Length <= 1 ? 0f : i / (float)(text.Length - 1);
				float lit = Math.Max(0f, 1f - Math.Abs(position - head) * 5f);

				string glyph = text[i].ToString();
				Utils.DrawBorderString(sb, glyph, pen, Fade(Color.Lerp(color, gleam, lit * lit)), scale);
				pen.X += Measure(glyph, scale).X;
			}
		}

		public static void DrawTextRight(SpriteBatch sb, string text, Vector2 rightMiddle, Color color, float scale = 1f)
		{
			Vector2 size = Measure(text, scale);
			Utils.DrawBorderString(sb, text, new Vector2(rightMiddle.X - size.X, rightMiddle.Y - InkCenterY(size.Y)), Fade(color), scale);
		}

		/// <summary>Trims with an ellipsis so long modded item names cannot overflow their cell.</summary>
		public static string Truncate(string text, float maxWidth, float scale = 1f)
		{
			if (string.IsNullOrEmpty(text) || Measure(text, scale).X <= maxWidth)
				return text;

			for (int length = text.Length - 1; length > 1; length--)
			{
				string candidate = text[..length] + "…";
				if (Measure(candidate, scale).X <= maxWidth)
					return candidate;
			}

			return "…";
		}

		public static float EaseOutCubic(float t)
		{
			t = MathHelper.Clamp(t, 0f, 1f);
			float inverted = 1f - t;
			return 1f - inverted * inverted * inverted;
		}

		public static float EaseOutBack(float t)
		{
			t = MathHelper.Clamp(t, 0f, 1f);
			const float c1 = 1.70158f;
			const float c3 = c1 + 1f;
			float inverted = t - 1f;
			return 1f + c3 * inverted * inverted * inverted + c1 * inverted * inverted;
		}
	}
}
