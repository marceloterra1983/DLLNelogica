using DLLNelogica.TimesAndTrades;

namespace DLLNelogica.Tests;

internal sealed class TradeOutputProbe : ITradeStorage
{
    internal List<TradeRecord> Records { get; } = [];
    internal Func<TradeRecord, ValueTask>? BeforeWrite { get; init; }

    public async ValueTask WriteAsync(TradeRecord trade)
    {
        if (BeforeWrite is not null)
        {
            await BeforeWrite(trade);
        }

        Records.Add(trade);
    }

    public IReadOnlyList<TradeRecord> Maintain(DateTimeOffset now, bool complete, bool forceFlush = false) => [];

    public void Dispose()
    {
    }
}
