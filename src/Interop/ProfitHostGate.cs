namespace DLLNelogica.Interop;

internal static class ProfitHostGate
{
    internal static string? Refusal(string libraryPath) =>
        Refusal(
            libraryPath,
            static () => OperatingSystem.IsWindows(),
            static () => Environment.Is64BitProcess,
            File.Exists);

    internal static string? Refusal(
        string libraryPath,
        Func<bool> isWindows,
        Func<bool> is64BitProcess,
        Func<string, bool> fileExists)
    {
        if (!isWindows())
        {
            return "A ProfitDLL somente pode ser carregada em um processo Windows.";
        }

        if (!is64BitProcess())
        {
            return "A ProfitDLL Win64 exige que o processo seja executado em arquitetura x64.";
        }

        if (!fileExists(libraryPath))
        {
            return $"ProfitDLL.dll não foi encontrada no diretório da aplicação: {libraryPath}";
        }

        return null;
    }
}

internal sealed class ProfitHostRejectedException(string message) : InvalidOperationException(message);
