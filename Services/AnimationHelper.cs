using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace GleemLet.Services;

/// <summary>
/// Doğru/yanlış cevap animasyonları ve sayfa geçiş efektleri.
/// </summary>
public static class AnimationHelper
{
    // ── YANLIŞ CEVAP: yatay shake ────────────────────────────────────────────
    public static void Shake(UIElement element)
    {
        var tt = new TranslateTransform();
        element.RenderTransform = tt;

        var anim = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(400),
            FillBehavior = FillBehavior.Stop
        };
        // 7 adımda sağa-sola-sağa-sola...
        int[] offsets = [0, -10, 10, -8, 8, -5, 5, 0];
        double step = 400.0 / (offsets.Length - 1);
        for (int i = 0; i < offsets.Length; i++)
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(offsets[i], KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(i * step))));

        anim.Completed += (_, _) => element.RenderTransform = null;
        tt.BeginAnimation(TranslateTransform.XProperty, anim);
    }

    // ── DOĞRU CEVAP: yeşil glow pulse ────────────────────────────────────────
    public static void PulseGreen(UIElement element)
    {
        // Scale yukarı → aşağı
        var st = new ScaleTransform(1, 1);
        element.RenderTransformOrigin = new Point(0.5, 0.5);
        element.RenderTransform = st;

        var scaleUp = new DoubleAnimation(1.0, 1.04, TimeSpan.FromMilliseconds(120))
        { AutoReverse = true, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        scaleUp.Completed += (_, _) => element.RenderTransform = null;

        st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.0, 1.04, TimeSpan.FromMilliseconds(120))
        { AutoReverse = true, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });

        // Eğer element bir UIElement ise kısa süre glow efekti ekle
        if (element is FrameworkElement fe)
        {
            var originalEffect = fe.Effect;
            fe.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(79, 172, 130),
                BlurRadius = 20,
                ShadowDepth = 0,
                Opacity = 0.8
            };
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(350)
            };
            timer.Tick += (_, _) => { fe.Effect = originalEffect; timer.Stop(); };
            timer.Start();
        }
    }

    // ── YANLIŞ CEVAP: kırmızı pulse ──────────────────────────────────────────
    public static void PulseRed(UIElement element)
    {
        if (element is FrameworkElement fe)
        {
            var originalEffect = fe.Effect;
            fe.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(224, 108, 117),
                BlurRadius = 20,
                ShadowDepth = 0,
                Opacity = 0.8
            };
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(350)
            };
            timer.Tick += (_, _) => { fe.Effect = originalEffect; timer.Stop(); };
            timer.Start();
        }
    }

    // ── SAYFA GEÇİŞİ: fade-in ────────────────────────────────────────────────
    public static void FadeIn(UIElement element, int durationMs = 220)
    {
        element.Opacity = 0;
        var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(durationMs))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        element.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    // ── KART GİRİŞİ: yukarıdan kayarak gelme ─────────────────────────────────
    public static void SlideInFromTop(UIElement element, int durationMs = 200)
    {
        var tt = new TranslateTransform(0, -16);
        element.RenderTransform = tt;
        element.Opacity = 0;

        var slideAnim = new DoubleAnimation(-16, 0, TimeSpan.FromMilliseconds(durationMs))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var fadeAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(durationMs));
        slideAnim.Completed += (_, _) => element.RenderTransform = null;

        tt.BeginAnimation(TranslateTransform.YProperty, slideAnim);
        element.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
    }
}
