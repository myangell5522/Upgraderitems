using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ModLoader;
using Upgraderitems.Common.UI;

namespace Upgraderitems.Common.Systems
{
	public class UpgraderKeybindSystem : ModSystem
	{
		public static ModKeybind ToggleWindow { get; private set; }

		public override void Load()
		{
			ToggleWindow = KeybindLoader.RegisterKeybind(Mod, "ToggleUpgrader", Keys.U);
		}

		public override void Unload()
		{
			ToggleWindow = null;
		}
	}

	public class UpgraderKeybindPlayer : ModPlayer
	{
		public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet)
		{
			if (Main.blockInput || UpgraderKeybindSystem.ToggleWindow == null)
				return;

			UpgraderUISystem system = ModContent.GetInstance<UpgraderUISystem>();

			if (UpgraderKeybindSystem.ToggleWindow.JustPressed)
			{
				// The window lives inside the inventory screen, so open that too.
				if (!system.Visible && !Main.playerInventory)
					Main.playerInventory = true;

				system.Toggle();
			}

			if (system.Visible && Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
			{
				// Escape peels off one layer at a time: the catalog first, then the window itself.
				if (system.Window?.CatalogOpen == true)
					system.Window.CloseCatalog();
				else
					system.Close();
			}
		}
	}
}
