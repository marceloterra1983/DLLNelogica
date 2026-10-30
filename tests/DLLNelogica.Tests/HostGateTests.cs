using DLLNelogica.Interop;

namespace DLLNelogica.Tests;

public sealed class HostGateTests
{
    [Fact]
    public void NativeAdapterRejectsThisHostBeforeLoadingOrTouchingProcessLifetime()
    {
        var api = new ProfitNativeApi(Path.Combine(Path.GetTempPath(), "profit-host-gate"));
        var rejected = Assert.Throws<ProfitHostRejectedException>(() =>
            api.Initialize("test", "test", "test", ProfitCallbackRoots.Callbacks));
        Assert.Equal("A ProfitDLL somente pode ser carregada em um processo Windows.", rejected.Message);
    }

    [Fact]
    public void RefusalNamesTheFailingHostCondition()
    {
        const string path = "/opt/app/ProfitDLL.dll";
        Assert.Equal(
            "A ProfitDLL somente pode ser carregada em um processo Windows.",
            ProfitHostGate.Refusal(path, () => false, () => true, _ => true));
        Assert.Equal(
            "A ProfitDLL Win64 exige que o processo seja executado em arquitetura x64.",
            ProfitHostGate.Refusal(path, () => true, () => false, _ => true));
        Assert.Equal(
            $"ProfitDLL.dll não foi encontrada no diretório da aplicação: {path}",
            ProfitHostGate.Refusal(path, () => true, () => true, _ => false));
        Assert.Null(ProfitHostGate.Refusal(path, () => true, () => true, _ => true));
    }
}
