using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace HstMulligan.Core.Localization
{
    public interface ILocalization
    {
        string LocaleCode { get; }
        string T(string key, string fallback = null);
    }

    public sealed class NullLocalization : ILocalization
    {
        public static readonly NullLocalization Instance = new NullLocalization();
        public string LocaleCode => "en";
        public string T(string key, string fallback = null) => fallback ?? key;
    }

    public sealed class JsonLocalization : ILocalization
    {
        public string LocaleCode { get; }
        private readonly IReadOnlyDictionary<string, string> _entries;

        public JsonLocalization(string localeCode, IReadOnlyDictionary<string, string> entries)
        {
            LocaleCode = localeCode ?? "en";
            _entries = entries ?? new Dictionary<string, string>();
        }

        public static JsonLocalization LoadFile(string path, string localeCode)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return new JsonLocalization(localeCode, null);
                using var stream = File.OpenRead(path);
                return Parse(stream, localeCode);
            }
            catch { return new JsonLocalization(localeCode, null); }
        }

        public static JsonLocalization Parse(Stream jsonStream, string localeCode)
        {
            using var doc = JsonDocument.Parse(jsonStream);
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
                foreach (var p in doc.RootElement.EnumerateObject())
                    if (p.Value.ValueKind == JsonValueKind.String)
                        entries[p.Name] = p.Value.GetString();
            return new JsonLocalization(localeCode, entries);
        }

        public string T(string key, string fallback = null)
        {
            if (key != null && _entries.TryGetValue(key, out var v)) return v;
            return fallback ?? key;
        }
    }

    /// <summary>
    /// Ambient localization source. Plugin bootstrap sets it on load; tests
    /// can reset it to NullLocalization.Instance between runs. Deliberately
    /// static so the formatter helpers don't have to thread a service
    /// through every call site.
    /// </summary>
    public static class Localization
    {
        private static ILocalization _current = NullLocalization.Instance;
        public static ILocalization Current
        {
            get => _current;
            set => _current = value ?? NullLocalization.Instance;
        }
    }
}
