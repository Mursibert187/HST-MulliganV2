using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HstMulligan.Core.Data;
using HstMulligan.Core.Models;
using Xunit;

namespace HstMulligan.Core.Tests
{
    public class DeckPageScraperSourceTests
    {
        private static MulliganQuery Q() =>
            new MulliganQuery(FormatType.Standard, RankBracket.AllRanks,
                deckCode: null, archetypeId: null, deckShortId: "AbC123XyZ");

        [Fact]
        public void NextDataBlobYieldsOverallAndPerOpponentRows()
        {
            var html = @"<html><body>
                <script id=""__NEXT_DATA__"" type=""application/json"">
                {""props"":{""pageProps"":{""deck"":{""mulligan"":{
                    ""ALL"":  [{""dbf_id"":1001,""sample_size"":200,""kept_percent"":80,""winrate_when_kept"":58},
                               {""dbf_id"":1002,""sample_size"":150,""kept_percent"":40,""winrate_when_kept"":46}],
                    ""MAGE"": [{""dbf_id"":1001,""sample_size"":50,""kept_percent"":92,""winrate_when_kept"":62}]
                }}}}}
                </script></body></html>";
            var ds = DeckPageScraperSource.ParseHtml(html, Q());
            Assert.True(ds.TryGet(1001, out var c1));
            Assert.Equal(200, c1.Overall.Total);
            Assert.Equal(0.80, c1.Overall.KeepRate, precision: 3);
            Assert.Equal(0.58, c1.Overall.KeptWinrate, precision: 3);
            Assert.True(c1.ByOpponent.TryGetValue(OpponentClass.Mage, out var mage));
            Assert.Equal(50, mage.Total);
            Assert.Equal(0.92, mage.KeepRate, precision: 3);

            Assert.True(ds.TryGet(1002, out var c2));
            Assert.Equal(150, c2.Overall.Total);
            Assert.Equal(0.40, c2.Overall.KeepRate, precision: 3);
        }

        [Fact]
        public void DataPropsReactContainerIsAlsoAccepted()
        {
            var inner = @"{""rows"":[{""dbf_id"":42,""sample_size"":1000,""kept_percent"":74.0,""winrate_when_kept"":55.5}]}";
            var escaped = inner.Replace("\"", "&quot;");
            var html = $@"<html><body>
                <div data-react-component=""SingleDeckMulliganGuide"" data-props=""{escaped}""></div>
            </body></html>";
            var ds = DeckPageScraperSource.ParseHtml(html, Q());
            Assert.True(ds.TryGet(42, out var c));
            Assert.Equal(1000, c.Overall.Total);
            Assert.Equal(0.74, c.Overall.KeepRate, precision: 3);
            Assert.Equal(0.555, c.Overall.KeptWinrate, precision: 3);
        }

        [Fact]
        public void EmptyHtmlYieldsEmptyDataset()
        {
            var ds = DeckPageScraperSource.ParseHtml("", Q());
            Assert.Equal(0, ds.Cards.Count);
        }

        [Fact]
        public void HtmlWithoutRecognizableBlobYieldsEmptyDataset()
        {
            var ds = DeckPageScraperSource.ParseHtml(
                "<html><body><h1>Login required</h1></body></html>", Q());
            Assert.Equal(0, ds.Cards.Count);
        }

        [Fact]
        public async Task FetchAsyncWithNoShortIdReturnsEmpty()
        {
            var src = new DeckPageScraperSource(new HttpClient(new DummyHandler()));
            var ds = await src.FetchAsync(
                new MulliganQuery(FormatType.Standard, RankBracket.AllRanks),
                CancellationToken.None);
            Assert.Same(MulliganDataset.Empty, ds);
        }

        [Fact]
        public async Task FetchAsyncIssuesGetAgainstTemplate()
        {
            var seen = new List<Uri>();
            var html = @"<script id=""__NEXT_DATA__"" type=""application/json"">
                {""rows"":[{""dbf_id"":7,""sample_size"":100,""kept_percent"":60,""winrate_when_kept"":52}]}
                </script>";
            var handler = new DummyHandler
            {
                OnSend = req =>
                {
                    seen.Add(req.RequestUri);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(html, Encoding.UTF8, "text/html"),
                    };
                },
            };
            var src = new DeckPageScraperSource(new HttpClient(handler),
                urlTemplate: "https://example.test/decks/{shortId}/");
            var ds = await src.FetchAsync(Q(), CancellationToken.None);
            Assert.Single(seen);
            Assert.Equal("https://example.test/decks/AbC123XyZ/", seen[0].ToString());
            Assert.True(ds.TryGet(7, out _));
        }

        private sealed class DummyHandler : HttpMessageHandler
        {
            public Func<HttpRequestMessage, HttpResponseMessage> OnSend { get; set; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
            {
                var resp = OnSend?.Invoke(req) ?? new HttpResponseMessage(HttpStatusCode.NoContent);
                return Task.FromResult(resp);
            }
        }
    }
}
