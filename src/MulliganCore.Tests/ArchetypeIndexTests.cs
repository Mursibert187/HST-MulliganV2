using System.Collections.Generic;
using System.IO;
using HstMulligan.Core.Abstractions;
using HstMulligan.Core.Live;
using HstMulligan.Core.Models;
using Xunit;

namespace HstMulligan.Core.Tests
{
    public class ArchetypeIndexTests
    {
        [Fact]
        public void SignatureIsDeterministicRegardlessOfOrder()
        {
            var a = Signature.Compute(new[] { 100, 200, 300, 100 });
            var b = Signature.Compute(new[] { 300, 100, 200, 100 });
            Assert.Equal(a, b);
            Assert.NotEmpty(a);
        }

        [Fact]
        public void SignatureIgnoresNonPositiveIds()
        {
            var a = Signature.Compute(new[] { 100, 200, 300 });
            var b = Signature.Compute(new[] { 100, 200, 300, 0, -1 });
            Assert.Equal(a, b);
        }

        [Fact]
        public void JsonIndexResolvesKnownSignature()
        {
            var ids = new[] { 1, 2, 3, 4, 5 };
            var sig = Signature.Compute(ids);
            var json = "{\"classes\":[{\"class\":\"MAGE\",\"archetypes\":[" +
                       "{\"id\":\"big-mage\",\"signature\":\"" + sig + "\"}]}]}";
            var path = Path.Combine(Path.GetTempPath(), $"archetype-{System.Guid.NewGuid():N}.json");
            File.WriteAllText(path, json);
            try
            {
                var idx = new JsonArchetypeIndex(path);
                Assert.Equal("big-mage", idx.Resolve(OpponentClass.Mage, ids));
                Assert.Null(idx.Resolve(OpponentClass.Warrior, ids));
                Assert.Null(idx.Resolve(OpponentClass.Mage, new[] { 999 }));
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void MissingFileYieldsEmptyIndex()
        {
            var idx = new JsonArchetypeIndex(Path.Combine(Path.GetTempPath(), "nope-" + System.Guid.NewGuid()));
            Assert.Null(idx.Resolve(OpponentClass.Mage, new[] { 1 }));
        }

        [Fact]
        public void CardStatsPrefersArchetypeOverOpponent()
        {
            var byOpp = new Dictionary<OpponentClass, KeepRateSample>
            {
                [OpponentClass.Mage] = new KeepRateSample(40, 60, 0.45),
            };
            var byArch = new Dictionary<string, KeepRateSample>
            {
                ["aggro-paladin-fs"] = new KeepRateSample(300, 100, 0.60),
            };
            var stats = new CardStats(1, new KeepRateSample(500, 500, 0.50), byOpp,
                byDeck: null, byArchetype: byArch);
            var picked = stats.Resolve(OpponentClass.Mage, deckCode: null, archetypeId: "aggro-paladin-fs");
            Assert.Equal(400, picked.Total);
            Assert.Equal(0.75, picked.KeepRate, precision: 3);
        }
    }
}
