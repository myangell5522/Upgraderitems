using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Upgraderitems.Common.Configs;
using Upgraderitems.Common.Economy;
using Upgraderitems.Common.Players;
using Upgraderitems.Common.UI.Elements;

namespace Upgraderitems.Common.UI
{
	internal enum UpgradePhase
	{
		Idle,
		Spinning,
		Result
	}

	public class UpgraderUIState : UIState
	{
		public const int PanelWidth = 640;
		public const int PanelHeight = 592;
		private const int Padding = 12;
		private const int ContentWidth = PanelWidth - Padding * 2;

		private const int SlotPanelWidth = 176;
		private const int SlotPanelHeight = 190;
		private const int SlotsY = 44;
		private const int ButtonsY = 244;
		private const int ButtonHeight = 40;
		private const int StatsY = 292;
		private const int GridY = 342;

		private const int ResultFlashTicks = 100;

		private UIElement root;
		private UIDragHandle header;
		private UIUpgradeSlot stakeSlot;
		private UIUpgradeSlot targetSlot;
		private UIChanceWheel wheel;
		private UIAccentButton upgradeButton;
		private UIInventoryGrid inventoryGrid;
		private UICatalog catalog;
		private readonly List<UIAccentButton> quickButtons = new();

		private Item targetPreview = new();
		private int targetPreviewType = ItemID.None;

		private UpgradePhase phase = UpgradePhase.Idle;
		private int phaseTimer;
		private float rollValue;
		private bool pendingWin;
		private int pendingTargetType;
		private float spinStartAngle;
		private float spinEndAngle;

		private readonly List<UiSpark> sparks = new();
		private string statusMessage = string.Empty;
		private int statusTimer;

		/// <summary>Odds that a fresh window greets you with the gambling meme instead of the plain label.</summary>
		private const float MemeLabelChance = 0.08f;

		private bool memeLabel;

		/// <summary>0 = fully hidden, 1 = fully open. Drives the pop-in animation.</summary>
		public float Animation { get; private set; }
		public bool Closing { get; private set; }
		public bool CatalogOpen { get; private set; }

		private static UpgraderPlayer ModPlayer => Main.LocalPlayer.GetModPlayer<UpgraderPlayer>();

		public override void OnInitialize()
		{
			root = new UIElement();
			root.Width.Set(PanelWidth, 0f);
			root.Height.Set(PanelHeight, 0f);
			root.HAlign = 0.5f;
			root.VAlign = 0.5f;
			Append(root);

			header = new UIDragHandle(root);
			header.Left.Set(0f, 0f);
			header.Top.Set(0f, 0f);
			header.Width.Set(PanelWidth, 0f);
			header.Height.Set(SlotsY, 0f);
			root.Append(header);

			stakeSlot = new UIUpgradeSlot
			{
				Provider = () => ModPlayer.StakedItem,
				Activated = UnstakeItem,
				Highlight = true
			};
			Place(stakeSlot, Padding, SlotsY, SlotPanelWidth, SlotPanelHeight);
			root.Append(stakeSlot);

			targetSlot = new UIUpgradeSlot
			{
				Provider = GetTargetPreview,
				Activated = OpenCatalog
			};
			Place(targetSlot, PanelWidth - Padding - SlotPanelWidth, SlotsY, SlotPanelWidth, SlotPanelHeight);
			root.Append(targetSlot);

			wheel = new UIChanceWheel();
			int wheelX = Padding + SlotPanelWidth;
			Place(wheel, wheelX, SlotsY, ContentWidth - SlotPanelWidth * 2, SlotPanelHeight);
			root.Append(wheel);

			upgradeButton = new UIAccentButton(string.Empty, ButtonStyle.Primary)
			{
				TextScale = 1.15f,
				Gleam = true,
				IsEnabled = () => phase == UpgradePhase.Idle && CurrentBlockReason() == UpgradeBlockReason.Ready
			};
			upgradeButton.OnLeftClick += (_, _) => StartUpgrade();
			Place(upgradeButton, Padding, ButtonsY, 326, ButtonHeight);
			root.Append(upgradeButton);

			BuildQuickButtons();

			inventoryGrid = new UIInventoryGrid
			{
				OnSlotClicked = StakeFromInventory,
				IsEligible = IsEligible
			};
			int gridWidth = UIInventoryGrid.Columns * UIInventoryGrid.CellSize + (UIInventoryGrid.Columns - 1) * UIInventoryGrid.Gap;
			inventoryGrid.Left.Set((PanelWidth - gridWidth) / 2f, 0f);
			inventoryGrid.Top.Set(GridY, 0f);
			root.Append(inventoryGrid);

			catalog = new UICatalog
			{
				OnPick = SetTarget,
				OnClose = CloseCatalog
			};
			Place(catalog, 0, 0, PanelWidth, PanelHeight);
			root.Append(catalog);
			catalog.Remove();
		}

