using System.Text.Json;

namespace HealthRecord.API.Common.Helpers;

// Parsed visit group (一次就診 = 一筆 HealthRecord)
public record NhiVisitGroup(
    DateTime VisitDate,
    string? InstitutionCode,
    string? InstitutionName,
    string? VisitSeq,
    string? PrimaryIcdCode,
    string? PrimaryDiagnosis,
    string? SecondaryDiagnosesJson,
    int? Copay,
    int? TotalPoints,
    List<NhiMedItem> MedicationItems);

public record NhiMedItem(
    string? Code,
    string DrugName,
    decimal? Quantity,
    int? Days);

// Parsed lab group (同日期+機構 → 多筆檢驗結果)
public record NhiLabGroup(
    DateTime VisitDate,
    string? InstitutionCode,
    List<NhiLabItem> Items);

public record NhiLabItem(
    string? NhiCode,
    string? NhiItemName,
    string? RawValue,
    string? RawRange,
    bool IsNumeric,
    decimal? NumericValue);

public static class NhiJsonParser
{
    public static string GetDataDate(JsonElement root)
    {
        return TryGet(root, "myhealthbank", "bdata", "b1.2") ?? DateTime.UtcNow.ToString("yyyyMMdd");
    }

    public static List<NhiVisitGroup> ParseR1(JsonElement root)
    {
        var groups = new List<NhiVisitGroup>();

        if (!TryGetBdataArray(root, "r1", out var r1)) return groups;

        // Group by (date + institutionCode + visitSeq) to deduplicate multiple rows per visit
        var rowsByKey = new Dictionary<string, (JsonElement firstRow, List<JsonElement> allRows)>();
        foreach (var row in r1)
        {
            var date = GetField(row, "5", "r1") ?? "";
            var code = GetField(row, "3", "r1") ?? "";
            var seq  = GetField(row, "7", "r1") ?? "";
            var key  = $"{date}|{code}|{seq}";

            if (!rowsByKey.ContainsKey(key))
                rowsByKey[key] = (row, [row]);
            else
                rowsByKey[key].allRows.Add(row);
        }

        foreach (var (_, (firstRow, allRows)) in rowsByKey)
        {
            if (!TryParseDate(GetField(firstRow, "5", "r1"), out var visitDate)) continue;

            // Collect secondary diagnoses from all rows (fields 10–27 per row)
            var secondary = new List<object>();
            foreach (var row in allRows)
                for (int i = 10; i <= 27; i += 2)
                {
                    var icdCode = GetField(row, i.ToString(), "r1");
                    var icdName = GetField(row, (i + 1).ToString(), "r1");
                    if (!string.IsNullOrWhiteSpace(icdCode))
                        secondary.Add(new { code = icdCode, name = icdName ?? "" });
                }

            string? secondaryJson = secondary.Count > 0
                ? JsonSerializer.Serialize(secondary)
                : null;

            // r1_1 nested array inside first row
            var meds = ParseR1_1(firstRow);

            groups.Add(new NhiVisitGroup(
                visitDate,
                InstitutionCode:      GetField(firstRow, "3", "r1"),
                InstitutionName:      GetField(firstRow, "4", "r1"),
                VisitSeq:             GetField(firstRow, "7", "r1"),
                PrimaryIcdCode:       GetField(firstRow, "8", "r1"),
                PrimaryDiagnosis:     GetField(firstRow, "9", "r1"),
                SecondaryDiagnosesJson: secondaryJson,
                Copay:       ParseInt(GetField(firstRow, "12", "r1")),
                TotalPoints: ParseInt(GetField(firstRow, "13", "r1")),
                MedicationItems: meds));
        }

        return groups;
    }

    private static List<NhiMedItem> ParseR1_1(JsonElement visitRow)
    {
        var result = new List<NhiMedItem>();
        if (!visitRow.TryGetProperty("r1_1", out var r1_1)
            || r1_1.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var med in r1_1.EnumerateArray())
        {
            result.Add(new NhiMedItem(
                Code:     GetField(med, "1", "r1_1"),
                DrugName: GetField(med, "2", "r1_1") ?? GetField(med, "3", "r1_1") ?? "Unknown",
                Quantity: ParseDecimal(GetField(med, "3", "r1_1")),
                Days:     ParseInt(GetField(med, "4", "r1_1"))));
        }
        return result;
    }

