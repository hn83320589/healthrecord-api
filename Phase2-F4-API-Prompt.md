# Phase 2 Feature 4 — 回診前彙整 PDF 匯出（後端）

請閱讀 CLAUDE.md 後，實作回診摘要 PDF 匯出功能。

## 功能說明

將一次回診的完整關聯資料（Feature 1）+ 症狀日誌（Feature 2）+ 服藥率（Feature 3）彙整為一頁式 PDF，
方便使用者回診前列印或手機上給醫師看。

## 依賴套件

安裝 QuestPDF（MIT License，.NET PDF 生成庫）：

```bash
dotnet add package QuestPDF
```

> QuestPDF 使用 Fluent API 組建 PDF，不需要 HTML 模板。

## API 端點

### GET /visits/{id}/summary-pdf

回傳 PDF 檔案（application/pdf）。

```csharp
[HttpGet("{id}/summary-pdf")]
[Authorize]
public async Task<IActionResult> GetVisitSummaryPdf(int id)
{
    var userId = GetCurrentUserId();
    var pdfBytes = await visitSummaryService.GeneratePdfAsync(userId, id);
    return File(pdfBytes, "application/pdf", $"visit-summary-{id}.pdf");
}
```

### GET /visits/latest-summary-pdf

回傳最近一次回診的 PDF（方便快速取用，不需要先查 visitId）。

### GET /visits/{id}/summary-json

回傳 PDF 內容的 JSON 版本（給前端預覽用，不生成 PDF）。

Response：

```json
{
  "success": true,
  "data": {
    "visit": {
      "date": "2026-03-28",
      "institution": "高雄榮總",
      "department": "腎臟科",
      "diagnoses": [
        { "code": "M3214", "name": "全身性紅斑性狼瘡腎絲球疾病" },
        { "code": "I129", "name": "高血壓性慢性腎臟病" }
      ]
    },
    "bloodPressure": {
      "recentAvg": { "systolic": 130, "diastolic": 83 },
      "recentMax": { "systolic": 152, "diastolic": 96 },
      "recentMin": { "systolic": 118, "diastolic": 75 },
      "trend": [
        { "date": "2026-01-05", "avgSystolic": 135, "avgDiastolic": 87 },
        { "date": "2026-01-12", "avgSystolic": 132, "avgDiastolic": 85 }
      ],
      "totalMeasurements": 180,
      "periodMonths": 3
    },
    "labResults": {
      "abnormalCount": 2,
      "totalCount": 18,
      "byCategory": [
        {
          "category": "腎功能",
          "items": [
            { "displayName": "肌酸酐", "value": 1.15, "unit": "mg/dL", "status": "normal", "trend": [1.8, 1.5, 1.3, 1.2, 1.15] },
            { "displayName": "eGFR", "value": 73.2, "unit": "mL/min/1.73m²", "status": "normal", "trend": [45, 52, 60, 68, 73.2] }
          ]
        }
      ]
    },
    "labTrends": [
      { "displayName": "肌酸酐", "unit": "mg/dL", "points": [
        { "date": "2025-04-01", "value": 1.8 },
        { "date": "2025-07-01", "value": 1.5 },
        { "date": "2025-10-01", "value": 1.3 },
        { "date": "2026-01-15", "value": 1.2 },
        { "date": "2026-03-28", "value": 1.15 }
      ]},
      { "displayName": "eGFR", "unit": "mL/min/1.73m²", "points": [...] },
      { "displayName": "C3", "unit": "mg/dL", "points": [...] }
    ],
    "medications": {
      "current": [
        { "name": "Prednisolone 5mg", "frequency": "QD", "adherenceRate": 93.3 },
        { "name": "MMF 1000mg", "frequency": "BID", "adherenceRate": 80.0 },
        { "name": "HCQ 200mg", "frequency": "BID", "adherenceRate": 85.0 }
      ],
      "overallAdherenceRate": 86.7,
      "adherencePeriodDays": 30
    },
    "symptoms": {
      "periodDays": 30,
      "totalCount": 15,
      "byType": [
        { "type": "疲倦", "count": 7, "avgSeverity": 4.2 },
        { "type": "關節痛", "count": 5, "avgSeverity": 5.1 },
        { "type": "水腫", "count": 3, "avgSeverity": 3.0 }
      ]
    },
    "generatedAt": "2026-04-04T10:30:00"
  }
}
```

## PDF 版面設計

A4 直式，單頁（內容多時可延伸至第二頁）。

