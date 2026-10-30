using DLLNelogica.Configuration;
using DLLNelogica.Connection;
using DLLNelogica.Interop;
using DLLNelogica.Logging;

namespace DLLNelogica.MarketData;

internal sealed class MarketDataPipelineFactory
{
    private readonly ProfitCallbackBridge _callbackBridge;
    private readonly ConnectionStateEventPump _stateEvents;
    private readonly MarketDataSubscriptionManager _subscriptions;
    private readonly MarketDataMetrics _metrics;
    private readonly IReportLog _reportLog;
    private readonly MarketDataSnapshot _prices;

    internal MarketDataPipelineFactory(
        ProfitCallbackBridge callbackBridge,
        ConnectionStateEventPump stateEvents,
        MarketDataSubscriptionManager subscriptions,
        MarketDataMetrics metrics,
        IReportLog reportLog,
        MarketDataSnapshot prices)
    {
        _callbackBridge = callbackBridge;
        _stateEvents = stateEvents;
        _subscriptions = subscriptions;
        _metrics = metrics;
        _reportLog = reportLog;
        _prices = prices;
    }

    internal MarketDataRuntimePipeline Start(
        MarketDataOptions marketData,
        CancellationTokenSource shutdownRequested) =>
        new(
            marketData,
            shutdownRequested,
            _callbackBridge,
            _stateEvents,
            _subscriptions,
            _metrics,
            _reportLog,
            _prices);
}
