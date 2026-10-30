using DLLNelogica.Connection;

namespace DLLNelogica.Tests;

public sealed class ConnectionStateMachineTests
{
    [Fact]
    public async Task OrdemInversaDosQuatroSucessosCompletaAEsperaUmaUnicaVez()
    {
        var machine = new ConnectionStateMachine();
        ProcessarSucessosEmOrdemInversa(machine);

        var espera = await machine.WaitForConnectionAsync(TimeSpan.FromSeconds(1));
        var pronto = machine.GetSnapshot();
        Assert.True(espera.IsConnected);
        Assert.Equal(1, pronto.ReadinessTransitionCount);
        Assert.True(pronto.IsInitialConnectionReady);

        ProcessarSucessosEmOrdemInversa(machine);
        Assert.Equal(1, machine.GetSnapshot().ReadinessTransitionCount);
    }

    [Fact]
    public async Task RoteamentoCincoContinuaProntoDepoisDoResultadoUm()
    {
        var machine = new ConnectionStateMachine();
        machine.Process(ConnectionStateType.Routing, (int)RoutingStateResult.BrokerConnected);
        machine.Process(ConnectionStateType.Routing, 1);
        Assert.True(machine.GetSnapshot().IsRoutingReady);

        machine.Process(ConnectionStateType.Login, (int)LoginStateResult.Connected);
        machine.Process(ConnectionStateType.MarketData, (int)MarketDataStateResult.Connected);
        machine.Process(ConnectionStateType.Activation, (int)ActivationStateResult.Valid);
        var espera = await machine.WaitForConnectionAsync(TimeSpan.FromSeconds(1));
        Assert.True(espera.IsConnected);
    }

    [Fact]
    public async Task MarketDataCincoNaoAlcancaEQuatroNaoSeApagaComCincoOuSeis()
    {
        var soCinco = new ConnectionStateMachine();
        soCinco.Process(ConnectionStateType.MarketData, (int)MarketDataStateResult.Degraded);
        var espera = await soCinco.WaitForConnectionAsync(TimeSpan.FromMilliseconds(200));
        var degradado = soCinco.GetSnapshot();
        Assert.False(espera.IsConnected);
        Assert.Contains("market data", espera.Message, StringComparison.Ordinal);
        Assert.False(degradado.HasReachedMarketData);
        Assert.Equal(MarketDataHealth.Degraded, degradado.CurrentMarketDataHealth);

        var quatroECinco = new ConnectionStateMachine();
        quatroECinco.Process(ConnectionStateType.MarketData, (int)MarketDataStateResult.Connected);
        quatroECinco.Process(ConnectionStateType.MarketData, (int)MarketDataStateResult.Degraded);
        var aposCinco = quatroECinco.GetSnapshot();
        Assert.True(aposCinco.HasReachedMarketData);
        Assert.Equal(MarketDataHealth.Degraded, aposCinco.CurrentMarketDataHealth);

        var quatroESeis = new ConnectionStateMachine();
        quatroESeis.Process(ConnectionStateType.MarketData, (int)MarketDataStateResult.Connected);
        quatroESeis.Process(ConnectionStateType.MarketData, (int)MarketDataStateResult.Critical);
        var aposSeis = quatroESeis.GetSnapshot();
        Assert.True(aposSeis.HasReachedMarketData);
        Assert.Equal(MarketDataHealth.Critical, aposSeis.CurrentMarketDataHealth);
    }

