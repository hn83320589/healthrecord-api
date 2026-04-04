using System.Globalization;
using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.VisitSummary;
using HealthRecord.API.Models.Entities;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HealthRecord.API.Services;

public class VisitSummaryService(AppDbContext db) : IVisitSummaryService
{
    // Core lab items to track trends
    private static readonly (string Code, string Name, string Display)[] CoreTrendItems =
    [
        ("09015C", "CRE(肌酸酐)", "Cr"),
        ("09015C", "eGFR", "eGFR"),
        ("12034B", "C3", "C3"),
    ];

    private const int BpMonths = 3;
    private const int MedAdherenceDays = 30;
    private const int SymptomDays = 30;
    private const int TrendMonths = 6;

    public async Task<VisitSummaryJsonResponse> GetSummaryJsonAsync(int userId, int visitId)
    {
        // 1. Visit info
        var visit = await db.VisitDetails
            .Include(v => v.HealthRecord)
            .FirstOrDefaultAsync(v => v.Id == visitId && v.HealthRecord.UserId == userId)
            ?? throw new KeyNotFoundException("Visit not found.");

        var visitDate = visit.HealthRecord.RecordedAt;
        var visitInfo = BuildVisitInfo(visit);

        // 2. Blood pressure (3 months before visit)
        var bpData = await BuildBpDataAsync(userId, visitDate);

        // 3. Lab results from this visit
        var labData = await BuildLabDataAsync(userId, visitDate, visit.HealthRecord.NhiInstitutionCode);

        // 4. Lab trends (6 months)
        var labTrends = await BuildLabTrendsAsync(userId, visitDate);

        // 5. Medications + adherence (30 days)
        var medData = await BuildMedDataAsync(userId, visitDate);

        // 6. Symptoms (30 days)
        var symptomData = await BuildSymptomDataAsync(userId, visitDate);

        return new VisitSummaryJsonResponse(
            visitInfo, bpData, labData, labTrends, medData, symptomData,
            DateTime.UtcNow);
    }

    public async Task<byte[]> GeneratePdfAsync(int userId, int visitId)
    {
        var data = await GetSummaryJsonAsync(userId, visitId);
        return GeneratePdfDocument(data);
    }

    public async Task<int?> GetLatestVisitIdAsync(int userId)
    {
        var latest = await db.VisitDetails
            .Include(v => v.HealthRecord)
            .Where(v => v.HealthRecord.UserId == userId && v.HealthRecord.RecordType == "visit")
            .OrderByDescending(v => v.HealthRecord.RecordedAt)
            .FirstOrDefaultAsync();

        return latest?.Id;
    }

    // ── Data builders ───────────────────────────────────────────────

    private static VisitSummaryVisitDto BuildVisitInfo(VisitDetail visit)
    {
        var diagnoses = new List<SummaryDiagnosisDto>();
        AddDiag(diagnoses, visit.DiagnosisCode1, visit.DiagnosisName1);
        AddDiag(diagnoses, visit.DiagnosisCode2, visit.DiagnosisName2);
        AddDiag(diagnoses, visit.DiagnosisCode3, visit.DiagnosisName3);
        AddDiag(diagnoses, visit.DiagnosisCode4, visit.DiagnosisName4);
        AddDiag(diagnoses, visit.DiagnosisCode5, visit.DiagnosisName5);

        return new VisitSummaryVisitDto(
            DateOnly.FromDateTime(visit.HealthRecord.RecordedAt),
            visit.HealthRecord.NhiInstitution,
            visit.Department,
            diagnoses);
    }

    private static void AddDiag(List<SummaryDiagnosisDto> list, string? code, string? name)
    {
        if (!string.IsNullOrWhiteSpace(code) || !string.IsNullOrWhiteSpace(name))
            list.Add(new SummaryDiagnosisDto(code ?? "", name ?? ""));
    }

