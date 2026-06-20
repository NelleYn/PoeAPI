using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpDX.Multimedia;
using SharpDX.XAudio2;

namespace ExileCore;

/// <summary>Holds a decoded WAV sound ready for playback through XAudio2.</summary>
public class MyWave
{
    /// <summary>The audio buffer to submit to a source voice.</summary>
    public AudioBuffer Buffer { get; set; }

    /// <summary>Decoded packet metadata for the buffer.</summary>
    public uint[] DecodedPacketsInfo { get; set; }

    /// <summary>The wave format of the decoded sound.</summary>
    public WaveFormat WaveFormat { get; set; }
}

/// <summary>
/// Loads and plays WAV sounds from a directory using XAudio2. Sounds are lazily loaded and
/// cached on first playback; if the sounds directory is missing the controller becomes a no-op.
/// </summary>
public class SoundController : IDisposable
{
    private readonly List<SourceVoice> _list = new List<SourceVoice>();
    private readonly bool initialized;
    private readonly MasteringVoice masteringVoice;
    private readonly Dictionary<string, MyWave> Sounds;
    private readonly string soundsDir;
    private readonly XAudio2 xAudio2;

    /// <summary>Creates a controller that loads sounds from the given directory (relative to the app base directory).</summary>
    public SoundController(string dir)
    {
        soundsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);

        if (!Directory.Exists(soundsDir))
        {
            initialized = false;
            DebugWindow.LogError("Sounds dir not found, continue working without any sound.");
            return;
        }

        xAudio2 = new XAudio2();
        xAudio2.StartEngine();
        masteringVoice = new MasteringVoice(xAudio2);
        var soundFiles = Directory.GetFiles(soundsDir, "*.wav");
        Sounds = new Dictionary<string, MyWave>(soundFiles.Length);

        initialized = true;
    }

    /// <summary>Stops the audio engine and releases all audio resources.</summary>
    public void Dispose()
    {
        foreach (var wave in Sounds)
        {
            wave.Value.Buffer.Stream.Dispose();
        }

        xAudio2.StopEngine();
        masteringVoice?.Dispose();
        xAudio2?.Dispose();
    }

    /// <summary>Plays the named sound, loading and caching it on first use. No-op if not initialized.</summary>
    public void PlaySound(string name)
    {
        if (!initialized)
            return;

        if (Sounds.TryGetValue(name, out var wave))
        {
            if (wave == null)
                wave = LoadSound(name);
        }
        else
            wave = LoadSound(name);

        if (wave == null)
        {
            DebugWindow.LogError($"Sound file: {name}.wav not found.");
            return;
        }

        var sourceVoice = new SourceVoice(xAudio2, wave.WaveFormat, true);
        sourceVoice.SubmitSourceBuffer(wave.Buffer, wave.DecodedPacketsInfo);
        sourceVoice.Start();
        _list.Add(sourceVoice);

        for (var i = 0; i < _list.Count; i++)
        {
            var sv = _list[i];

            if (sv.State.BuffersQueued <= 0)
            {
                sv.Stop();
                sv.DestroyVoice();
                sv.Dispose();
                _list.RemoveAt(i);
            }
        }
    }

    /// <summary>Loads and caches the named sound ahead of playback.</summary>
    public void PreloadSound(string name)
    {
        LoadSound(name);
    }

    private MyWave LoadSound(string name)
    {
        if (name.IndexOf(".wav", StringComparison.Ordinal) == -1)
            name = Path.Combine(soundsDir, $"{name}.wav");

        var fileInfo = new FileInfo(name);
        if (!fileInfo.Exists) return null;
        var soundStream = new SoundStream(File.OpenRead(name));
        var waveFormat = soundStream.Format;

        var buffer = new AudioBuffer
        {
            Stream = soundStream.ToDataStream(), AudioBytes = (int) soundStream.Length, Flags = BufferFlags.EndOfStream
        };

        soundStream.Close();
        var wave = new MyWave {Buffer = buffer, WaveFormat = waveFormat, DecodedPacketsInfo = soundStream.DecodedPacketsInfo};
        Sounds[fileInfo.Name.Split('.').First()] = wave;
        Sounds[fileInfo.Name] = wave;
        return wave;
    }

    /// <summary>Sets the master output volume (0..1).</summary>
    public void SetVolume(float volume)
    {
        masteringVoice.SetVolume(volume);
    }
}
