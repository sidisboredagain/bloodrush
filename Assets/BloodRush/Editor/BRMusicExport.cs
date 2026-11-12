using System.IO;
using UnityEngine;

namespace BR
{
    // batchmode: renders the synth music tracks to .wav files (for trailers).  -brwav <dir>
    public static class BRMusicExport
    {
        public static void Run()
        {
            var args = System.Environment.GetCommandLineArgs(); string dir = Path.Combine(Application.dataPath, "..", "MusicExport");
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-brwav") dir = args[i + 1];
            Directory.CreateDirectory(dir);
            Save(dir, "1i", Tracks.Render(Tracks.Track1, 160, 0.25, 0, 0, 64, false, true, 11));
            Save(dir, "1a", Tracks.Render(Tracks.Track1, 160, 0.25, 0, 64, 320, true, false, 12));
            Save(dir, "1b", Tracks.Render(Tracks.Track1, 160 * 1.05, 0.25, 1, 64, 320, true, false, 13));
            Save(dir, "6i", Tracks.Render(Tracks.Track6, 84, 0.7, 0, 0, 16, false, false, 61));
            Save(dir, "6a", Tracks.Render(Tracks.Track6, 84, 0.7, 0, 16, 272, true, false, 62));
            Save(dir, "2a", Tracks.Render(Tracks.Track2, 132, 0.6, 0, 256, 448, true, false, 22));
            Save(dir, "2b", Tracks.Render(Tracks.Track2, 132, 0.6, 1, 256, 448, true, false, 23));
            Debug.Log("BR_WAV_DONE " + dir);
        }
        static void Save(string dir, string name, float[] d)
        {
            using var w = new BinaryWriter(File.Create(Path.Combine(dir, name + ".wav")));
            int sr = Syn.SR, n = d.Length;
            w.Write("RIFF".ToCharArray()); w.Write(36 + n * 2); w.Write("WAVE".ToCharArray());
            w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(sr); w.Write(sr * 2); w.Write((short)2); w.Write((short)16);
            w.Write("data".ToCharArray()); w.Write(n * 2);
            foreach (var v in d) w.Write((short)(Mathf.Clamp(v, -1, 1) * 32767));
        }
    }
}