    public static List<NhiLabGroup> ParseR7(JsonElement root)
    {
        var groups = new List<NhiLabGroup>();

        if (!TryGetBdataArray(root, "r7", out var r7)) return groups;

        var byKey = new Dictionary<string, NhiLabGroup>();

        foreach (var row in r7)
        {
            if (!TryParseDate(GetField(row, "5", "r7"), out var visitDate)) continue;

            var instCode = GetField(row, "3", "r7");
            var key = $"{visitDate:yyyyMMdd}|{instCode}";

            if (!byKey.ContainsKey(key))
                byKey[key] = new NhiLabGroup(visitDate, instCode, []);

            var rawValue = GetField(row, "11", "r7");
            var isNumeric = IsNumericValue(rawValue);
            decimal? numericValue = isNumeric && decimal.TryParse(rawValue?.Trim(), out var n) ? n : null;

            byKey[key].Items.Add(new NhiLabItem(
                NhiCode:     GetField(row, "8", "r7"),
                NhiItemName: GetField(row, "10", "r7"),
                RawValue:    rawValue,
                RawRange:    GetField(row, "12", "r7"),
                IsNumeric:   isNumeric,
                NumericValue: numericValue));
        }

        groups.AddRange(byKey.Values);
        return groups;
    }

    public static bool IsNumericValue(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var qualitative = new[]
        {
            "-", "+", "1+", "2+", "3+", "4+", "N", "P",
            "Negative", "Positive", "Clear", "Pale yellow",
            "Yellow", "Normal", "Colorless",
        };
        if (qualitative.Any(q => raw.Trim().Equals(q, StringComparison.OrdinalIgnoreCase)))
            return false;
        if (raw.Contains(':')) return false;
        if (raw.StartsWith("＜") || raw.StartsWith("＞")) return false;
        return decimal.TryParse(raw.Trim(), out _);
    }

    public static string ClassifyDrugType(string? code)
    {
        if (string.IsNullOrEmpty(code)) return "medication";
        if (char.IsLetter(code[0])) return "medication";
        if (code.StartsWith("00") || code.StartsWith("05")) return "service";
        return "exam";
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static bool TryGetBdataArray(JsonElement root, string key, out IEnumerable<JsonElement> items)
    {
        items = [];
        if (!root.TryGetProperty("myhealthbank", out var mhb)) return false;
        if (!mhb.TryGetProperty("bdata", out var bdata)) return false;
        if (!bdata.TryGetProperty(key, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return false;
        items = arr.EnumerateArray();
        return true;
    }

    private static string? TryGet(JsonElement root, params string[] path)
    {
        var cur = root;
        foreach (var p in path)
        {
            if (!cur.TryGetProperty(p, out cur)) return null;
        }
        return cur.ValueKind == JsonValueKind.String ? cur.GetString() : cur.ToString();
    }

    private static string? GetField(JsonElement item, string key, string? prefix = null)
    {
        // Try short key first ("5"), then prefixed key ("r1.5")
        if (item.TryGetProperty(key, out var val))
            return val.ValueKind == JsonValueKind.String ? val.GetString() : val.ToString();
        if (prefix != null && item.TryGetProperty($"{prefix}.{key}", out val))
            return val.ValueKind == JsonValueKind.String ? val.GetString() : val.ToString();
        return null;
    }

    private static bool TryParseDate(string? value, out DateTime result)
    {
        result = default;
        if (string.IsNullOrEmpty(value)) return false;
        var clean = value.Replace("/", "").Replace("-", "").Trim();
        if (!DateTime.TryParseExact(clean, "yyyyMMdd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out result))
            return false;
        result = DateTime.SpecifyKind(result, DateTimeKind.Utc);
        return true;
    }

    private static int? ParseInt(string? v) =>
        int.TryParse(v, out var r) ? r : null;

    private static decimal? ParseDecimal(string? v) =>
        decimal.TryParse(v, out var r) ? r : null;
}
