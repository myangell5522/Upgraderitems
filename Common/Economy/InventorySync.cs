using Terraria;
using Terraria.ID;

namespace Upgraderitems.Common.Economy
{
	public static class InventorySync
	{
		/// <summary>
		/// Vanilla only syncs inventory slots that were touched through <c>ItemSlot</c>. This mod writes to
		/// them directly, so the change has to be announced by hand or the server keeps the stale item.
		/// </summary>
		public static void SyncSlot(Player player, int slot)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient || player.whoAmI != Main.myPlayer)
				return;

			NetMessage.SendData(MessageID.SyncEquipment, number: player.whoAmI, number2: slot,
				number3: player.inventory[slot].prefix);
		}
	}
}
