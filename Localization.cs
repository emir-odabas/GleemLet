namespace GleemLet;

public enum AppLanguage { Turkish, English }

public static class L
{
    public static AppLanguage Lang { get; set; } = AppLanguage.English;
    private static bool TR => Lang == AppLanguage.Turkish;

    // ── NAV ──
    public static string NavHome     => TR ? "Ana Sayfa"   : "Home";
    public static string NavSets     => TR ? "Setlerim"    : "My Sets";
    public static string NavStats    => TR ? "İstatistik"  : "Statistics";
    public static string NavBadges   => TR ? "Rozetler"    : "Badges";
    public static string NavProfile  => TR ? "Profil"      : "Profile";
    public static string QuickStudy  => TR ? "Hızlı Çalış": "Quick Study";

    // ── HOME ──
    public static string WelcomeBack(string name) => TR ? $"👋 Tekrar hoş geldin, {name}!" : $"👋 Welcome back, {name}!";
    public static string ConsistencyTip => TR ? "Strîki devam ettir — tutarlılık anahtardır." : "Keep your streak going — consistency is key.";
    public static string RecentSets  => TR ? "Son Setler"    : "Recent Sets";
    public static string NewSet      => TR ? "+ Yeni Set"    : "+ New Set";
    public static string RecentActivity => TR ? "Son Aktiviteler" : "Recent Activity";
    public static string NoSetsYet   => TR ? "Henüz set yok" : "No sets yet";
    public static string CreateFirstSet => TR ? "İlk kelime setini oluştur" : "Create your first vocabulary set";
    public static string CreateSet   => TR ? "Set Oluştur"  : "Create Set";
    public static string WordsLabel  => TR ? "Kelime"       : "Words";
    public static string LearnedLabel => TR ? "Öğrenildi"   : "Learned";
    public static string SessionsLabel => TR ? "Oturum"     : "Sessions";
    public static string StreakLabel => TR ? "Seri"         : "Streak";
    public static string LevelLabel  => TR ? "Seviye"       : "Level";
    public static string XPToNextLevel(int xp) => TR ? $"{xp}/200 XP — sonraki seviye" : $"{xp}/200 XP to next level";

    // ── SETS ──
    public static string MySets      => TR ? "Setlerim"     : "My Sets";
    public static string SearchPlaceholder => TR ? "🔍  Set ve kelime ara..." : "🔍  Search sets and words...";
    public static string AllCategories => TR ? "Tüm Kategoriler" : "All Categories";
    public static string NoSetsFound => TR ? "Set bulunamadı" : "No sets found";
    public static string CreateSetToStart => TR ? "Başlamak için yeni bir set oluştur" : "Create a new set to get started";
    public static string StudiedX(int n) => TR ? $"  ·  {n}x çalışıldı" : $"  ·  studied {n}x";
    public static string LearnedOf(int l, int t) => TR ? $"{l}/{t} öğrenildi" : $"{l}/{t} learned";

    // ── DETAIL ──
    public static string BackToSets  => TR ? "← Setlere Dön" : "← Back to Sets";
    public static string WordsCount(int w, int l, string cat, double p)
        => TR ? $"{w} kelime  ·  {l} öğrenildi  ·  {cat}  ·  %{p:F0} tamamlandı"
              : $"{w} words  ·  {l} learned  ·  {cat}  ·  {p:F0}% complete";
    public static string EditSet     => TR ? "✏ Düzenle"    : "✏ Edit";
    public static string DeleteSet   => TR ? "🗑 Sil"       : "🗑 Delete";
    public static string MarkLearned => TR ? "Öğrendim olarak işaretle" : "Mark as learned";
    public static string DetailSearchHint => TR ? "🔍  Kelime ara..." : "🔍  Search words in this set...";
    public static string Flashcards  => TR ? "Kartlar"      : "Flashcards";
    public static string Learn       => TR ? "Öğren"        : "Learn";
    public static string Test        => TR ? "Test"         : "Test";
    public static string Timed       => TR ? "Zamanlı"      : "Timed";

