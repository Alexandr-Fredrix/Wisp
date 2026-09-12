using System;
using System.IO;

namespace Wisp.Core
{
    public sealed class ProgressRead
    {
        public SaveProgress Data;
        public bool WriteAllowed = true;
        public bool PreserveOriginal;
        public bool Recovered;
        public string Message = "";
    }

    public sealed class ProgressStore
    {
        private readonly Func<string, SaveProgress> decode;
        private readonly Func<SaveProgress, string> encode;
        public ProgressStore(Func<string, SaveProgress> decode, Func<SaveProgress, string> encode)
        { this.decode = decode; this.encode = encode; }

        private SaveProgress Read(string path, Func<string, SaveProgress> parser)
        {
            if (!File.Exists(path)) return null;
            try { var value = parser(File.ReadAllText(path)); if (value != null) value.Normalize(); return value; }
            catch (Exception) { return null; }
        }

        public ProgressRead Load(string path, string legacy, Func<string, SaveProgress> legacyDecode, bool fresh)
        {
            if (fresh) return new ProgressRead { Data = new SaveProgress() };
            var value = Read(path, decode);
            if (value != null) return new ProgressRead { Data = value };
            bool damaged = File.Exists(path) || File.Exists(path + ".bak");
            value = Read(path + ".bak", decode);
            if (value == null) value = Read(legacy, legacyDecode);
            if (value != null) return new ProgressRead { Data = value, PreserveOriginal = damaged, Recovered = damaged, Message = damaged ? "Progress recovered; original files will be preserved." : "" };
            // A legacy file may belong to other mods: the adapter distinguishes that case.
            bool legacyDamaged = false;
            if (File.Exists(legacy))
                try { legacyDecode(File.ReadAllText(legacy)); } catch (Exception) { legacyDamaged = true; }
            return new ProgressRead { Data = new SaveProgress(), WriteAllowed = !damaged && !legacyDamaged,
                Message = damaged || legacyDamaged ? "Cannot recover Wisp progress. Saving marks is disabled; original files are untouched." : "" };
        }

        public void Save(string path, ProgressRead state)
        {
            if (!state.WriteAllowed) throw new InvalidOperationException(state.Message);
            string temp = path + ".tmp";
            File.WriteAllText(temp, encode(state.Data));
            if (File.Exists(path))
            {
                if (state.PreserveOriginal)
                    File.Copy(path, path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N"));
                File.Replace(temp, path, state.PreserveOriginal ? null : path + ".bak");
            }
            else File.Move(temp, path);
            state.PreserveOriginal = false;
        }
    }
}