```
┌─────────────────────────────────────────────┐
│  BodyRecord 回診摘要報告                       │
│  生成時間：2026/04/04 10:30                    │
├─────────────────────────────────────────────┤
│                                             │
│  ■ 回診資訊                                  │
│  日期：2026/03/28                            │
│  機構：高雄榮總 腎臟科                          │
│  診斷：M3214 全身性紅斑性狼瘡腎絲球疾病           │
│       I129 高血壓性慢性腎臟病                   │
│                                             │
├─────────────────────────────────────────────┤
│                                             │
│  ■ 近 3 個月血壓趨勢                           │
│  ┌─────────────────────────────────┐       │
│  │  平均: 130/83  最高: 152/96      │       │
│  │  最低: 118/75  量測: 180 次      │       │
│  │                                 │       │
│  │  [每週平均折線圖]                  │       │
│  │  收縮壓 ── / 舒張壓 - -          │       │
│  └─────────────────────────────────┘       │
│                                             │
├─────────────────────────────────────────────┤
│                                             │
│  ■ 本次檢驗結果（18 項，2 項異常）              │
│                                             │
│  腎功能                                      │
│  ┌──────────┬────────┬────────┬──────┐     │
│  │ 項目      │ 數值    │ 單位   │ 狀態  │     │
│  ├──────────┼────────┼────────┼──────┤     │
│  │ 肌酸酐    │ 1.15   │ mg/dL  │ ✓    │     │
│  │ eGFR     │ 73.2   │ ml/min │ ✓    │     │
│  │ BUN      │ 12     │ mg/dL  │ ✓    │     │
│  └──────────┴────────┴────────┴──────┘     │
│                                             │
│  免疫                                        │
│  ┌──────────┬────────┬────────┬──────┐     │
│  │ C3       │ 108.5  │ mg/dL  │ ✓    │     │
│  │ C4       │ 28.3   │ mg/dL  │ ✓    │     │
│  │ dsDNA Ab │ 5.2    │ IU/mL  │ ✓    │     │
│  └──────────┴────────┴────────┴──────┘     │
│  ... (其他 category)                         │
│                                             │
├─────────────────────────────────────────────┤
│                                             │
│  ■ 核心指標趨勢（近 6 個月）                    │
│  ┌─────────────────────────────────┐       │
│  │ Cr:   1.8 → 1.5 → 1.3 → 1.2 → 1.15   │
│  │ eGFR: 45  → 52  → 60  → 68  → 73.2   │
│  │ C3:   85  → 95  → 102 → 105 → 108.5  │
│  │                                 │       │
│  │ [簡易折線圖，3 條線]               │       │
│  └─────────────────────────────────┘       │
│                                             │
├─────────────────────────────────────────────┤
│                                             │
│  ■ 目前用藥（服藥率：近 30 天）                 │
│  ┌────────────────┬──────┬──────────┐      │
│  │ 藥品            │ 頻率  │ 服藥率    │      │
│  ├────────────────┼──────┼──────────┤      │
│  │ Prednisolone   │ QD   │ 93.3%   │      │
│  │ MMF 1000mg     │ BID  │ 80.0%   │      │
│  │ HCQ 200mg      │ BID  │ 85.0%   │      │
│  └────────────────┴──────┴──────────┘      │
│  整體服藥率：86.7%                            │
│                                             │
├─────────────────────────────────────────────┤
│                                             │
│  ■ 近 30 天症狀紀錄                           │
│  疲倦 ×7 (平均嚴重度 4.2)                     │
│  關節痛 ×5 (平均嚴重度 5.1)                    │
│  水腫 ×3 (平均嚴重度 3.0)                     │
│                                             │
├─────────────────────────────────────────────┤
│  BodyRecord v1.0 | 此報告由系統自動生成          │
│  報告內容僅供參考，不代表醫療診斷                  │
└─────────────────────────────────────────────┘
```

## Service 實作

