# Phase 2 Feature 1 — 回診關聯查詢（後端）

請閱讀 CLAUDE.md 和這份需求後，實作回診關聯查詢功能。

## 核心邏輯

回診的關聯資料不透過新 FK，而是 Service 層查詢比對：
- 回診 → 用藥：已有 `MedicationDetails.visit_detail_id` FK
- 回診 → 檢驗：同 `recorded_at` 日期 + 同 `nhi_institution_code`
- 回診 → 血壓：`recorded_at` 在回診日期 ±3 天內

## 實作項目

### 1. 新增 VisitRelationService

```csharp
public class VisitRelationService
{
    // 同日同機構的檢驗結果
    Task<List<LabResultWithStatus>> GetRelatedLabResults(
        int userId, DateTime visitDate, string institutionCode);

    // ±N 天血壓
    Task<List<BloodPressureWithDate>> GetNearbyBloodPressures(
        int userId, DateTime visitDate, int dayRange = 3);

    // 完整關聯資料（組合以上 + 用藥）
    Task<VisitRelatedResponse> GetVisitRelated(int userId, int visitId);

    // 檢驗值狀態判斷
    string GetLabStatus(decimal? value, decimal? normalMin, decimal? normalMax);
    // → "normal" | "high" | "low" | "unknown"
}
```

### 2. 新增/修改 Controller 端點

```
GET /visits/{id}/related
```
回傳完整關聯：visit + medications + labResults（含 status） + bloodPressures（含 daysFromVisit） + summary

```
GET /visits/timeline?startDate=...&endDate=...
```
回診時間軸列表，每筆含：
- 基本資訊（日期、機構、主診斷）
- medicationCount, labCount, labAbnormalCount
- bpOnDay（回診當天血壓，nullable）
- keyLabs（最多 3 個重要指標 + status）

keyLabs 選取邏輯：
1. 優先顯示異常值（status = "high" 或 "low"）
2. 其次 UserLabItems 中 sort_order 最前的
3. 預設：Cr (09015C/CRE(肌酸酐))、eGFR (09015C/eGFR)、C3 (12034B/C3)

```
GET /lab-results/by-visit/{visitId}
```
特定回診關聯的完整檢驗結果（不限數量），含 UserLabItems 的 displayName、category、normalMin/Max。

```
GET /blood-pressure/around-date?date=2026-03-28&days=3
```
指定日期 ±N 天的血壓紀錄。

### 3. Response DTOs

```csharp
public class VisitRelatedResponse
{
    public VisitInfoDto Visit { get; set; }
    public List<MedicationDto> Medications { get; set; }
    public List<LabResultWithStatusDto> LabResults { get; set; }
    public List<BloodPressureWithDateDto> BloodPressures { get; set; }
    public VisitSummaryDto Summary { get; set; }
}

public class VisitInfoDto
{
    public int Id { get; set; }
    public DateTime RecordedAt { get; set; }
    public string Institution { get; set; }
    public string InstitutionCode { get; set; }
    public string VisitTypeCode { get; set; }
    public List<DiagnosisDto> Diagnoses { get; set; }  // 最多5組，過濾空值
    public string CopaymentCode { get; set; }
    public decimal? MedicalCost { get; set; }
    public string Source { get; set; }
}

public class DiagnosisDto
{
    public string Code { get; set; }   // "M3214"
    public string Name { get; set; }   // "全身性紅斑性狼瘡腎絲球疾病"
}

public class LabResultWithStatusDto
{
    public int Id { get; set; }
    public string ItemCode { get; set; }
    public string ItemName { get; set; }
    public string DisplayName { get; set; }  // 從 UserLabItems 取
    public string Category { get; set; }     // 從 UserLabItems 取
    public decimal? Value { get; set; }
    public string ValueText { get; set; }
    public bool IsNumeric { get; set; }
    public string Unit { get; set; }
    public string ReferenceRange { get; set; }
    public decimal? NormalMin { get; set; }   // 從 UserLabItems 取
    public decimal? NormalMax { get; set; }   // 從 UserLabItems 取
    public string Status { get; set; }        // "normal" | "high" | "low" | "unknown"
}

public class BloodPressureWithDateDto
{
    public int Id { get; set; }
    public DateTime RecordedAt { get; set; }
    public int Systolic { get; set; }
    public int Diastolic { get; set; }
    public int? Pulse { get; set; }
    public int DaysFromVisit { get; set; }  // -1=前一天, 0=當天, 1=後一天
}

public class VisitSummaryDto
{
    public int LabCount { get; set; }
    public int LabAbnormalCount { get; set; }
    public int MedicationCount { get; set; }
    public int BpCount { get; set; }
    public int? BpAvgSystolic { get; set; }
    public int? BpAvgDiastolic { get; set; }
}

public class VisitTimelineItemDto
{
    public int VisitId { get; set; }
    public DateTime RecordedAt { get; set; }
    public string Institution { get; set; }
    public string PrimaryDiagnosis { get; set; }  // 第一個非空診斷名稱
    public int MedicationCount { get; set; }
    public int LabCount { get; set; }
    public int LabAbnormalCount { get; set; }
    public BloodPressureSimpleDto BpOnDay { get; set; }  // nullable
    public List<KeyLabDto> KeyLabs { get; set; }  // 最多3個
}

public class KeyLabDto
{
    public string DisplayName { get; set; }
    public decimal? Value { get; set; }
    public string Unit { get; set; }
    public string Status { get; set; }
}
```

### 4. Seed 驗證

確認測試帳號 sle@test.com 的資料可以正確關聯：
- 回診日期和檢驗日期在同一天
- 同機構代碼（如 '0602030026' 高雄榮總）
- 回診前後有血壓紀錄

### 5. 完成後

- `dotnet build` 無錯誤
- 更新 CLAUDE.md Phase 2 進度
