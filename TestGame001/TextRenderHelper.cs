using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TestGame001
{
    // Draws a string word-by-word with an explicit pixel gap between words, instead of relying
    // on the space character's own glyph width - MonoGame's compiled SpriteFont gives space an
    // unreliably narrow advance even on a monospace source font, so multi-word strings drawn
    // normally end up looking squished together.
    public static class TextRenderHelper
    {
        public static void DrawSpacedString(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, Color color, float wordGapPixels = 8f)
        {
            string[] words = text.Split(' ');
            Vector2 cursor = position;

            foreach (string word in words)
            {
                spriteBatch.DrawString(font, word, cursor, color);
                float wordWidth = font.MeasureString(word).X;
                cursor.X += wordWidth + wordGapPixels;
            }
        }

        // Same as above, but returns the total rendered width without drawing anything - useful
        // for centering a spaced string before you know its final draw position.
        public static float MeasureSpacedString(SpriteFont font, string text, float wordGapPixels = 8f)
        {
            string[] words = text.Split(' ');
            float totalWidth = 0f;

            for (int i = 0; i < words.Length; i++)
            {
                totalWidth += font.MeasureString(words[i]).X;
                if (i < words.Length - 1)
                {
                    totalWidth += wordGapPixels;
                }
            }

            return totalWidth;
        }
    }
}