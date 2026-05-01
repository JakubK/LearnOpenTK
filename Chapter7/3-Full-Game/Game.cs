using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KeyboardKeys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

namespace LearnOpenTK;

public class Game
{
    public static readonly Vector2 PlayerSize = new (100, 20);
    public static float PlayerVelocity = 100000f;
    public static readonly Vector2 InitialBallVelocity = new (100f, -350f);
    public static float BallRadius = 12.5f;

    public GameState State;
    public int Width;
    public int Height;

    public List<GameLevel> Levels = new();
    public List<PowerUp> PowerUps = new();

    public int Level;
    public int Lives;

    private SpriteRenderer renderer;
    private GameObject player;
    private BallObject ball;
    private ParticleGenerator particles;
    private PostProcessor effects;
    private TextRenderer text;

    private float shakeTime;
    

    public Game(int width, int height)
    {
        Width = width;
        Height = height;
        State = GameState.GameMenu;
    }
    
    
    public void Init()
    {
        // load shaders
        ResourceManager.LoadShader("Shaders/sprite.vs", "Shaders/sprite.fs", null, "sprite");
        ResourceManager.LoadShader("Shaders/particle.vs", "Shaders/particle.fs", null, "particle");
        ResourceManager.LoadShader("Shaders/post_processing.vs", "Shaders/post_processing.fs", null, "postprocessing");
        
        // configure shaders
        var projection = Matrix4.CreateOrthographicOffCenter(
            0.0f, Width,
            0.0f, Height,
            -1.0f, 1.0f
        );
        
        ResourceManager.GetShader("sprite").Use().SetInt("sprite", 0);
        ResourceManager.GetShader("sprite").SetMatrix4("projection", projection);
        
        ResourceManager.GetShader("particle").Use().SetInt("sprite", 0);
        ResourceManager.GetShader("particle").SetMatrix4("projection", projection);
        
        // load textures
        ResourceManager.LoadTexture("Resources/background.jpg", false, "background");
        ResourceManager.LoadTexture("Resources/awesomeface.png", true, "face");
        ResourceManager.LoadTexture("Resources/block.png", false, "block");
        ResourceManager.LoadTexture("Resources/block_solid.png", false, "block_solid");
        ResourceManager.LoadTexture("Resources/paddle.png", true, "paddle");
        ResourceManager.LoadTexture("Resources/particle.png", true, "particle");
        ResourceManager.LoadTexture("Resources/powerup_speed.png", true, "powerup_speed");
        ResourceManager.LoadTexture("Resources/powerup_sticky.png", true, "powerup_sticky");
        ResourceManager.LoadTexture("Resources/powerup_increase.png", true, "powerup_increase");
        ResourceManager.LoadTexture("Resources/powerup_confuse.png", true, "powerup_confuse");
        ResourceManager.LoadTexture("Resources/powerup_chaos.png", true, "powerup_chaos");
        ResourceManager.LoadTexture("Resources/powerup_passthrough.png", true, "powerup_passthrough");
        
        // set render-specific controls
        renderer = new SpriteRenderer(ResourceManager.GetShader("sprite"));
        particles = new ParticleGenerator(ResourceManager.GetShader("particle"), ResourceManager.GetTexture("particle"),
            500);
        
        effects = new PostProcessor(ResourceManager.GetShader("postprocessing"), Width, Height);
        text = new TextRenderer(Width, Height);
        text.Load("Resources/OCRAEXT.ttf", 24);
        
        // load levels
        var one = new GameLevel();
        one.Load("Levels/one.lvl", Width, Height / 2);
        
        var two = new GameLevel();
        two.Load("Levels/two.lvl", Width, Height / 2);
        
        var three = new GameLevel();
        three.Load("Levels/three.lvl", Width, Height / 2);

        var four = new GameLevel();
        four.Load("Levels/four.lvl", Width, Height / 2);
        
        Levels.AddRange(one, two, three, four);
        Level = 0;

        var playerPos = new Vector2(Width / 2f - PlayerSize.X / 2f, Height - (PlayerSize.Y * 4));
        player = new GameObject(playerPos, PlayerSize, new (PlayerVelocity, PlayerVelocity), Vector3.One,
            ResourceManager.GetTexture("paddle"));
        player.Name = "player";

        var ballPos = playerPos + new Vector2(PlayerSize.X / 2f - BallRadius, -BallRadius * 4f);
        ball = new BallObject(ballPos, BallRadius, InitialBallVelocity, ResourceManager.GetTexture("face"));
    }

