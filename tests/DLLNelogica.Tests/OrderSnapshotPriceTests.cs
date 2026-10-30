using System.Diagnostics;
using DLLNelogica.Configuration;
using DLLNelogica.Interop;
using DLLNelogica.MarketData;
using DLLNelogica.Orders;

namespace DLLNelogica.Tests;

public sealed class OrderSnapshotPriceTests
{
    [Fact]
    public async Task LimitPriceFollowsTheSnapshotAgeRule()
    {
        var scenario = new AccountApiScenario();
        double? sent = null;
        scenario.Api.OnSendOrder = order =>
        {
            sent = order.Price;
            return 1;
        };
        scenario.Api.OnOrderDetails = (ref TConnectorOrderOut order) =>
        {
            order.OrderStatus = 2;
            return 0;
        };
        var gateway = new ProfitOrderGateway(scenario.Api);
        gateway.Start();
        var lines = new List<string>();
        var stale = new MarketDataSnapshot();
        stale.Record(new RawPriceChange(new MarketInstrument("WINV26", "F", 0), "data", 1, 1, 1000)
        {
            ReceivedTimestamp = Stopwatch.GetTimestamp() - (31 * Stopwatch.Frequency)
        });
        await new OrderSequence(gateway, lines.Add, (_, _) => Task.CompletedTask, prices: stale)
            .RunAsync(LimitOnly(), CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Null(sent);
        Assert.Contains(lines, line => line.Contains("sequência interrompida por falha", StringComparison.Ordinal));

        sent = null;
        lines.Clear();
        await new OrderSequence(gateway, lines.Add, (_, _) => Task.CompletedTask, prices: AccountApiScenario.FreshQuote(1000))
            .RunAsync(LimitOnly(), CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(990d, sent);
        Assert.Contains(lines, line => line.Contains("referência=1000", StringComparison.Ordinal));
    }

    private static OrderSequenceOptions LimitOnly() => new()
    {
        Enabled = true, BrokerId = 15, AccountId = "0012703", RoutingPassword = "secret",
        Ticker = "WINV26", Exchange = "F", Quantity = 1, TickSize = 5,
        BuyLimitOffset = 10, SellLimitOffset = 10, BuyMarket = false, SellMarket = false, SellLimit = false,
        WaitSeconds = [0, 0, 0, 0, 0]
    };
}
