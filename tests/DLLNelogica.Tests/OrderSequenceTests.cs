using DLLNelogica.Configuration;
using DLLNelogica.Interop;
using DLLNelogica.Orders;

namespace DLLNelogica.Tests;

public sealed class OrderSequenceTests
{
    [Theory]
    [InlineData(true, false, false, 4, 2)]
    [InlineData(false, false, false, 2, 2)]
    [InlineData(true, true, false, 4, 0)]
    [InlineData(true, false, true, 4, 0)]
    public async Task SequenceTracksFillsRejectionsAndCancelsOnlyLiveLimits(bool markets, bool filled, bool rejected,
        int expectedSends, int expectedCancels)
    {
        var scenario = new AccountApiScenario();
        var sent = new List<TConnectorSendOrder>();
        var canceled = new HashSet<long>();
        scenario.Api.OnSendOrder = order => { sent.Add(order); return sent.Count; };
        scenario.Api.OnCancelOrder = order => { canceled.Add(order.OrderId.LocalOrderId); return 0; };
        scenario.Api.OnOrderDetails = (ref TConnectorOrderOut order) =>
        {
            var sentOrder = sent[(int)order.OrderId.LocalOrderId - 1];
            order.OrderStatus = rejected ? (byte)8 : canceled.Contains(order.OrderId.LocalOrderId) ? (byte)4 :
                filled || sentOrder.OrderType == 1 ? (byte)2 : (byte)0;
            order.TradedQuantity = order.OrderStatus == 2 ? 1 : 0;
            return 0;
        };
        var gateway = new ProfitOrderGateway(scenario.Api, _ => 1003);
        gateway.Start();
        var lines = new List<string>();
        await new OrderSequence(gateway, lines.Add, (_, _) => Task.CompletedTask)
            .RunAsync(Options(markets), CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(expectedSends, sent.Count);
        Assert.Equal(expectedCancels, canceled.Count);
        Assert.Equal(expectedCancels == 0 ? 2 : 0,
            lines.Count(line => line.Contains("já final; cancelamento dispensado", StringComparison.Ordinal)));
        Assert.All(sent.Where(order => order.OrderType == 2), order => Assert.Equal(0, order.Price % 5));
        Assert.Equal(990, sent.Where(order => order.OrderType == 2 && order.OrderSide == 1).Single().Price);
        Assert.Equal(1015, sent.Where(order => order.OrderType == 2 && order.OrderSide == 2).Single().Price);
        Assert.DoesNotContain(lines, line => line.Contains("secret", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true)]
    public async Task CotacaoAusentePulaLimitadasEAindaEnviaOsMercados(bool markets)
    {
        var options = Options(markets);
        var scenario = new AccountApiScenario();
        var sent = new List<TConnectorSendOrder>();
        scenario.Api.OnSendOrder = order => { sent.Add(order); return sent.Count; };
        scenario.Api.OnOrderDetails = (ref TConnectorOrderOut order) =>
        {
            order.OrderStatus = 2;
            order.TradedQuantity = 1;
            return 0;
        };
        var gateway = new ProfitOrderGateway(scenario.Api, _ => null);
        gateway.Start();
        var lines = new List<string>();
        await new OrderSequence(gateway, lines.Add, (_, _) => Task.CompletedTask)
            .RunAsync(options, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, sent.Count);
        Assert.All(sent, order =>
        {
            Assert.Equal((byte)1, order.OrderType);
            Assert.Equal(-1, order.Price);
        });
        Assert.Equal((byte)1, sent[0].OrderSide);
        Assert.Equal((byte)2, sent[1].OrderSide);
        Assert.Contains(lines, line => line == "Ordens: passo 2 limitada não enviada; sem referência de preço viva (menos de duas cotações ou cotação com mais de 30s).");
        Assert.Contains(lines, line => line == "Ordens: passo 4 limitada não enviada; sem referência de preço viva (menos de duas cotações ou cotação com mais de 30s).");
        Assert.DoesNotContain(lines, line => line.Contains("sequência interrompida", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, line => line.Contains(options.RoutingPassword, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    public async Task CotacaoNaoFinitaPulaALimitada(bool markets)
    {
        var options = Options(markets);
        var scenario = new AccountApiScenario();
        var sent = new List<TConnectorSendOrder>();
        scenario.Api.OnSendOrder = order => { sent.Add(order); return sent.Count; };
        scenario.Api.OnOrderDetails = (ref TConnectorOrderOut order) =>
        {
            order.OrderStatus = 2;
            order.TradedQuantity = 1;
            return 0;
        };
        var gateway = new ProfitOrderGateway(scenario.Api, _ => double.NaN);
        gateway.Start();
        var lines = new List<string>();
        await new OrderSequence(gateway, lines.Add, (_, _) => Task.CompletedTask)
            .RunAsync(options, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Empty(sent);
        Assert.Contains(lines, line => line == "Ordens: passo 2 limitada não enviada; sem referência de preço viva (menos de duas cotações ou cotação com mais de 30s).");
        Assert.Contains(lines, line => line == "Ordens: passo 4 limitada não enviada; sem referência de preço viva (menos de duas cotações ou cotação com mais de 30s).");
        Assert.DoesNotContain(lines, line => line.Contains("sequência interrompida", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, line => line.Contains(options.RoutingPassword, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    public async Task PrecoAlinhadoInvalidoPulaSoEssePasso(bool markets)
    {
        var options = Options(markets);
        var scenario = new AccountApiScenario();
        var sent = new List<TConnectorSendOrder>();
        scenario.Api.OnSendOrder = order => { sent.Add(order); return sent.Count; };
        scenario.Api.OnOrderDetails = (ref TConnectorOrderOut order) =>
        {
            order.OrderStatus = 2;
            order.TradedQuantity = 1;
            return 0;
        };
        var gateway = new ProfitOrderGateway(scenario.Api, _ => 1d);
        gateway.Start();
        var lines = new List<string>();
        await new OrderSequence(gateway, lines.Add, (_, _) => Task.CompletedTask)
            .RunAsync(options, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Single(sent);
        Assert.Equal((byte)2, sent[0].OrderSide);
        Assert.Equal((byte)2, sent[0].OrderType);
        Assert.Equal(15, sent[0].Price);
        Assert.Contains(lines, line => line == "Ordens: passo 2 limitada não enviada; preço alinhado inválido.");
        Assert.DoesNotContain(lines, line => line.Contains("sequência interrompida", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, line => line.Contains(options.RoutingPassword, StringComparison.Ordinal));
    }

    private static OrderSequenceOptions Options(bool markets) => new()
    {
        Enabled = true, BrokerId = 15, AccountId = "0012703", RoutingPassword = "secret",
        Ticker = "WINV26", Exchange = "F", Quantity = 1, TickSize = 5,
        BuyLimitOffset = 10, SellLimitOffset = 10, BuyMarket = markets, SellMarket = markets,
        WaitSeconds = [0, 0, 0, 0, 0]
    };
}
