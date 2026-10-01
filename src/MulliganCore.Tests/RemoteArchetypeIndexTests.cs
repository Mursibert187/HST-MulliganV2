using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HstMulligan.Core.Abstractions;
using HstMulligan.Core.Models;
using Xunit;

namespace HstMulligan.Core.Tests
{
    public class RemoteArchetypeIndexTests
    {
        private sealed class StubHandler : HttpMessageHandler
        {
            public Func<HttpRequestMessage, HttpResponseMessage> OnSend { get; set; }
            public int Calls { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
            {
                Calls++;
                return Task.FromResult(OnSend(req));
            }
        }

        private static HttpResponseMessage Ok(string body) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        private static string EnvelopeFor(string classWire, string archetypeId, string signature) =>
            "{\"classes\":[{\"class\":\"" + classWire + "\",\"archetypes\":[" +
            "{\"id\":\"" + archetypeId + "\",\"signature\":\"" + signature + "\"}]}]}";

        [Fact]
        public async Task PrimeAsyncLoadsAndResolves()
        {
            var ids = new[] { 10, 20, 30 };
            var sig = Signature.Compute(ids);
            var stub = new StubHandler { OnSend = _ => Ok(EnvelopeFor("MAGE", "big-mage", sig)) };
            var http = new HttpClient(stub);
            var idx = new RemoteArchetypeIndex(http, "https://example.test/idx.json",
                TimeSpan.FromMinutes(10), primeImmediately: false);
            await idx.PrimeAsync(CancellationToken.None);
            Assert.Equal("big-mage", idx.Resolve(OpponentClass.Mage, ids));
            Assert.Null(idx.Resolve(OpponentClass.Warrior, ids));
            Assert.Equal(1, stub.Calls);
        }

        [Fact]
        public async Task ResolveRefetchesAfterTtl()
        {
            var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var ids = new[] { 1, 2, 3 };
            var sig = Signature.Compute(ids);
            var stub = new StubHandler { OnSend = _ => Ok(EnvelopeFor("ROGUE", "mech-rogue", sig)) };
            var http = new HttpClient(stub);
            var idx = new RemoteArchetypeIndex(http, "https://example.test/idx.json",
                TimeSpan.FromMinutes(10), clock: () => now, primeImmediately: false);
            await idx.PrimeAsync(CancellationToken.None);
            var before = stub.Calls;
            idx.Resolve(OpponentClass.Rogue, ids);
            Assert.Equal(before, stub.Calls); // within TTL, no re-fetch

            now = now.AddMinutes(30); // past TTL
            idx.Resolve(OpponentClass.Rogue, ids);
            // wait briefly for background refresh scheduled by Resolve
            for (int i = 0; i < 20 && stub.Calls == before; i++)
                await Task.Delay(25);
            Assert.True(stub.Calls > before, $"expected refetch, got {stub.Calls} calls");
        }

        [Fact]
        public async Task FetchFailureKeepsLastGoodSnapshot()
        {
            var ids = new[] { 7, 7, 7 };
            var sig = Signature.Compute(ids);
            var stub = new StubHandler { OnSend = _ => Ok(EnvelopeFor("DRUID", "ramp-druid", sig)) };
            var http = new HttpClient(stub);
            var idx = new RemoteArchetypeIndex(http, "https://example.test/idx.json",
                TimeSpan.FromMinutes(10), primeImmediately: false);
            await idx.PrimeAsync(CancellationToken.None);
            Assert.Equal("ramp-druid", idx.Resolve(OpponentClass.Druid, ids));

            stub.OnSend = _ => throw new HttpRequestException("boom");
            await idx.PrimeAsync(CancellationToken.None); // should swallow the failure
            Assert.Equal("ramp-druid", idx.Resolve(OpponentClass.Druid, ids));
        }
    }
}