    public void ProcessInput(float dt, KeyboardKeyEventArgs e)
    {
        if (State == GameState.GameMenu)
        {
            if (e.Key == KeyboardKeys.Enter)
            {
                State = GameState.GameActive;
                ResetLevel();
                ResetPlayer();
            }
            
            if (e.Key == KeyboardKeys.W)
            {
                Level = (Level + 1) % 4;
            }
            if (e.Key == KeyboardKeys.S)
            {
                if (Level > 0)
                    --Level;
                else
                    Level = 3;
            }
        }

        if (State == GameState.GameWin)
        {
            if (e.Key == KeyboardKeys.Enter)
            {
                effects.Chaos = false;
                State = GameState.GameActive;
            }
        }
        
        if (State == GameState.GameActive)
        {
            var velocity = dt * PlayerVelocity;
            
            // move playerboard
            if (e.Key == KeyboardKeys.A)
            {
                if (player.Position.X >= 0)
                {
                    player.Position.X -= velocity;
                    if (ball.Stuck)
                    {
                        ball.Position.X -= velocity;
                    }
                }
            }
            
            if (e.Key == KeyboardKeys.D)
            {
                if (player.Position.X <= Width - player.Size.X)
                {
                    player.Position.X += velocity;
                    if (ball.Stuck)
                    {
                        ball.Position.X += velocity;
                    }
                }
            }

            if (e.Key == KeyboardKeys.Space)
            {
                ball.Stuck = false;
            }
        }
    }

    public void Update(float dt)
    {
        ball.Move(dt, Width);
        
        DoCollisions();
        
        particles.Update(dt, ball, 2, new Vector2(ball.Radius / 2f));
        
        UpdatePowerUps(dt);

        if (shakeTime > 0)
        {
            shakeTime -= dt;
            if (shakeTime <= 0)
            {
                effects.Shake = false;
            }
        }

        if (ball.Position.Y > Height) // did ball reach bottom edge?
        {
            --Lives;
            if (Lives == 0)
            {
                ResetLevel();
                State = GameState.GameMenu;
            }
            ResetPlayer();
        }

        // check win condition
        if (State == GameState.GameActive && Levels[Level].IsCompleted())
        {
            ResetLevel();
            ResetPlayer();
            effects.Chaos = true;
            State = GameState.GameWin;
        }
    }

    public void Render()
    {
        Console.WriteLine(State);
        // begin rendering to postprocessing framebuffer
        effects.BeginRender();
            // draw background
            renderer.DrawSprite(ResourceManager.GetTexture("background"), Vector2.One, new (Width, Height), 0.0f, Vector3.One);
            // draw level
            Levels[Level].Draw(renderer);
            // draw player
            player.Draw(renderer);
            // draw PowerUps
            foreach (var powerUp in PowerUps)
            {
                if (!powerUp.IsDestroyed)
                    powerUp.Draw(renderer);
            }
            
            // draw particles	
            particles.Draw();
            // draw ball
            ball.Draw(renderer);
        // end rendering to postprocessing framebuffer
        effects.EndRender();
        // render postprocessing quad
        effects.Render((float)GLFW.GetTime());
        
        // render text (don't include in postprocessing)
        text.RenderText("Lives:" + Lives, 5.0f, 5.0f, 1.0f, Vector3.One);
    }

