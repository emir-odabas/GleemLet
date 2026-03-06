using System.Windows;
using System.Windows.Media;

namespace GleemLet.Services;

/// <summary>
/// Uygulama temasını (koyu/aydınlık) ve aksent rengini runtime'da değiştirir.
/// Application.Current.Resources'daki SolidColorBrush nesnelerini doğrudan günceller.
/// </summary>
public static class ThemeService
{
    // ── AKSENT PALETLERİ ──────────────────────────────────────────────────────
    private static readonly Dictionary<string, (Color main, Color bright, Color dark, Color bg)> Accents = new()
    {
        ["teal"]   = (Color("#4FAC82"), Color("#5CDEB5"), Color("#3A8A68"), Color("#1A2E24")),
        ["purple"] = (Color("#9B59B6"), Color("#BF7FD4"), Color("#7D3F9A"), Color("#2A1535")),
        ["blue"]   = (Color("#61AFEF"), Color("#85C8FF"), Color("#4A8CC0"), Color("#152035")),
        ["orange"] = (Color("#E5A44A"), Color("#FFCC70"), Color("#C07A20"), Color("#2E1E0A")),
        ["pink"]   = (Color("#E06C9F"), Color("#FF99C0"), Color("#B84A7C"), Color("#2E1020")),
        ["red"]    = (Color("#E06C75"), Color("#FF9FAA"), Color("#B84A55"), Color("#2E1015")),
    };

    // ── KOYU TEMA RENK SETPRİ ──────────────────────────────────────────────────
    private static readonly Dictionary<string, Color> DarkBase = new()
    {
        ["Bg"]      = Color("#121216"),
        ["Surface"] = Color("#1C1C22"),
        ["Surface2"]= Color("#242430"),
        ["Border"]  = Color("#2E2E3A"),
        ["Border2"] = Color("#3A3A48"),
        ["Text"]    = Color("#E8E8F0"),
        ["Muted"]   = Color("#8888A0"),
        ["Dim"]     = Color("#4A4A60"),
        ["Sidebar"] = Color("#0E0E12"),
        ["Red"]     = Color("#E06C75"),
        ["RedBg"]   = Color("#2E1A1E"),
        ["Yellow"]  = Color("#E5C07B"),
        ["Blue"]    = Color("#61AFEF"),
    };

    // ── AYDINLIK TEMA RENK SETİ ──────────────────────────────────────────────
    private static readonly Dictionary<string, Color> LightBase = new()
    {
        ["Bg"]      = Color("#F0F0F8"),
        ["Surface"] = Color("#FFFFFF"),
        ["Surface2"]= Color("#E8E8F0"),
        ["Border"]  = Color("#D0D0E0"),
        ["Border2"] = Color("#B8B8D0"),
        ["Text"]    = Color("#1A1A2E"),
        ["Muted"]   = Color("#666688"),
        ["Dim"]     = Color("#8888A8"),
        ["Sidebar"] = Color("#E0E0EE"),
        ["Red"]     = Color("#C0404A"),
        ["RedBg"]   = Color("#FDECEA"),
        ["Yellow"]  = Color("#C07820"),
        ["Blue"]    = Color("#2070C0"),
    };

    // ── GENEL API ────────────────────────────────────────────────────────────
    // Aktif tema renklerine kod-behind'dan erişim (RebuildUI sırasında geçerli)
    public static Dictionary<string, Color> Current { get; } = new();

    /// Semantic renk adından Color döndürür (ör. "Bg", "Surface", "Surface2", "Teal", "Text", "Muted")
    public static Color C(string key) => Current.TryGetValue(key, out var c) ? c : Colors.Transparent;
    public static SolidColorBrush B(string key) => new(C(key));

    public static void Apply(string theme, string accent)
    {
        var res      = Application.Current.Resources;
        var isDark   = theme != "light";
        var baseColors = isDark ? DarkBase : LightBase;

        // Base renklerini uygula + Current'a kaydet
        foreach (var (key, color) in baseColors)
        {
            SetBrush(res, key, color);
            Current[key] = color;
        }

        // Aksent renklerini uygula
        if (!Accents.TryGetValue(accent, out var acc))
            acc = Accents["teal"];

        SetBrush(res, "Teal",   acc.main);  Current["Teal"]   = acc.main;
        SetBrush(res, "Teal2",  acc.dark);  Current["Teal2"]  = acc.dark;
        SetBrush(res, "TealBg", acc.bg);    Current["TealBg"] = acc.bg;

        SetColor(res, "AccentMain",   acc.main);
        SetColor(res, "AccentBright", acc.bright);
        SetColor(res, "AccentDark",   acc.dark);
        SetColor(res, "AccentBg",     acc.bg);
    }

    private static void SetBrush(ResourceDictionary res, string key, Color c)
    {
        // App.xaml'da tanımlı brush'lar WPF tarafından frozen edilir.
        // Frozen brush'ların .Color'ı mutate edilemez → her zaman yeni brush oluştur.
        res[key] = new SolidColorBrush(c);
    }

    private static void SetColor(ResourceDictionary res, string key, Color c)
    {
        res[key] = c;
    }

    private static Color Color(string hex) =>
        (Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
}
