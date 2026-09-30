using System.Threading;
using System.Threading.Tasks;
using HstMulligan.Core.Models;

namespace HstMulligan.Core.Data
{
    public interface IMulliganDataSource
    {
        string Name { get; }
        Task<MulliganDataset> FetchAsync(MulliganQuery query, CancellationToken ct);
    }
}