    public void DoCollisions()
    {
        foreach (var box in Levels[Level].Bricks)
        {
            if (!box.IsDestroyed)
            {
                var collision = CheckCollision(ball, box);
                if (collision.Occured)
                {
                    if (!box.IsSolid)
                    {
                        box.IsDestroyed = true;
                        SpawnPowerUps(box);
                    }
                    else
                    {
                        shakeTime = 0.05f;
                        effects.Shake = true;
                    }
                    
                    // collision resolution
                    var dir = collision.Direction;
                    var diffVector = collision.DiffVector;

                    if (!(ball.PassThrough && !box.IsSolid)) // don't do collision resolution on non-solid bricks if pass-through is activated
                    {
                        if (dir == Direction.Left || dir == Direction.Right) // horizontal collision
                        {
                            ball.Velocity.X = -ball.Velocity.X; // reverse horizontal velocity
                            // relocate
                            var penetration = ball.Radius - MathF.Abs(diffVector.X);
                            if (dir == Direction.Left)
                                ball.Position.X += penetration; // move ball to right
                            else
                                ball.Position.X -= penetration; // move ball to left;   
                        }
                        else // vertical collision
                        {
                            ball.Velocity.Y = -ball.Velocity.Y; // reverse vertical velocity
                            // relocate
                            float penetration = ball.Radius - MathF.Abs(diffVector.Y);
                            if (dir == Direction.Up)
                                ball.Position.Y -= penetration; // move ball bback up
                            else
                                ball.Position.Y += penetration; // move ball back down
                        }
                    }
                }
                
            }
        }

        // also check collisions on PowerUps and if so, activate them
        foreach (var powerUp in PowerUps)
        {
            if (!powerUp.IsDestroyed)
            {
                // first check if powerup passed bottom edge, if so: keep as inactive and destroy
                if (powerUp.Position.Y >= this.Height)
                {
                    powerUp.IsDestroyed = true;
                }
                
                if (CheckCollision(player, powerUp))
                {	// collided with player, now activate powerup
                    ActivatePowerUp(powerUp);
                    powerUp.IsDestroyed = true;
                    powerUp.Activated = true;
                }
            }
        }
        
        // and finally check collisions for player pad (unless stuck)
        var result = CheckCollision(ball, player);
        if (!ball.Stuck && result.Occured)
        {
            // check where it hit the board, and change velocity based on where it hit the board
            var centerBoard = player.Position.X + player.Size.X / 2.0f;
            var distance = (ball.Position.X + ball.Radius) - centerBoard;
            var percentage = distance / (player.Size.X / 2.0f);
            // then move accordingly
            var strength = 2.0f;
            var oldVelocity = ball.Velocity;
            ball.Velocity.X = InitialBallVelocity.X * percentage * strength; 
            //Ball->Velocity.y = -Ball->Velocity.y;
            ball.Velocity = Vector2.Normalize(ball.Velocity) * oldVelocity.Length; // keep speed consistent over both axes (multiply by length of old velocity, so total strength is not changed)
            // fix sticky paddle
            ball.Velocity.Y = -1.0f * MathF.Abs(ball.Velocity.Y);

            // if Sticky powerup is activated, also stick ball to paddle once new velocity vectors were calculated
            ball.Stuck = ball.Sticky;
        }
    }

    public void ResetLevel()
    {
        if (Level == 0) 
            Levels[0].Load("Levels/one.lvl", Width, Height / 2);
        else if (Level == 1)
            Levels[1].Load("Levels/two.lvl", Width, Height / 2);
        else if (Level == 2)
            Levels[2].Load("Levels/three.lvl", Width, Height / 2);
        else if (Level == 3)
            Levels[3].Load("Levels/four.lvl", Width, Height / 2);

        Lives = 3;
    }

