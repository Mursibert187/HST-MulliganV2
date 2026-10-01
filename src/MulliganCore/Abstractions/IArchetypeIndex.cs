using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Abstractions
{
    /// <summary>
    /// Maps a canonicalized decklist signature to an archetype slug the data
    /// source understands (e.g. "aggro-paladin-fs"). Implementations may be
    /// bundled JSON, a remote index, or an in-memory fake for tests.
    /// </summary>
    public interface IArchetypeIndex
    {
        string Resolve(OpponentClass heroClass, IReadOnlyCollection<int> dbfIds);
    }

    public sealed class NullArchetypeIndex : IArchetypeIndex
    {
        public static readonly NullArchetypeIndex Instance = new NullArchetypeIndex();
        public string Resolve(OpponentClass heroClass, IReadOnlyCollection<int> dbfIds) => null;
    }

    /// <summary>
    /// Signature → archetype slug lookup backed by a JSON file matching:
    /// <code>{"class":"MAGE","archetypes":[{"id":"big-mage","signature":"3f1a…"},…]}</code>.
    /// The signature is a SHA1 over the sorted dbfId list, hex-encoded.
    /// </summary>
    public sealed class JsonArchetypeIndex : IArchetypeIndex
    {
        private readonly Dictionary<OpponentClass, Dictionary<string, string>> _byClass
            = new Dictionary<OpponentClass, Dictionary<string, string>>();

        public JsonArchetypeIndex(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            try
            {
                using var stream = File.OpenRead(path);
                using var doc = JsonDocument.Parse(stream);
                var root = doc.RootElement;
                if (!root.TryGetProperty("classes", out var classes) || classes.ValueKind != JsonValueKind.Array)
                    return;
                foreach (var cls in classes.EnumerateArray())
                {
                    if (!cls.TryGetProperty("class", out var cName)) continue;
                    var oc = OpponentClassExtensions.FromWireString(cName.GetString());
                    if (oc == OpponentClass.Unknown) continue;
                    if (!_byClass.TryGetValue(oc, out var sigMap))
                        _byClass[oc] = sigMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (!cls.TryGetProperty("archetypes", out var archs) || archs.ValueKind != JsonValueKind.Array)
                        continue;
                    foreach (var a in archs.EnumerateArray())
                    {
                        if (!a.TryGetProperty("id", out var id)) continue;
                        if (!a.TryGetProperty("signature", out var sig)) continue;
                        sigMap[sig.GetString() ?? ""] = id.GetString();
                    }
                }
            }
            catch { }
        }

        public string Resolve(OpponentClass heroClass, IReadOnlyCollection<int> dbfIds)
        {
            if (dbfIds == null || dbfIds.Count == 0) return null;
            if (!_byClass.TryGetValue(heroClass, out var sigs)) return null;
            var signature = Signature.Compute(dbfIds);
            return sigs.TryGetValue(signature, out var id) ? id : null;
        }
    }

    public static class Signature
    {
        public static string Compute(IEnumerable<int> dbfIds)
        {
            var sorted = dbfIds.Where(i => i > 0).OrderBy(i => i).ToArray();
            if (sorted.Length == 0) return "";
            var sb = new StringBuilder(sorted.Length * 6);
            for (int i = 0; i < sorted.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(sorted[i]);
            }
            using var sha = SHA1.Create();
            var bytes = sha.ComputeHash(Encoding.ASCII.GetBytes(sb.ToString()));
            var hex = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++) hex.Append(bytes[i].ToString("x2"));
            return hex.ToString();
        }
    }
}
