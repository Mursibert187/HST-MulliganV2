using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HstMulligan.Core.Data;
using HstMulligan.Core.Models;
using Xunit;

namespace HstMulligan.Core.Tests
{
    public class DataSourceTests
    {
        private static Stream S(string s) => new MemoryStream(Encoding.UTF8.GetBytes(s));

        [Fact]
        public void FirestoneEnvelopeParses()
        {
            var json = @"{
              ""format"": ""standard"",
              ""rankBracket"": ""platinum-diamond"",
              ""baseWinrate"": 0.51,
              ""cards"": [
                {
                  ""dbfId"": 1,
                  ""overall"": {""kept"": 800, ""mulliganed"": 200, ""keptWinrate"": 0.54},
                  ""byOpponent"": { ""MAGE"": {""kept"": 100, ""mulliganed"": 20, ""keptWinrate"": 0.60} },
                  ""byDeck"": { ""AAA"": {""kept"": 40, ""mulliganed"": 10, ""keptWinrate"": 0.66} }
                }
              ]
            }";
            var q = new MulliganQuery(FormatType.Standard, RankBracket.PlatinumDiamond);
            var ds = FirestoneDataSource.Parse(S(json), q);
            Assert.Equal(FormatType.Standard, ds.Format);
            Assert.Equal(0.51, ds.BaseWinrate, precision: 4);
            Assert.True(ds.TryGet(1, out var stats));
            Assert.Equal(1000, stats.Overall.Total);
            Assert.Equal(0.80, stats.Overall.KeepRate, precision: 4);
            Assert.Contains(OpponentClass.Mage, stats.ByOpponent.Keys);
            Assert.Contains("AAA", stats.ByDeck.Keys);
        }

        [Fact]
        public void HsReplayEnvelopePercentsConvertToFractions()
        {
            var json = @"{
              ""series"": {
                ""metadata"": {""base_winrate"": 50.0},
                ""data"": {
                  ""ALL"":  [ {""dbf_id"": 42, ""sample_size"": 1000, ""kept_percent"": 74.0, ""winrate_when_kept"": 55.0} ],
                  ""MAGE"": [ {""dbf_id"": 42, ""sample_size"": 200,  ""kept_percent"": 82.0, ""winrate_when_kept"": 60.0} ]
                }
              }
            }";
            var q = new MulliganQuery(FormatType.Standard, RankBracket.AllRanks);
            var ds = HsReplayDataSource.Parse(S(json), q);
            Assert.Equal(0.50, ds.BaseWinrate, precision: 4);
            Assert.True(ds.TryGet(42, out var stats));
            Assert.Equal(1000, stats.Overall.Total);
            Assert.Equal(0.74, stats.Overall.KeepRate, precision: 3);
            Assert.True(stats.ByOpponent.TryGetValue(OpponentClass.Mage, out var mage));
            Assert.Equal(200, mage.Total);
            Assert.Equal(0.60, mage.KeptWinrate, precision: 3);
        }

        [Fact]
        public async Task CachingDataSourceReturnsLastGoodOnError()
        {
            var stub = new StubSource();
            stub.Result = MakeDataset(dbf: 5, kept: 100, mull: 100);
            var now = DateTimeOffset.UtcNow;
            var cache = new CachingDataSource(stub, TimeSpan.FromMinutes(5), () => now);
            var q = new MulliganQuery(FormatType.Standard, RankBracket.AllRanks);
            var first = await cache.FetchAsync(q, CancellationToken.None);
            Assert.NotEmpty(first.Cards);

            now = now.AddMinutes(10); // TTL expired
            stub.ThrowOnFetch = true;
            var second = await cache.FetchAsync(q, CancellationToken.None);
            Assert.Equal(first, second);
        }

        private sealed class StubSource : IMulliganDataSource
        {
            public string Name => "stub";
            public MulliganDataset Result { get; set; }
            public bool ThrowOnFetch { get; set; }
            public Task<MulliganDataset> FetchAsync(MulliganQuery query, CancellationToken ct)
            {
                if (ThrowOnFetch) throw new IOException("boom");
                return Task.FromResult(Result);
            }
        }

        private static MulliganDataset MakeDataset(int dbf, int kept, int mull)
        {
            var stats = new CardStats(dbf, new KeepRateSample(kept, mull, 0.5));
            return new MulliganDataset(FormatType.Standard, RankBracket.AllRanks,
                DateTimeOffset.UtcNow, 0.5,
                new System.Collections.Generic.Dictionary<int, CardStats> { [dbf] = stats });
        }
    }
}