    private async Task<VisitSummaryBpDto> BuildBpDataAsync(int userId, DateTime visitDate)
    {
        var start = visitDate.AddMonths(-BpMonths);
        var end = visitDate;

        var bpRecords = await db.BloodPressureDetails
            .Include(b => b.HealthRecord)
            .Where(b => b.HealthRecord.UserId == userId
                && b.HealthRecord.RecordedAt >= start
                && b.HealthRecord.RecordedAt <= end)
            .ToListAsync();

        if (bpRecords.Count == 0)
        {
            return new VisitSummaryBpDto(
                new BpValueDto(0, 0), new BpValueDto(0, 0), new BpValueDto(0, 0),
                [], 0, BpMonths);
        }

        var avgSys = (int)Math.Round(bpRecords.Average(b => b.Systolic));
        var avgDia = (int)Math.Round(bpRecords.Average(b => b.Diastolic));
        var maxSys = bpRecords.Max(b => b.Systolic);
        var maxDia = bpRecords.Max(b => b.Diastolic);
        var minSys = bpRecords.Min(b => b.Systolic);
        var minDia = bpRecords.Min(b => b.Diastolic);

        var weeklyTrend = bpRecords
            .GroupBy(b =>
            {
                var dt = b.HealthRecord.RecordedAt;
                var weekStart = dt.Date.AddDays(-(int)dt.DayOfWeek + (int)DayOfWeek.Monday);
                if (dt.DayOfWeek == DayOfWeek.Sunday) weekStart = weekStart.AddDays(-7);
                return DateOnly.FromDateTime(weekStart);
            })
            .OrderBy(g => g.Key)
            .Select(g => new BpWeeklyTrendDto(
                g.Key,
                (int)Math.Round(g.Average(b => b.Systolic)),
                (int)Math.Round(g.Average(b => b.Diastolic))))
            .ToList();

        return new VisitSummaryBpDto(
            new BpValueDto(avgSys, avgDia),
            new BpValueDto(maxSys, maxDia),
            new BpValueDto(minSys, minDia),
            weeklyTrend,
            bpRecords.Count,
            BpMonths);
    }

    private async Task<VisitSummaryLabDto> BuildLabDataAsync(
        int userId, DateTime visitDate, string? institutionCode)
    {
        var query = db.LabResultDetails
            .Include(l => l.HealthRecord)
            .Include(l => l.UserLabItem)
            .Where(l => l.HealthRecord.UserId == userId
                && l.HealthRecord.RecordType == "lab_result"
                && l.HealthRecord.RecordedAt.Date == visitDate.Date);

        if (!string.IsNullOrEmpty(institutionCode))
            query = query.Where(l => l.HealthRecord.NhiInstitutionCode == institutionCode);

        var labs = await query.ToListAsync();

        var userItems = await db.UserLabItems
            .Where(i => i.UserId == userId)
            .ToDictionaryAsync(i => (i.ItemCode, i.ItemName));

        var itemSummaries = labs.Select(l =>
        {
            var item = l.UserLabItem;
            if (item == null) userItems.TryGetValue((l.ItemCode, l.ItemName), out item);

            var status = GetLabStatus(l.ValueNumeric, item?.NormalMin, item?.NormalMax, l.IsNumeric);
            return new
            {
                Category = item?.Category ?? "其他",
                Dto = new LabItemSummaryDto(
                    item?.DisplayName ?? l.ItemName,
                    l.ValueNumeric, l.ValueText, l.Unit ?? item?.Unit, status)
            };
        }).ToList();

        var byCategory = itemSummaries
            .GroupBy(x => x.Category)
            .Select(g => new LabCategoryGroupDto(g.Key, g.Select(x => x.Dto).ToList()))
            .ToList();

        var abnormalCount = itemSummaries.Count(x => x.Dto.Status is "high" or "low");

        return new VisitSummaryLabDto(abnormalCount, itemSummaries.Count, byCategory);
    }

    private async Task<List<LabTrendDto>> BuildLabTrendsAsync(int userId, DateTime visitDate)
    {
        var start = visitDate.AddMonths(-TrendMonths);
        var trends = new List<LabTrendDto>();

        var userItems = await db.UserLabItems
            .Where(i => i.UserId == userId)
            .ToDictionaryAsync(i => (i.ItemCode, i.ItemName));

        foreach (var (code, name, display) in CoreTrendItems)
        {
            var points = await db.LabResultDetails
                .Include(l => l.HealthRecord)
                .Where(l => l.HealthRecord.UserId == userId
                    && l.ItemCode == code
                    && l.ItemName == name
                    && l.IsNumeric
                    && l.ValueNumeric != null
                    && l.HealthRecord.RecordedAt >= start
                    && l.HealthRecord.RecordedAt <= visitDate)
                .OrderBy(l => l.HealthRecord.RecordedAt)
                .Select(l => new LabTrendPointDto(
                    DateOnly.FromDateTime(l.HealthRecord.RecordedAt),
                    l.ValueNumeric!.Value))
                .ToListAsync();

            if (points.Count < 2) continue;

            var first = points[0].Value;
            var last = points[^1].Value;
            var changeRate = first != 0 ? (double)(last - first) / (double)first : 0;

            var direction = changeRate switch
            {
                > 0.05 => "↑",
                < -0.05 => "↓",
                _ => "→"
            };

            userItems.TryGetValue((code, name), out var labItem);
            trends.Add(new LabTrendDto(display, labItem?.Unit, points, direction));
        }

        return trends;
    }

