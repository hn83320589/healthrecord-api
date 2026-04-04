# Phase 2 Feature 3 — 服藥提醒（後端）

請閱讀 CLAUDE.md 後，實作服藥提醒與服藥紀錄功能。

## Schema

### MedicationReminders（CLAUDE.md 已預留，擴充欄位）

```sql
MedicationReminders
  id                   INT PK AUTO_INCREMENT
  user_id              INT FK → Users(id) ON DELETE CASCADE
  medication_detail_id INT NULL FK → MedicationDetails(id) ON DELETE SET NULL
  medication_name      VARCHAR(200) NOT NULL    -- 冗餘存儲，即使原始用藥刪除仍可顯示
  dosage               VARCHAR(100) NULL        -- '5mg'
  frequency            VARCHAR(100) NULL        -- 'QD' | 'BID' | 'TID'
  remind_times         JSON NOT NULL            -- ["08:00"] 或 ["08:00","20:00"]（BID 兩個時間）
  days_of_week         VARCHAR(20) NOT NULL DEFAULT 'MON,TUE,WED,THU,FRI,SAT,SUN'
  start_date           DATE NULL                -- 開始日期（NULL = 立即開始）
  end_date             DATE NULL                -- 結束日期（NULL = 無期限）
  is_enabled           BOOLEAN NOT NULL DEFAULT TRUE
  created_at           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
  updated_at           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP

  INDEX idx_user_enabled (user_id, is_enabled)
```

> `remind_times` 改為 JSON array，支援一天多次提醒（BID = 早晚各一次）。

### MedicationLogs（新增）

```sql
MedicationLogs
  id                   INT PK AUTO_INCREMENT
  user_id              INT FK → Users(id) ON DELETE CASCADE
  reminder_id          INT NULL FK → MedicationReminders(id) ON DELETE SET NULL
  medication_name      VARCHAR(200) NOT NULL
  dosage               VARCHAR(100) NULL
  scheduled_at         DATETIME NOT NULL       -- 排定服藥時間
  taken_at             DATETIME NULL           -- 實際服藥時間（NULL = 尚未服藥）
  status               VARCHAR(20) NOT NULL DEFAULT 'pending'
                       -- 'pending' | 'taken' | 'skipped' | 'late'
  note                 TEXT NULL
  created_at           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP

  INDEX idx_user_date (user_id, scheduled_at)
  INDEX idx_reminder (reminder_id, scheduled_at)
  INDEX idx_status (user_id, status, scheduled_at)
```

### MedicationDetails 擴充

更新既有的 MedicationDetails，啟用 Phase 2 欄位：

```sql
-- 這些欄位 Schema 已有，確認 Service 層有使用
is_active    BOOLEAN NOT NULL DEFAULT FALSE   -- 目前是否在服用
start_date   DATE NULL                        -- 開始服藥日
end_date     DATE NULL                        -- 停藥日
```

## API 端點

### 服藥提醒 CRUD

```
GET    /medication-reminders
       回傳所有提醒（含 is_enabled 狀態）

POST   /medication-reminders
       手動建立提醒

PUT    /medication-reminders/{id}
       更新（修改時間、啟用/停用等）

DELETE /medication-reminders/{id}
       刪除提醒（關聯的未來 pending logs 也一併刪除）

PATCH  /medication-reminders/{id}/toggle
       快速切換 is_enabled（不需傳 body）
```

### 從已有用藥快速建立提醒

```
POST /medication-reminders/from-medication/{medicationDetailId}
```

自動從 MedicationDetails 取得 medication_name、dosage，
建立提醒並設定 medication_detail_id FK。

Request body（可選覆蓋）：
```json
{
  "remindTimes": ["08:00", "20:00"],
  "daysOfWeek": "MON,TUE,WED,THU,FRI,SAT,SUN",
  "startDate": "2026-04-05",
  "endDate": null
}
```

若不傳 body，預設：
- remindTimes = ["08:00"]（QD 一天一次）
- 如果 MedicationDetails.frequency 含 "BID" → ["08:00", "20:00"]
- 如果含 "TID" → ["08:00", "14:00", "20:00"]
- daysOfWeek = 全週
- startDate = 今天
- endDate = null

### 服藥紀錄

```
GET /medication-logs?startDate=...&endDate=...&status=...
    回傳指定期間的服藥紀錄

GET /medication-logs/today
    回傳今天所有排定的服藥紀錄（含已服/未服/跳過）

POST /medication-logs/{id}/take
    標記已服藥，taken_at = now()，status = 'taken'
    若距離 scheduled_at 超過 1 小時，status = 'late'

POST /medication-logs/{id}/skip
    標記跳過，status = 'skipped'
    可選 body: { "note": "忘記帶藥出門" }

POST /medication-logs/{id}/undo
    撤銷（重設為 pending，清除 taken_at）
```

### 服藥率統計

```
GET /medication-logs/adherence?days=30
```

Response：
```json
{
  "success": true,
  "data": {
    "period": { "start": "2026-03-05", "end": "2026-04-04" },
    "overall": {
      "totalScheduled": 60,
      "taken": 52,
      "late": 3,
      "skipped": 2,
      "missed": 3,
      "adherenceRate": 86.7
    },
    "byMedication": [
      {
        "medicationName": "Prednisolone 5mg",
        "totalScheduled": 30,
        "taken": 28,
        "adherenceRate": 93.3
      },
      {
        "medicationName": "MMF 1000mg",
        "totalScheduled": 30,
        "taken": 24,
        "adherenceRate": 80.0
      }
    ],
    "dailyTrend": [
      { "date": "2026-04-04", "scheduled": 2, "taken": 2, "rate": 100 },
      { "date": "2026-04-03", "scheduled": 2, "taken": 1, "rate": 50 }
    ]
  }
}
```

