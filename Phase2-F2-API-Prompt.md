# Phase 2 Feature 2 — 症狀日誌（後端）

請閱讀 CLAUDE.md 後，實作症狀日誌功能。

## Schema

CLAUDE.md 已預留 SymptomLogs 表，直接建立 Entity 和 Migration：

```sql
SymptomLogs
  id               INT PK AUTO_INCREMENT
  user_id          INT FK → Users(id) ON DELETE CASCADE
  logged_at        DATETIME NOT NULL
  symptom_type     VARCHAR(100) NOT NULL
  severity         INT NOT NULL              -- 1-10
  body_location    VARCHAR(100) NULL
  duration_minutes INT NULL
  triggers         TEXT NULL
  note             TEXT NULL
  created_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP

  INDEX idx_user_date (user_id, logged_at)
  INDEX idx_user_type (user_id, symptom_type)
```

## 預設症狀類型

提供一組預設選項，但使用者可自由輸入（不限定 enum，VARCHAR 儲存）：

```
關節痛, 水腫, 疲倦, 皮疹, 發燒, 口腔潰瘍, 掉髮, 肌肉痠痛, 頭痛, 噁心, 食慾不振, 失眠, 其他
```

## API 端點

### CRUD

```
GET    /symptoms?startDate=...&endDate=...&type=...&page=1&pageSize=20
POST   /symptoms
PUT    /symptoms/{id}
DELETE /symptoms/{id}
GET    /symptoms/{id}
```

### 統計摘要

```
GET /symptoms/summary?months=3
```

回傳：

```json
{
  "success": true,
  "data": {
    "period": { "start": "2026-01-04", "end": "2026-04-04" },
    "totalCount": 27,
    "byType": [
      { "type": "疲倦", "count": 12, "avgSeverity": 5.3 },
      { "type": "關節痛", "count": 8, "avgSeverity": 6.1 },
      { "type": "水腫", "count": 4, "avgSeverity": 3.5 },
      { "type": "頭痛", "count": 3, "avgSeverity": 4.0 }
    ],
    "severityTrend": [
      { "week": "2026-W01", "avgSeverity": 5.8 },
      { "week": "2026-W02", "avgSeverity": 5.2 },
      { "week": "2026-W03", "avgSeverity": 4.5 }
    ],
    "topTriggers": ["睡眠不足", "天氣變化", "壓力大"],
    "calendar": [
      { "date": "2026-04-01", "count": 2, "maxSeverity": 6 },
      { "date": "2026-03-28", "count": 1, "maxSeverity": 3 }
    ]
  }
}
```

- `byType`：按類型統計，依 count 降序
- `severityTrend`：每週平均嚴重度（ISO week）
- `topTriggers`：從 triggers 欄位解析，取出現次數最多的前 5 個
- `calendar`：每日症狀數 + 最高嚴重度（用於熱力圖）

### 類型清單

```
GET /symptoms/types
```

回傳預設清單 + 使用者曾記錄過的自訂類型（去重合併）：

```json
{
  "success": true,
  "data": ["關節痛", "水腫", "疲倦", "皮疹", "發燒", "口腔潰瘍", "掉髮", "肌肉痠痛", "頭痛", "噁心", "食慾不振", "失眠", "胸悶", "其他"]
}
```

其中「胸悶」是使用者之前自訂輸入過的，自動出現在清單中。

## DTOs

```csharp
public class CreateSymptomRequest
{
    public DateTime LoggedAt { get; set; }
    public string SymptomType { get; set; }      // required
    public int Severity { get; set; }             // 1-10, required
    public string? BodyLocation { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Triggers { get; set; }         // 逗號分隔或自由文字
    public string? Note { get; set; }
}

public class UpdateSymptomRequest
{
    public DateTime? LoggedAt { get; set; }
    public string? SymptomType { get; set; }
    public int? Severity { get; set; }
    public string? BodyLocation { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Triggers { get; set; }
    public string? Note { get; set; }
}

public class SymptomResponse
{
    public int Id { get; set; }
    public DateTime LoggedAt { get; set; }
    public string SymptomType { get; set; }
    public int Severity { get; set; }
    public string? BodyLocation { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Triggers { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SymptomSummaryResponse
{
    public PeriodDto Period { get; set; }
    public int TotalCount { get; set; }
    public List<TypeStatDto> ByType { get; set; }
    public List<WeekTrendDto> SeverityTrend { get; set; }
    public List<string> TopTriggers { get; set; }
    public List<CalendarDayDto> Calendar { get; set; }
}
```

## Validation

- `Severity` 必須在 1~10 之間
- `SymptomType` 不可為空
- `LoggedAt` 不可為未來時間

## Seed 測試資料

為 sle@test.com 帳號新增過去 3 個月的症狀日誌：

- 疲倦：每週 2-3 次，severity 3-6（波動）
- 關節痛：每週 1-2 次，severity 4-7，body_location 交替 '雙膝' / '手指關節'
- 水腫：回診前後偶爾，severity 2-4，body_location '腳踝'
- 掉髮：偶爾，severity 2-3
- triggers 填入：'睡眠不足'、'天氣變化'、'工作壓力'、'經期前'

為 dm@test.com 新增少量：
- 頭暈：偶爾，severity 3-4，triggers '低血糖'

## 回診關聯擴充

在 Feature 1 的 `GET /visits/{id}/related` response 中新增 `symptoms` 欄位：
查詢回診日期 ±7 天內的症狀日誌，讓醫師能看到回診前後的症狀紀錄。

```json
{
  "data": {
    "visit": { ... },
    "medications": [ ... ],
    "labResults": [ ... ],
    "bloodPressures": [ ... ],
    "symptoms": [
      {
        "id": 1,
        "loggedAt": "2026-03-27T21:00:00",
        "symptomType": "關節痛",
        "severity": 5,
        "bodyLocation": "雙膝",
        "daysFromVisit": -1
      }
    ],
    "summary": { ..., "symptomCount": 3 }
  }
}
```

## 完成後

- `dotnet build` 無錯誤
- Migration 可正常 apply
- Seed 資料正確建立且冪等
- 回診關聯 API 包含症狀資料
- 更新 CLAUDE.md Phase 2 進度
