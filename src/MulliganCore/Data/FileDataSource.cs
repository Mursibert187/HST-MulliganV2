using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Data
{
    /// <summary>
    /// Loads a MulliganDataset from a local JSON file matching the Firestone
    /// envelope shape. Useful for offline priming and reproducible tests.
    /// </summary>
    public sealed class FileDataSource : IMulliganDataSource
    {
        public string Name => "file";

        private readonly string _path;

        public FileDataSource(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public Task<MulliganDataset> FetchAsync(MulliganQuery query, CancellationToken ct)
        {
            if (!File.Exists(_path))
                return Task.FromResult(MulliganDataset.Empty);
            using (var stream = File.OpenRead(_path))
            {
                return Task.FromResult(FirestoneDataSource.Parse(stream, query));
            }
        }
    }
}