- `missed`：scheduled_at 已過且 status 仍為 'pending'
- `adherenceRate`：(taken + late) / totalScheduled * 100

### 目前服用中的藥物

```
GET /medications/active
```

回傳 MedicationDetails 中 `is_active = true` 的藥物清單。

## Log 自動生成邏輯

### 方案：API 呼叫時動態生成

不用排程，在以下時機動態生成當天的 MedicationLogs：

```csharp
// 每次呼叫 GET /medication-logs/today 時
public async Task<List<MedicationLog>> GetTodayLogs(int userId)
{
    var today = DateTime.Today;
    var dayOfWeek = today.DayOfWeek; // → "MON", "TUE" ...

    // 1. 取得所有啟用中的提醒
    var reminders = await context.MedicationReminders
        .Where(r => r.UserId == userId
                  && r.IsEnabled
                  && (r.StartDate == null || r.StartDate <= today)
                  && (r.EndDate == null || r.EndDate >= today)
                  && r.DaysOfWeek.Contains(DayToString(dayOfWeek)))
        .ToListAsync();

    // 2. 檢查今天的 logs 是否已建立
    var existingLogs = await context.MedicationLogs
        .Where(l => l.UserId == userId
                  && l.ScheduledAt.Date == today)
        .ToListAsync();

    // 3. 補建缺少的 logs
    foreach (var reminder in reminders)
    {
        var times = JsonSerializer.Deserialize<string[]>(reminder.RemindTimes);
        foreach (var time in times)
        {
            var scheduledAt = today.Add(TimeSpan.Parse(time));
            var exists = existingLogs.Any(l =>
                l.ReminderId == reminder.Id
                && l.ScheduledAt == scheduledAt);

            if (!exists)
            {
                var log = new MedicationLog
                {
                    UserId = userId,
                    ReminderId = reminder.Id,
                    MedicationName = reminder.MedicationName,
                    Dosage = reminder.Dosage,
                    ScheduledAt = scheduledAt,
                    Status = "pending"
                };
                context.MedicationLogs.Add(log);
            }
        }
    }

    await context.SaveChangesAsync();

    // 4. 回傳今天所有 logs
    return await context.MedicationLogs
        .Where(l => l.UserId == userId && l.ScheduledAt.Date == today)
        .OrderBy(l => l.ScheduledAt)
        .ToListAsync();
}
```

### 過期未服藥處理

每次查詢 today logs 時，順便檢查過去 24 小時內 status='pending' 且 scheduled_at 已過的：

```csharp
// 超過排定時間 2 小時仍為 pending → 標記 missed
var cutoff = DateTime.Now.AddHours(-2);
var missedLogs = await context.MedicationLogs
    .Where(l => l.UserId == userId
              && l.Status == "pending"
              && l.ScheduledAt < cutoff)
    .ToListAsync();

foreach (var log in missedLogs)
{
    log.Status = "missed";
}
```

## DTOs

```csharp
public class CreateReminderRequest
{
    public string MedicationName { get; set; }     // required
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public List<string> RemindTimes { get; set; }  // ["08:00", "20:00"]
    public string DaysOfWeek { get; set; } = "MON,TUE,WED,THU,FRI,SAT,SUN";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class UpdateReminderRequest
{
    public string? MedicationName { get; set; }
    public string? Dosage { get; set; }
    public List<string>? RemindTimes { get; set; }
    public string? DaysOfWeek { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsEnabled { get; set; }
}

public class ReminderResponse
{
    public int Id { get; set; }
    public int? MedicationDetailId { get; set; }
    public string MedicationName { get; set; }
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public List<string> RemindTimes { get; set; }
    public string DaysOfWeek { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsEnabled { get; set; }
}

public class MedicationLogResponse
{
    public int Id { get; set; }
    public int? ReminderId { get; set; }
    public string MedicationName { get; set; }
    public string? Dosage { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? TakenAt { get; set; }
    public string Status { get; set; }    // pending | taken | late | skipped | missed
    public string? Note { get; set; }
}

public class AdherenceResponse
{
    public PeriodDto Period { get; set; }
    public AdherenceOverallDto Overall { get; set; }
    public List<AdherenceByMedDto> ByMedication { get; set; }
    public List<AdherenceDailyDto> DailyTrend { get; set; }
}
```

## Seed 測試資料

為 sle@test.com 建立：

**提醒**：
- Prednisolone 5mg QD → remind_times: ["08:00"]
- MMF 1000mg BID → remind_times: ["08:00", "20:00"]
- Hydroxychloroquine 200mg BID → remind_times: ["08:00", "20:00"]

**服藥紀錄**：過去 14 天的 logs
- Prednisolone：全部 taken（乖乖吃）
- MMF：偶爾 late（晚吃 1-2 小時），1 次 skipped
- HCQ：2 次 missed

為 htn@test.com 建立：
- Amlodipine 5mg QD → ["08:00"]
- 過去 7 天 logs，adherence 約 85%

## 回診關聯擴充

在 `GET /visits/{id}/related` response 中新增 `activeReminders` 欄位：
顯示回診當時正在服用的藥物提醒清單（判斷 start_date/end_date 涵蓋回診日期）。

## 完成後

- `dotnet build` 無錯誤
- Migration 正常 apply
- Seed 資料正確（提醒 + logs）
- GET /medication-logs/today 可動態生成當天 logs
- 服藥率計算正確
- 更新 CLAUDE.md Phase 2 進度