		private static void Place(UIElement element, int x, int y, int width, int height)
		{
			element.Left.Set(x, 0f);
			element.Top.Set(y, 0f);
			element.Width.Set(width, 0f);
			element.Height.Set(height, 0f);
		}

		private void BuildQuickButtons()
		{
			(string key, Action action)[] definitions =
			{
				("x2", () => SetTargetByMultiplier(2)),
				("x4", () => SetTargetByMultiplier(4)),
				("x8", () => SetTargetByMultiplier(8)),
				("30%", () => SetTargetByChance(0.30f)),
				("50%", () => SetTargetByChance(0.50f)),
				("70%", () => SetTargetByChance(0.70f))
			};

			int x = Padding + 334;
			foreach ((string key, Action action) in definitions)
			{
				UIAccentButton button = new(key)
				{
					TextScale = 0.9f,
					Tooltip = Language.GetTextValue("Mods.Upgraderitems.UI.QuickHint." + key.Replace("%", "Percent")),
					IsEnabled = () => phase == UpgradePhase.Idle && !ModPlayer.StakedItem.IsAir
				};

				button.OnLeftClick += (_, _) => action();
				Place(button, x, ButtonsY, 44, ButtonHeight);
				root.Append(button);
				quickButtons.Add(button);
				x += 47;
			}
		}

		// ------------------------------------------------------------------ visibility

		public void Show()
		{
			Closing = false;
			if (Animation > 0f)
				return;

			Animation = 0.01f;

			// Rolled once per opening so the joke stays a surprise instead of flickering in and out.
			memeLabel = Main.rand.NextFloat() < MemeLabelChance;
		}

		public void BeginClose()
		{
			Closing = true;
			CloseCatalog();
			Main.blockInput = false;
		}

		// ------------------------------------------------------------------ item flow

		private static bool IsEligible(Item item)
		{
			if (item == null || item.IsAir)
				return false;
			if ((UpgraderServerConfig.Instance?.ProtectFavoritedItems ?? true) && item.favorited)
				return false;

			return UpgradeCategoryUtils.Classify(item) != UpgradeCategory.None;
		}

		private void StakeFromInventory(int index)
		{
			if (phase != UpgradePhase.Idle)
				return;

			Player player = Main.LocalPlayer;
			Item clicked = player.inventory[index];
			if (clicked.IsAir)
				return;

			if (!IsEligible(clicked))
			{
				SetStatus(Language.GetTextValue(clicked.favorited
					? "Mods.Upgraderitems.Blocked.Favorited"
					: "Mods.Upgraderitems.Blocked.CategoryMismatch"));
				return;
			}

			UpgraderPlayer modPlayer = ModPlayer;
			Item previous = modPlayer.StakedItem;

			Item staked = clicked.Clone();
			staked.stack = 1;

			if (clicked.stack > 1)
			{
				clicked.stack--;
				ReturnToInventory(previous);
			}
			else
			{
				player.inventory[index] = previous is { IsAir: false } ? previous : new Item();
			}

			InventorySync.SyncSlot(player, index);
			modPlayer.StakedItem = staked;

			// A stake from a different class invalidates the chosen target.
			if (targetPreviewType > ItemID.None
				&& ContentSamples.ItemsByType.TryGetValue(targetPreviewType, out Item sample)
				&& UpgradeCategoryUtils.Classify(sample) != UpgradeCategoryUtils.Classify(staked))
			{
				SetTarget(ItemID.None);
			}

			if (UpgraderClientConfig.Instance?.PlaySounds ?? true)
				SoundEngine.PlaySound(SoundID.Grab);
		}