    [Fact]
    public async Task AtivacaoZeroSeguidaDeUmBloqueiaAteUmNovoZero()
    {
        var machine = new ConnectionStateMachine();
        machine.Process(ConnectionStateType.Activation, (int)ActivationStateResult.Valid);
        machine.Process(ConnectionStateType.Activation, 1);
        Assert.False(machine.GetSnapshot().IsActivationValid);

        machine.Process(ConnectionStateType.Login, (int)LoginStateResult.Connected);
        machine.Process(ConnectionStateType.Routing, (int)RoutingStateResult.BrokerConnected);
        machine.Process(ConnectionStateType.MarketData, (int)MarketDataStateResult.Connected);
        var espera = await machine.WaitForConnectionAsync(TimeSpan.FromMilliseconds(200));
        Assert.False(espera.IsConnected);
        Assert.Contains("ativação", espera.Message, StringComparison.Ordinal);

        machine.Process(ConnectionStateType.Activation, (int)ActivationStateResult.Valid);
        var conectada = await machine.WaitForConnectionAsync(TimeSpan.FromSeconds(1));
        Assert.True(conectada.IsConnected);
    }

    [Fact]
    public async Task AtivacaoSeteDepoisDaEsperaConectadaNaoDesfazOSucesso()
    {
        var machine = new ConnectionStateMachine();
        MarcarQuatroSucessos(machine);
        var conectada = await machine.WaitForConnectionAsync(TimeSpan.FromSeconds(1));
        Assert.True(conectada.IsConnected);

        machine.Process(ConnectionStateType.Activation, 7);
        var deNovo = await machine.WaitForConnectionAsync(TimeSpan.FromSeconds(1));
        var snapshot = machine.GetSnapshot();
        Assert.True(deNovo.IsConnected);
        Assert.False(snapshot.IsActivationValid);
        Assert.Equal(1, snapshot.ReadinessTransitionCount);
    }

    [Fact]
    public async Task ResultadoDoisDeLoginFalhaAEsperaAntesDeConectar()
    {
        var machine = new ConnectionStateMachine();
        machine.Process(ConnectionStateType.Login, (int)LoginStateResult.InvalidPassword);
        var espera = await machine.WaitForConnectionAsync(TimeSpan.FromSeconds(1));
        Assert.False(espera.IsConnected);
        Assert.Equal("Senha inválida (resultado 2).", espera.Message);
    }

    [Fact]
    public async Task ResultadoDoisDepoisDosSucessosMantemLoginPronto()
    {
        var machine = new ConnectionStateMachine();
        MarcarQuatroSucessos(machine);
        machine.Process(ConnectionStateType.Login, (int)LoginStateResult.InvalidPassword);
        var espera = await machine.WaitForConnectionAsync(TimeSpan.FromSeconds(1));
        Assert.True(espera.IsConnected);
        Assert.True(machine.GetSnapshot().IsLoginReady);
    }

    [Fact]
    public async Task TipoDesconhecidoNaoCompletaEListaOsQuatroEstados()
    {
        var machine = new ConnectionStateMachine();
        machine.Process((ConnectionStateType)9, 0);
        var espera = await machine.WaitForConnectionAsync(TimeSpan.FromMilliseconds(200));
        Assert.False(espera.IsConnected);
        Assert.Contains("login", espera.Message, StringComparison.Ordinal);
        Assert.Contains("roteamento", espera.Message, StringComparison.Ordinal);
        Assert.Contains("market data", espera.Message, StringComparison.Ordinal);
        Assert.Contains("ativação", espera.Message, StringComparison.Ordinal);
    }

    private static void MarcarQuatroSucessos(ConnectionStateMachine machine)
    {
        machine.Process(ConnectionStateType.Login, (int)LoginStateResult.Connected);
        machine.Process(ConnectionStateType.Routing, (int)RoutingStateResult.BrokerConnected);
        machine.Process(ConnectionStateType.MarketData, (int)MarketDataStateResult.Connected);
        machine.Process(ConnectionStateType.Activation, (int)ActivationStateResult.Valid);
    }

    private static void ProcessarSucessosEmOrdemInversa(ConnectionStateMachine machine)
    {
        machine.Process(ConnectionStateType.Activation, (int)ActivationStateResult.Valid);
        machine.Process(ConnectionStateType.MarketData, (int)MarketDataStateResult.Connected);
        machine.Process(ConnectionStateType.Routing, (int)RoutingStateResult.BrokerConnected);
        machine.Process(ConnectionStateType.Login, (int)LoginStateResult.Connected);
    }
}
