using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestGame001
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        List<Tower> towers = new List<Tower>();
        Texture2D mapTexture;
        Texture2D towerTexture;
        Texture2D enemyTexture;

        Texture2D rangeTexture;


        int gridSize = 32;

        List<Vector2> path = new List<Vector2>();
        List<Enemy> enemies = new List<Enemy>();
        float spawnTimer = 0f;

        float shootTimer = 0f;

        Texture2D pixel;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();

            path.Add(new Vector2(-32, 128));   // Start
            path.Add(new Vector2(320, 128)); // First turn
            path.Add(new Vector2(320, 480)); // Second turn
            path.Add(new Vector2(1280, 480)); // Exit

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: make these 64x64 instead of 32x32???
            towerTexture = Content.Load<Texture2D>("tower");
            enemyTexture = Content.Load<Texture2D>("enemy");
            mapTexture = Content.Load<Texture2D>("map_tile");

            rangeTexture = Content.Load<Texture2D>("range_circle2");

            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var mouseState = Mouse.GetState();

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                // 1. Calculate the grid cell (e.g., mouse at 70 / 64 = 1)
                int gridX = mouseState.X / gridSize;
                int gridY = mouseState.Y / gridSize;

                // 2. Snap back to pixels (e.g., 1 * 64 = 64)
                Vector2 snappedPosition = new Vector2(gridX * gridSize, gridY * gridSize);

                // 3. Check if a tower already exists here (Optional but recommended)
                if (!towers.Any(t => t.Position == snappedPosition))
                {
                    towers.Add(new Tower(snappedPosition));
                }
            }
            shootTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (shootTimer >= 0.5f) // Shoot every half-second
            {
                foreach (var tower in towers)
                {
                    foreach (var enemy in enemies)
                    {
                        // Calculate distance between tower and enemy
                        float distance = Vector2.Distance(tower.Position, enemy.Position);

                        if (distance <= tower.Range && enemy.IsActive)
                        {
                            enemy.Health -= 10; // Deal damage
                            if (enemy.Health <= 0) enemy.IsActive = false;

                            // Only shoot one enemy at a time
                            break;
                        }
                    }
                }
                shootTimer = 0f;
            }

            // Clean up dead enemies
            enemies.RemoveAll(e => !e.IsActive);

            // 1. Spawn logic (simple timer)
            spawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (spawnTimer > 2.0f)
            { // Every 2 seconds
                enemies.Add(new Enemy(path[0]));
                spawnTimer = 0;
            }

            // 2. Movement logic
            foreach (var enemy in enemies)
            {
                if (enemy.CurrentWaypointIndex < path.Count)
                {
                    Vector2 target = path[enemy.CurrentWaypointIndex];
                    Vector2 direction = target - enemy.Position;

                    if (direction.Length() < enemy.Speed)
                    {
                        enemy.CurrentWaypointIndex++; // Reached waypoint
                    }
                    else
                    {
                        direction.Normalize();
                        enemy.Position += direction * enemy.Speed;
                    }
                }
            }


            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            // 1. Draw Map Tiles
            for (int x = 0; x < GraphicsDevice.Viewport.Width; x += gridSize)
            {
                for (int y = 0; y < GraphicsDevice.Viewport.Height; y += gridSize)
                {
                    _spriteBatch.Draw(mapTexture, new Vector2(x, y), Color.White);
                }
            }

            // 2. Draw Enemies (Just once!)
            foreach (var enemy in enemies)
            {
                Color enemyColor = enemy.Health < 50 ? Color.Orange : Color.White;
                _spriteBatch.Draw(enemyTexture, enemy.Position, enemyColor);
            }

            // 3. Draw Towers and their Range Circles
            foreach (var tower in towers)
            {
                // Draw the range circle FIRST so it sits "under" the tower
                Vector2 centerOfTower = new Vector2(tower.Position.X + gridSize / 2, tower.Position.Y + gridSize / 2);

                // We create a square rectangle based on the range
                // If range is 150, the box is 300x300
                int diameter = (int)tower.Range * 2;
                Rectangle rangeRect = new Rectangle(
                    (int)centerOfTower.X,
                    (int)centerOfTower.Y,
                    diameter,
                    diameter
                );

                _spriteBatch.Draw(
                    rangeTexture,
                    rangeRect,
                    null,
                    Color.White * 0.3f,
                    0f,
                    new Vector2(rangeTexture.Width / 2, rangeTexture.Height / 2), // Center origin
                    SpriteEffects.None,
                    0f
                );

                // Draw the tower on top
                _spriteBatch.Draw(towerTexture, tower.Position, Color.White);
            }

            var mState = Mouse.GetState();

            // 1. Calculate the snapped grid position for the preview
            int previewX = (mState.X / gridSize) * gridSize;
            int previewY = (mState.Y / gridSize) * gridSize;
            Vector2 previewPos = new Vector2(previewX, previewY);
            Vector2 previewCenter = new Vector2(previewX + gridSize / 2, previewY + gridSize / 2);

            // 2. Draw a "ghost" tower so you see where it will land
            _spriteBatch.Draw(towerTexture, previewPos, Color.White * 0.5f);

            // 3. Draw the range circle for the preview
            // (Using 150 as the range; make sure this matches your Tower class)
            float previewDiameter = Tower.DefaultRange * 2;
            Rectangle previewRect = new Rectangle(
                (int)previewCenter.X,
                (int)previewCenter.Y,
                (int)previewDiameter,
                (int)previewDiameter
            );

            _spriteBatch.Draw(
                rangeTexture,
                previewRect,
                null,
                Color.Green * 0.2f, // Use Green so the player knows it's a placement preview
                0f,
                new Vector2(rangeTexture.Width / 2, rangeTexture.Height / 2),
                SpriteEffects.None,
                0f
            );

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