		private void UnstakeItem()
		{
			if (phase != UpgradePhase.Idle)
				return;

			UpgraderPlayer modPlayer = ModPlayer;
			if (modPlayer.StakedItem.IsAir)
				return;

			ReturnToInventory(modPlayer.StakedItem);
			modPlayer.StakedItem = new Item();
		}

		private static void ReturnToInventory(Item item)
		{
			if (item == null || item.IsAir)
				return;

			Player player = Main.LocalPlayer;
			IEntitySource source = player.GetSource_Misc("Upgraderitems:Return");
			player.QuickSpawnItem(source, item, item.stack);
		}

		// ------------------------------------------------------------------ target

		private Item GetTargetPreview()
		{
			int type = ModPlayer.TargetType;
			if (type != targetPreviewType)
			{
				targetPreviewType = type;
				targetPreview = type > ItemID.None && ContentSamples.ItemsByType.TryGetValue(type, out Item sample)
					? sample.Clone()
					: new Item();
			}

			return targetPreview;
		}

		private void SetTarget(int type)
		{
			ModPlayer.TargetType = type;
			GetTargetPreview();
		}

		private void OpenCatalog()
		{
			if (phase != UpgradePhase.Idle)
				return;

			Item stake = ModPlayer.StakedItem;
			if (stake.IsAir)
			{
				SetStatus(Language.GetTextValue("Mods.Upgraderitems.Blocked.NoStake"));
				return;
			}

			catalog.Open(UpgradeCategoryUtils.Classify(stake), ItemValue.Get(stake));
			if (catalog.Parent == null)
				root.Append(catalog);

			CatalogOpen = true;
		}

		public void CloseCatalog()
		{
			if (!CatalogOpen)
				return;

			catalog.Close();
			catalog.Remove();
			CatalogOpen = false;
		}

		private void SetTargetByMultiplier(int multiplier)
		{
			Item stake = ModPlayer.StakedItem;
			if (stake.IsAir)
				return;

			long stakeValue = ItemValue.Get(stake);
			int type = ItemCatalog.FindClosestToValue(UpgradeCategoryUtils.Classify(stake), stakeValue * multiplier, stake.type);
			if (type > ItemID.None)
				SetTarget(type);
		}

		private void SetTargetByChance(float chance)
		{
			Item stake = ModPlayer.StakedItem;
			if (stake.IsAir)
				return;

			long stakeValue = ItemValue.Get(stake);
			long wanted = UpgradeEngine.TargetValueForChance(stakeValue, chance);
			int type = ItemCatalog.FindClosestToValue(UpgradeCategoryUtils.Classify(stake), wanted, stake.type);
			if (type > ItemID.None)
				SetTarget(type);
		}

		// ------------------------------------------------------------------ the gamble

		private UpgradeBlockReason CurrentBlockReason()
			=> UpgradeEngine.Validate(Main.LocalPlayer, ModPlayer.StakedItem, ModPlayer.TargetType);

		private float CurrentChance()
		{
			Item stake = ModPlayer.StakedItem;
			if (stake.IsAir || ModPlayer.TargetType <= ItemID.None)
				return 0f;

			return UpgradeEngine.ComputeChance(ItemValue.Get(stake), ItemValue.Get(ModPlayer.TargetType));
		}

		private void StartUpgrade()
		{
			UpgradeBlockReason reason = CurrentBlockReason();
			if (reason != UpgradeBlockReason.Ready)
			{
				SetStatus(UpgradeEngine.BlockMessage(reason));
				return;
			}

			float chance = CurrentChance();
			rollValue = Main.rand.NextFloat();
			pendingWin = rollValue < chance;
			pendingTargetType = ModPlayer.TargetType;

			// The needle lands exactly on the rolled position, several turns later.
			spinStartAngle = wheel.NeedleAngle % MathHelper.TwoPi;
			spinEndAngle = MathHelper.TwoPi * (4 + Main.rand.Next(2)) + rollValue * MathHelper.TwoPi;

			phase = UpgradePhase.Spinning;
			phaseTimer = 0;
			wheel.Spinning = true;
			wheel.ResultFlash = 0;

			CloseCatalog();

			if (UpgraderClientConfig.Instance?.PlaySounds ?? true)
				SoundEngine.PlaySound(SoundID.MenuOpen);
		}

