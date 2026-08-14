using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TestGame001
{
    public class GameStateManager
    {
        public bool IsPaused { get; private set; }
        private bool isUpgradeChoiceActive; // true while the 3-card offer is showing

        public void Update(KeyboardState keyboardState, KeyboardState previousKeyboardState, bool pauseButtonClicked)
        {
            if (isUpgradeChoiceActive) return; // ignore manual toggle entirely during a card choice

            bool toggleTriggered = (keyboardState.IsKeyDown(Keys.Space) && !previousKeyboardState.IsKeyDown(Keys.Space)) || pauseButtonClicked;
            if (toggleTriggered)
            {
                IsPaused = !IsPaused;
            }
        }

        // Called by the (future) upgrade system when the 3-card offer appears.
        public void BeginUpgradeChoice()
        {
            isUpgradeChoiceActive = true;
            IsPaused = true;
        }

        // Called by the (future) upgrade system once the player picks a card.
        public void EndUpgradeChoice()
        {
            isUpgradeChoiceActive = false;
            IsPaused = false;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Rectangle screenBounds)
        {
            if (!IsPaused) return;

            spriteBatch.Draw(pixel, screenBounds, Color.Black * 0.5f);

            string text = "PAUSED";
            Vector2 textSize = font.MeasureString(text);
            Vector2 textPos = new Vector2(
                screenBounds.Width / 2f - textSize.X / 2f,
                screenBounds.Height / 2f - textSize.Y / 2f);

            spriteBatch.DrawString(font, text, textPos, Color.White);
        }
    }
}