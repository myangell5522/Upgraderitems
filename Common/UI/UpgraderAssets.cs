using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace Upgraderitems.Common.UI
{
	public static class UpgraderAssets
	{
		public const string Root = "Upgraderitems/Assets/UI/";

		/// <summary>8 frames of a spinning gold coin stacked vertically, 24x32 each.</summary>
		public const int CoinFrames = 8;

		public const int CoinFrameWidth = 24;

		public const int CoinFrameHeight = 32;

		/// <summary>
		/// The sheet is pre-scaled 2x so it survives resampling; drawn at full size the coin looms over
		/// the text, so everything renders it at this fraction unless the player overrides it.
		/// </summary>
		public const float CoinDrawScale = 0.55f;

		public const float CoinDrawWidth = CoinFrameWidth * CoinDrawScale;

		public const float CoinDrawHeight = CoinFrameHeight * CoinDrawScale;

		private static Asset<Texture2D> coin;
		private static Asset<Texture2D> cash;

		public static Texture2D Coin => (coin ??= ModContent.Request<Texture2D>(Root + "GoldCoin", AssetRequestMode.ImmediateLoad)).Value;

		public static Texture2D Cash => (cash ??= ModContent.Request<Texture2D>(Root + "CashIcon", AssetRequestMode.ImmediateLoad)).Value;

		/// <summary>Source rectangle of the coin frame for the current game tick.</summary>
		public static Rectangle CoinFrame(uint tick, int ticksPerFrame)
		{
			if (ticksPerFrame < 1)
				ticksPerFrame = 1;

			int frame = (int)(tick / (uint)ticksPerFrame % CoinFrames);
			return new Rectangle(0, frame * CoinFrameHeight, CoinFrameWidth, CoinFrameHeight);
		}

		public static void Unload()
		{
			coin = null;
			cash = null;
		}
	}
}