		private void ResolveUpgrade()
		{
			UpgraderPlayer modPlayer = ModPlayer;
			Item stake = modPlayer.StakedItem;

			UpgradeEngine.ApplyResult(Main.LocalPlayer, pendingWin, pendingTargetType, stake);

			modPlayer.StakedItem = new Item();

			phase = UpgradePhase.Result;
			phaseTimer = 0;
			wheel.Spinning = false;
			wheel.ResultFlash = ResultFlashTicks;
			wheel.LastResultWon = pendingWin;

			UpgraderClientConfig config = UpgraderClientConfig.Instance;
			if (config?.PlaySounds ?? true)
				SoundEngine.PlaySound(pendingWin ? SoundID.AchievementComplete : SoundID.Shatter);

			if (config?.ScreenShakeOnResult ?? true)
			{
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(
					Main.LocalPlayer.Center, Main.rand.NextVector2Unit(),
					pendingWin ? 7f : 4f, 7f, pendingWin ? 22 : 14, 1000f, "Upgraderitems"));
			}

			if (pendingWin)
			{
				SpawnSparks();
				string name = ContentSamples.ItemsByType.TryGetValue(pendingTargetType, out Item sample) ? sample.Name : "?";
				SetStatus(Language.GetTextValue("Mods.Upgraderitems.UI.WonMessage", name));
			}
			else
			{
				SetStatus(Language.GetTextValue("Mods.Upgraderitems.UI.LostMessage"));
			}
		}

		private void SpawnSparks()
		{
			Vector2 center = wheel.GetDimensions().Center();
			for (int i = 0; i < 40; i++)
			{
				float angle = MathHelper.TwoPi * i / 40f + Main.rand.NextFloat(-0.1f, 0.1f);
				float speed = Main.rand.NextFloat(2.5f, 7f);
				sparks.Add(new UiSpark
				{
					Position = center,
					Velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed,
					Life = Main.rand.Next(30, 60),
					MaxLife = 60,
					Color = Color.Lerp(UpgraderStyle.Accent, new Color(150, 255, 170), Main.rand.NextFloat())
				});
			}
		}

		private void SetStatus(string message)
		{
			statusMessage = message;
			statusTimer = 180;
		}

		// ------------------------------------------------------------------ update / draw

		public override void Update(GameTime gameTime)
		{
			Animation = MathHelper.Clamp(Animation + (Closing ? -0.12f : 0.14f), 0f, 1f);
			if (Animation <= 0f)
				return;

			base.Update(gameTime);

			if (root.GetDimensions().ToRectangle().Contains(Main.MouseScreen.ToPoint()))
				Main.LocalPlayer.mouseInterface = true;

			UpdateWheel();
			UpdateSparks();

			if (statusTimer > 0)
				statusTimer--;

			upgradeButton.Text = phase switch
			{
				UpgradePhase.Spinning => Language.GetTextValue("Mods.Upgraderitems.UI.Rolling"),
				_ when memeLabel => Language.GetTextValue("Mods.Upgraderitems.UI.UpgradeMeme"),
				_ => Language.GetTextValue("Mods.Upgraderitems.UI.Upgrade")
			};

			// Refreshed every frame so switching game language updates the window live.
			stakeSlot.EmptyTitle = Language.GetTextValue("Mods.Upgraderitems.UI.StakeEmptyTitle");
			stakeSlot.EmptyHint = Language.GetTextValue("Mods.Upgraderitems.UI.StakeEmptyHint");
			targetSlot.EmptyTitle = Language.GetTextValue("Mods.Upgraderitems.UI.TargetEmptyTitle");
			targetSlot.EmptyHint = Language.GetTextValue("Mods.Upgraderitems.UI.TargetEmptyHint");
		}