    public void ResetPlayer()
    {
        // reset player/ball stats
        player.Size = PlayerSize;
        player.Position = new Vector2(Width / 2f - PlayerSize.X / 2f, Height - (PlayerSize.Y * 4));
        ball.Reset(player.Position + new Vector2(PlayerSize.X / 2.0f - BallRadius, -(BallRadius * 4.0f)), InitialBallVelocity);
        // also disable all active powerups
        effects.Chaos = effects.Confuse = false;
        ball.PassThrough = ball.Sticky = false;
        player.Color = Vector3.One;
        ball.Color = Vector3.One;
    }

    public void SpawnPowerUps(GameObject block)
    {
        if (ShouldSpawn(75)) // 1 in 75 chance
            PowerUps.Add(new PowerUp("speed", new (0.5f, 0.5f, 1.0f), 0.0f, block.Position, ResourceManager.GetTexture("powerup_speed")));
        if (ShouldSpawn(75))
            PowerUps.Add(new PowerUp("sticky", new(1.0f, 0.5f, 1.0f), 20.0f, block.Position, ResourceManager.GetTexture("powerup_sticky")));
        if (ShouldSpawn(75))
            PowerUps.Add(new PowerUp("pass-through", new(0.5f, 1.0f, 0.5f), 10.0f, block.Position, ResourceManager.GetTexture("powerup_passthrough")));
        if (ShouldSpawn(75))
            PowerUps.Add(new PowerUp("pad-size-increase", new(1.0f, 0.6f, 0.4f), 0.0f, block.Position, ResourceManager.GetTexture("powerup_increase")));
        if (ShouldSpawn(15)) // Negative powerups should spawn more often
            PowerUps.Add(new PowerUp("confuse", new(1.0f, 0.3f, 0.3f), 15.0f, block.Position, ResourceManager.GetTexture("powerup_confuse")));
        if (ShouldSpawn(15))
            PowerUps.Add(new PowerUp("chaos", new(0.9f, 0.25f, 0.25f), 15.0f, block.Position, ResourceManager.GetTexture("powerup_chaos")));
    }

    public void UpdatePowerUps(float dt)
    {
        foreach (var powerUp in PowerUps)
        {
            powerUp.Position += powerUp.Velocity * dt;
            if (powerUp.Activated)
            {
                powerUp.Duration -= dt;
                if (powerUp.Duration <= 0.0f)
                {
                    // remove powerup from list (will later be removed)
                    powerUp.Activated = false;
                    // deactivate effects
                    if (powerUp.Type == "sticky")
                    {
                        if (!IsOtherPowerUpActive(PowerUps, "sticky"))
                        {	// only reset if no other PowerUp of type sticky is active
                            ball.Sticky = false;
                            player.Color = Vector3.One;
                        }
                    }
                    else if (powerUp.Type == "pass-through")
                    {
                        if (!IsOtherPowerUpActive(PowerUps, "pass-through"))
                        {	// only reset if no other PowerUp of type pass-through is active
                            ball.PassThrough = false;
                            ball.Color = Vector3.One;
                        }
                    }
                    else if (powerUp.Type == "confuse")
                    {
                        if (!IsOtherPowerUpActive(PowerUps, "confuse"))
                        {	// only reset if no other PowerUp of type confuse is active
                            effects.Confuse = false;
                        }
                    }
                    else if (powerUp.Type == "chaos")
                    {
                        if (!IsOtherPowerUpActive(PowerUps, "chaos"))
                        {	// only reset if no other PowerUp of type chaos is active
                            effects.Chaos = false;
                        }
                    }
                }
            }
        }

        // // Remove all PowerUps from vector that are destroyed AND !activated (thus either off the map or finished)
        // // Note we use a lambda expression to remove each PowerUp which is destroyed and not activated
        PowerUps = PowerUps.Where(x => !(x.IsDestroyed && !x.Activated)).ToList();
    }
    
