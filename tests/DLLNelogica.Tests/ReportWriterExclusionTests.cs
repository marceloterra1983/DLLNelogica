using System.Globalization;
using DLLNelogica.Logging;

namespace DLLNelogica.Tests;

public sealed class ReportWriterExclusionTests
{
    [Fact]
    public void SecondWriterDoesNotAppendAndReportsOnce()
    {
        var directory = Directory.CreateTempSubdirectory("DLLNelogica-report-");
        var fallback = new StringWriter();
        ReportFileWriter? first = null;
        ReportFileWriter? second = null;
        try
        {
            var now = DateTime.Now;
            first = new ReportFileWriter(directory.FullName, "origem", TextWriter.Null);
            second = new ReportFileWriter(directory.FullName, "origem", fallback);
            first.Write(now, "_Sessao.txt", LogLinePrefix.SessionStamp, "primeira\n");
            first.Flush();

            var path = Path.Combine(first.CurrentDirectory, "_Sessao.txt");
            Assert.Equal(
                now.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                Path.GetFileName(first.CurrentDirectory));
            Assert.Contains(
                SharedReportReader.Events(path),
                line => line.Contains("primeira", StringComparison.Ordinal));

            second.Write(now, "_Sessao.txt", LogLinePrefix.SessionStamp, "segunda\n");
            second.Flush();
            Assert.DoesNotContain(
                "segunda",
                string.Join('\n', SharedReportReader.Events(path)),
                StringComparison.Ordinal);

            second.Write(now, "_Sessao.txt", LogLinePrefix.SessionStamp, "segunda\n");
            var warnings = fallback.ToString();
            var index = warnings.IndexOf("IOException", StringComparison.Ordinal);
            Assert.True(index >= 0);
            Assert.DoesNotContain(
                "IOException",
                warnings[(index + "IOException".Length)..],
                StringComparison.Ordinal);

            second.Dispose();
            first.Dispose();
            second = null;
            first = null;

            var text = File.ReadAllText(path);
            Assert.Contains("primeira", text, StringComparison.Ordinal);
            Assert.DoesNotContain("segunda", text, StringComparison.Ordinal);
        }
        finally
        {
            second?.Dispose();
            first?.Dispose();
            fallback.Dispose();
            directory.Delete(true);
        }
    }
}
