using System.Diagnostics;
using DLLNelogica.Configuration;
using DLLNelogica.Interop;
using DLLNelogica.MarketData;

namespace DLLNelogica.Tests;

public sealed class OrderConcurrencyTests
{
    [Fact]
    public async Task FinalizationWaitsForNativeOrderAndBlocksSubsequentCalls()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var api = new FakeProfitApi
        {
            OnOrderDetails = (ref TConnectorOrderOut _) =>
            {
                entered.Set();
                Assert.True(release.Wait(TimeSpan.FromSeconds(5)));
                return 0;
            }
        };
        var gateway = new ProfitOrderGateway(api);
        gateway.Start();
        var query = Task.Run(() => gateway.Read(1, CancellationToken.None));
        Task? stop = null;
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
            stop = Task.Run(() => { gateway.StopAndWait(); api.FinalizeServices(); });
            Assert.True(SpinWait.SpinUntil(() => gateway.IsStopping, TimeSpan.FromSeconds(5)));
            Assert.Equal(0, api.FinalizationCalls);
        }
        finally
        {
            release.Set();
            await query;
            if (stop is not null) { await stop; }
        }
        Assert.Equal(1, api.FinalizationCalls);
        Assert.Throws<OperationCanceledException>(() => gateway.Read(1, CancellationToken.None));
        Assert.Throws<OperationCanceledException>(() => gateway.Cancel(new OrderSequenceOptions(), 1, CancellationToken.None));
        Assert.Throws<OperationCanceledException>(() => gateway.Send(new OrderSequenceOptions(), 1, true, 100, CancellationToken.None));
    }

    [Fact]
    public void OldQueuedQuoteIsNotFreshenedByConsumer()
    {
        var staleTimestamp = Stopwatch.GetTimestamp() - (31 * Stopwatch.Frequency);
        var cached = new RawPriceChange(new MarketInstrument("WINV26", "F", 0), "data", 1, 1, 1000)
        {
            ReceivedTimestamp = staleTimestamp
        };
        var stale = new MarketDataSnapshot();
        stale.Record(cached);
        Assert.Null(stale.LatestPrice("WINV26:F"));

        var fresh = new MarketDataSnapshot();
        fresh.Record(cached with { ReceivedTimestamp = Stopwatch.GetTimestamp() });
        Assert.Null(fresh.LatestPrice("WINV26:F"));
        fresh.Record(cached with { ReceivedTimestamp = Stopwatch.GetTimestamp(), Price = 1004 });
        Assert.Equal(1004, fresh.LatestPrice("WINV26:F"));
        fresh.Record(cached with { ReceivedTimestamp = staleTimestamp, Price = 1008 });
        Assert.Null(fresh.LatestPrice("WINV26:F"));

        var shared = new MarketDataSnapshot();
        shared.Record(cached with { ReceivedTimestamp = Stopwatch.GetTimestamp(), Price = 900 });
        Assert.Null(shared.LatestPrice("winv26:f"));
        shared.Record(new RawPriceChange(new MarketInstrument("winv26", "f", 0), "data", 2, 2, 950)
        {
            ReceivedTimestamp = Stopwatch.GetTimestamp()
        });
        Assert.Equal(950, shared.LatestPrice("WINV26:F"));
    }
}
