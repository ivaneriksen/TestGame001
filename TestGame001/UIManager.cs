using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TestGame001
{
    public class UIManager
    {
        public bool IsBuildMode { get; private set; }
        public bool ShowTowerDropdown { get; private set; }
        public Func<Vector2, Tower> SelectedTowerFactory { get; private set; } = pos => new BasicTower(pos);

        // True if this frame's click was consumed by a UI element (build button, bar, dropdown
        // option) - lets Game1 know not to also treat the click as a world/placement click.
        public bool ConsumedClickThisFrame { get; private set; }

        private readonly SpriteFont uiFont;
        private readonly Texture2D pixel;

        private readonly Rectangle uiBarRect;
        private readonly Rectangle buildButtonRect;
        private readonly Rectangle towerOptionBasicRect;
        private readonly Rectangle towerOptionSniperRect;

        public Rectangle TargetClosestRect { get; private set; }
        public Rectangle TargetMostHealthRect { get; private set; }
        public Rectangle TargetLeastHealthRect { get; private set; }
        public Rectangle TargetExitRect { get; private set; }
        public Rectangle TargetEntranceRect { get; private set; }
        public Rectangle TargetFocusRect { get; private set; }

        public UIManager(SpriteFont uiFont, Texture2D pixel)
        {
            this.uiFont = uiFont;
            this.pixel = pixel;

            uiBarRect = new Rectangle(0, 0, GameConstants.ScreenWidth, GameConstants.UIBarHeight);
            buildButtonRect = new Rectangle(20, 15, 160, 50);
            towerOptionBasicRect = new Rectangle(20, 90, 160, 50);
            towerOptionSniperRect = new Rectangle(20, 150, 160, 50);

            TargetClosestRect = new Rectangle(300, 15, 120, 40);
            TargetLeastHealthRect = new Rectangle(440, 15, 120, 40);
            TargetMostHealthRect = new Rectangle(580, 15, 120, 40);
            TargetExitRect = new Rectangle(300, 65, 120, 40);
            TargetEntranceRect = new Rectangle(440, 65, 120, 40);
            TargetFocusRect = new Rectangle(580, 65, 120, 40);
        }

        public bool IsPointInBar(Point p) => uiBarRect.Contains(p);

        public void Update(MouseState mouseState, MouseState previousMouseState, KeyboardState keyboardState, KeyboardState previousKeyboardState, Tower selectedTower)
        {
            bool clickedThisFrame = mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released;
            bool clickedBuildButton = clickedThisFrame && buildButtonRect.Contains(mouseState.Position);
            bool clickedInBar = clickedThisFrame && uiBarRect.Contains(mouseState.Position);

            bool buildToggleTriggered = (keyboardState.IsKeyDown(Keys.B) && !previousKeyboardState.IsKeyDown(Keys.B)) || clickedBuildButton;
            if (buildToggleTriggered)
            {
                if (IsBuildMode)
                {
                    IsBuildMode = false;
                    ShowTowerDropdown = false;
                }
                else
                {
                    ShowTowerDropdown = !ShowTowerDropdown;
                }
            }

            bool dropdownOptionClicked = false;
            if (ShowTowerDropdown && clickedThisFrame)
            {
                if (towerOptionBasicRect.Contains(mouseState.Position))
                {
                    SelectedTowerFactory = pos => new BasicTower(pos);
                    IsBuildMode = true;
                    ShowTowerDropdown = false;
                    dropdownOptionClicked = true;
                }
                else if (towerOptionSniperRect.Contains(mouseState.Position))
                {
                    SelectedTowerFactory = pos => new SniperTower(pos);
                    IsBuildMode = true;
                    ShowTowerDropdown = false;
                    dropdownOptionClicked = true;
                }
            }

            bool targetingButtonClicked = false;
            if (selectedTower != null && clickedThisFrame)
            {
                if (TargetClosestRect.Contains(mouseState.Position))
                {
                    selectedTower.TargetingMode = TargetingMode.ClosestToTower;
                    selectedTower.CurrentTarget = null;
                    targetingButtonClicked = true;
                }
                else if (TargetMostHealthRect.Contains(mouseState.Position))
                {
                    selectedTower.TargetingMode = TargetingMode.MostHealth;
                    selectedTower.CurrentTarget = null;
                    targetingButtonClicked = true;
                }
                else if (TargetLeastHealthRect.Contains(mouseState.Position))
                {
                    selectedTower.TargetingMode = TargetingMode.LeastHealth;
                    selectedTower.CurrentTarget = null;
                    targetingButtonClicked = true;
                }
                else if (TargetExitRect.Contains(mouseState.Position))
                {
                    selectedTower.TargetingMode = TargetingMode.ClosestToExit;
                    selectedTower.CurrentTarget = null;
                    targetingButtonClicked = true;
                }
                else if (TargetEntranceRect.Contains(mouseState.Position))
                {
                    selectedTower.TargetingMode = TargetingMode.ClosestToEntrance;
                    selectedTower.CurrentTarget = null;
                    targetingButtonClicked = true;
                }
                else if (TargetFocusRect.Contains(mouseState.Position))
                {
                    selectedTower.TargetingMode = TargetingMode.Focus;
                    selectedTower.CurrentTarget = null;
                    targetingButtonClicked = true;
                }
            }

            ConsumedClickThisFrame = clickedBuildButton || clickedInBar || dropdownOptionClicked || targetingButtonClicked;
        }

        // Called by Game1 on right-click to cancel build mode/dropdown from outside.
        public void CancelBuildMode()
        {
            IsBuildMode = false;
            ShowTowerDropdown = false;
        }

        public void Draw(SpriteBatch spriteBatch, Tower selectedTower)
        {
            spriteBatch.Draw(pixel, uiBarRect, Color.DarkSlateGray);

            Color buttonColor = (IsBuildMode || ShowTowerDropdown) ? Color.LimeGreen : Color.DarkGray;
            spriteBatch.Draw(pixel, buildButtonRect, buttonColor);
            spriteBatch.DrawString(uiFont, "TOWER", new Vector2(buildButtonRect.X + 18, buildButtonRect.Y + 22), Color.White);

            if (ShowTowerDropdown)
            {
                spriteBatch.Draw(pixel, towerOptionBasicRect, Color.DarkGray);
                spriteBatch.DrawString(uiFont, "BASIC", new Vector2(towerOptionBasicRect.X + 15, towerOptionBasicRect.Y + 15), Color.White);

                spriteBatch.Draw(pixel, towerOptionSniperRect, Color.DarkGray);
                spriteBatch.DrawString(uiFont, "SNIPER", new Vector2(towerOptionSniperRect.X + 15, towerOptionSniperRect.Y + 15), Color.White);
            }

            if (selectedTower == null) return;

            spriteBatch.Draw(pixel, TargetClosestRect, selectedTower.TargetingMode == TargetingMode.ClosestToTower ? Color.LimeGreen : Color.DarkGray);
            spriteBatch.DrawString(uiFont, "CLOSEST", new Vector2(TargetClosestRect.X + 8, TargetClosestRect.Y + 10), Color.White);

            spriteBatch.Draw(pixel, TargetMostHealthRect, selectedTower.TargetingMode == TargetingMode.MostHealth ? Color.LimeGreen : Color.DarkGray);
            spriteBatch.DrawString(uiFont, "MOST HP", new Vector2(TargetMostHealthRect.X + 8, TargetMostHealthRect.Y + 10), Color.White);

            spriteBatch.Draw(pixel, TargetLeastHealthRect, selectedTower.TargetingMode == TargetingMode.LeastHealth ? Color.LimeGreen : Color.DarkGray);
            spriteBatch.DrawString(uiFont, "LEAST HP", new Vector2(TargetLeastHealthRect.X + 8, TargetLeastHealthRect.Y + 10), Color.White);

            spriteBatch.Draw(pixel, TargetExitRect, selectedTower.TargetingMode == TargetingMode.ClosestToExit ? Color.LimeGreen : Color.DarkGray);
            spriteBatch.DrawString(uiFont, "EXIT", new Vector2(TargetExitRect.X + 8, TargetExitRect.Y + 10), Color.White);

            spriteBatch.Draw(pixel, TargetEntranceRect, selectedTower.TargetingMode == TargetingMode.ClosestToEntrance ? Color.LimeGreen : Color.DarkGray);
            spriteBatch.DrawString(uiFont, "ENTRANCE", new Vector2(TargetEntranceRect.X + 8, TargetEntranceRect.Y + 10), Color.White);

            spriteBatch.Draw(pixel, TargetFocusRect, selectedTower.TargetingMode == TargetingMode.Focus ? Color.LimeGreen : Color.DarkGray);
            spriteBatch.DrawString(uiFont, "FOCUS", new Vector2(TargetFocusRect.X + 8, TargetFocusRect.Y + 10), Color.White);

            // Stats readout.
            Vector2 statsOrigin = new Vector2(750, 10);
            int lineHeight = 22;
            int valueColumnX = 150;

            string[] labels = { "Damage:", "Bullet speed:", "Range:", "Cooldown:" };
            string[] values =
            {
                selectedTower.Damage.ToString(),
                selectedTower.BulletSpeed.ToString(),
                selectedTower.Range.ToString(),
                selectedTower.Cooldown.TotalSeconds + "s"
            };

            for (int i = 0; i < labels.Length; i++)
            {
                Vector2 rowPos = statsOrigin + new Vector2(0, i * lineHeight);
                spriteBatch.DrawString(uiFont, labels[i], rowPos, Color.White);
                spriteBatch.DrawString(uiFont, values[i], rowPos + new Vector2(valueColumnX, 0), Color.White);
            }
        }
    }
}