    private bool CheckCollision(GameObject one, GameObject two) // AABB - AABB collision
    {
        // collision x-axis?
        bool collisionX = one.Position.X + one.Size.X >= two.Position.X &&
                          two.Position.X + two.Size.X >= one.Position.X;
        // collision y-axis?
        bool collisionY = one.Position.Y + one.Size.Y >= two.Position.Y &&
                          two.Position.Y + two.Size.Y >= one.Position.Y;
        // collision only if on both axes
        return collisionX && collisionY;
    }
    
    private Collision CheckCollision(BallObject one, GameObject two) // AABB - Circle collision
    {
        // get center point circle first 
        var center = new Vector2(one.Position.X + one.Radius, one.Position.Y + one.Radius);
        
        // calculate AABB info (center, half-extents)
        var aabb_half_extents = new Vector2(two.Size.X / 2.0f, two.Size.Y / 2.0f);
        var aabb_center = new Vector2(two.Position.X + aabb_half_extents.X, two.Position.Y + aabb_half_extents.Y);
        // get difference vector between both centers
        var difference = center - aabb_center;
        var clamped = new Vector2(MathHelper.Clamp(difference.X, -aabb_half_extents.X, aabb_half_extents.X), MathHelper.Clamp(difference.Y, -aabb_half_extents.Y, aabb_half_extents.Y));
        // now that we know the clamped values, add this to AABB_center and we get the value of box closest to circle
        var closest = aabb_center + clamped;
        // now retrieve vector between center circle and closest point AABB and check if length < radius
        difference = closest - center;

        if (difference.Length < one.Radius) // not <= since in that case a collision also occurs when object one exactly touches object two, which they are at the end of each collision resolution stage.
            return new Collision(true, VectorDirection(difference), difference);

        return new Collision(false, Direction.Up, Vector2.Zero);
    }
    
    // calculates which direction a vector is facing (N,E,S or W)
    private Direction VectorDirection(Vector2 target)
    {
        Vector2[] compass = {
            new(0.0f, 1.0f),	// up
            new(1.0f, 0.0f),	// right
            new(0.0f, -1.0f),	// down
            new(-1.0f, 0.0f)	// left
        };
        
        var max = 0.0f;
        var best = -1;
        for (int i = 0; i < 4; i++)
        {
            var dot = Vector2.Dot(Vector2.Normalize(target), compass[i]);
            if (best > max)
            {
                max = dot;
                best = i;
            }
        }
        return (Direction)best;
    }
    
    private bool ShouldSpawn(int chance)
    {
        var random = new Random().Next() % chance;
        return random == 0;
    }
    
    private bool IsOtherPowerUpActive(List<PowerUp> powerUpsToCheck, string type)
    {
        // Check if another PowerUp of the same type is still active
        // in which case we don't disable its effect (yet)
        foreach (var powerUp in powerUpsToCheck)
        {
            if (powerUp.Activated)
                if (powerUp.Type == type)
                    return true;
        }
        return false;
    }
    
    private void ActivatePowerUp(PowerUp powerUp)
    {
        if (powerUp.Type == "speed")
        {
            ball.Velocity = new (ball.Velocity.X * 1.2f, ball.Velocity.Y * 1.2f);
        }
        else if (powerUp.Type == "sticky")
        {
            ball.Sticky = true;
            player.Color = new Vector3(1.0f, 0.5f, 1.0f);
        }
        else if (powerUp.Type == "pass-through")
        {
            ball.PassThrough = true;
            ball.Color = new Vector3(1.0f, 0.5f, 0.5f);
        }
        else if (powerUp.Type == "pad-size-increase")
        {
            player.Size.X += 50;
        }
        else if (powerUp.Type == "confuse")
        {
            if (!effects.Chaos)
                effects.Confuse = true; // only activate if chaos wasn't already active
        }
        else if (powerUp.Type == "chaos")
        {
            if (!effects.Confuse)
                effects.Chaos = true;
        }
    }
}