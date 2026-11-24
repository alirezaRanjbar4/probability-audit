using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ProbabilityAudit;

public sealed record Bin(int Index, double LowerInclusive, double Upper, bool UpperInclusive, int Count, int Positives, double? MeanProbability, double? PositiveRate);
public sealed record Report(int SchemaVersion, string InputSha256, int Rows, int Positives, int BinCount, double BrierScore, double ExpectedCalibrationError, IReadOnlyList<Bin> Bins);

public static class App
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args is ["--help"] or ["-h"]) { output.WriteLine("probability-audit INPUT.jsonl [--bins 1..100]\nRead labeled binary probabilities; emit JSON reliability bins, Brier score and ECE. Exit 0 success, 2 invalid input/file. No training or network."); return 0; }
        if (args is ["--version"]) { output.WriteLine("0.1.0"); return 0; }
        try
        {
            if (args.Length is not (1 or 3) || args[0].StartsWith('-') || (args.Length == 3 && args[1] != "--bins")) throw new InvalidDataException("Expected INPUT.jsonl [--bins 1..100].");
            var bins = 10;
            if (args.Length == 3 && (!int.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out bins) || bins is < 1 or > 100)) throw new InvalidDataException("Bins must be an integer from 1 to 100.");
            var report = Inspect(args[0], bins);
            output.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }));
            return 0;
        }
        catch (Exception exception) when (exception is InvalidDataException or JsonException or IOException or UnauthorizedAccessException or DecoderFallbackException or ArgumentException or InvalidOperationException)
        { error.WriteLine("Invalid input or unreadable file: " + (exception is InvalidDataException ? exception.Message : "check JSONL, encoding and file permissions.")); return 2; }
    }

    public static Report Inspect(string path, int binCount)
    {
        if (binCount is < 1 or > 100) throw new InvalidDataException("Bins must be from 1 to 100.");
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length > 32 * 1024 * 1024) throw new InvalidDataException("Input exceeds 32 MiB.");
        using var buffer = new MemoryStream();
        var block = new byte[8192]; int size;
        while ((size = file.Read(block)) > 0) { if (buffer.Length + size > 32 * 1024 * 1024) throw new InvalidDataException("Input exceeds 32 MiB."); buffer.Write(block, 0, size); }
        var bytes = buffer.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var text = new UTF8Encoding(false, true).GetString(bytes);
        if (text.StartsWith('\uFEFF')) text = text[1..];
        using var reader = new StringReader(text);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var counts = new int[binCount]; var positives = new int[binCount]; var sums = new double[binCount];
        var rows = 0; var totalPositive = 0; double brier = 0; string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length > 1024 * 1024 || ++rows > 100_000) throw new InvalidDataException("Input exceeds line or row limit.");
            using var json = JsonDocument.Parse(line, new JsonDocumentOptions { MaxDepth = 8 });
            if (json.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Each line must be an object.");
            var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var field in json.RootElement.EnumerateObject())
                if (field.Name is not ("id" or "probability" or "label") || !fields.TryAdd(field.Name, field.Value)) throw new InvalidDataException("Unknown or duplicate property.");
            if (!fields.TryGetValue("id", out var id) || id.ValueKind != JsonValueKind.String || id.GetString() is not { Length: > 0 and <= 256 } name || string.IsNullOrWhiteSpace(name) || !ids.Add(name)) throw new InvalidDataException("IDs must be unique nonblank strings at most 256 characters.");
            if (!fields.TryGetValue("probability", out var prob) || prob.ValueKind != JsonValueKind.Number || !prob.TryGetDouble(out var p) || !double.IsFinite(p) || p is < 0 or > 1) throw new InvalidDataException("Probability must be a finite number in [0,1].");
            if (!fields.TryGetValue("label", out var label) || label.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw new InvalidDataException("Label must be a JSON boolean.");
            var y = label.GetBoolean() ? 1 : 0;
            var index = Math.Min((int)(p * binCount), binCount - 1);
            counts[index]++; positives[index] += y; sums[index] += p; totalPositive += y; brier += (p - y) * (p - y);
        }
        if (rows == 0) throw new InvalidDataException("Input is empty.");
        var results = new List<Bin>(); double ece = 0;
        for (var i = 0; i < binCount; i++)
        {
            double? mean = counts[i] == 0 ? null : sums[i] / counts[i];
            double? rate = counts[i] == 0 ? null : (double)positives[i] / counts[i];
            if (mean.HasValue) ece += (double)counts[i] / rows * Math.Abs(mean.Value - rate!.Value);
            results.Add(new(i, (double)i / binCount, (double)(i + 1) / binCount, i == binCount - 1, counts[i], positives[i], mean, rate));
        }
        return new(1, hash, rows, totalPositive, binCount, brier / rows, ece, results);
    }
}
