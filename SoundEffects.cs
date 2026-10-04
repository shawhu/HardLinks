using System.Media;

sealed class SoundEffects
{
    readonly SoundPlayer successSound;
    readonly SoundPlayer failedSound;

    public SoundEffects(float successVolume, float failedVolume)
    {
        successSound = CreateSound(
            successVolume,
            [
                (523.25, 392.00, 0.08),
                (659.25, 523.25, 0.08),
                (783.99, 659.25, 0.08),
                (1046.50, 783.99, 0.18)
            ]
        );
        failedSound = CreateSound(
            failedVolume,
            [
                (349.23, 261.63, 0.08),
                (349.23, 261.63, 0.18)
            ]
        );
    }

    public void PlaySuccess() => successSound.Play();
    public void PlayFailure() => failedSound.Play();

    static SoundPlayer CreateSound(float volume, (double MelodyFrequency, double HarmonyFrequency, double Duration)[] notes)
    {
        if (volume is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(volume), "Sound volume must be between 0 and 1.");

        const int sampleRate = 22050;
        const short bitsPerSample = 16;
        const short channels = 1;
        const double gapDuration = 0.02;
        var gapSamples = (int)(sampleRate * gapDuration);
        var noteSampleCounts = notes.Select(note => (int)(sampleRate * note.Duration)).ToArray();
        var dataLength = (noteSampleCounts.Sum() + gapSamples * (notes.Length - 1)) * sizeof(short);
        var stream = new MemoryStream(44 + dataLength);
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write("RIFF"u8);
            writer.Write(36 + dataLength);
            writer.Write("WAVE"u8);
            writer.Write("fmt "u8);
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * bitsPerSample / 8);
            writer.Write((short)(channels * bitsPerSample / 8));
            writer.Write(bitsPerSample);
            writer.Write("data"u8);
            writer.Write(dataLength);

            for (var noteIndex = 0; noteIndex < notes.Length; noteIndex++)
            {
                var (melodyFrequency, harmonyFrequency, _) = notes[noteIndex];
                var noteSamples = noteSampleCounts[noteIndex];
                for (var i = 0; i < noteSamples; i++)
                {
                    var progress = (double)i / noteSamples;
                    var envelope = Math.Min(1, progress * 20) * Math.Min(1, (1 - progress) * 5);
                    var melody = Math.Sin(2 * Math.PI * melodyFrequency * i / sampleRate);
                    var harmony = Math.Sin(2 * Math.PI * harmonyFrequency * i / sampleRate);
                    var sample = (melody + harmony) * 0.5 * envelope * short.MaxValue * volume;
                    writer.Write((short)Math.Clamp(sample, short.MinValue, short.MaxValue));
                }
                if (noteIndex < notes.Length - 1)
                    for (var i = 0; i < gapSamples; i++)
                        writer.Write((short)0);
            }
        }
        stream.Position = 0;
        var player = new SoundPlayer(stream);
        player.Load();
        return player;
    }
}