    private async Task<VisitSummaryMedDto> BuildMedDataAsync(int userId, DateTime visitDate)
    {
        var start = visitDate.AddDays(-MedAdherenceDays);
        var end = visitDate;
        var visitDateOnly = DateOnly.FromDateTime(visitDate);

        var reminders = await db.MedicationReminders
            .Where(r => r.UserId == userId && r.IsEnabled)
            .ToListAsync();

        var activeReminders = reminders
            .Where(r => (r.StartDate == null || r.StartDate <= visitDateOnly)
                     && (r.EndDate == null || r.EndDate >= visitDateOnly))
            .ToList();

        var logs = await db.MedicationLogs
            .Where(l => l.UserId == userId && l.ScheduledAt >= start && l.ScheduledAt <= end)
            .ToListAsync();

        var medItems = new List<MedAdherenceItemDto>();
        var totalScheduled = 0;
        var totalTaken = 0;

        foreach (var reminder in activeReminders)
        {
            var medLogs = logs.Where(l => l.ReminderId == reminder.Id).ToList();
            var scheduled = medLogs.Count(l => !(l.Status == "pending" && l.ScheduledAt >= end));
            var taken = medLogs.Count(l => l.Status is "taken" or "late");
            var rate = scheduled > 0 ? Math.Round((double)taken / scheduled * 100, 1) : 0;

            totalScheduled += scheduled;
            totalTaken += taken;

            medItems.Add(new MedAdherenceItemDto(reminder.MedicationName, reminder.Frequency, rate));
        }

        var overallRate = totalScheduled > 0
            ? Math.Round((double)totalTaken / totalScheduled * 100, 1) : 0;

        return new VisitSummaryMedDto(medItems, overallRate, MedAdherenceDays);
    }

    private async Task<VisitSummarySymptomDto> BuildSymptomDataAsync(int userId, DateTime visitDate)
    {
        var start = visitDate.AddDays(-SymptomDays);
        var end = visitDate;

        var symptoms = await db.SymptomLogs
            .Where(s => s.UserId == userId && s.LoggedAt >= start && s.LoggedAt <= end)
            .ToListAsync();

        var byType = symptoms
            .GroupBy(s => s.SymptomType)
            .Select(g => new SymptomTypeCountDto(
                g.Key, g.Count(), Math.Round(g.Average(s => s.Severity), 1)))
            .OrderByDescending(t => t.Count)
            .ToList();

        return new VisitSummarySymptomDto(SymptomDays, symptoms.Count, byType);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static string GetLabStatus(decimal? value, decimal? normalMin, decimal? normalMax, bool isNumeric)
    {
        if (!isNumeric || value == null) return "unknown";
        if (normalMin.HasValue && value < normalMin) return "low";
        if (normalMax.HasValue && value > normalMax) return "high";
        if (normalMin.HasValue || normalMax.HasValue) return "normal";
        return "unknown";
    }

    // ── PDF Generation ──────────────────────────────────────────────

    private static byte[] GeneratePdfDocument(VisitSummaryJsonResponse data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                // Header
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("回診摘要報告")
                            .FontSize(18).Bold().FontColor(Colors.Grey.Darken3);
                        row.ConstantItem(120).AlignRight().Text(text =>
                        {
                            text.Span("回診日期: ").FontSize(9);
                            text.Span(data.Visit.Date.ToString("yyyy-MM-dd"))
                                .FontSize(9).Bold();
                        });
                    });
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                // Content
                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Spacing(12);

                    // Section 1: Visit Info
                    RenderVisitSection(col, data.Visit);

                    // Section 2: Blood Pressure
                    RenderBpSection(col, data.BloodPressure);

                    // Section 3: Lab Results
                    RenderLabSection(col, data.LabResults);

                    // Section 4: Lab Trends
                    RenderTrendSection(col, data.LabTrends);

                    // Section 5: Medications
                    RenderMedSection(col, data.Medications);