		private void UpdateWheel()
		{
			wheel.Chance = CurrentChance();

			switch (phase)
			{
				case UpgradePhase.Spinning:
				{
					int duration = Math.Max(20, UpgraderClientConfig.Instance?.SpinDurationTicks ?? 120);
					phaseTimer++;
					float progress = UpgraderStyle.EaseOutCubic(phaseTimer / (float)duration);
					wheel.NeedleAngle = MathHelper.Lerp(spinStartAngle, spinEndAngle, progress);

					if ((UpgraderClientConfig.Instance?.PlaySounds ?? true) && phaseTimer % 6 == 0 && progress < 0.98f)
						SoundEngine.PlaySound(SoundID.MenuTick);

					if (phaseTimer >= duration)
					{
						wheel.NeedleAngle = spinEndAngle;
						ResolveUpgrade();
					}

					break;
				}

				case UpgradePhase.Result:
				{
					phaseTimer++;
					if (wheel.ResultFlash > 0)
						wheel.ResultFlash--;

					if (phaseTimer >= ResultFlashTicks)
					{
						phase = UpgradePhase.Idle;
						phaseTimer = 0;
					}

					break;
				}
			}
		}

		private void UpdateSparks()
		{
			for (int i = sparks.Count - 1; i >= 0; i--)
			{
				UiSpark spark = sparks[i];
				spark.Position += spark.Velocity;
				spark.Velocity *= 0.94f;
				spark.Velocity.Y += 0.18f;
				spark.Life--;

				if (spark.Life <= 0)
					sparks.RemoveAt(i);
				else
					sparks[i] = spark;
			}
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			if (Animation <= 0.001f)
				return;

			float scale = MathHelper.Lerp(0.9f, 1f, UpgraderStyle.EaseOutBack(Animation));
			UpgraderStyle.Opacity = Animation;

			bool transformed = Math.Abs(scale - 1f) > 0.002f;
			if (transformed)
			{
				Vector2 center = root.GetDimensions().Center();
				Matrix matrix =
					Matrix.CreateTranslation(-center.X, -center.Y, 0f) *
					Matrix.CreateScale(scale) *
					Matrix.CreateTranslation(center.X, center.Y, 0f) *
					Main.UIScaleMatrix;

				spriteBatch.End();
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
					DepthStencilState.None, Main.Rasterizer, null, matrix);
			}

			base.Draw(spriteBatch);

			if (transformed)
			{
				spriteBatch.End();
				spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
					DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
			}

