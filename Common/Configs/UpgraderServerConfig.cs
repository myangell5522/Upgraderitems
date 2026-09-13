using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace Upgraderitems.Common.Configs
{
	public class UpgraderServerConfig : ModConfig
	{
		public static UpgraderServerConfig Instance => ModContent.GetInstance<UpgraderServerConfig>();

		public override ConfigScope Mode => ConfigScope.ServerSide;

		[Header("Odds")]
		[Range(0.10f, 1.00f)]
		[Increment(0.01f)]
		[DefaultValue(0.90f)]
		[Slider]
		public float HouseEdge { get; set; }

		[Range(0.10f, 1.00f)]
		[Increment(0.01f)]
		[DefaultValue(0.90f)]
		[Slider]
		public float MaxChance { get; set; }

		[Range(0.001f, 0.50f)]
		[Increment(0.005f)]
		[DefaultValue(0.005f)]
		[Slider]
		public float MinChance { get; set; }

		[Header("Rules")]
		[Range(0f, 1f)]
		[Increment(0.05f)]
		[DefaultValue(0f)]
		[Slider]
		public float FailureRefund { get; set; }

		[DefaultValue(true)]
		public bool ProtectFavoritedItems { get; set; }

		[DefaultValue(true)]
		public bool AllowInMultiplayer { get; set; }

		[Range(0, 600)]
		[DefaultValue(0)]
		public int CooldownTicks { get; set; }

		[Header("Categories")]
		[DefaultValue(true)]
		public bool StrictCategories { get; set; }
	}
}
