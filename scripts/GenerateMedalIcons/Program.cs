using SkiaSharp;

const int size = 128;
var outputDir = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "src", "HobbyXP", "Assets", "Medals"));

Directory.CreateDirectory(outputDir);

var medals = new (string File, SKColor Rim, SKColor Fill, Action<SKCanvas, float> Draw)[]
{
    ("official-race.png", SKColor.Parse("#D4AF37"), SKColor.Parse("#2A2418"), DrawLaurel),
    ("running-session.png", SKColor.Parse("#4FC3F7"), SKColor.Parse("#152238"), DrawShoe),
    ("running-km.png", SKColor.Parse("#81C784"), SKColor.Parse("#14261A"), DrawKm),
    ("progressive-overload.png", SKColor.Parse("#FF8A65"), SKColor.Parse("#2A1810"), DrawBarbellUp),
    ("gym-workout.png", SKColor.Parse("#CE93D8"), SKColor.Parse("#221428"), DrawDumbbell),
    ("platinum-game.png", SKColor.Parse("#B0BEC5"), SKColor.Parse("#1A2228"), DrawController),
    ("book-completed.png", SKColor.Parse("#FFD54F"), SKColor.Parse("#2A2410"), DrawClosedBook),
    ("book-pages.png", SKColor.Parse("#FFF176"), SKColor.Parse("#2A2818"), DrawOpenBook),
    ("course-completed.png", SKColor.Parse("#64B5F6"), SKColor.Parse("#102030"), DrawCap),
    ("course-sessions.png", SKColor.Parse("#4DD0E1"), SKColor.Parse("#102228"), DrawNotes),
    ("puzzle.png", SKColor.Parse("#A5D6A7"), SKColor.Parse("#142818"), DrawPuzzle),
    ("media.png", SKColor.Parse("#F48FB1"), SKColor.Parse("#281820"), DrawClapper),
};

foreach (var (file, rim, fill, draw) in medals)
{
    using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
    var canvas = surface.Canvas;
    canvas.Clear(SKColors.Transparent);

    var center = size / 2f;
    using var shadow = new SKPaint { Color = SKColor.Parse("#80000000"), IsAntialias = true };
    canvas.DrawCircle(center, center + 3, 46, shadow);

    using var fillPaint = new SKPaint { Color = fill, IsAntialias = true };
    canvas.DrawCircle(center, center, 46, fillPaint);

    using var rimPaint = new SKPaint
    {
        Color = rim,
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 5
    };
    canvas.DrawCircle(center, center, 46, rimPaint);

    using var gloss = new SKPaint { Color = SKColor.Parse("#30FFFFFF"), IsAntialias = true };
    canvas.DrawOval(center - 18, center - 30, 36, 18, gloss);

    canvas.Save();
    canvas.Translate(center, center);
    draw(canvas, 1f);
    canvas.Restore();

    var path = Path.Combine(outputDir, file);
    using var image = surface.Snapshot();
    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
    using var stream = File.OpenWrite(path);
    data.SaveTo(stream);
    Console.WriteLine($"Wrote {path}");
}

static void DrawLaurel(SKCanvas c, float s)
{
    using var p = IconPaint("#D4AF37");
    c.DrawCircle(-12 * s, 0, 8 * s, p);
    c.DrawCircle(12 * s, 0, 8 * s, p);
    c.DrawRect(SKRect.Create(-4 * s, -14 * s, 8 * s, 28 * s), p);
}

static void DrawShoe(SKCanvas c, float s)
{
    using var p = IconPaint("#4FC3F7");
    c.DrawRoundRect(SKRect.Create(-22 * s, -4 * s, 44 * s, 18 * s), 8 * s, 8 * s, p);
    c.DrawRoundRect(SKRect.Create(-10 * s, -14 * s, 18 * s, 12 * s), 4 * s, 4 * s, p);
}

static void DrawKm(SKCanvas c, float s)
{
    using var p = IconPaint("#81C784");
    c.DrawRect(SKRect.Create(-18 * s, 6 * s, 36 * s, 8 * s), p);
    c.DrawRect(SKRect.Create(-4 * s, -16 * s, 8 * s, 22 * s), p);
    using var t = TextPaint("#E8F5E9", 16 * s);
    c.DrawText("KM", -14 * s, 2 * s, t);
}

static void DrawBarbellUp(SKCanvas c, float s)
{
    using var p = IconPaint("#FF8A65");
    c.DrawRect(SKRect.Create(-24 * s, -2 * s, 48 * s, 6 * s), p);
    c.DrawRect(SKRect.Create(-28 * s, -8 * s, 8 * s, 18 * s), p);
    c.DrawRect(SKRect.Create(20 * s, -8 * s, 8 * s, 18 * s), p);
    c.DrawLine(-8 * s, 8 * s, 0, -18 * s, p);
    c.DrawLine(8 * s, 8 * s, 0, -18 * s, p);
}

