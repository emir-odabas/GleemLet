using System;
using System.IO;

namespace GleemLet.Services;

/// <summary>
/// Manages sound effects for study feedback (correct / wrong).
/// </summary>
public static class SoundService
{
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
            player.Play();
        }
        catch { /* Audio hatası uygulamayı çökertmemeli */ }
    }

    private static string GetResourcePath(string fileName)
    {
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var inResources = Path.Combine(exeDir, "Resources", fileName);
        if (File.Exists(inResources)) return inResources;
        return Path.Combine(exeDir, fileName);
    }
}
