using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace Upgraderitems.Common.Configs
{
	public class UpgraderClientConfig : ModConfig
	{
		public static UpgraderClientConfig Instance => ModContent.GetInstance<UpgraderClientConfig>();

		public override ConfigScope Mode => ConfigScope.ClientSide;

		[Header("Tooltip")]
		[DefaultValue(true)]
		public bool ShowValueInTooltip { get; set; }

		[DefaultValue(true)]
		public bool ShowStackValueInTooltip { get; set; }

		[DefaultValue(true)]
		public bool AnimateTooltipIcon { get; set; }

		/// <summary>Ticks each frame of the spinning coin is held. Higher = slower.</summary>
		[Range(2, 30)]
		[Increment(1)]
		[DefaultValue(9)]
		[Slider]
		public int TooltipIconFrameTicks { get; set; }

		/// <summary>
		/// Multiplier on the coin's natural size. The sheet is pixel art, so whole steps stay crisp.
		/// </summary>
		[Range(0.5f, 2f)]
		[Increment(0.25f)]
		[DefaultValue(1f)]
		[Slider]
		public float TooltipIconSize { get; set; }

		[Header("Interface")]
		[DefaultValue(true)]
		public bool ShowInventoryButton { get; set; }

		[DefaultValue(true)]
		public bool PlaySounds { get; set; }

		/// <summary>Length of the wheel spin in ticks.</summary>
		[Range(20, 300)]
		[Increment(10)]
		[DefaultValue(120)]
		[Slider]
		public int SpinDurationTicks { get; set; }

		[DefaultValue(true)]
		public bool ScreenShakeOnResult { get; set; }
	}
}
