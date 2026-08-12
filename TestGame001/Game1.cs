using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TestGame001
{
    public class Game1 : Game
    {
        // --- Core MonoGame / rendering ---
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // --- Map tile textures ---
        Texture2D grassMapTexture;
        Texture2D dirtMapTexture;
        Texture2D edgeNorthTexture, edgeSouthTexture, edgeEastTexture, edgeWestTexture;
        Texture2D cornerConcaveTexture; // "inner" turn - dirt wraps around this corner
        Texture2D cornerConvexTexture;  // "outer" turn - dirt pokes into this corner diagonally

        // Per-cell render info (which transition texture + rotation) for every non-path tile
        // adjacent to the path. Built once in GenerateMapTiles.
        private struct TileRenderInfo
        {
            public Texture2D Texture;
            public float Rotation;
        }
        Dictionary<Point, TileRenderInfo> mapTileLookup = new Dictionary<Point, TileRenderInfo>();

        // Every grid cell the dirt path itself passes through. Built once in GeneratePathTiles.
        HashSet<Point> pathTiles = new HashSet<Point>();

        // 1x1 white texture, stretched/tinted to draw solid-color UI rectangles (bars, buttons, health bars).
        Texture2D pixel;

        // --- Gameplay entity textures ---
        Texture2D towerTexture;
        Texture2D enemyTexture;
        Texture2D bulletTexture;
        Texture2D rangeTexture;

        // --- UI font ---
        SpriteFont uiFont;

        // --- Path & entities ---
        List<Vector2> path = new List<Vector2>();
        List<Tower> towers = new List<Tower>();
        List<Enemy> enemies = new List<Enemy>();
        List<Bullet> bullets = new List<Bullet>();
        float spawnTimer = 0f;

        // --- Input state (previous frame, for edge-detecting clicks/key presses) ---
        KeyboardState _previousKeyboardState = new KeyboardState();
        MouseState _previousMouseState = new MouseState();

        // --- UI state ---
        bool isBuildMode = false;
        Tower selectedTower = null; // currently clicked/selected placed tower, or null if none selected

        bool showTowerDropdown = false;
        Func<Vector2, Tower> selectedTowerFactory = pos => new BasicTower(pos);

        Rectangle towerOptionBasicRect = new Rectangle(20, 90, 160, 50);
        Rectangle towerOptionSniperRect = new Rectangle(20, 150, 160, 50);

        // Shared tint for every range-indicator circle (selected tower, build preview, etc.) - change this
        // one value to restyle all of them at once.
        static readonly Color RangeIndicatorTint = Color.White * 0.4f;

        // UI bar (top of screen) and its buttons.
        Rectangle uiBarRect = new Rectangle(0, 0, GameConstants.ScreenWidth, GameConstants.UIBarHeight);
        Rectangle buildButtonRect = new Rectangle(20, 15, 120, 60);
        Rectangle targetClosestRect = new Rectangle(300, 15, 120, 40);
        Rectangle targetLeastHealthRect = new Rectangle(440, 15, 120, 40);
        Rectangle targetMostHealthRect = new Rectangle(580, 15, 120, 40);
        Rectangle targetExitRect = new Rectangle(300, 65, 120, 40);

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = GameConstants.ScreenWidth;
            _graphics.PreferredBackBufferHeight = GameConstants.ScreenHeight;
            _graphics.ApplyChanges();

            // Path waypoints, snapped to the grid so the enemy sprite always sits fully within a
            // tile row/column instead of straddling two (this broke once before when a waypoint
            // landed on a non-grid-aligned pixel value).
            path.Add(SnapToGrid(new Vector2(-48, 192)));    // Start
            path.Add(SnapToGrid(new Vector2(480, 192)));
            path.Add(SnapToGrid(new Vector2(480, 320)));
            path.Add(SnapToGrid(new Vector2(180, 320)));
            path.Add(SnapToGrid(new Vector2(180, 720)));
            path.Add(SnapToGrid(new Vector2(1940, 720)));   // Exit

            GeneratePathTiles();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            uiFont = Content.Load<SpriteFont>("UIFont");

            towerTexture = Content.Load<Texture2D>("tower");
            enemyTexture = Content.Load<Texture2D>("enemy");
            bulletTexture = Content.Load<Texture2D>("bullet");
            rangeTexture = Content.Load<Texture2D>("range_circle2");

            grassMapTexture = Content.Load<Texture2D>("map_tile");
            dirtMapTexture = Content.Load<Texture2D>("dirt_map_tile");
            edgeNorthTexture = Content.Load<Texture2D>("dirt_north_grass_south");
            edgeSouthTexture = Content.Load<Texture2D>("dirt_south_grass_north");
            edgeEastTexture = Content.Load<Texture2D>("dirt_east_grass_west");
            edgeWestTexture = Content.Load<Texture2D>("dirt_west_grass_east");
            cornerConcaveTexture = Content.Load<Texture2D>("corner_grass_sw");
            cornerConvexTexture = Content.Load<Texture2D>("corner_dirt_sw");

            GenerateMapTiles();

            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();

            bool clickedThisFrame = mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;
            bool clickedBuildButton = clickedThisFrame && buildButtonRect.Contains(mouseState.Position);
            bool clickedInBar = clickedThisFrame && uiBarRect.Contains(mouseState.Position);

            bool buildToggleTriggered = (keyboardState.IsKeyDown(Keys.B) && !_previousKeyboardState.IsKeyDown(Keys.B)) || clickedBuildButton;
            if (buildToggleTriggered)
            {
                if (isBuildMode)
                {
                    // Already placing - toggle off entirely.
                    isBuildMode = false;
                    showTowerDropdown = false;
                }
                else
                {
                    // Not placing yet - open the tower type dropdown instead of placing immediately.
                    showTowerDropdown = !showTowerDropdown;
                }
            }

            // Right-click cancels build mode, the dropdown, and clears the selected tower's info panel.
            if (mouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
            {
                isBuildMode = false;
                showTowerDropdown = false;
                selectedTower = null;
            }

            // Picking a tower type from the dropdown commits to build mode for that type.
            bool dropdownOptionClicked = false;
            if (showTowerDropdown && clickedThisFrame)
            {
                if (towerOptionBasicRect.Contains(mouseState.Position))
                {
                    selectedTowerFactory = pos => new BasicTower(pos);
                    isBuildMode = true;
                    showTowerDropdown = false;
                    dropdownOptionClicked = true;
                }
                else if (towerOptionSniperRect.Contains(mouseState.Position))
                {
                    selectedTowerFactory = pos => new SniperTower(pos);
                    isBuildMode = true;
                    showTowerDropdown = false;
                    dropdownOptionClicked = true;
                }
            }

            // Place a tower on click while in build mode.
            if (isBuildMode && clickedThisFrame && !clickedBuildButton && !clickedInBar && !dropdownOptionClicked)
            {
                int gridX = mouseState.X / GameConstants.GridSize;
                int gridY = mouseState.Y / GameConstants.GridSize;
                Vector2 snappedPosition = new Vector2(gridX * GameConstants.GridSize, gridY * GameConstants.GridSize);

                if (!towers.Any(t => t.Position == snappedPosition) && IsBuildableTile(snappedPosition))
                {
                    towers.Add(selectedTowerFactory(snappedPosition));
                }
            }

            // Outside build mode and with the dropdown closed, clicking a placed tower selects it.
            if (!isBuildMode && !showTowerDropdown && clickedThisFrame && !clickedInBar)
            {
                selectedTower = towers.FirstOrDefault(t =>
                    new Rectangle((int)t.Position.X, (int)t.Position.Y, GameConstants.GridSize, GameConstants.GridSize)
                        .Contains(mouseState.Position));
            }

            // While a tower is selected, clicking a targeting button changes its targeting mode.
            if (selectedTower != null && clickedThisFrame)
            {
                if (targetClosestRect.Contains(mouseState.Position))
                    selectedTower.TargetingMode = TargetingMode.ClosestToTower;
                else if (targetMostHealthRect.Contains(mouseState.Position))
                    selectedTower.TargetingMode = TargetingMode.MostHealth;
                else if (targetLeastHealthRect.Contains(mouseState.Position))
                    selectedTower.TargetingMode = TargetingMode.LeastHealth;
                else if (targetExitRect.Contains(mouseState.Position))
                    selectedTower.TargetingMode = TargetingMode.ClosestToExit;
            }

            _previousMouseState = mouseState;
            _previousKeyboardState = keyboardState;

            // Tower firing: each tower on cooldown picks a target per its TargetingMode and fires.
            foreach (var tower in towers)
            {
                tower.TimeSinceLastShot += gameTime.ElapsedGameTime;
                if (tower.TimeSinceLastShot >= tower.Cooldown)
                {
                    Enemy target = SelectTarget(tower);
                    if (target != null)
                    {
                        bullets.Add(new Bullet(tower.GetCenter(), target.GetCenter(), tower.Damage, tower.BulletSpeed, tower.Range));
                        tower.TimeSinceLastShot = TimeSpan.Zero;
                    }
                }
            }

            // Bullet movement/collision.
            foreach (var bullet in bullets)
            {
                Enemy hitEnemy = bullet.Update(enemies, (float)gameTime.ElapsedGameTime.TotalSeconds);
                if (hitEnemy != null)
                {
                    hitEnemy.Health -= bullet.Damage;
                    if (hitEnemy.Health <= 0) hitEnemy.IsActive = false;
                }
            }
            bullets.RemoveAll(b => !b.IsActive);
            enemies.RemoveAll(e => !e.IsActive);

            // Enemy spawn timer.
            spawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (spawnTimer > 2.0f)
            {
                enemies.Add(new BasicEnemy(path[0], enemyTexture));
                spawnTimer = 0;
            }

            // Enemy movement along the path.
            foreach (var enemy in enemies)
            {
                if (enemy.CurrentWaypointIndex < path.Count)
                {
                    Vector2 target = path[enemy.CurrentWaypointIndex];
                    Vector2 direction = target - enemy.Position;

                    if (direction.Length() < enemy.Speed)
                    {
                        enemy.CurrentWaypointIndex++; // Reached this waypoint - advance to the next.
                    }
                    else
                    {
                        direction.Normalize();
                        enemy.Position += direction * enemy.Speed;
                    }
                }
                else
                {
                    enemy.IsActive = false; // Reached the end of the path - exited.
                }
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            DrawMapTiles();
            DrawEnemies();
            DrawBullets();
            DrawTowers();
            DrawBuildPreview();
            DrawUIBar();
            DrawTowerDropdown();

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        // --- Draw helpers ---

        private void DrawMapTiles()
        {
            for (int x = 0; x < GraphicsDevice.Viewport.Width; x += GameConstants.GridSize)
            {
                for (int y = GameConstants.PlayableAreaTop; y < GraphicsDevice.Viewport.Height; y += GameConstants.GridSize)
                {
                    int gridX = x / GameConstants.GridSize;
                    int gridY = y / GameConstants.GridSize;
                    Point cell = new Point(gridX, gridY);

                    _spriteBatch.Draw(grassMapTexture, new Vector2(x, y), Color.White);

                    if (pathTiles.Contains(cell))
                    {
                        _spriteBatch.Draw(dirtMapTexture, new Vector2(x, y), Color.White);
                    }
                    else if (mapTileLookup.TryGetValue(cell, out TileRenderInfo tileInfo))
                    {
                        Vector2 tileCenter = new Vector2(x + GameConstants.GridSize / 2f, y + GameConstants.GridSize / 2f);
                        _spriteBatch.Draw(
                            tileInfo.Texture,
                            tileCenter,
                            null,
                            Color.White,
                            tileInfo.Rotation,
                            new Vector2(tileInfo.Texture.Width / 2f, tileInfo.Texture.Height / 2f),
                            1f,
                            SpriteEffects.None,
                            0f
                        );
                    }
                }
            }
        }

        private void DrawEnemies()
        {
            foreach (var enemy in enemies)
            {
                _spriteBatch.Draw(enemyTexture, enemy.Position, Color.White);

                // Health bar above the enemy.
                float healthPercent = enemy.Health / enemy.MaxHealth;
                int barWidth = GameConstants.GridSize;
                int barHeight = 5;
                Vector2 barPos = enemy.Position + new Vector2(0, -barHeight - 2);

                _spriteBatch.Draw(pixel, new Rectangle((int)barPos.X, (int)barPos.Y, barWidth, barHeight), Color.DarkRed);
                _spriteBatch.Draw(pixel, new Rectangle((int)barPos.X, (int)barPos.Y, (int)(barWidth * healthPercent), barHeight), Color.LimeGreen);
            }
        }

        private void DrawBullets()
        {
            foreach (var bullet in bullets)
            {
                _spriteBatch.Draw(bulletTexture, bullet.Position, Color.Yellow);
            }
        }

        private void DrawTowers()
        {
            foreach (var tower in towers)
            {
                _spriteBatch.Draw(towerTexture, tower.Position, Color.White);
            }

            // Range circle for the currently selected tower only (not shown for every tower).
            if (selectedTower != null)
            {
                DrawRangeCircle(selectedTower.GetCenter(), selectedTower.Range, RangeIndicatorTint);
            }
        }

        private void DrawBuildPreview()
        {
            var mState = Mouse.GetState();
            if (!isBuildMode || mState.Y < GameConstants.PlayableAreaTop) return;

            int previewX = (mState.X / GameConstants.GridSize) * GameConstants.GridSize;
            int previewY = (mState.Y / GameConstants.GridSize) * GameConstants.GridSize;
            Vector2 previewPos = new Vector2(previewX, previewY);
            Vector2 previewCenter = new Vector2(previewX + GameConstants.GridSize / 2, previewY + GameConstants.GridSize / 2);

            bool validPlacement = IsBuildableTile(previewPos) && !towers.Any(t => t.Position == previewPos);
            Color ghostTint = validPlacement ? Color.White * 0.5f : Color.Red * 0.5f;
            _spriteBatch.Draw(towerTexture, previewPos, ghostTint);

            Tower selectedTowerType = selectedTowerFactory(Vector2.Zero);
            DrawRangeCircle(previewCenter, selectedTowerType.Range, RangeIndicatorTint);
        }

        private void DrawTowerDropdown()
        {
            if (!showTowerDropdown) return;

            _spriteBatch.Draw(pixel, towerOptionBasicRect, Color.DarkGray);
            _spriteBatch.DrawString(uiFont, "BASIC", new Vector2(towerOptionBasicRect.X + 15, towerOptionBasicRect.Y + 15), Color.White);

            _spriteBatch.Draw(pixel, towerOptionSniperRect, Color.DarkGray);
            _spriteBatch.DrawString(uiFont, "SNIPER", new Vector2(towerOptionSniperRect.X + 15, towerOptionSniperRect.Y + 15), Color.White);
        }

        // Shared helper for drawing a tower's range as a circle centered on a world position.
        private void DrawRangeCircle(Vector2 center, float range, Color tint)
        {
            int diameter = (int)range * 2;
            Rectangle rangeRect = new Rectangle((int)center.X, (int)center.Y, diameter, diameter);

            _spriteBatch.Draw(
                rangeTexture,
                rangeRect,
                null,
                tint,
                0f,
                new Vector2(rangeTexture.Width / 2, rangeTexture.Height / 2),
                SpriteEffects.None,
                0f
            );
        }

        private void DrawUIBar()
        {
            _spriteBatch.Draw(pixel, uiBarRect, Color.DarkSlateGray);

            Color buttonColor = (isBuildMode || showTowerDropdown) ? Color.LimeGreen : Color.DarkGray;
            _spriteBatch.Draw(pixel, buildButtonRect, buttonColor);
            _spriteBatch.DrawString(uiFont, "TOWER", new Vector2(buildButtonRect.X + 18, buildButtonRect.Y + 22), Color.White);

            if (selectedTower == null) return;

            // Targeting mode buttons - highlighted green when active.
            _spriteBatch.Draw(pixel, targetClosestRect, selectedTower.TargetingMode == TargetingMode.ClosestToTower ? Color.LimeGreen : Color.DarkGray);
            _spriteBatch.DrawString(uiFont, "CLOSEST", new Vector2(targetClosestRect.X + 8, targetClosestRect.Y + 10), Color.White);

            _spriteBatch.Draw(pixel, targetMostHealthRect, selectedTower.TargetingMode == TargetingMode.MostHealth ? Color.LimeGreen : Color.DarkGray);
            _spriteBatch.DrawString(uiFont, "MOST HP", new Vector2(targetMostHealthRect.X + 8, targetMostHealthRect.Y + 10), Color.White);

            _spriteBatch.Draw(pixel, targetLeastHealthRect, selectedTower.TargetingMode == TargetingMode.LeastHealth ? Color.LimeGreen : Color.DarkGray);
            _spriteBatch.DrawString(uiFont, "LEAST HP", new Vector2(targetLeastHealthRect.X + 8, targetLeastHealthRect.Y + 10), Color.White);

            _spriteBatch.Draw(pixel, targetExitRect, selectedTower.TargetingMode == TargetingMode.ClosestToExit ? Color.LimeGreen : Color.DarkGray);
            _spriteBatch.DrawString(uiFont, "EXIT", new Vector2(targetExitRect.X + 8, targetExitRect.Y + 10), Color.White);

            // Stats readout - labels and values drawn as separate fixed columns so numbers stay
            // aligned regardless of font metrics.
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
                _spriteBatch.DrawString(uiFont, labels[i], rowPos, Color.White);
                _spriteBatch.DrawString(uiFont, values[i], rowPos + new Vector2(valueColumnX, 0), Color.White);
            }
        }

        // --- Targeting ---

        // Picks which enemy a firing tower should aim at, based on its TargetingMode, among
        // enemies currently within range.
        private Enemy SelectTarget(Tower tower)
        {
            var inRange = enemies.Where(e =>
                e.IsActive && Vector2.Distance(tower.GetCenter(), e.GetCenter()) <= tower.Range);

            switch (tower.TargetingMode)
            {
                case TargetingMode.MostHealth:
                    return inRange.OrderByDescending(e => e.Health).FirstOrDefault();
                case TargetingMode.LeastHealth:
                    return inRange.OrderBy(e => e.Health).FirstOrDefault();
                case TargetingMode.ClosestToExit:
                    return inRange.OrderBy(e => e.GetRemainingDistance(path)).FirstOrDefault();
                case TargetingMode.ClosestToTower:
                default:
                    return inRange.OrderBy(e => Vector2.Distance(tower.GetCenter(), e.GetCenter())).FirstOrDefault();
            }
        }

        // --- Grid / path helpers ---

        // Rounds a world position to the nearest grid cell corner, so path waypoints always land
        // exactly on a tile boundary regardless of the raw pixel values used to define them.
        private Vector2 SnapToGrid(Vector2 v)
        {
            return new Vector2(
                (float)Math.Round(v.X / GameConstants.GridSize) * GameConstants.GridSize,
                (float)Math.Round(v.Y / GameConstants.GridSize) * GameConstants.GridSize
            );
        }

        // Walks every path segment and records every grid cell it passes through, into pathTiles.
        private void GeneratePathTiles()
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector2 start = path[i];
                Vector2 end = path[i + 1];
                Vector2 direction = end - start;
                float distance = direction.Length();
                if (distance == 0) continue;
                direction.Normalize();

                // Step along the segment in half-tile increments so no cell is skipped.
                float step = GameConstants.GridSize / 2f;
                for (float traveled = 0; traveled <= distance; traveled += step)
                {
                    Vector2 point = start + direction * traveled;
                    pathTiles.Add(GridCellOf(point));
                }

                pathTiles.Add(GridCellOf(end));
            }
        }
        // True if this grid cell is plain grass - not part of the path, and not one of the
        // grass/dirt transition tiles (edges/corners) bordering it.
        private bool IsBuildableTile(Vector2 snappedPosition)
        {
            Point cell = new Point(
                (int)(snappedPosition.X / GameConstants.GridSize),
                (int)(snappedPosition.Y / GameConstants.GridSize));

            return !pathTiles.Contains(cell) && !mapTileLookup.ContainsKey(cell);
        }
        // Converts a world position to its containing grid cell. Uses Math.Floor (not integer
        // division) so negative coordinates snap correctly.
        private Point GridCellOf(Vector2 worldPos)
        {
            int gx = (int)Math.Floor(worldPos.X / GameConstants.GridSize);
            int gy = (int)Math.Floor(worldPos.Y / GameConstants.GridSize);
            return new Point(gx, gy);
        }

        // Builds mapTileLookup: for every grass cell bordering the path, picks the correct
        // grass-to-dirt transition texture (straight edge or corner) and rotation based on which
        // of its neighbors are path tiles.
        private void GenerateMapTiles()
        {
            int gridWidth = GraphicsDevice.Viewport.Width / GameConstants.GridSize;
            int gridHeight = GraphicsDevice.Viewport.Height / GameConstants.GridSize;

            for (int gx = -1; gx <= gridWidth + 1; gx++)
            {
                for (int gy = -1; gy <= gridHeight + 1; gy++)
                {
                    Point cell = new Point(gx, gy);
                    if (pathTiles.Contains(cell)) continue; // handled as plain dirt in Draw

                    bool north = pathTiles.Contains(new Point(gx, gy - 1));
                    bool south = pathTiles.Contains(new Point(gx, gy + 1));
                    bool east = pathTiles.Contains(new Point(gx + 1, gy));
                    bool west = pathTiles.Contains(new Point(gx - 1, gy));

                    bool ne = pathTiles.Contains(new Point(gx + 1, gy - 1));
                    bool se = pathTiles.Contains(new Point(gx + 1, gy + 1));
                    bool sw = pathTiles.Contains(new Point(gx - 1, gy + 1));
                    bool nw = pathTiles.Contains(new Point(gx - 1, gy - 1));

                    int straightCount = (north ? 1 : 0) + (south ? 1 : 0) + (east ? 1 : 0) + (west ? 1 : 0);

                    if (straightCount == 1)
                    {
                        if (north) mapTileLookup[cell] = new TileRenderInfo { Texture = edgeNorthTexture, Rotation = 0f };
                        else if (south) mapTileLookup[cell] = new TileRenderInfo { Texture = edgeSouthTexture, Rotation = 0f };
                        else if (east) mapTileLookup[cell] = new TileRenderInfo { Texture = edgeEastTexture, Rotation = 0f };
                        else if (west) mapTileLookup[cell] = new TileRenderInfo { Texture = edgeWestTexture, Rotation = 0f };
                    }
                    else if (north && east)
                        mapTileLookup[cell] = new TileRenderInfo { Texture = cornerConcaveTexture, Rotation = MathHelper.ToRadians(0) };
                    else if (south && east)
                        mapTileLookup[cell] = new TileRenderInfo { Texture = cornerConcaveTexture, Rotation = MathHelper.ToRadians(90) };
                    else if (south && west)
                        mapTileLookup[cell] = new TileRenderInfo { Texture = cornerConcaveTexture, Rotation = MathHelper.ToRadians(180) };
                    else if (north && west)
                        mapTileLookup[cell] = new TileRenderInfo { Texture = cornerConcaveTexture, Rotation = MathHelper.ToRadians(270) };
                    else if (ne && !north && !east)
                        mapTileLookup[cell] = new TileRenderInfo { Texture = cornerConvexTexture, Rotation = MathHelper.ToRadians(0) };
                    else if (se && !south && !east)
                        mapTileLookup[cell] = new TileRenderInfo { Texture = cornerConvexTexture, Rotation = MathHelper.ToRadians(90) };
                    else if (sw && !south && !west)
                        mapTileLookup[cell] = new TileRenderInfo { Texture = cornerConvexTexture, Rotation = MathHelper.ToRadians(180) };
                    else if (nw && !north && !west)
                        mapTileLookup[cell] = new TileRenderInfo { Texture = cornerConvexTexture, Rotation = MathHelper.ToRadians(270) };
                }
            }
        }
    }
}