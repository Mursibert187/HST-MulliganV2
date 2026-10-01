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
    /// <code>{"classes":[{"class":"MAGE","archetypes":[{"id":"big-mage","signature":"3f1a…"}]}]}</code>.
    /// Signature is a SHA1 over the comma-joined sorted dbfId list.
    /// </summary>
    public sealed class JsonArchetypeIndex : IArchetypeIndex
    {
        private readonly IReadOnlyDictionary<OpponentClass, IReadOnlyDictionary<string, string>> _byClass;

        public JsonArchetypeIndex(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                _byClass = ArchetypeIndexParser.Empty;
                return;
            }
            try
            {
                using var stream = File.OpenRead(path);
                _byClass = ArchetypeIndexParser.Parse(stream);
            }
            catch { _byClass = ArchetypeIndexParser.Empty; }
        }

        public string Resolve(OpponentClass heroClass, IReadOnlyCollection<int> dbfIds)
        {
            if (dbfIds == null || dbfIds.Count == 0) return null;
            if (!_byClass.TryGetValue(heroClass, out var sigs)) return null;
            return sigs.TryGetValue(Signature.Compute(dbfIds), out var id) ? id : null;
        }
    }

    public sealed class CompositeArchetypeIndex : IArchetypeIndex
    {
        private readonly IArchetypeIndex[] _layers;

        public CompositeArchetypeIndex(params IArchetypeIndex[] layers)
        {
            _layers = (layers ?? Array.Empty<IArchetypeIndex>())
                .Where(l => l != null).ToArray();
        }

        public string Resolve(OpponentClass heroClass, IReadOnlyCollection<int> dbfIds)
        {
            foreach (var l in _layers)
            {
                var hit = l.Resolve(heroClass, dbfIds);
                if (!string.IsNullOrEmpty(hit)) return hit;
            }
            return null;
        }
    }

    public static class ArchetypeIndexParser
    {
        public static readonly IReadOnlyDictionary<OpponentClass, IReadOnlyDictionary<string, string>> Empty =
            new Dictionary<OpponentClass, IReadOnlyDictionary<string, string>>();

        public static IReadOnlyDictionary<OpponentClass, IReadOnlyDictionary<string, string>> Parse(Stream jsonStream)
        {
            using var doc = JsonDocument.Parse(jsonStream);
            return Parse(doc.RootElement);
        }

        public static IReadOnlyDictionary<OpponentClass, IReadOnlyDictionary<string, string>> Parse(JsonElement root)
        {
            var byClass = new Dictionary<OpponentClass, IReadOnlyDictionary<string, string>>();
            if (!root.TryGetProperty("classes", out var classes) || classes.ValueKind != JsonValueKind.Array)
                return byClass;
            foreach (var cls in classes.EnumerateArray())
            {
                if (!cls.TryGetProperty("class", out var cName)) continue;
                var oc = OpponentClassExtensions.FromWireString(cName.GetString());
                if (oc == OpponentClass.Unknown) continue;
                var sigMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (cls.TryGetProperty("archetypes", out var archs) && archs.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in archs.EnumerateArray())
                    {
                        if (!a.TryGetProperty("id", out var id)) continue;
                        if (!a.TryGetProperty("signature", out var sig)) continue;
                        sigMap[sig.GetString() ?? ""] = id.GetString();
                    }
                }
                byClass[oc] = sigMap;
            }
            return byClass;
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
