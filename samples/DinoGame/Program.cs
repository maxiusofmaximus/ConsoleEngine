using ConsoleEngine.Locale;
using ConsoleEngine.Rendering;
using ConsoleEngine.Scenes;
using DinoGame;

// ── Bootstrap ──────────────────────────────────────────────────────────────────
PixelArtRenderer.EnableAnsi();
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.CursorVisible  = false;
Console.Title          = "DinoGame — ConsoleEngine v0.2.0";

string gameDataRoot = Path.Combine(AppContext.BaseDirectory, "GameData");
CL.Initialize(gameDataRoot, "en");

// ── Intro scene ────────────────────────────────────────────────────────────────
ScenePlayer.Play(SceneLoader.Load(Path.Combine(gameDataRoot, "scenes", "intro.scene.json")));

// ── Game loop ──────────────────────────────────────────────────────────────────
int highScore = 0;
while (true)
{
    int result = PlayRound(ref highScore);
    if (result < 0) break; // ESC → quit
}

AnimationEngine.ShowCursor();
Console.ResetColor();

// ══════════════════════════════════════════════════════════════════════════════
//  SINGLE ROUND
//  Returns the final score, or -1 if the player pressed ESC.
// ══════════════════════════════════════════════════════════════════════════════

int PlayRound(ref int highScore)
{
    Console.Clear();
    AnimationEngine.HideCursor();

    int winH = Console.WindowHeight;
    int winW = Console.WindowWidth;

    // ── Layout ─────────────────────────────────────────────────────────────
    const int DinoCol    = 8;
    const int HeaderRows = 2;         // rows 0–1 (HUD + separator)
    int groundRow        = winH - 3;  // feet of dino and base of cacti
    int groundLineRow    = winH - 2;  // ═══ visual ground
    int floorRow         = winH - 1;  // ▓▓▓ floor

    // ── Static terrain ─────────────────────────────────────────────────────
    Console.ForegroundColor = ConsoleColor.DarkGreen;
    try { Console.SetCursorPosition(0, groundLineRow); Console.Write(new string('═', winW)); } catch { }
    Console.ForegroundColor = ConsoleColor.DarkGray;
    try { Console.SetCursorPosition(0, floorRow);      Console.Write(new string('▓', winW)); } catch { }
    Console.ResetColor();

    WriteHUD(winW, 0, highScore);

    // ── State ──────────────────────────────────────────────────────────────
    float dinoYF   = groundRow;
    float dinoVelY = 0f;
    bool  onGround = true;

    int  score     = 0;
    int  animTick  = 0;
    bool gameOver  = false;

    var obstacles  = new List<(float X, int Variant)>();
    int spawnTimer = 40;         // first obstacle spawns after 40 frames
    var rng        = new Random();

    // A couple of background clouds
    var clouds = new List<(float X, int Y)>
    {
        (rng.Next(winW / 2, winW - 10), rng.Next(HeaderRows, Math.Max(HeaderRows + 1, groundRow - 12))),
        (rng.Next(winW * 3 / 4, winW),  rng.Next(HeaderRows, Math.Max(HeaderRows + 1, groundRow - 12))),
    };

    // ── Main loop ───────────────────────────────────────────────────────────
    while (true)
    {
        // ── Input ────────────────────────────────────────────────────────
        while (Console.KeyAvailable)
        {
            var k = Console.ReadKey(intercept: true).Key;

            if (k == ConsoleKey.Escape) return -1;

            if (!gameOver && onGround &&
                (k == ConsoleKey.Spacebar || k == ConsoleKey.UpArrow))
            {
                dinoVelY = -2.0f;
                onGround = false;
            }

            if (gameOver && (k == ConsoleKey.Spacebar || k == ConsoleKey.Enter || k == ConsoleKey.UpArrow))
                return score; // caller will restart
        }

        if (gameOver)
        {
            Thread.Sleep(60);
            continue;
        }

        // ── Physics ───────────────────────────────────────────────────────
        int prevDinoRow = (int)Math.Round(dinoYF);

        dinoVelY += 0.26f;   // gravity
        dinoYF   += dinoVelY;

        if (dinoYF >= groundRow)
        {
            dinoYF   = groundRow;
            dinoVelY = 0f;
            onGround = true;
        }

        int dinoRow = (int)Math.Round(dinoYF);

        // ── Clouds ────────────────────────────────────────────────────────
        int cloudLen = DinoSprites.Cloud[0].Length;

        for (int i = 0; i < clouds.Count; i++)
        {
            var (cx, cy) = clouds[i];
            int oldX = (int)Math.Round(cx);

            // Erase old
            if (oldX >= 0 && cy >= HeaderRows && cy + 1 < groundRow - 4)
            {
                AnimationEngine.DrawAt(oldX, cy,     new string(' ', cloudLen + 1));
                AnimationEngine.DrawAt(oldX, cy + 1, new string(' ', cloudLen + 1));
            }

            float nx = cx - 0.35f;
            if (nx + cloudLen < 0)
                clouds[i] = (winW + rng.Next(5, 20),
                             rng.Next(HeaderRows, Math.Max(HeaderRows + 1, groundRow - 12)));
            else
                clouds[i] = (nx, cy);
        }

        // Draw new
        foreach (var (cx, cy) in clouds)
        {
            int ci = (int)Math.Round(cx);
            if (ci >= 0 && ci + cloudLen < winW && cy >= HeaderRows && cy + 1 < groundRow - 4)
            {
                AnimationEngine.DrawAt(ci, cy,     DinoSprites.Cloud[0], ConsoleColor.DarkGray);
                AnimationEngine.DrawAt(ci, cy + 1, DinoSprites.Cloud[1], ConsoleColor.DarkGray);
            }
        }

        // ── Obstacles ─────────────────────────────────────────────────────
        float speed = 2.0f + score * 0.001f; // gentle acceleration

        // Erase old
        foreach (var (ox, ov) in obstacles)
        {
            int ix      = (int)Math.Round(ox);
            int cHeight = DinoSprites.CactusVariants[ov].Length;
            int cTop    = groundRow - cHeight + 1;
            for (int r = cTop; r <= groundRow; r++)
                AnimationEngine.DrawAt(ix, r, "    ");
        }

        // Move
        for (int i = 0; i < obstacles.Count; i++)
        {
            var (ox, ov) = obstacles[i];
            obstacles[i] = (ox - speed, ov);
        }
        obstacles.RemoveAll(o => o.X + DinoSprites.CactusWidth + 2 < 0);

        // Spawn
        spawnTimer--;
        if (spawnTimer <= 0)
        {
            obstacles.Add((winW, rng.Next(0, DinoSprites.CactusVariants.Length)));
            spawnTimer = Math.Max(20, rng.Next(28, 55) - score / 400);
        }

        // Draw new
        foreach (var (ox, ov) in obstacles)
        {
            int ix     = (int)Math.Round(ox);
            var sprite = DinoSprites.CactusVariants[ov];
            int cTop   = groundRow - sprite.Length + 1;
            if (ix + DinoSprites.CactusWidth >= 0 && ix < winW)
                AnimationEngine.DrawSprite(sprite, ix, cTop, ConsoleColor.DarkGreen);
        }

        // ── Dino ──────────────────────────────────────────────────────────
        int minRow = Math.Min(prevDinoRow, dinoRow) - DinoSprites.Height;
        int maxRow = Math.Max(prevDinoRow, dinoRow);

        for (int r = Math.Max(HeaderRows, minRow); r <= Math.Min(groundRow, maxRow); r++)
            AnimationEngine.DrawAt(DinoCol, r, new string(' ', DinoSprites.Width + 1));

        string[] frame = onGround
            ? (animTick / 5 % 2 == 0 ? DinoSprites.RunA : DinoSprites.RunB)
            : DinoSprites.Jump;

        int dinoTopRow = Math.Max(HeaderRows, dinoRow - (DinoSprites.Height - 1));
        AnimationEngine.DrawSprite(frame, DinoCol, dinoTopRow, ConsoleColor.Green);

        // ── Collision ─────────────────────────────────────────────────────
        // Hitbox is shrunk 1 char on every side for forgiving feel.
        int hLeft  = DinoCol + 1;
        int hRight = DinoCol + DinoSprites.Width - 2;
        int hTop   = dinoTopRow + 1;
        int hBot   = dinoRow;

        bool hit = false;
        foreach (var (ox, ov) in obstacles)
        {
            int ix     = (int)Math.Round(ox);
            var sprite = DinoSprites.CactusVariants[ov];
            int cTop   = groundRow - sprite.Length + 2; // +1 forgiveness
            int cLeft  = ix + 1;
            int cRight = ix + DinoSprites.CactusWidth - 2;

            if (hRight >= cLeft && hLeft <= cRight && hBot >= cTop)
            {
                hit = true;
                break;
            }
        }

        if (hit)
        {
            // Redraw dino as dead
            for (int r = dinoTopRow; r <= dinoRow; r++)
                AnimationEngine.DrawAt(DinoCol, r, new string(' ', DinoSprites.Width + 1));
            AnimationEngine.DrawSprite(DinoSprites.Dead, DinoCol, dinoTopRow, ConsoleColor.Red);

            highScore = Math.Max(highScore, score);
            DrawGameOver(winW, winH, score, highScore);
            gameOver = true;
        }
        else
        {
            score++;
            WriteHUD(winW, score, highScore);
        }

        animTick++;
        Thread.Sleep(60);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
//  HELPERS
// ══════════════════════════════════════════════════════════════════════════════

void WriteHUD(int winW, int score, int highScore)
{
    string title   = "  DINO GAME";
    string right   = $"BEST: {highScore:D5}   SCORE: {score:D5}   [SPACE] Jump   [ESC] Quit  ";
    int    gap     = Math.Max(1, winW - title.Length - right.Length);
    string row0    = title + new string(' ', gap) + right;
    if (row0.Length > winW) row0 = row0[..winW];

    AnimationEngine.DrawAt(0, 0, row0, ConsoleColor.Cyan);
    AnimationEngine.DrawAt(0, 1, new string('─', winW), ConsoleColor.DarkGray);
}

void DrawGameOver(int winW, int winH, int score, int highScore)
{
    int midY = Math.Max(3, winH / 2 - 4);
    int boxW = 36;
    int midX = Math.Max(0, (winW - boxW) / 2);

    string[] lines =
    {
        "  ╔════════════════════════════════╗",
        "  ║                                ║",
       $"  ║         G A M E   O V E R      ║",
        "  ║                                ║",
       $"  ║   SCORE  :  {score,6}               ║",
       $"  ║   BEST   :  {highScore,6}               ║",
        "  ║                                ║",
        "  ║   [SPACE / ENTER]  Restart     ║",
        "  ║   [ESC]            Quit        ║",
        "  ║                                ║",
        "  ╚════════════════════════════════╝",
    };

    for (int i = 0; i < lines.Length; i++)
        AnimationEngine.DrawAt(midX, midY + i, lines[i], ConsoleColor.Yellow);
}
