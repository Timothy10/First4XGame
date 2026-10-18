using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace First4XGame;

public class First4XGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    
    // Textures for our terrain types
    private Texture2D _grasslandTexture;
    private Texture2D _plainsTexture;
    private Texture2D _waterTexture;
    private Texture2D _deepWaterTexture;
    private Texture2D _grasslandHillsTexture;
    private Texture2D _plainsHillsTexture;
    private Texture2D _grasslandMountainsTexture;
    private Texture2D _plainsMountainsTexture;

    // Standard 2:1 isometric tile dimensions (in pixels)
    private const int TileWidth = 64;
    private const int TileHeight = 32;

    // Grid size (40x40 map)
    private const int GridWidth = 40;
    private const int GridHeight = 40;

    // Expanded Enum matching your 8 terrain and elevation tiers
    private enum TerrainType
    {
        DeepWater,          // Below sea level
        Water,              // Sea level
        Grassland,          // Sea level (wet)
        Plains,             // Sea level (dry)
        GrasslandHills,     // High (wet)
        PlainsHills,        // High (dry)
        GrasslandMountains, // Highest (wet)
        PlainsMountains     // Highest (dry)
    }

    // 2D array to hold the map data
    private TerrainType[,] _mapGrid;

    // Screen offset to center/pan the grid view
    private Vector2 _screenOffset = new Vector2(640, 50);

    // Mouse state tracking for click-to-center
    private MouseState _previousMouseState;

    public First4XGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();

        // Generate map using the two-layer elevation and moisture model (unchanged)
        GenerateTwoLayerMap();

        base.Initialize();
    }

    private void GenerateTwoLayerMap()
    {
        _mapGrid = new TerrainType[GridWidth, GridHeight];

        // Frequencies for our two independent noise layers
        float elevationFreq = 0.08f;
        float moistureFreq = 0.15f;

        for (int q = 0; q < GridWidth; q++)
        {
            for (int r = 0; r < GridHeight; r++)
            {
                // Layer 1: Elevation (determines depth, sea level, hills, and mountains)
                float elevation = GetSmoothNoise(q * elevationFreq, r * elevationFreq, seed: 0.0f);

                // Layer 2: Moisture (determines grassland vs plains climate bands)
                float moisture = GetSmoothNoise(q * moistureFreq, r * moistureFreq, seed: 50.0f);

                // Multi-tier elevation mapping
                if (elevation < -0.35f)
                {
                    _mapGrid[q, r] = TerrainType.DeepWater;
                }
                else if (elevation < -0.1f)
                {
                    _mapGrid[q, r] = TerrainType.Water;
                }
                else if (elevation < 0.3f)
                {
                    // Sea Level / Lowlands
                    _mapGrid[q, r] = (moisture < 0.0f) ? TerrainType.Plains : TerrainType.Grassland;
                }
                else if (elevation < 0.65f)
                {
                    // High Elevation / Hills
                    _mapGrid[q, r] = (moisture < 0.0f) ? TerrainType.PlainsHills : TerrainType.GrasslandHills;
                }
                else
                {
                    // Highest Elevation / Mountains
                    _mapGrid[q, r] = (moisture < 0.0f) ? TerrainType.PlainsMountains : TerrainType.GrasslandMountains;
                }
            }
        }
    }

    // Smooth noise function with a seed offset to vary separate layers
    private float GetSmoothNoise(float x, float y, float seed)
    {
        float px = x + seed;
        float py = y + seed;

        float n1 = (float)(Math.Sin(px) * Math.Cos(py));
        float n2 = (float)(Math.Sin(px * 0.5f + 1.0f) * Math.Cos(py * 0.5f + 1.0f));
        float n3 = (float)(Math.Sin(px * 2.0f) * Math.Sin(py * 2.0f) * 0.25f);
        
        return (n1 + n2 + n3) / 2.25f;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Load core assets currently available in your MGCB pipeline
        _grasslandTexture = Content.Load<Texture2D>("maptiles/grassland");
        _plainsTexture = Content.Load<Texture2D>("maptiles/plains");
        _waterTexture = Content.Load<Texture2D>("maptiles/water");
        _deepWaterTexture = Content.Load<Texture2D>("maptiles/deep water");
        _grasslandHillsTexture = Content.Load<Texture2D>("maptiles/grassland hill");
        _plainsHillsTexture = Content.Load<Texture2D>("maptiles/plains hill");
        _grasslandMountainsTexture = Content.Load<Texture2D>("maptiles/grassland mountain");
        _plainsMountainsTexture = Content.Load<Texture2D>("maptiles/plains mountain");
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // Handle click-to-recenter
        MouseState currentMouseState = Mouse.GetState();
        if (currentMouseState.LeftButton == ButtonState.Pressed && 
            _previousMouseState.LeftButton == ButtonState.Released)
        {
            // Find the center of the screen
            Vector2 screenCenter = new Vector2(_graphics.PreferredBackBufferWidth / 2f, _graphics.PreferredBackBufferHeight / 2f);
            Vector2 mouseClickPos = new Vector2(currentMouseState.X, currentMouseState.Y);

            // Adjust the screen offset so the clicked point moves to the center of the screen
            _screenOffset += (screenCenter - mouseClickPos);
        }
        _previousMouseState = currentMouseState;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(25, 25, 35)); // Dark background

        _spriteBatch.Begin();

        // Loop through 2D grid coordinates (Q, R)
        for (int q = 0; q < GridWidth; q++)
        {
            for (int r = 0; r < GridHeight; r++)
            {
                // Core 2:1 Isometric Projection Math
                float xScreen = (q - r) * (TileWidth / 2f) + _screenOffset.X;
                float yScreen = (q + r) * (TileHeight / 2f) + _screenOffset.Y;

                // Select the texture based on terrain type
                Texture2D currentTexture = _mapGrid[q, r] switch
                {
                    TerrainType.DeepWater => _deepWaterTexture,
                    TerrainType.Water => _waterTexture,
                    TerrainType.Grassland => _grasslandTexture,
                    TerrainType.Plains => _plainsTexture,
                    TerrainType.GrasslandHills => _grasslandHillsTexture,
                    TerrainType.PlainsHills => _plainsHillsTexture,
                    TerrainType.GrasslandMountains => _grasslandMountainsTexture,
                    TerrainType.PlainsMountains => _plainsMountainsTexture,
                    _ => _grasslandTexture
                };

                _spriteBatch.Draw(currentTexture, new Vector2(xScreen, yScreen), Color.White);
            }
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}