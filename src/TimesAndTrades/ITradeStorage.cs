using DLLNelogica.Logging;

namespace DLLNelogica.TimesAndTrades;

internal interface ITradeStorage : ITradeOutput, IDisposable
{
    // Executado exclusivamente pelo consumidor, inclusive em períodos sem negócios.
    IReadOnlyList<TradeRecord> Maintain(DateTimeOffset now, bool complete, bool forceFlush = false);
}

internal interface ITradeFileSystem
{
    void CreateDirectory(string path);
    Stream OpenAppend(string path);
}

internal sealed class TradeFileSystem : ITradeFileSystem
{
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    // FileShare.Read exclui segundo escritor no Windows, mas no Linux/Unix não.
    // Trava sidecar com FileShare.None dá exclusividade portátil e mantém leitura do .txt.
    public Stream OpenAppend(string path) => ExclusiveAppendStream.Open(path);
}
