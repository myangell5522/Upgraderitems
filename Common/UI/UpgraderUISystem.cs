using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Upgraderitems.Common.Configs;

namespace Upgraderitems.Common.UI
{
	public class UpgraderUISystem : ModSystem
	{
		internal UserInterface WindowInterface;
		internal UpgraderUIState Window;

		internal UserInterface ButtonInterface;
		internal UpgraderButtonState ButtonState;

		private bool inventoryWasOpen;
		private GameTime lastUpdate = new();

		public bool Visible { get; private set; }

		public override void Load()
		{
			if (Main.dedServ)
				return;

			Window = new UpgraderUIState();
			Window.Activate();
			WindowInterface = new UserInterface();

			ButtonState = new UpgraderButtonState();
			ButtonState.Activate();
			ButtonInterface = new UserInterface();
			ButtonInterface.SetState(ButtonState);
		}

		public override void Unload()
		{
			WindowInterface = null;
			Window = null;
			ButtonInterface = null;
			ButtonState = null;

			UpgraderStyle.Unload();
			UpgraderAssets.Unload();
		}

		public void Toggle()
		{
			if (Visible)
				Close();
			else
				Open();
		}

		public void Open()
		{
			if (Visible)
				return;

			Visible = true;
			Window.Show();
			WindowInterface.SetState(Window);

			if (UpgraderClientConfig.Instance?.PlaySounds ?? true)
				SoundEngine.PlaySound(SoundID.MenuOpen);
		}

		public void Close()
		{
			if (!Visible)
				return;

			Visible = false;
			Window.BeginClose();

			if (UpgraderClientConfig.Instance?.PlaySounds ?? true)
				SoundEngine.PlaySound(SoundID.MenuClose);
		}

		public override void UpdateUI(GameTime gameTime)
		{
			if (Main.dedServ)
				return;

			lastUpdate = gameTime;
			bool inventoryOpen = Main.playerInventory && !Main.gameMenu;

			if (inventoryOpen && !inventoryWasOpen)
				ButtonState.OnInventoryOpened();
			inventoryWasOpen = inventoryOpen;

			if (inventoryOpen && (UpgraderClientConfig.Instance?.ShowInventoryButton ?? true))
				ButtonInterface?.Update(gameTime);

			// Closing the inventory closes the window, mirroring how vanilla stations behave.
			if (!inventoryOpen && Visible)
				Close();

			// Keep updating while the close animation plays out.
			if (Visible || Window is { Animation: > 0f })
			{
				if (WindowInterface?.CurrentState == null)
					WindowInterface?.SetState(Window);

				WindowInterface?.Update(gameTime);

				if (!Visible && Window.Animation <= 0f)
					WindowInterface?.SetState(null);
			}
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (index == -1)
				return;

			layers.Insert(index, new LegacyGameInterfaceLayer(
				"Upgraderitems: Upgrade Window",
				() =>
				{
					if (Main.playerInventory && (UpgraderClientConfig.Instance?.ShowInventoryButton ?? true))
						ButtonInterface?.Draw(Main.spriteBatch, lastUpdate);

					if (Visible || Window is { Animation: > 0f })
						WindowInterface?.Draw(Main.spriteBatch, lastUpdate);

					return true;
				},
				InterfaceScaleType.UI));
		}
	}
}