			UpgraderStyle.Opacity = 1f;
		}

		protected override void DrawChildren(SpriteBatch spriteBatch)
		{
			Rectangle panel = root.GetDimensions().ToRectangle();
			UpgraderStyle.DrawFrame(spriteBatch, panel);
			DrawHeader(spriteBatch, panel);

			// Drawn before the children so the catalog overlay, which is the last child, hides them.
			DrawStats(spriteBatch, panel);
			DrawInventoryLabel(spriteBatch, panel);

			base.DrawChildren(spriteBatch);

			DrawSparks(spriteBatch);
			DrawStatus(spriteBatch, panel);
		}

		private void DrawHeader(SpriteBatch spriteBatch, Rectangle panel)
		{
			string title = Language.GetTextValue("Mods.Upgraderitems.UI.Title");
			UpgraderStyle.DrawTextCentered(spriteBatch, title,
				new Vector2(panel.Center.X, panel.Y + 22), UpgraderStyle.Accent, 1.3f);

			Rectangle underline = new(panel.X + 24, panel.Y + 38, panel.Width - 48, 2);
			UpgraderStyle.FillRect(spriteBatch, underline, UpgraderStyle.PanelBorder * 0.6f);
		}

		private void DrawStats(SpriteBatch spriteBatch, Rectangle panel)
		{
			UpgraderPlayer modPlayer = ModPlayer;
			int y = panel.Y + StatsY;

			Rectangle strip = new(panel.X + Padding, y, ContentWidth, 26);
			UpgraderStyle.FillRect(spriteBatch, strip, new Color(20, 26, 54) * 0.85f);
			UpgraderStyle.OutlineRect(spriteBatch, strip, UpgraderStyle.InnerBorder * 0.7f, 1);

			string stats = Language.GetTextValue("Mods.Upgraderitems.UI.Stats",
				modPlayer.Wins, modPlayer.Losses, modPlayer.Streak, modPlayer.BestStreak);
			UpgraderStyle.DrawTextMiddle(spriteBatch, stats, new Vector2(strip.X + 8, strip.Center.Y), UpgraderStyle.TextDim, 0.82f);

			// Recent results as coloured pips, newest on the right.
			int pipSize = 12;
			int pipX = strip.Right - 8 - UpgraderPlayer.HistoryLength * (pipSize + 3);
			for (int i = 0; i < UpgraderPlayer.HistoryLength; i++)
			{
				Rectangle pip = new(pipX + i * (pipSize + 3), strip.Y + 7, pipSize, pipSize);
				int historyIndex = i - (UpgraderPlayer.HistoryLength - modPlayer.History.Count);

				Color color = historyIndex < 0
					? new Color(40, 48, 82)
					: modPlayer.History[historyIndex] ? new Color(110, 220, 120) : new Color(200, 80, 80);

				UpgraderStyle.FillRect(spriteBatch, pip, color);
			}
		}

		private void DrawInventoryLabel(SpriteBatch spriteBatch, Rectangle panel)
		{
			int middleY = panel.Y + GridY - 16;

			UpgraderStyle.DrawTextMiddle(spriteBatch, Language.GetTextValue("Mods.Upgraderitems.UI.Inventory"),
				new Vector2(panel.X + Padding + 4, middleY), UpgraderStyle.TextBright, 0.95f);

			string hint = Language.GetTextValue("Mods.Upgraderitems.UI.InventoryHint");
			UpgraderStyle.DrawTextRight(spriteBatch, hint,
				new Vector2(panel.Right - Padding - 4, middleY), UpgraderStyle.TextDim, 0.75f);
		}

		private void DrawSparks(SpriteBatch spriteBatch)
		{
			foreach (UiSpark spark in sparks)
			{
				float life = spark.Life / (float)spark.MaxLife;
				int size = Math.Max(2, (int)(5 * life));
				Rectangle rect = new((int)spark.Position.X - size / 2, (int)spark.Position.Y - size / 2, size, size);
				UpgraderStyle.FillRect(spriteBatch, rect, spark.Color * life);
			}
		}

		private void DrawStatus(SpriteBatch spriteBatch, Rectangle panel)
		{
			if (statusTimer <= 0 || string.IsNullOrEmpty(statusMessage))
				return;

			float alpha = Math.Min(1f, statusTimer / 40f);
			Vector2 size = UpgraderStyle.Measure(statusMessage, 0.9f);
			// Sits in the empty gap under the wheel so it never covers the stats strip below the buttons.
			Rectangle box = new(
				(int)(panel.Center.X - size.X * 0.5f) - 10,
				panel.Y + ButtonsY - 28,
				(int)size.X + 20,
				24);

			UpgraderStyle.FillRect(spriteBatch, box, new Color(12, 16, 34) * (0.85f * alpha));
			UpgraderStyle.OutlineRect(spriteBatch, box, UpgraderStyle.Accent * alpha, 1);
			UpgraderStyle.DrawTextCentered(spriteBatch, statusMessage,
				box.Center.ToVector2(), UpgraderStyle.TextBright * alpha, 0.9f);
		}

		private struct UiSpark
		{
			public Vector2 Position;
			public Vector2 Velocity;
			public int Life;
			public int MaxLife;
			public Color Color;
		}
	}

	/// <summary>Lets the player drag the window by its title bar; the position is clamped on screen.</summary>
	public class UIDragHandle : UIElement
	{
		private readonly UIElement target;
		private bool dragging;
		private Vector2 grabOffset;

		public UIDragHandle(UIElement target) => this.target = target;

		public override void LeftMouseDown(UIMouseEvent evt)
		{
			base.LeftMouseDown(evt);
			dragging = true;
			grabOffset = evt.MousePosition - target.GetDimensions().Position();
		}

		public override void LeftMouseUp(UIMouseEvent evt)
		{
			base.LeftMouseUp(evt);
			dragging = false;
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (!dragging)
				return;

			if (!Main.mouseLeft)
			{
				dragging = false;
				return;
			}

			Vector2 position = Main.MouseScreen - grabOffset;
			CalculatedStyle dimensions = target.GetDimensions();

			position.X = MathHelper.Clamp(position.X, 0f, Main.screenWidth - dimensions.Width);
			position.Y = MathHelper.Clamp(position.Y, 0f, Main.screenHeight - dimensions.Height);

			target.HAlign = 0f;
			target.VAlign = 0f;
			target.Left.Set(position.X, 0f);
			target.Top.Set(position.Y, 0f);
			target.Recalculate();
		}
	}
}
