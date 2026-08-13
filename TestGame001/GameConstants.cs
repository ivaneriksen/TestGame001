namespace TestGame001
{
    // Central place for values that are shared across the game (grid size, screen dimensions,
    // UI layout) so resolution/layout changes only need to happen in one spot.
    public static class GameConstants
    {
        // Size, in pixels, of one square grid tile (map tiles, tower/enemy footprints all align to this).
        public const int GridSize = 32;

        // Game window dimensions.
        public const int ScreenWidth = 1920;
        public const int ScreenHeight = 1080;

        // Height, in pixels, of the UI bar docked to the top of the screen.
        // Must stay a multiple of GridSize, or the playable grid will misalign with the visual grid
        // (this caused enemies to render partially off the path when it wasn't).
        public const int UIBarHeight = 128;

        // Y coordinate where the playable map area begins (i.e. just below the UI bar).
        public const int PlayableAreaTop = UIBarHeight;

        public const int DefaultButtonWidth = 120;
        public const int DefaultButtonHeight = 40;
    }
}