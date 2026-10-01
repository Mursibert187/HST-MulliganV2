using System;
using HstMulligan.Core.Abstractions;
using Xunit;

namespace HstMulligan.Core.Tests
{
    public class EventBusTests
    {
        private sealed class Ping { public int N { get; set; } }
        private sealed class Pong { }

        [Fact]
        public void HandlerReceivesPublishedEvent()
        {
            var bus = new EventBus();
            int observed = 0;
            bus.Subscribe<Ping>(p => observed = p.N);
            bus.Publish(new Ping { N = 42 });
            Assert.Equal(42, observed);
        }

        [Fact]
        public void HandlersOfOtherTypesDoNotFire()
        {
            var bus = new EventBus();
            int fired = 0;
            bus.Subscribe<Pong>(_ => fired++);
            bus.Publish(new Ping { N = 1 });
            Assert.Equal(0, fired);
        }

        [Fact]
        public void DisposingSubscriptionStopsDelivery()
        {
            var bus = new EventBus();
            int fired = 0;
            var sub = bus.Subscribe<Ping>(_ => fired++);
            bus.Publish(new Ping());
            sub.Dispose();
            bus.Publish(new Ping());
            Assert.Equal(1, fired);
        }

        [Fact]
        public void ThrowingHandlerDoesNotFaultOthers()
        {
            var bus = new EventBus();
            int survivor = 0;
            bus.Subscribe<Ping>(_ => throw new InvalidOperationException("boom"));
            bus.Subscribe<Ping>(_ => survivor++);
            bus.Publish(new Ping());
            Assert.Equal(1, survivor);
        }
    }
}
