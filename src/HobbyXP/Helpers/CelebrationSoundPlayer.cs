using System.IO;
using System.Media;

namespace HobbyXP.Helpers;

/// <summary>
/// Reproduce un fanfarria corta de level-up sin depender de archivos de audio embebidos.
/// </summary>
public static class CelebrationSoundPlayer
{
    private const int SampleRate = 22050;

    public static void PlayLevelUp()
    {
        try
        {
            using var stream = BuildLevelUpWave();
            using var player = new SoundPlayer(stream);
            player.Play();
        }
        catch
        {
            // Audio opcional: no interrumpir la celebración si falla el dispositivo.
        }
    }

    private static MemoryStream BuildLevelUpWave()
    {
        var notes = new (double FrequencyHz, double DurationSeconds, double Volume)[]
        {
            (523.25, 0.10, 0.55),
            (659.25, 0.10, 0.60),
            (783.99, 0.14, 0.70),
            (1046.50, 0.22, 0.65)
        };

        var samples = new List<short>();
        foreach (var (frequencyHz, durationSeconds, volume) in notes)
            AppendTone(samples, frequencyHz, durationSeconds, volume);

        AppendTone(samples, 1046.50, 0.18, 0.35);

        var stream = new MemoryStream();
        WriteWaveHeader(stream, samples.Count);
        WriteSamples(stream, samples);
        stream.Position = 0;
        return stream;
    }

    private static void AppendTone(List<short> samples, double frequencyHz, double durationSeconds, double volume)
    {
        var sampleCount = (int)(SampleRate * durationSeconds);
        for (var i = 0; i < sampleCount; i++)
        {
            var t = (double)i / SampleRate;
            var envelope = Math.Min(1d, i / (SampleRate * 0.012)) *
                           Math.Min(1d, (sampleCount - i) / (SampleRate * 0.08));
            var wave = Math.Sin(2d * Math.PI * frequencyHz * t);
            var harmonic = Math.Sin(2d * Math.PI * frequencyHz * 2d * t) * 0.18;
            var value = (wave + harmonic) * envelope * volume;
            samples.Add((short)Math.Clamp(value * short.MaxValue, short.MinValue, short.MaxValue));
        }
    }

    private static void WriteWaveHeader(Stream stream, int sampleCount)
    {
        const short channels = 1;
        const short bitsPerSample = 16;
        var byteRate = SampleRate * channels * bitsPerSample / 8;
        var blockAlign = channels * bitsPerSample / 8;
        var dataSize = sampleCount * blockAlign;

        using var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(SampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data"u8);
        writer.Write(dataSize);
    }

    private static void WriteSamples(Stream stream, IReadOnlyList<short> samples)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        foreach (var sample in samples)
            writer.Write(sample);
    }
}
