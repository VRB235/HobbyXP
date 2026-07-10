using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using HobbyXP.Helpers;

namespace HobbyXP.Views.Controls;

public partial class LevelUpOverlay
{
    private readonly List<ConfettiPiece> _confetti = [];
    private readonly Random _random = new();
    private bool _isAnimating;

    private static readonly Color[] ConfettiPalette =
    [
        Color.FromRgb(0x00, 0xE6, 0x76),
        Color.FromRgb(0x00, 0xE5, 0xFF),
        Color.FromRgb(0xFF, 0xD5, 0x4F),
        Color.FromRgb(0x7C, 0x4D, 0xFF),
        Color.FromRgb(0xFF, 0x52, 0x52),
        Color.FromRgb(0xFF, 0x91, 0xEA),
        Color.FromRgb(0xFF, 0xFF, 0xFF)
    ];

    public LevelUpOverlay()
    {
        InitializeComponent();
        IsVisibleChanged += OnIsVisibleChanged;
        OverlayRoot.SizeChanged += (_, _) => RespawnConfettiIfNeeded();
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
            BeginCelebration();
        else
            EndCelebration();
    }

    private void BeginCelebration()
    {
        CelebrationSoundPlayer.PlayLevelUp();
        SpawnConfettiBurst();
        BeginStoryboard("GlowPulseStoryboard");
        BeginStoryboard("EntranceStoryboard");
        BeginStoryboard("SparkleStoryboard");
        StartConfettiLoop();
    }

    private void EndCelebration()
    {
        StopStoryboard("GlowPulseStoryboard");
        StopStoryboard("EntranceStoryboard");
        StopStoryboard("SparkleStoryboard");
        StopConfettiLoop();
        ConfettiCanvas.Children.Clear();
        _confetti.Clear();
    }

    private void BeginStoryboard(string key)
    {
        if (Resources[key] is Storyboard storyboard)
            storyboard.Begin(this, true);
    }

    private void StopStoryboard(string key)
    {
        if (Resources[key] is Storyboard storyboard)
            storyboard.Stop(this);
    }

    private void SpawnConfettiBurst()
    {
        ConfettiCanvas.Children.Clear();
        _confetti.Clear();

        var width = Math.Max(OverlayRoot.ActualWidth, 900);
        var height = Math.Max(OverlayRoot.ActualHeight, 600);
        var originX = width / 2d;
        var originY = height / 2d - 40d;

        for (var i = 0; i < 72; i++)
            _confetti.Add(CreateConfettiPiece(originX, originY, width, height));

        foreach (var piece in _confetti)
            ConfettiCanvas.Children.Add(piece.Element);
    }

    private void RespawnConfettiIfNeeded()
    {
        if (!IsVisible || _confetti.Count == 0)
            return;

        var width = Math.Max(OverlayRoot.ActualWidth, 900);
        var height = Math.Max(OverlayRoot.ActualHeight, 600);
        var originX = width / 2d;
        var originY = height / 2d - 40d;

        foreach (var piece in _confetti)
            piece.Reset(originX, originY, width, height, _random);
    }

    private ConfettiPiece CreateConfettiPiece(double originX, double originY, double width, double height)
    {
        var piece = new ConfettiPiece(_random);
        piece.Reset(originX, originY, width, height, _random);
        return piece;
    }

    private void StartConfettiLoop()
    {
        if (_isAnimating)
            return;

        _isAnimating = true;
        CompositionTarget.Rendering += OnConfettiRendering;
    }

    private void StopConfettiLoop()
    {
        if (!_isAnimating)
            return;

        _isAnimating = false;
        CompositionTarget.Rendering -= OnConfettiRendering;
    }

    private void OnConfettiRendering(object? sender, EventArgs e)
    {
        if (_confetti.Count == 0)
            return;

        var width = Math.Max(OverlayRoot.ActualWidth, 1);
        var height = Math.Max(OverlayRoot.ActualHeight, 1);

        foreach (var piece in _confetti)
            piece.Tick(width, height);
    }

    private sealed class ConfettiPiece
    {
        private readonly RotateTransform _rotateTransform = new();

        public ConfettiPiece(Random random)
        {
            if (random.NextDouble() > 0.35)
            {
                Element = new Rectangle
                {
                    Width = random.Next(6, 11),
                    Height = random.Next(14, 24),
                    RadiusX = 1.5,
                    RadiusY = 1.5
                };
            }
            else
            {
                Element = new Ellipse
                {
                    Width = random.Next(6, 12),
                    Height = random.Next(6, 12)
                };
            }

            Element.RenderTransformOrigin = new Point(0.5, 0.5);
            Element.RenderTransform = _rotateTransform;
            Element.Opacity = random.NextDouble() * 0.25 + 0.75;
        }

        public Shape Element { get; }

        public double X { get; private set; }

        public double Y { get; private set; }

        private double _velocityX;

        private double _velocityY;

        private double _rotationSpeed;

        private double _gravity;

        private double _life;

        public void Reset(double originX, double originY, double width, double height, Random random)
        {
            var angle = random.NextDouble() * Math.PI * 2d;
            var speed = random.NextDouble() * 9d + 4d;

            X = originX + (random.NextDouble() - 0.5d) * 80d;
            Y = originY + (random.NextDouble() - 0.5d) * 40d;
            _velocityX = Math.Cos(angle) * speed;
            _velocityY = Math.Sin(angle) * speed - random.NextDouble() * 6d - 2d;
            _rotationSpeed = (random.NextDouble() - 0.5d) * 14d;
            _gravity = random.NextDouble() * 0.22d + 0.18d;
            _life = random.NextDouble() * 0.4d + 0.8d;

            Element.Fill = new SolidColorBrush(ConfettiPalette[random.Next(ConfettiPalette.Length)]);
            Canvas.SetLeft(Element, X);
            Canvas.SetTop(Element, Y);
            _rotateTransform.Angle = random.NextDouble() * 360d;
            Element.Opacity = random.NextDouble() * 0.2d + 0.8d;
        }

        public void Tick(double width, double height)
        {
            _velocityY += _gravity;
            _velocityX *= 0.995d;
            X += _velocityX;
            Y += _velocityY;
            _rotateTransform.Angle += _rotationSpeed;
            _life -= 0.012d;

            if (_life < 0.35d)
                Element.Opacity = Math.Max(0, Element.Opacity - 0.03d);

            if (Y > height + 30 || X < -40 || X > width + 40 || Element.Opacity <= 0.01d)
            {
                var random = new Random(unchecked((int)DateTime.UtcNow.Ticks) ^ Element.GetHashCode());
                Reset(width / 2d, height * 0.35d, width, height, random);
                _velocityY = random.NextDouble() * 2d + 1d;
            }

            Canvas.SetLeft(Element, X);
            Canvas.SetTop(Element, Y);
        }
    }
}
