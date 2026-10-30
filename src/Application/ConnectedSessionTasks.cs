using DLLNelogica.Configuration;
using DLLNelogica.Interop;
using DLLNelogica.MarketData;
using DLLNelogica.Orders;

namespace DLLNelogica.Application;

internal static class ConnectedSessionTasks
{
    internal static async Task RunAsync(
        ProfitSession session, ApplicationOptions options, ConsoleShutdown shutdown, MarketDataSnapshot prices)
    {
        var accounts = session.MonitorAccountsAsync(shutdown.Source.Token);
        var orders = Task.Run(() => new OrderSequence(session.Orders, prices: prices)
            .RunAsync(options.OrderSequence, shutdown.Source.Token));
        await shutdown.WaitAsync().ConfigureAwait(false);
        await orders.ConfigureAwait(false);
        await accounts.ConfigureAwait(false);
    }
}
