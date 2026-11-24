using System.Text;
using System.Text.Json;
using ProbabilityAudit;

namespace Tests;

public sealed class AuditTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "prob-audit-" + Guid.NewGuid().ToString("N"));
    public AuditTests() => Directory.CreateDirectory(directory);
    private string Input(string content) { var path = Path.Combine(directory, Guid.NewGuid() + ".jsonl"); File.WriteAllText(path, content, new UTF8Encoding(false)); return path; }
    private static (int Code, string Output, string Error) Run(params string[] args) { using var output = new StringWriter(); using var error = new StringWriter(); var code = App.Run(args, output, error); return (code, output.ToString(), error.ToString()); }

    [Fact]
    public void HandComputedBinsBrierEceAndPreservation()
    {
        var path = Input("{\"id\":\"a\",\"probability\":0,\"label\":false}\n{\"id\":\"b\",\"probability\":0.5,\"label\":true}\n{\"id\":\"c\",\"probability\":1,\"label\":true}\n{\"id\":\"d\",\"probability\":0.5,\"label\":false}\n");
        var before = File.ReadAllBytes(path);
        var result = Run(path, "--bins", "2");
        Assert.Equal(0, result.Code);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        Assert.Equal(4, root.GetProperty("rows").GetInt32());
        Assert.Equal(2, root.GetProperty("positives").GetInt32());
        Assert.Equal(0.125, root.GetProperty("brierScore").GetDouble(), 12);
        Assert.Equal(0, root.GetProperty("expectedCalibrationError").GetDouble(), 12);
        Assert.Equal(1, root.GetProperty("bins")[0].GetProperty("count").GetInt32());
        Assert.Equal(3, root.GetProperty("bins")[1].GetProperty("count").GetInt32());
        Assert.True(root.GetProperty("bins")[1].GetProperty("upperInclusive").GetBoolean());
        Assert.Equal(before, File.ReadAllBytes(path));
        Assert.DoesNotContain("\"id\"", result.Output);
    }

    [Fact]
    public void EmptyBinsAreExplicitAndBothEndpointsWork()
    {
        var result = App.Inspect(Input("{\"id\":\"a\",\"probability\":0,\"label\":false}\n{\"id\":\"b\",\"probability\":1,\"label\":true}\n"), 4);
        Assert.Equal(0, result.BrierScore);
        Assert.Equal(0, result.ExpectedCalibrationError);
        Assert.Null(result.Bins[1].MeanProbability);
        Assert.Null(result.Bins[1].PositiveRate);
        Assert.Equal(1, result.Bins[3].Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("{\"id\":\"a\",\"probability\":0.5,\"label\":1}")]
    [InlineData("{\"id\":\"a\",\"probability\":-0.1,\"label\":true}")]
    [InlineData("{\"id\":\"a\",\"probability\":1.1,\"label\":true}")]
    [InlineData("{\"id\":\"a\",\"probability\":1e999,\"label\":true}")]
    [InlineData("{\"id\":\"a\",\"probability\":\"0.5\",\"label\":true}")]
    [InlineData("{\"id\":\"a\",\"probability\":0.5,\"label\":true,\"extra\":0}")]
    [InlineData("{\"id\":\"a\",\"probability\":0.5,\"probability\":0.6,\"label\":true}")]
    [InlineData("{\"id\":\"a\",\"probability\":0.5,\"label\":true}\n{\"id\":\"a\",\"probability\":0.5,\"label\":false}")]
    [InlineData("{\"id\":\"\\ud800\",\"probability\":0.5,\"label\":true}")]
    [InlineData("{not json}")]
    public void InvalidInputExitsTwoWithoutPartialJson(string text)
    {
        var result = Run(Input(text));
        Assert.Equal(2, result.Code);
        Assert.Empty(result.Output);
        Assert.NotEmpty(result.Error);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("101")]
    [InlineData("-1")]
    [InlineData("2.5")]
    public void InvalidBinCountIsArgumentError(string value) => Assert.Equal(2, Run("unused", "--bins", value).Code);

    [Fact]
    public void Utf8AndFileLimitsAreEnforced()
    {
        var path = Input(""); File.WriteAllBytes(path, [0xff]);
        Assert.Equal(2, Run(path).Code);
        using (var file = new FileStream(path, FileMode.Create)) file.SetLength(32L * 1024 * 1024 + 1);
        Assert.Equal(2, Run(path).Code);
    }

    [Fact]
    public void ByteHashIncludesBomAndWhitespace()
    {
        var path = Input("\ufeff{\"id\":\"a\",\"probability\":0.5,\"label\":true}\n");
        var result = App.Inspect(path, 1);
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant(), result.InputSha256);
    }

    public void Dispose() => Directory.Delete(directory, true);
}