                    // Section 6: Symptoms
                    RenderSymptomSection(col, data.Symptoms);
                });

                // Footer
                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
                        text.Span("BodyRecord v1.0 | 此報告由系統自動生成，內容僅供參考 | Page ");
                        text.CurrentPageNumber();
                        text.Span("/");
                        text.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void RenderSectionHeader(ColumnDescriptor col, string title)
    {
        col.Item()
            .Background(Colors.Grey.Lighten3)
            .Padding(6)
            .Text(title)
            .FontSize(12).Bold().FontColor(Colors.Grey.Darken3);
    }

    private static void RenderVisitSection(ColumnDescriptor col, VisitSummaryVisitDto visit)
    {
        RenderSectionHeader(col, "回診資訊");

        col.Item().PaddingLeft(8).Column(inner =>
        {
            inner.Spacing(3);
            inner.Item().Text(text =>
            {
                text.Span("機構: ").Bold();
                text.Span(visit.Institution ?? "未提供");
            });
            inner.Item().Text(text =>
            {
                text.Span("科別: ").Bold();
                text.Span(visit.Department ?? "未提供");
            });

            if (visit.Diagnoses.Count > 0)
            {
                inner.Item().Text("診斷:").Bold();
                foreach (var d in visit.Diagnoses)
                {
                    inner.Item().PaddingLeft(12).Text($"  {d.Code} {d.Name}");
                }
            }
        });
    }

    private static void RenderBpSection(ColumnDescriptor col, VisitSummaryBpDto bp)
    {
        RenderSectionHeader(col, $"血壓概況（近 {bp.PeriodMonths} 個月，共 {bp.TotalMeasurements} 筆）");

        if (bp.TotalMeasurements == 0)
        {
            col.Item().PaddingLeft(8).Text("期間內無血壓紀錄").FontColor(Colors.Grey.Medium);
            return;
        }

        col.Item().PaddingLeft(8).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(80);
                columns.ConstantColumn(100);
                columns.ConstantColumn(100);
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Element(HeaderStyle).Text("");
                header.Cell().Element(HeaderStyle).Text("收縮壓");
                header.Cell().Element(HeaderStyle).Text("舒張壓");
            });

            // Rows
            table.Cell().Element(CellStyle).Text("平均").Bold();
            table.Cell().Element(CellStyle).Text($"{bp.RecentAvg.Systolic}");
            table.Cell().Element(CellStyle).Text($"{bp.RecentAvg.Diastolic}");

            table.Cell().Element(CellStyle).Text("最高").Bold();
            table.Cell().Element(CellStyle).Text($"{bp.RecentMax.Systolic}");
            table.Cell().Element(CellStyle).Text($"{bp.RecentMax.Diastolic}");

            table.Cell().Element(CellStyle).Text("最低").Bold();
            table.Cell().Element(CellStyle).Text($"{bp.RecentMin.Systolic}");
            table.Cell().Element(CellStyle).Text($"{bp.RecentMin.Diastolic}");
        });

        if (bp.Trend.Count > 0)
        {
            col.Item().PaddingLeft(8).PaddingTop(4).Text("週趨勢:").Bold().FontSize(9);
            col.Item().PaddingLeft(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(90);
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(80);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).Text("週起始");
                    header.Cell().Element(HeaderStyle).Text("平均收縮壓");
                    header.Cell().Element(HeaderStyle).Text("平均舒張壓");
                });

                foreach (var w in bp.Trend)
                {
                    table.Cell().Element(CellStyle).Text(w.WeekStart.ToString("MM/dd"));
                    table.Cell().Element(CellStyle).Text($"{w.AvgSystolic}");
                    table.Cell().Element(CellStyle).Text($"{w.AvgDiastolic}");
                }
            });
        }
    }

    private static void RenderLabSection(ColumnDescriptor col, VisitSummaryLabDto lab)
    {
        RenderSectionHeader(col, $"檢驗結果（共 {lab.TotalCount} 項，異常 {lab.AbnormalCount} 項）");

        if (lab.TotalCount == 0)
        {
            col.Item().PaddingLeft(8).Text("本次回診無關聯檢驗").FontColor(Colors.Grey.Medium);
            return;
        }

        foreach (var category in lab.ByCategory)
        {
            col.Item().PaddingLeft(8).PaddingTop(4)
                .Text(category.Category).Bold().FontSize(10).FontColor(Colors.Grey.Darken2);

            col.Item().PaddingLeft(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(40);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).Text("項目");
                    header.Cell().Element(HeaderStyle).Text("數值");
                    header.Cell().Element(HeaderStyle).Text("單位");
                    header.Cell().Element(HeaderStyle).Text("狀態");
                });

                foreach (var item in category.Items)
                {
                    table.Cell().Element(CellStyle).Text(item.DisplayName ?? "");

                    var valueText = item.Value.HasValue ? item.Value.Value.ToString("G") : (item.ValueText ?? "");
                    var isAbnormal = item.Status is "high" or "low";

                    table.Cell().Element(CellStyle).Text(text =>
                    {
                        text.Span(valueText)
                            .FontColor(isAbnormal ? Colors.Red.Medium : Colors.Black);
                    });

                    table.Cell().Element(CellStyle).Text(item.Unit ?? "");

                    table.Cell().Element(CellStyle).Text(text =>
                    {
                        var (symbol, color) = item.Status switch
                        {
                            "normal" => ("\u2713", Colors.Green.Medium),
                            "high" => ("\u25B2", Colors.Red.Medium),
                            "low" => ("\u25BC", Colors.Red.Medium),
                            _ => ("-", Colors.Grey.Medium)
                        };
                        text.Span(symbol).FontColor(color);
                    });
                }
            });
        }
    }

    private static void RenderTrendSection(ColumnDescriptor col, List<LabTrendDto> trends)
    {
        RenderSectionHeader(col, "關鍵指標趨勢（近 6 個月）");

        if (trends.Count == 0)
        {
            col.Item().PaddingLeft(8).Text("趨勢資料不足").FontColor(Colors.Grey.Medium);
            return;
        }

        col.Item().PaddingLeft(8).Column(inner =>
        {
            inner.Spacing(4);
            foreach (var trend in trends)
            {
                inner.Item().Text(text =>
                {
                    text.Span($"{trend.DisplayName}: ").Bold();
                    var pointTexts = trend.Points.Select(p => p.Value.ToString("G"));
                    text.Span(string.Join(" \u2192 ", pointTexts));
                    text.Span($" {trend.TrendDirection}").Bold()
                        .FontColor(trend.TrendDirection == "\u2191" ? Colors.Red.Medium
                            : trend.TrendDirection == "\u2193" ? Colors.Green.Medium
                            : Colors.Grey.Medium);
                    if (!string.IsNullOrEmpty(trend.Unit))
                        text.Span($" ({trend.Unit})").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            }
        });
    }

    private static void RenderMedSection(ColumnDescriptor col, VisitSummaryMedDto med)
    {
        RenderSectionHeader(col, $"用藥狀況（近 {med.AdherencePeriodDays} 天）");

        if (med.Current.Count == 0)
        {
            col.Item().PaddingLeft(8).Text("無進行中的用藥提醒").FontColor(Colors.Grey.Medium);
            return;
        }

        col.Item().PaddingLeft(8).Text(text =>
        {
            text.Span("整體服藥遵從率: ").Bold();
            text.Span($"{med.OverallAdherenceRate}%")
                .FontColor(med.OverallAdherenceRate >= 80 ? Colors.Green.Medium : Colors.Red.Medium);
        });

        col.Item().PaddingLeft(8).PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.ConstantColumn(80);
                columns.ConstantColumn(80);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderStyle).Text("藥物名稱");
                header.Cell().Element(HeaderStyle).Text("頻率");
                header.Cell().Element(HeaderStyle).Text("遵從率");
            });

            foreach (var item in med.Current)
            {
                table.Cell().Element(CellStyle).Text(item.Name);
                table.Cell().Element(CellStyle).Text(item.Frequency ?? "-");
                table.Cell().Element(CellStyle).Text(text =>
                {
                    text.Span($"{item.AdherenceRate}%")
                        .FontColor(item.AdherenceRate >= 80 ? Colors.Green.Medium : Colors.Red.Medium);
                });
            }
        });
    }

    private static void RenderSymptomSection(ColumnDescriptor col, VisitSummarySymptomDto symptom)
    {
        RenderSectionHeader(col, $"症狀紀錄（近 {symptom.PeriodDays} 天，共 {symptom.TotalCount} 筆）");

        if (symptom.TotalCount == 0)
        {
            col.Item().PaddingLeft(8).Text("期間內無症狀紀錄").FontColor(Colors.Grey.Medium);
            return;
        }

        col.Item().PaddingLeft(8).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.ConstantColumn(60);
                columns.ConstantColumn(80);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderStyle).Text("症狀類型");
                header.Cell().Element(HeaderStyle).Text("次數");
                header.Cell().Element(HeaderStyle).Text("平均嚴重度");
            });

            foreach (var t in symptom.ByType)
            {
                table.Cell().Element(CellStyle).Text(t.Type);
                table.Cell().Element(CellStyle).Text($"{t.Count}");
                table.Cell().Element(CellStyle).Text(text =>
                {
                    text.Span($"{t.AvgSeverity}/10")
                        .FontColor(t.AvgSeverity >= 7 ? Colors.Red.Medium : Colors.Black);
                });
            }
        });
    }

    // ── Table style helpers ─────────────────────────────────────────

    private static IContainer HeaderStyle(IContainer container) =>
        container
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(4).PaddingHorizontal(6);

    private static IContainer CellStyle(IContainer container) =>
        container
            .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3).PaddingHorizontal(6);
}