static void DrawDumbbell(SKCanvas c, float s)
{
    using var p = IconPaint("#CE93D8");
    c.DrawRect(SKRect.Create(-24 * s, -3 * s, 48 * s, 6 * s), p);
    c.DrawRect(SKRect.Create(-30 * s, -10 * s, 10 * s, 20 * s), p);
    c.DrawRect(SKRect.Create(20 * s, -10 * s, 10 * s, 20 * s), p);
}

static void DrawController(SKCanvas c, float s)
{
    using var p = IconPaint("#ECEFF1");
    c.DrawRoundRect(SKRect.Create(-24 * s, -12 * s, 48 * s, 28 * s), 12 * s, 12 * s, p);
    c.DrawCircle(-10 * s, 2 * s, 4 * s, IconPaint("#455A64"));
    c.DrawCircle(10 * s, 2 * s, 4 * s, IconPaint("#455A64"));
    c.DrawCircle(0, -4 * s, 5 * s, IconPaint("#B0BEC5"));
}

static void DrawClosedBook(SKCanvas c, float s)
{
    using var p = IconPaint("#FFD54F");
    c.DrawRect(SKRect.Create(-18 * s, -20 * s, 36 * s, 40 * s), p);
    c.DrawLine(0, -20 * s, 0, 20 * s, IconPaint("#5D4037"));
}

static void DrawOpenBook(SKCanvas c, float s)
{
    using var p = IconPaint("#FFF176");
    var path = new SKPath();
    path.MoveTo(0, -20 * s);
    path.LineTo(-22 * s, 18 * s);
    path.LineTo(0, 10 * s);
    path.Close();
    c.DrawPath(path, p);
    var path2 = new SKPath();
    path2.MoveTo(0, -20 * s);
    path2.LineTo(22 * s, 18 * s);
    path2.LineTo(0, 10 * s);
    path2.Close();
    c.DrawPath(path2, IconPaint("#FFE082"));
}

static void DrawCap(SKCanvas c, float s)
{
    using var p = IconPaint("#64B5F6");
    c.DrawRect(SKRect.Create(-24 * s, 0, 48 * s, 10 * s), p);
    var top = new SKPath();
    top.MoveTo(-22 * s, 0);
    top.LineTo(0, -18 * s);
    top.LineTo(22 * s, 0);
    top.Close();
    c.DrawPath(top, p);
    c.DrawRect(SKRect.Create(-4 * s, -18 * s, 8 * s, 24 * s), IconPaint("#1565C0"));
}

static void DrawNotes(SKCanvas c, float s)
{
    using var p = IconPaint("#4DD0E1");
    c.DrawRoundRect(SKRect.Create(-16 * s, -18 * s, 28 * s, 36 * s), 3 * s, 3 * s, p);
    c.DrawRoundRect(SKRect.Create(-8 * s, -12 * s, 28 * s, 36 * s), 3 * s, 3 * s, IconPaint("#26C6DA"));
    for (var y = -4f; y <= 16f; y += 8f)
        c.DrawLine(-2 * s, y * s, 14 * s, y * s, IconPaint("#004D40"));
}

static void DrawPuzzle(SKCanvas c, float s)
{
    using var p = IconPaint("#A5D6A7");
    c.DrawRect(SKRect.Create(-16 * s, -16 * s, 32 * s, 32 * s), p);
    c.DrawCircle(0, -16 * s, 6 * s, p);
    c.DrawCircle(16 * s, 0, 6 * s, p);
    c.DrawCircle(-16 * s, 0, 6 * s, IconPaint("#1B5E20"));
}

static void DrawClapper(SKCanvas c, float s)
{
    using var p = IconPaint("#F48FB1");
    c.DrawRect(SKRect.Create(-22 * s, 4 * s, 44 * s, 14 * s), p);
    c.DrawRect(SKRect.Create(-22 * s, -16 * s, 18 * s, 20 * s), p);
    c.DrawRect(SKRect.Create(0, -10 * s, 18 * s, 14 * s), IconPaint("#F06292"));
    c.DrawRect(SKRect.Create(16 * s, -16 * s, 6 * s, 20 * s), IconPaint("#EC407A"));
}

static SKPaint IconPaint(string hex) => new()
{
    Color = SKColor.Parse(hex),
    IsAntialias = true,
    Style = SKPaintStyle.Fill
};

static SKPaint TextPaint(string hex, float size) => new()
{
    Color = SKColor.Parse(hex),
    IsAntialias = true,
    TextSize = size,
    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
    TextAlign = SKTextAlign.Left
};
