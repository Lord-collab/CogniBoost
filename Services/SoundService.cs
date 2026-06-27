namespace CogniBoost.Services;

public static class SoundService
{
    private static readonly byte[] CorrectWav = GenerateWav(523, 0.12);
    private static readonly byte[] WrongWav = GenerateWav(220, 0.25);
    private static readonly byte[] CompleteWav = GenerateWav(784, 0.35);
    private static readonly byte[] TickWav = GenerateWav(440, 0.06);

    public static void PlayCorrect() { if (SettingsService.SoundEnabled) _ = PlayWavAsync(CorrectWav); }
    public static void PlayWrong() { if (SettingsService.SoundEnabled) _ = PlayWavAsync(WrongWav); }
    public static void PlayComplete() { if (SettingsService.SoundEnabled) _ = PlayWavAsync(CompleteWav); }
    public static void PlayTick() { if (SettingsService.SoundEnabled) _ = PlayWavAsync(TickWav); }

    private static async Task PlayWavAsync(byte[] data)
    {
#if ANDROID
        try
        {
            var path = System.IO.Path.Combine(FileSystem.CacheDirectory, "sfx.wav");
            await System.IO.File.WriteAllBytesAsync(path, data);
            var player = new Android.Media.MediaPlayer();
            player.SetDataSource(path);
            player.Prepare();
            player.Start();
            player.Completion += (_, _) =>
            {
                player.Release();
                try { System.IO.File.Delete(path); } catch { }
            };
        }
        catch { }
#endif
    }

    private static byte[] GenerateWav(double frequency, double durationSec)
    {
        var sampleRate = 22050;
        var sampleCount = (int)(sampleRate * durationSec);
        var samples = new short[sampleCount];
        for (var i = 0; i < sampleCount; i++)
        {
            var t = (double)i / sampleRate;
            var envelope = 1.0 - (double)i / sampleCount;
            samples[i] = (short)(short.MaxValue * 0.4 * envelope * Math.Sin(2 * Math.PI * frequency * t));
        }

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        var dataSize = sampleCount * 2;
        bw.Write("RIFF".ToCharArray());
        bw.Write(36 + dataSize);
        bw.Write("WAVE".ToCharArray());
        bw.Write("fmt ".ToCharArray());
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write("data".ToCharArray());
        bw.Write(dataSize);
        foreach (var s in samples) bw.Write(s);
        return ms.ToArray();
    }
}