    // ── STUDY ──
    public static string ClickToFlip => TR ? "ÇEVIRMEK İÇİN TIKLA  (veya Boşluk)" : "CLICK TO FLIP  (or press Space)";
    public static string Meaning     => TR ? "ANLAM"        : "MEANING";
    public static string GotIt       => TR ? "✓  Bildim!"   : "✓  Got it!";
    public static string StillLearning => TR ? "😕  Hâlâ öğreniyorum" : "😕  Still learning";
    public static string Prev        => TR ? "← Önceki"    : "← Prev";
    public static string Next        => TR ? "Sonraki →"   : "Next →";
    public static string Shuffle     => TR ? "🔀 Karıştır" : "🔀 Shuffle";
    public static string WhatMeaning => TR ? "Bu kelimenin anlamı nedir:" : "What is the meaning of:";
    public static string TypeMeaning => TR ? "Anlamını yaz:" : "Type the meaning of:";
    public static string Check       => TR ? "Kontrol Et →" : "Check →";
    public static string Correct     => TR ? "✓  Doğru!"    : "✓  Correct!";
    public static string Incorrect(string ans) => TR ? $"✗  Yanlış. Doğrusu: {ans}" : $"✗  Incorrect. Answer: {ans}";
    public static string EndStudy    => TR ? "✕ Bitir"      : "✕ End";
    public static string BackToSet   => TR ? "← Sete Dön"  : "← Back to Set";
    public static string TryAgain    => TR ? "🔄 Tekrar Dene" : "🔄 Try Again";

    // ── RESULTS ──
    public static string Perfect     => TR ? "Mükemmel!"    : "Perfect!";
    public static string GreatJob    => TR ? "Harika iş!"   : "Great job!";
    public static string GoodEffort  => TR ? "İyi çaba!"    : "Good effort!";
    public static string KeepGoing   => TR ? "Devam et!"    : "Keep going!";
    public static string Score(double p) => TR ? $"Puan: %{p:F0}" : $"Score: {p:F0}%";
    public static string CorrectLabel => TR ? "Doğru"       : "Correct";
    public static string WrongLabel  => TR ? "Yanlış"       : "Wrong";
    public static string TotalLabel  => TR ? "Toplam"       : "Total";
    public static string TimeLabel   => TR ? "Süre"         : "Time";
    public static string NewBadges   => TR ? "🏅 Yeni rozetler kazanıldı!" : "🏅 New badges earned!";

    // ── STATS ──
    public static string Statistics  => TR ? "İstatistikler" : "Statistics";
    public static string DayStreak   => TR ? "Günlük Seri"  : "Day Streak";
    public static string TotalXP     => TR ? "Toplam XP"    : "Total XP";
    public static string Sessions    => TR ? "Oturumlar"    : "Sessions";
    public static string AvgAccuracy => TR ? "Ort. Doğruluk": "Avg Accuracy";
    public static string StudyTime   => TR ? "Çalışma Süresi": "Study Time";
    public static string ProgressPerSet => TR ? "Set Başına İlerleme" : "Progress per Set";
    public static string RecentSessions => TR ? "Son Oturumlar" : "Recent Sessions";

    // ── BADGES ──
    public static string Badges      => TR ? "Rozetler"     : "Badges";
    public static string BadgesEarned(int e, int t) => TR ? $"{e} / {t} rozet kazanıldı" : $"{e} / {t} badges earned";

    // ── PROFILE ──
    public static string ProfileSettings => TR ? "Profil & Ayarlar" : "Profile & Settings";
    public static string ProfileSection  => TR ? "👤 Profil"       : "👤 Profile";
    public static string DisplayName     => TR ? "Görünen Ad"      : "Display Name";
    public static string LearningGoal    => TR ? "Öğrenme Hedefi"  : "Learning Goal";
    public static string Save            => TR ? "Kaydet"          : "Save";
    public static string Preferences     => TR ? "⚙ Tercihler"    : "⚙ Preferences";
    public static string SoundEffects    => TR ? "Ses Efektleri"   : "Sound Effects";
    public static string SoundDesc       => TR ? "Doğru/yanlış cevaplarda ses çal" : "Play sound on correct/wrong answers";
    public static string LobbyMusic      => TR ? "Lobi Müziği"     : "Lobby Music";
    public static string LobbyMusicDesc  => TR ? "Ana ekranda arka plan müziği çal" : "Play background music on the home screen";
    public static string ShuffleDefault  => TR ? "Varsayılan Karıştır" : "Shuffle by Default";
    public static string ShuffleDesc     => TR ? "Çalışma oturumlarında kartları karıştır" : "Randomize card order in study sessions";
    public static string LanguageSection => TR ? "🌐 Dil / Language" : "🌐 Language / Dil";
    public static string LanguageDesc    => TR ? "Uygulama dilini seçin" : "Select application language";

