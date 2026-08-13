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

        private readonly Random random = new Random();

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
        
        Tower selectedTower = null; // currently clicked/selected placed tower, or null if none selected
        Enemy selectedEnemy = null;






        // Shared tint for every range-indicator circle (selected tower, build preview, etc.) - change this
        // one value to restyle all of them at once.
        static readonly Color RangeIndicatorTint = Color.White * 0.4f;

        // UI bar (top of screen) and its buttons.
        UIManager uiManager;
        GameStateManager gameStateManager = new GameStateManager();

        Economy economy = new Economy(GameConstants.StartingGold);

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
            path.Add(SnapToGrid(new Vector2(1968, 720)));   // Exit

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

            uiManager = new UIManager(uiFont, pixel);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();

            bool clickedThisFrame = mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;

            uiManager.Update(mouseState, _previousMouseState, keyboardState, _previousKeyboardState, selectedTower);
            gameStateManager.Update(keyboardState, _previousKeyboardState, uiManager.PauseButtonClicked);

            if (mouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
            {
                uiManager.CancelBuildMode();
                selectedTower = null;
                selectedEnemy = null;
            }

            // Place a tower on click while in build mode.
            if (uiManager.IsBuildMode && clickedThisFrame && !uiManager.ConsumedClickThisFrame)
            {
                int gridX = mouseState.X / GameConstants.GridSize;
                int gridY = mouseState.Y / GameConstants.GridSize;
                Vector2 snappedPosition = new Vector2(gridX * GameConstants.GridSize, gridY * GameConstants.GridSize);

                Type selectedType = uiManager.SelectedTowerFactory(Vector2.Zero).GetType();

                if (!towers.Any(t => t.Position == snappedPosition) && IsBuildableTile(snappedPosition) && economy.CanAfford(selectedType))
                {
                    towers.Add(uiManager.SelectedTowerFactory(snappedPosition));
                    economy.PurchaseTower(selectedType);
                }
            }

            // Outside build mode and with the dropdown closed, clicking a placed tower selects it;
            // clicking empty space or an enemy elsewhere is handled below.
            if (!uiManager.IsBuildMode && !uiManager.ShowTowerDropdown && clickedThisFrame && !uiManager.IsPointInBar(mouseState.Position))
            {
                Tower clickedTower = towers.FirstOrDefault(t =>
                    new Rectangle((int)t.Position.X, (int)t.Position.Y, GameConstants.GridSize, GameConstants.GridSize)
                        .Contains(mouseState.Position));

                if (clickedTower != null)
                {
                    selectedTower = clickedTower;
                    selectedEnemy = null;
                }
                else
                {
                    Enemy clickedEnemy = enemies.FirstOrDefault(e =>
                    {
                        float hitboxWidth = enemyTexture.Width * GameConstants.EnemyScale;
                        float hitboxHeight = enemyTexture.Height * GameConstants.EnemyScale;
                        Vector2 center = e.Position + new Vector2(GameConstants.GridSize / 2f, GameConstants.GridSize / 2f);
                        Rectangle hitbox = new Rectangle(
                            (int)(center.X - hitboxWidth / 2f),
                            (int)(center.Y - hitboxHeight / 2f),
                            (int)hitboxWidth, (int)hitboxHeight);
                        return hitbox.Contains(mouseState.Position);
                    });

                    if (clickedEnemy != null)
                    {
                        selectedEnemy = clickedEnemy;
                        selectedTower = null;
                    }
                }
            }

            _previousMouseState = mouseState;
            _previousKeyboardState = keyboardState;

            if (!gameStateManager.IsPaused)
            {
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
                        if (hitEnemy.Health <= 0)
                        {
                            hitEnemy.IsActive = false;
                            economy.AddGold(hitEnemy.GoldValue);
                        }
                    }
                }
                bullets.RemoveAll(b => !b.IsActive);
                enemies.RemoveAll(e => !e.IsActive);

                if (selectedEnemy != null && !selectedEnemy.IsActive)
                {
                    selectedEnemy = null;
                }

                // Enemy spawn timer.
                spawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (spawnTimer > 2.0f)
                {
                    var newEnemy = new BasicEnemy(path[0], enemyTexture);
                    newEnemy.CurrentTargetPoint = GetRandomPointInWaypointCircle(path[0], GameConstants.WaypointRadius);
                    enemies.Add(newEnemy);
                    spawnTimer = 0;
                }

                // Enemy movement along the path.
                foreach (var enemy in enemies)
                {
                    if (enemy.CurrentWaypointIndex < path.Count)
                    {
                        Vector2 direction = enemy.CurrentTargetPoint - enemy.Position;

                        if (direction.Length() < enemy.Speed)
                        {
                            enemy.CurrentWaypointIndex++;

                            if (enemy.CurrentWaypointIndex < path.Count)
                            {
                                enemy.CurrentTargetPoint = GetRandomPointInWaypointCircle(path[enemy.CurrentWaypointIndex], GameConstants.WaypointRadius);
                            }
                        }
                        else
                        {
                            direction.Normalize();
                            enemy.Position += direction * enemy.Speed;
                        }
                    }
                    else
                    {
                        enemy.IsActive = false;
                    }
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
            uiManager.Draw(_spriteBatch, selectedTower, selectedEnemy, gameStateManager.IsPaused, economy.Gold);
            gameStateManager.Draw(_spriteBatch, pixel, uiFont, GraphicsDevice.Viewport.Bounds);

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
                Vector2 origin = new Vector2(enemyTexture.Width / 2f, enemyTexture.Height / 2f);
                Vector2 drawCenter = enemy.Position + new Vector2(GameConstants.GridSize / 2f, GameConstants.GridSize / 2f);

                _spriteBatch.Draw(
                    enemyTexture,
                    drawCenter,
                    null,
                    Color.White,
                    0f,
                    origin,
                    GameConstants.EnemyScale,
                    SpriteEffects.None,
                    0f
                );

                // Health bar above the enemy - scaled to match the smaller sprite width.
                float healthPercent = enemy.Health / enemy.MaxHealth;
                int barWidth = (int)(GameConstants.GridSize * GameConstants.EnemyScale);
                int barHeight = 5;
                Vector2 barPos = new Vector2(
                    drawCenter.X - barWidth / 2f,
                    drawCenter.Y - (enemyTexture.Height * GameConstants.EnemyScale / 2f) - barHeight - 2);

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
            if (!uiManager.IsBuildMode || mState.Y < GameConstants.PlayableAreaTop) return;

            int previewX = (mState.X / GameConstants.GridSize) * GameConstants.GridSize;
            int previewY = (mState.Y / GameConstants.GridSize) * GameConstants.GridSize;
            Vector2 previewPos = new Vector2(previewX, previewY);
            Vector2 previewCenter = new Vector2(previewX + GameConstants.GridSize / 2, previewY + GameConstants.GridSize / 2);

            Tower selectedTowerType = uiManager.SelectedTowerFactory(Vector2.Zero);

            bool validPlacement = IsBuildableTile(previewPos) && !towers.Any(t => t.Position == previewPos);
            bool canAfford = economy.CanAfford(selectedTowerType.GetType());
            Color ghostTint = (validPlacement && canAfford) ? Color.White * 0.5f : Color.Red * 0.5f;
            _spriteBatch.Draw(towerTexture, previewPos, ghostTint);

            
            DrawRangeCircle(previewCenter, selectedTowerType.Range, RangeIndicatorTint);
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

        

        // --- Targeting ---

        // Picks which enemy a firing tower should aim at, based on its TargetingMode, among
        // enemies currently within range.
        private Enemy SelectTarget(Tower tower)
        {
            var inRange = enemies.Where(e =>
                e.IsActive && Vector2.Distance(tower.GetCenter(), e.GetCenter()) <= tower.Range);

            if (tower.FocusEnabled)
            {
                bool currentTargetStillValid = tower.CurrentTarget != null
                    && tower.CurrentTarget.IsActive
                    && Vector2.Distance(tower.GetCenter(), tower.CurrentTarget.GetCenter()) <= tower.Range;

                if (currentTargetStillValid)
                {
                    return tower.CurrentTarget;
                }
            }

            Enemy picked;
            switch (tower.TargetingMode)
            {
                case TargetingMode.MostHealth:
                    picked = inRange.OrderByDescending(e => e.Health).FirstOrDefault();
                    break;
                case TargetingMode.LeastHealth:
                    picked = inRange.OrderBy(e => e.Health).FirstOrDefault();
                    break;
                case TargetingMode.ClosestToExit:
                    picked = inRange.OrderBy(e => e.GetRemainingDistance(path)).FirstOrDefault();
                    break;
                case TargetingMode.ClosestToEntrance:
                    picked = inRange.OrderByDescending(e => e.GetRemainingDistance(path)).FirstOrDefault();
                    break;
                case TargetingMode.ClosestToTower:
                default:
                    picked = inRange.OrderBy(e => Vector2.Distance(tower.GetCenter(), e.GetCenter())).FirstOrDefault();
                    break;
            }
            tower.CurrentTarget = picked;
            return picked;
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

        private Vector2 GetRandomPointInWaypointCircle(Vector2 center, float radius)
        {
            double angle = random.NextDouble() * Math.PI * 2;
            double distance = random.NextDouble() * radius;
            return center + new Vector2(
                (float)(Math.Cos(angle) * distance),
                (float)(Math.Sin(angle) * distance));
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