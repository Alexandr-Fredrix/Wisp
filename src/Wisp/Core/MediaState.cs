using System;
using System.Collections.Generic;

namespace Wisp.Core
{
    public enum ImageLoadState { Missing, Queued, Loading, Failed, Ready }

    public sealed class ImageLoads
    {
        private readonly Dictionary<string, ImageLoadState> states = new Dictionary<string, ImageLoadState>();
        public int Active { get; private set; }
        public ImageLoadState State(string key) { ImageLoadState value; return states.TryGetValue(key, out value) ? value : ImageLoadState.Missing; }
        public bool TryStart(string key)
        {
            var state = State(key);
            if (state == ImageLoadState.Loading || state == ImageLoadState.Failed || state == ImageLoadState.Ready) return false;
            if (Active >= 3) { states[key] = ImageLoadState.Queued; return false; }
            states[key] = ImageLoadState.Loading; Active++; return true;
        }
        public void Finish(string key, bool success)
        {
            if (State(key) == ImageLoadState.Loading) Active--;
            states[key] = success ? ImageLoadState.Ready : ImageLoadState.Failed;
        }
        public void Retry(string key) { if (State(key) == ImageLoadState.Failed) states.Remove(key); }
        public void Evicted(string key) { if (State(key) == ImageLoadState.Ready) states.Remove(key); }
    }

    // Account for decoded pixels, not compressed file size. The renderer owns textures.
    public sealed class ImageBudget
    {
        public const long DefaultBytes = 128L * 1024 * 1024;
        private readonly long limit;
        private readonly int countLimit;
        private readonly LinkedList<string> recent = new LinkedList<string>();
        private readonly Dictionary<string, long> sizes = new Dictionary<string, long>();
        public long Bytes { get; private set; }
        public ImageBudget(long limit = DefaultBytes, int countLimit = 24) { this.limit = limit; this.countLimit = countLimit; }
        public bool Fits(long bytes) { return bytes > 0 && bytes <= limit; }
        public void Touch(string key) { if (sizes.ContainsKey(key)) { recent.Remove(key); recent.AddLast(key); } }
        public string[] Admit(string key, long bytes)
        {
            if (!Fits(bytes)) throw new ArgumentOutOfRangeException("bytes");
            var removed = new List<string>();
            if (sizes.ContainsKey(key)) Remove(key);
            while (recent.Count >= countLimit || Bytes + bytes > limit)
            { var oldest = recent.First.Value; Remove(oldest); removed.Add(oldest); }
            sizes[key] = bytes; recent.AddLast(key); Bytes += bytes;
            return removed.ToArray();
        }
        public void Remove(string key) { long size; if (sizes.TryGetValue(key, out size)) { Bytes -= size; sizes.Remove(key); recent.Remove(key); } }
        public void Clear() { sizes.Clear(); recent.Clear(); Bytes = 0; }
    }
}