    // ── TEMA ──
    public static string ThemeSection   => TR ? "🎨 Görünüm" : "🎨 Appearance";
    public static string ThemeDark      => TR ? "🌙 Koyu" : "🌙 Dark";
    public static string ThemeLight     => TR ? "☀️ Aydınlık" : "☀️ Light";
    public static string AccentLabel    => TR ? "Aksent Rengi" : "Accent Color";

    // ── AVATAR & GÜNLÜK HEDEF ──
    public static string AvatarSection    => TR ? "🧑 Avatar" : "🧑 Avatar";
    public static string AvatarDesc       => TR ? "Profilinde görünecek avatarı seç" : "Choose your profile avatar";
    public static string DailyGoalSection => TR ? "🎯 Günlük Hedef" : "🎯 Daily Goal";
    public static string DailyGoalDesc    => TR ? "Günde kaç kelime öğrenmek istiyorsun?" : "How many words do you want to learn per day?";
    public static string DailyGoalWords(int n) => TR ? $"Günlük hedef: {n} kelime" : $"Daily goal: {n} words";
    public static string TodayProgress(int done, int goal) => TR ? $"Bugün: {done}/{goal} kelime" : $"Today: {done}/{goal} words";

    public static string DataSection     => TR ? "🗄 Veri"          : "🗄 Data";

    public static string ExportJSON      => TR ? "📤 JSON Dışa Aktar" : "📤 Export JSON";
    public static string ImportJSON      => TR ? "📥 JSON İçe Aktar" : "📥 Import JSON";
    public static string ClearAllData   => TR ? "🗑 Tüm Veriyi Sil" : "🗑 Clear All Data";
    public static string AboutSection   => TR ? "ℹ Hakkında"       : "ℹ About";
    public static string AppDesc        => TR ? "Akıllı kelime pratik uygulaması." : "A smart vocabulary practice app.";

    // ── DIALOGS ──
    public static string AddWordsFirst  => TR ? "Önce bu sete kelime ekle." : "Add words to this set first.";
    public static string NeedTwoWords   => TR ? "Öğren modu için en az 2 kelime gerekli." : "Need at least 2 words for Learn mode.";
    public static string NeedFourWords  => TR ? "Öğren modu için toplam en az 4 kelime gerekli." : "Need at least 4 total words across all sets for Learn mode.";
    public static string CreateSetFirst => TR ? "Önce bir set oluştur." : "Create a set first.";
    public static string ConfirmDelete(string name) => TR ? $"\"{name}\" silinsin mi?" : $"Delete \"{name}\"?";
    public static string Confirm        => TR ? "Onay"         : "Confirm";
    public static string DeleteAllWarning => TR ? "Tüm veriler silinsin mi? Bu işlem geri alınamaz." : "Delete ALL data? This cannot be undone.";
    public static string Warning        => TR ? "Uyarı"        : "Warning";
    public static string Exported       => TR ? "Dışa aktarıldı!" : "Exported!";
    public static string ImportFailed   => TR ? "İçe aktarma başarısız — geçersiz dosya." : "Import failed — invalid file.";
    public static string ImportedSets(int n) => TR ? $"{n} set içe aktarıldı." : $"Imported {n} sets.";
    public static string AppTitle       => "GleemLet";

    // ── CATEGORIES ──
    public static string CatGeneral    => TR ? "Genel"      : "General";
    public static string CatAcademic   => TR ? "Akademik"   : "Academic";
    public static string CatBusiness   => TR ? "İş"         : "Business";
    public static string CatDaily      => TR ? "Günlük"     : "Daily";
    public static string CatTechnical  => TR ? "Teknik"     : "Technical";
    public static string CatAllLabel   => TR ? "Tüm Kategoriler" : "All Categories";