```csharp
public class VisitSummaryService
{
    private readonly VisitRelationService _visitRelation;
    private readonly SymptomService _symptomService;
    private readonly MedicationLogService _medLogService;
    private readonly BloodPressureService _bpService;
    private readonly LabResultService _labService;

    public async Task<byte[]> GeneratePdfAsync(int userId, int visitId)
    {
        // 1. 取得回診關聯資料（Feature 1）
        var related = await _visitRelation.GetVisitRelated(userId, visitId);

        // 2. 取得血壓統計（近 3 個月）
        var visitDate = related.Visit.RecordedAt;
        var bpStart = visitDate.AddMonths(-3);
        var bpStats = await _bpService.GetStatistics(userId, bpStart, visitDate);
        var bpWeeklyTrend = await _bpService.GetWeeklyTrend(userId, bpStart, visitDate);

        // 3. 取得核心指標趨勢（近 6 個月，最多 3 個指標）
        var labTrends = await GetCoreLabTrends(userId, visitDate);

        // 4. 取得目前用藥 + 服藥率（Feature 3）
        var adherence = await _medLogService.GetAdherence(userId, 30);

        // 5. 取得症狀摘要（Feature 2，近 30 天）
        var symptomSummary = await _symptomService.GetSummary(userId, 1);

        // 6. 生成 PDF
        return GeneratePdf(related, bpStats, bpWeeklyTrend, labTrends, adherence, symptomSummary);
    }

    private async Task<List<LabTrendData>> GetCoreLabTrends(int userId, DateTime endDate)
    {
        var startDate = endDate.AddMonths(-6);
        // 預設追蹤：Cr、eGFR、C3（可依使用者 UserLabItems sort_order 調整）
        var coreItems = new[]
        {
            ("09015C", "CRE(肌酸酐)"),
            ("09015C", "eGFR"),
            ("12034B", "C3")
        };

        var trends = new List<LabTrendData>();
        foreach (var (code, name) in coreItems)
        {
            var points = await _labService.GetTrend(userId, code, name, startDate, endDate);
            if (points.Any())
            {
                var displayName = await GetDisplayName(userId, code, name);
                trends.Add(new LabTrendData { DisplayName = displayName, Points = points });
            }
        }
        return trends;
    }
}
```

## QuestPDF 實作提示

```csharp
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// 設定 License（Community License 免費用於年營收 < $1M）
QuestPDF.Settings.License = LicenseType.Community;

private byte[] GeneratePdf(...)
{
    var document = Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Noto Sans TC"));
            // 如果沒有 Noto Sans TC，使用系統預設字型
            // 部署環境需確認中文字型可用

            page.Header().Text("BodyRecord 回診摘要報告")
                .FontSize(16).Bold().FontColor(Colors.Grey.Darken3);

            page.Content().Column(col =>
            {
                // 回診資訊
                col.Item().PaddingVertical(8).Text(text => { ... });

                // 血壓統計（表格 + 簡易數值，不畫圖）
                col.Item().Table(table => { ... });

                // 檢驗結果（依 category 分組表格）
                col.Item().Table(table => { ... });

                // 核心指標趨勢（數值列表，PDF 內不畫折線圖）
                col.Item().Column(inner => {
                    // Cr: 1.8 → 1.5 → 1.3 → 1.2 → 1.15
                    // 用箭頭連接的數值序列，簡單明瞭
                });

                // 用藥 + 服藥率
                col.Item().Table(table => { ... });

                // 症狀摘要
                col.Item().Column(inner => { ... });
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("BodyRecord v1.0 | 此報告由系統自動生成，內容僅供參考");
                text.Span(" | ").FontColor(Colors.Grey.Medium);
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    });

    return document.GeneratePdf();
}
```

> PDF 內不畫折線圖（QuestPDF 畫圖複雜度高，投入產出比不佳）。
> 趨勢改用「數值序列 + 箭頭」呈現，如：`Cr: 1.8 → 1.5 → 1.3 → 1.2 → 1.15 ↓`
> 箭頭表示整體趨勢：↑ 上升 / ↓ 下降 / → 持平

## 中文字型處理

QuestPDF 需要中文字型才能正確顯示中文。

**開發環境**：macOS/Windows 通常已有中文字型。

**Zeabur 部署**（Linux 容器）：

```dockerfile
# 在 Dockerfile 中加入中文字型
RUN apt-get update && apt-get install -y fonts-noto-cjk && rm -rf /var/lib/apt/lists/*
```

或在 Program.cs 中指定字型回退：

```csharp
// QuestPDF 字型設定
QuestPDF.Settings.License = LicenseType.Community;

// 如果找不到指定字型，會 fallback 到系統預設
// 確保容器中有至少一個中文字型即可
```

## 完成後

- `dotnet build` 無錯誤
- GET /visits/{id}/summary-pdf 回傳可開啟的 PDF
- PDF 中文顯示正確
- 各區塊資料正確（血壓統計、檢驗、用藥率、症狀）
- GET /visits/{id}/summary-json 回傳完整 JSON
- 更新 CLAUDE.md Phase 2 進度（全部完成）
