using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace GleemLet.Services;

/// <summary>
/// Manages all audio in GleemLet:
///  - PlayCorrect() / PlayWrong()  — short sound effects for study feedback
///  - StartLobbyMusic() / StopLobbyMusic() — looping ambient music on the home screen
/// </summary>
public static class SoundService
{
    // ── SOUND EFFECTS ──────────────────────────────────────────────
    private static System.Media.SoundPlayer? _correctPlayer;
    private static System.Media.SoundPlayer? _wrongPlayer;

    public static void PlayCorrect() => PlayEffect(ref _correctPlayer, "correct.wav");
    public static void PlayWrong()   => PlayEffect(ref _wrongPlayer,   "wrong.wav");

    private static void PlayEffect(ref System.Media.SoundPlayer? player, string fileName)
    {
        try
        {
            var path = GetResourcePath(fileName);
            if (!File.Exists(path)) return;

            player ??= new System.Media.SoundPlayer(path);
            player.Play();   // async, non-blocking
        }
        catch { /* Audio hatası uygulamayı çökertmemeli */ }
    }

    // ── LOBBY MUSIC ────────────────────────────────────────────────
    private static MediaPlayer?        _musicPlayer;
    private static DispatcherTimer?    _loopTimer;
    private static bool                _musicRunning;

    public static bool IsMusicPlaying => _musicRunning;

    public static void StartLobbyMusic()
    {
        if (_musicRunning) return;

        try
        {
            var path = GetResourcePath("lobby.wav");
            if (!File.Exists(path)) return;

            _musicPlayer ??= new MediaPlayer();
            _musicPlayer.Volume = 0.35;
            _musicPlayer.Open(new Uri(path, UriKind.Absolute));
            _musicPlayer.Play();
            _musicRunning = true;

            // MediaPlayer ended event'i ile döngü
            _musicPlayer.MediaEnded -= OnMusicEnded;
            _musicPlayer.MediaEnded += OnMusicEnded;
        }
        catch { /* Sessiz hata */ }
    }

    private static void OnMusicEnded(object? sender, EventArgs e)
    {
        if (!_musicRunning || _musicPlayer == null) return;
        try
        {
            _musicPlayer.Position = TimeSpan.Zero;
            _musicPlayer.Play();
        }
        catch { }
    }

    public static void StopLobbyMusic()
    {
        if (!_musicRunning) return;
        _musicRunning = false;
        try
        {
            _musicPlayer?.Stop();
        }
        catch { }
    }

    // ── HELPERS ────────────────────────────────────────────────────
    /// <summary>
    /// Ses dosyalarını önce exe'nin yanındaki Resources klasöründen arar,
    /// bulamazsa doğrudan exe dizininde arar.
    /// </summary>
    private static string GetResourcePath(string fileName)
    {
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var inResources = Path.Combine(exeDir, "Resources", fileName);
        if (File.Exists(inResources)) return inResources;
        return Path.Combine(exeDir, fileName);
    }
}
