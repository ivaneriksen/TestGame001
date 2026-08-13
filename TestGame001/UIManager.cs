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
        public bool PauseButtonClicked { get; private set; }

        private readonly SpriteFont uiFont;
        private readonly Texture2D pixel;

        private readonly Rectangle uiBarRect;
        private readonly Rectangle buildButtonRect;
        private readonly Rectangle towerOptionBasicRect;
        private readonly Rectangle towerOptionSniperRect;
        private readonly Rectangle pauseButtonRect;

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
            buildButtonRect = new Rectangle(20, 15, GameConstants.DefaultButtonWidth, GameConstants.DefaultButtonHeight);
            towerOptionBasicRect = new Rectangle(20, 90, GameConstants.DefaultButtonWidth, GameConstants.DefaultButtonHeight);
            towerOptionSniperRect = new Rectangle(20, 150, GameConstants.DefaultButtonWidth, GameConstants.DefaultButtonHeight);

            pauseButtonRect = new Rectangle(GameConstants.ScreenWidth - 160, 15, GameConstants.DefaultButtonWidth, GameConstants.DefaultButtonHeight);

            TargetClosestRect = new Rectangle(300, 15, GameConstants.DefaultButtonWidth, GameConstants.DefaultButtonHeight);
            TargetLeastHealthRect = new Rectangle(440, 15, GameConstants.DefaultButtonWidth, GameConstants.DefaultButtonHeight);
            TargetMostHealthRect = new Rectangle(580, 15, GameConstants.DefaultButtonWidth, GameConstants.DefaultButtonHeight);
            TargetExitRect = new Rectangle(300, 65, GameConstants.DefaultButtonWidth, GameConstants.DefaultButtonHeight);
            TargetEntranceRect = new Rectangle(440, 65, GameConstants.DefaultButtonWidth, GameConstants.DefaultButtonHeight);
            TargetFocusRect = new Rectangle(580, 65, GameConstants.DefaultButtonWidth, GameConstants.DefaultButtonHeight);
        }

        public bool IsPointInBar(Point p) => uiBarRect.Contains(p);

        // Draws a button rect filled with the given color, with its label auto-centered using the
        // font's actual measured size - avoids hand-tuned pixel offsets that break when button size
        // or label text changes.
        private void DrawButton(SpriteBatch spriteBatch, Rectangle rect, string label, Color color)
        {
            spriteBatch.Draw(pixel, rect, color);

            Vector2 textSize = uiFont.MeasureString(label);
            Vector2 textPos = new Vector2(
                rect.X + (rect.Width - textSize.X) / 2f,
                rect.Y + (rect.Height - textSize.Y) / 2f + 4f);

            spriteBatch.DrawString(uiFont, label, textPos, Color.White);
        }
        public void Update(MouseState mouseState, MouseState previousMouseState, KeyboardState keyboardState, KeyboardState previousKeyboardState, Tower selectedTower)
        {
            bool clickedThisFrame = mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released;
            bool clickedBuildButton = clickedThisFrame && buildButtonRect.Contains(mouseState.Position);
            bool clickedInBar = clickedThisFrame && uiBarRect.Contains(mouseState.Position);
            bool clickedPauseButton = clickedThisFrame && pauseButtonRect.Contains(mouseState.Position);

            PauseButtonClicked = clickedPauseButton;
            

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


        public void Draw(SpriteBatch spriteBatch, Tower selectedTower, Enemy selectedEnemy, bool isPaused)
        {
            spriteBatch.Draw(pixel, uiBarRect, Color.DarkSlateGray);

            DrawButton(spriteBatch, buildButtonRect, "TOWER", (IsBuildMode || ShowTowerDropdown) ? Color.LimeGreen : Color.DarkGray);
            DrawButton(spriteBatch, pauseButtonRect, "PAUSE", isPaused ? Color.LimeGreen : Color.DarkGray);

            if (ShowTowerDropdown)
            {
                DrawButton(spriteBatch, towerOptionBasicRect, "BASIC", Color.DarkGray);
                DrawButton(spriteBatch, towerOptionSniperRect, "SNIPER", Color.DarkGray);
            }

            if (selectedTower != null)
            {
                DrawButton(spriteBatch, TargetClosestRect, "CLOSEST", selectedTower.TargetingMode == TargetingMode.ClosestToTower ? Color.LimeGreen : Color.DarkGray);
                DrawButton(spriteBatch, TargetMostHealthRect, "MOST HP", selectedTower.TargetingMode == TargetingMode.MostHealth ? Color.LimeGreen : Color.DarkGray);
                DrawButton(spriteBatch, TargetLeastHealthRect, "LEAST HP", selectedTower.TargetingMode == TargetingMode.LeastHealth ? Color.LimeGreen : Color.DarkGray);
                DrawButton(spriteBatch, TargetExitRect, "EXIT", selectedTower.TargetingMode == TargetingMode.ClosestToExit ? Color.LimeGreen : Color.DarkGray);
                DrawButton(spriteBatch, TargetEntranceRect, "ENTRANCE", selectedTower.TargetingMode == TargetingMode.ClosestToEntrance ? Color.LimeGreen : Color.DarkGray);
                DrawButton(spriteBatch, TargetFocusRect, "FOCUS", selectedTower.TargetingMode == TargetingMode.Focus ? Color.LimeGreen : Color.DarkGray);

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
            else if (selectedEnemy != null)
            {
                Vector2 statsOrigin = new Vector2(750, 10);
                int lineHeight = 22;
                int valueColumnX = 150;

                string[] nameParts = selectedEnemy.Name.Split(' ');
                string firstName = nameParts[0];
                string surname = nameParts.Length > 1 ? nameParts[1] : "";

                const int nameGapPixels = 14;
                spriteBatch.DrawString(uiFont, "Name:", statsOrigin, Color.White);

                Vector2 firstNamePos = statsOrigin + new Vector2(valueColumnX, 0);
                spriteBatch.DrawString(uiFont, firstName, firstNamePos, Color.White);

                float firstNameWidth = uiFont.MeasureString(firstName).X;
                Vector2 surnamePos = firstNamePos + new Vector2(firstNameWidth + nameGapPixels, 0);
                spriteBatch.DrawString(uiFont, surname, surnamePos, Color.White);

                string[] labels = { "Max HP:", "Current HP:", "Speed:" };
                string[] values =
                {
                    selectedEnemy.MaxHealth.ToString(),
                    selectedEnemy.Health.ToString(),
                    selectedEnemy.Speed.ToString()
                };

                for (int i = 0; i < labels.Length; i++)
                {
                    Vector2 rowPos = statsOrigin + new Vector2(0, (i + 1) * lineHeight);
                    spriteBatch.DrawString(uiFont, labels[i], rowPos, Color.White);
                    spriteBatch.DrawString(uiFont, values[i], rowPos + new Vector2(valueColumnX, 0), Color.White);
                }
            }
        }
    }
}