    // Returns localized category display name for a stored (English) category key
    public static string LocalizeCategory(string raw) => raw switch
    {
        "General"   => CatGeneral,
        "Academic"  => CatAcademic,
        "Business"  => CatBusiness,
        "Daily"     => CatDaily,
        "Technical" => CatTechnical,
        _           => raw
    };

    // Returns stored English category key from a localized display name
    public static string CategoryKey(string localized) =>
        TR ? localized switch
        {
            "Genel"    => "General",
            "Akademik" => "Academic",
            "\u0130\u015f"       => "Business",
            "G\u00fcnl\u00fck"   => "Daily",
            "Teknik"   => "Technical",
            _          => localized
        }
        : localized;

    // ── BADGE NAMES & DESCRIPTIONS ──
    public static string BadgeName(string id) => (TR, id) switch
    {
        (true, "first_word")        => "\u0130lk Ad\u0131m",
        (true, "ten_words")         => "Kelime Avc\u0131s\u0131",
        (true, "fifty_words")       => "Kitapkurt",
        (true, "hundred_words")     => "Vocab Pro",
        (true, "two_hundred_words") => "Kelime Hazinesi",
        (true, "first_study")       => "\u00d6\u011frenci",
        (true, "ten_sessions")      => "Kararl\u0131",
        (true, "fifty_sessions")    => "Uzman",
        (true, "streak_3")          => "Tutarl\u0131",
        (true, "streak_7")          => "Alevde",
        (true, "streak_14")         => "Demir \u0130rade",
        (true, "streak_30")         => "Efsane",
        (true, "perfect")           => "M\u00fckemmeliyetçi",
        (true, "five_sets")         => "Set Ustas\u0131",
        (true, "three_sets")        => "Koleksiyoncu",
        (true, "all_learned")       => "Ustala\u015ft\u0131",
        (true, "level_5")           => "Y\u00fckselen Y\u0131ld\u0131z",
        (true, "level_10")          => "Akademisyen",
        (true, "night_owl")         => "Gece Bayku\u015fu",
        (true, "speed_demon")       => "H\u0131z Canavar\u0131",
        (true, "multilingual")      => "\u00c7ok Dilli",
        _                           => ""
    };

    public static string BadgeDesc(string id) => (TR, id) switch
    {
        (true, "first_word")        => "\u0130lk kelimeni ekle",
        (true, "ten_words")         => "10 kelime ekle",
        (true, "fifty_words")       => "50 kelime ekle",
        (true, "hundred_words")     => "100 kelime ekle",
        (true, "two_hundred_words") => "200 kelime ekle",
        (true, "first_study")       => "\u0130lk \u00e7al\u0131\u015fma oturumunu tamamla",
        (true, "ten_sessions")      => "10 \u00e7al\u0131\u015fma oturumu tamamla",
        (true, "fifty_sessions")    => "50 \u00e7al\u0131\u015fma oturumu tamamla",
        (true, "streak_3")          => "3 g\u00fcnl\u00fck seri",
        (true, "streak_7")          => "7 g\u00fcnl\u00fck seri",
        (true, "streak_14")         => "14 g\u00fcnl\u00fck seri",
        (true, "streak_30")         => "30 g\u00fcnl\u00fck seri",
        (true, "perfect")           => "Bir oturumda %100 do\u011fruluk",
        (true, "five_sets")         => "5 set olu\u015ftur",
        (true, "three_sets")        => "3 set olu\u015ftur",
        (true, "all_learned")       => "Herhangi bir sette t\u00fcm kelimeleri \u00f6\u011fren",
        (true, "level_5")           => "Seviye 5'e ula\u015f",
        (true, "level_10")          => "Seviye 10'a ula\u015f",
        (true, "night_owl")         => "Gece 22:00'den sonra \u00e7al\u0131\u015f",
        (true, "speed_demon")       => "Zamanl\u0131 modda 60 saniyede bitir",
        (true, "multilingual")      => "T\u00fcrk\u00e7e'ye ge\u00e7",
        _                           => ""
    };
}
