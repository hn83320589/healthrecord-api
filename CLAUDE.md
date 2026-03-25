# HealthRecord.API

## 專案概述
個人身體紀錄後端 API。以狼瘡腎炎等慢性病日常管理為核心設計場景。
支援手動輸入與健保存摺 JSON 匯入（`source` 欄位區分來源）。

部署目標：Zeabur（Docker 容器）

---

## 開發路線圖

### ✅ Phase 1（現在做）
- 帳號系統（含身體基本資料、緊急聯絡人）
- 血壓紀錄（手動 CRUD、統計、圖表）
- 檢驗紀錄（手動 CRUD、趨勢圖）
- 看診紀錄（Phase 1 只支援 NHI 匯入，GET + DELETE）
- 用藥紀錄（Phase 1 只支援 NHI 匯入，GET + DELETE）
- 健保存摺 JSON 匯入（r7 檢驗 + r1 看診 + r1_1 用藥，新蓋舊策略）

### 🔜 Phase 2（之後做）
- 看診紀錄手動 CRUD + 回診前彙整 PDF
- 用藥紀錄手動 CRUD + 服藥提醒
- 症狀日誌

---

## Tech Stack
- **.NET 9** Web API
- **Entity Framework Core 9** + `Pomelo.EntityFrameworkCore.MySql`
- **MySQL 8.x**
- **JWT** (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- **FluentValidation** / **AutoMapper** / **Serilog**

---

## 架構原則
- 分層：Controller → Service → Repository → DbContext
- 統一回應格式：所有 API 回傳 `ApiResponse<T>`
- 路由：無版本前綴（直接 `/auth`、`/profile`、`/blood-pressure` 等）
- 全域錯誤處理：`ExceptionMiddleware`
- 各功能各自獨立的表，透過 nullable FK 弱關聯，不強制 join

---

## 設計哲學

```
每張 Detail 表（BloodPressureDetails / LabResultDetails / MedicationDetails）
  唯一必填外來鍵：user_id
  弱關聯：health_record_id（NULL = 與看診無關，NOT NULL = 隸屬於某次看診）
  匯入追蹤：nhi_import_log_id（NULL = 手動輸入，NOT NULL = NHI 匯入）

這樣 GET 查詢時：
  查血壓：SELECT * FROM BloodPressureDetails WHERE user_id = ?
  查某次看診的用藥：SELECT * FROM MedicationDetails WHERE health_record_id = ?
  查某批匯入的資料：SELECT * FROM LabResultDetails WHERE nhi_import_log_id = ?
  不需要 join 一堆表
```

---

## 資料庫 Schema

### Users

```sql
Users
  id                 INT PK AUTO_INCREMENT
  email              VARCHAR(255) UNIQUE NOT NULL
  password_hash      VARCHAR(255) NOT NULL
  display_name       VARCHAR(100) NOT NULL
  birth_date         DATE NULL
  gender             VARCHAR(20) NULL
  height_cm          DECIMAL(5,1) NULL
  weight_kg          DECIMAL(5,2) NULL
  blood_type         VARCHAR(5) NULL          -- 'A+' | 'B-' | 'O+' | 'AB+' 等
  chronic_conditions TEXT NULL
  allergies          TEXT NULL
  created_at         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
  updated_at         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP

EmergencyContacts
  id           INT PK AUTO_INCREMENT
  user_id      INT NOT NULL FK → Users(id) ON DELETE CASCADE
  name         VARCHAR(100) NOT NULL
  relationship VARCHAR(50) NOT NULL
  phone        VARCHAR(30) NOT NULL
  note         VARCHAR(255) NULL
  sort_order   INT NOT NULL DEFAULT 0
  created_at   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
```

### HealthRecords（看診主表）

```sql
-- 一次看診 = 一筆 HealthRecords
-- 血壓/檢驗/用藥可以獨立存在，不一定要有對應看診
HealthRecords
  id                   INT PK AUTO_INCREMENT
  user_id              INT NOT NULL FK → Users(id) ON DELETE CASCADE
  clinic_date          DATETIME NOT NULL              -- 就醫日期
  hospital             VARCHAR(100) NULL              -- 高雄榮總
  hospital_code        VARCHAR(20) NULL               -- 0602030026（NHI 機構代碼）
  visit_seq            VARCHAR(20) NULL               -- NHI 就醫序號，r1.7
  primary_icd_code     VARCHAR(20) NULL               -- 主診斷 ICD，r1.8
  primary_diagnosis    VARCHAR(200) NULL              -- 主診斷名稱，r1.9
  secondary_diagnoses  TEXT NULL                      -- 次診斷 JSON 陣列
                                                      -- [{"code":"I129","name":"高血壓性慢性腎臟病"}]
  copay                INT NULL                       -- 部分負擔，r1.12
  total_points         INT NULL                       -- 總點數，r1.13
  source               VARCHAR(20) NOT NULL DEFAULT 'manual'
                                                      -- 'manual' | 'nhi_import'
  nhi_import_log_id    INT NULL FK → NhiImportLogs(id) ON DELETE SET NULL
                                                      -- 記錄此筆來自哪一次匯入
  note                 TEXT NULL
  created_at           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
```

### NhiImportLogs

```sql
NhiImportLogs
  id               INT PK AUTO_INCREMENT
  user_id          INT NOT NULL FK → Users(id) ON DELETE CASCADE
  imported_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
  data_date        VARCHAR(8) NOT NULL     -- '20260324'（b1.2 資料截止日）
  date_range_start DATE NOT NULL           -- 本次資料最早日期
  date_range_end   DATE NOT NULL           -- 本次資料最晚日期
  health_record_count INT NOT NULL DEFAULT 0
  medication_count INT NOT NULL DEFAULT 0
  lab_count        INT NOT NULL DEFAULT 0
  skipped_labs     INT NOT NULL DEFAULT 0  -- 找不到對應指標的數量
```

### BloodPressureDetails

```sql
BloodPressureDetails
  id                   INT PK AUTO_INCREMENT
  user_id              INT NOT NULL FK → Users(id) ON DELETE CASCADE  -- 唯一必填 FK
  health_record_id             INT NULL FK → HealthRecords(id) ON DELETE SET NULL  -- 弱關聯
  nhi_import_log_id    INT NULL FK → NhiImportLogs(id) ON DELETE SET NULL
  recorded_at          DATETIME NOT NULL
  systolic             INT NOT NULL              -- 收縮壓
  diastolic            INT NOT NULL              -- 舒張壓
  pulse                INT NOT NULL              -- 脈搏
  measurement_position VARCHAR(20) NULL          -- 'left_arm' | 'right_arm'
  source               VARCHAR(20) NOT NULL DEFAULT 'manual'
                                                 -- 'manual' | 'nhi_import' | 'healthkit'
  note                 TEXT NULL
  created_at           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
```

### LabResultDetails（合併原 LabItemDefinitions）

```sql
-- 每筆結果自帶項目定義（名稱/代碼/單位/參考範圍）
-- 不再需要獨立的 LabItemDefinitions 表
LabResultDetails
  id                   INT PK AUTO_INCREMENT
  user_id              INT NOT NULL FK → Users(id) ON DELETE CASCADE  -- 唯一必填 FK
  health_record_id             INT NULL FK → HealthRecords(id) ON DELETE SET NULL  -- 弱關聯
  nhi_import_log_id    INT NULL FK → NhiImportLogs(id) ON DELETE SET NULL
  recorded_at          DATETIME NOT NULL
  -- 項目定義（原 LabItemDefinitions 的欄位）
  item_name            VARCHAR(100) NOT NULL     -- '肌酸酐'
  item_code            VARCHAR(50) NOT NULL      -- 'Cr'
  unit                 VARCHAR(30) NOT NULL      -- 'mg/dL'
  category             VARCHAR(50) NOT NULL      -- '腎功能'
  normal_min           DECIMAL(10,4) NULL
  normal_max           DECIMAL(10,4) NULL
  -- 結果值（定量 or 定性）
  is_numeric           BOOLEAN NOT NULL DEFAULT TRUE
  value_numeric        DECIMAL(10,4) NULL        -- 定量：1.20, 69.7
  value_text           VARCHAR(200) NULL         -- 定性：'-', '1+', '1:40 SP'
  is_abnormal          BOOLEAN NOT NULL DEFAULT FALSE
  -- NHI 對應與原始值保留
  nhi_code             VARCHAR(20) NULL          -- '09015C'（匯入比對用）
  nhi_item_name        VARCHAR(100) NULL         -- 'CRE(肌酸酐)'（匯入比對用）
  nhi_raw_value        VARCHAR(200) NULL         -- r7.11 原始值（debug）
  nhi_raw_range        VARCHAR(500) NULL         -- r7.12 原始範圍（debug）
  source               VARCHAR(20) NOT NULL DEFAULT 'manual'
                                                 -- 'manual' | 'nhi_import'
  note                 TEXT NULL
  created_at           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
```

### MedicationDetails

```sql
MedicationDetails
  id                   INT PK AUTO_INCREMENT
  user_id              INT NOT NULL FK → Users(id) ON DELETE CASCADE  -- 唯一必填 FK
  health_record_id             INT NULL FK → HealthRecords(id) ON DELETE SET NULL  -- 弱關聯
  nhi_import_log_id    INT NULL FK → NhiImportLogs(id) ON DELETE SET NULL
  recorded_at          DATETIME NOT NULL
  drug_name            VARCHAR(200) NOT NULL     -- '那寶膜衣錠50毫克'
  nhi_drug_code        VARCHAR(30) NULL          -- 'AB57103100'
  quantity             DECIMAL(8,2) NULL         -- 56.00（顆數）
  days                 INT NULL                  -- 28（給藥天數）
  drug_type            VARCHAR(20) NOT NULL DEFAULT 'medication'
                                                 -- 'medication' | 'exam' | 'service'
  source               VARCHAR(20) NOT NULL DEFAULT 'manual'
                                                 -- 'manual' | 'nhi_import'
  note                 TEXT NULL
  created_at           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
```

---

## 表結構總覽

```
Users (1)
  ├── EmergencyContacts (多，強關聯)
  ├── HealthRecords (多，看診主表)
  │     └── nhi_import_log_id → NhiImportLogs（nullable）
  ├── BloodPressureDetails (多，可獨立)
  │     ├── health_record_id → HealthRecords（nullable，弱關聯）
  │     └── nhi_import_log_id → NhiImportLogs（nullable）
  ├── LabResultDetails (多，可獨立)
  │     ├── health_record_id → HealthRecords（nullable，弱關聯）
  │     └── nhi_import_log_id → NhiImportLogs（nullable）
  ├── MedicationDetails (多，可獨立)
  │     ├── health_record_id → HealthRecords（nullable，弱關聯）
  │     └── nhi_import_log_id → NhiImportLogs（nullable）
  └── NhiImportLogs (多，匯入批次記錄)
```

---

## 預設檢驗項目 Seed（常數，非 DB 表）

手動新增檢驗時，前端提供「常用項目選單」，從 App 常數載入（不需 DB 查詢）。
NHI 匯入時，以 `nhi_code + nhi_item_name` 比對常數，填入 `item_name / item_code / unit / category / normal_min / normal_max`。

```csharp
// Common/Constants/LabItemPresets.cs
public static class LabItemPresets
{
    public static readonly List<LabItemPreset> Items = new()
    {
        // 腎功能
        new("肌酸酐",   "Cr",    "mg/dL",            "腎功能", 0.7m,  1.3m,  "09015C", "CRE(肌酸酐)"),
        new("腎絲球過濾率","eGFR","mL/min/1.73m²",   "腎功能", 60m,   null,  "09015C", "eGFR"),
        new("尿素氮",   "BUN",   "mg/dL",            "腎功能", 7m,    25m,   "09002C", "血中尿素氮"),
        new("尿肌酐",   "uCr",   "mg/dL",            "腎功能", null,  null,  "09016C", "CRE(肌酸酐)"),
        new("微白蛋白", "Microalbumin","mg/dL",       "腎功能", null,  1.9m,  "12111C", "Microalbumin"),
        new("UACR",    "UACR",  "mg/g",             "腎功能", null,  30m,   "12111C", "UACR"),
        new("UPCR",    "UPCR",  "mg/g",             "腎功能", null,  200m,  null,     null),
        // 免疫指標
        new("抗雙股DNA抗體","anti-dsDNA","IU/mL",    "免疫指標",0m,   15m,   "12060C", "Anti ds-DNA Ab"),
        new("抗核抗體", "ANA",   "–",                "免疫指標",null,  null,  "12053C", "ANA"),
        new("補體C3",   "C3",    "mg/dL",            "免疫指標",87m,   200m,  "12034B", "C3"),
        new("補體C4",   "C4",    "mg/dL",            "免疫指標",19m,   52m,   "12038B", "C4"),
        // 血球
        new("白血球",   "WBC",   "10³/μL",           "血球",    4.1m,  10.5m, "08011C", "WBC 白血球"),
        new("紅血球",   "RBC",   "10⁶/μL",           "血球",    4.3m,  6.0m,  "08011C", "RBC 紅血球"),
        new("血色素",   "Hb",    "g/dL",             "血球",    13.4m, 17.2m, "08011C", "Hemoglobin 血色素"),
        new("血球比容值","Hct",  "%",                "血球",    39.8m, 50.7m, "08011C", "Hematocrit 血球比容值"),
        new("血小板",   "PLT",   "10³/μL",           "血球",    160m,  370m,  "08011C", "Platelet 血小板"),
        new("平均血球容積","MCV","fL",               "血球",    83.4m, 98.5m, "08011C", "MCV 平均血球容積"),
        new("嗜中性球", "Neutrophil","%",            "血球",    41.8m, 70.8m, "08013C", "Neutrophil Seg.嗜中性"),
        new("淋巴球",   "Lymphocyte","%",            "血球",    20.7m, 49.2m, "08013C", "Lymphocyte 淋巴球"),
        // 發炎指標
        new("C反應蛋白","CRP",   "mg/L",             "發炎指標",0m,    1.0m,  "12015C", "Ｃ反應性蛋白試驗－免疫比濁法"),
        new("紅血球沈降","ESR",  "mm/hr",            "發炎指標",2m,    10m,   "08005C", "血球沉降率1小時"),
        // 肝功能
        new("丙胺酸轉胺酶","ALT","U/L",              "肝功能",  0m,    40m,   "09026C", "血清麩胺酸丙酮酸轉氨基"),
        new("天門冬胺酸","AST",  "U/L",              "肝功能",  13m,   39m,   "09025C", "血清麩胺酸苯醋酸轉氨基"),
        // 血脂
        new("總膽固醇", "Cholesterol","mg/dL",       "血脂",    null,  200m,  "09001C", "總膽固醇"),
        new("三酸甘油脂","TG",  "mg/dL",             "血脂",    null,  150m,  "09004C", "三酸甘油脂 TG"),
        new("高密度脂蛋白","HDL","mg/dL",            "血脂",    40m,   null,  "09043C", "高密度脂蛋白－膽固醇"),
        new("低密度脂蛋白","LDL","mg/dL",            "血脂",    null,  130m,  "09044C", "低密度脂蛋白 LDL"),
        // 血糖
        new("空腹血糖", "Glucose","mg/dL",           "血糖",    70m,   100m,  "09005C", "飯前血糖 Glucose"),
        new("醣化血色素","HbA1c","%",               "血糖",    null,  5.7m,  "09006C", "醣化血色素"),
        // 電解質
        new("鈉",       "Na",    "mEq/L",            "電解質",  136m,  146m,  "09021C", "鈉"),
        new("鉀",       "K",     "mEq/L",            "電解質",  3.5m,  5.1m,  "09022C", "鉀"),
        new("氯",       "Cl",    "mEq/L",            "電解質",  101m,  109m,  "09023C", "氯"),
        new("鈣",       "Ca",    "mg/dL",            "電解質",  8.6m,  10.3m, "09011C", "CA(鈣)"),
        // 其他
        new("尿酸",     "Uric_acid","mg/dL",         "其他",    4.4m,  7.6m,  "09013C", "尿酸"),
        new("白蛋白",   "Albumin","g/dL",            "其他",    3.5m,  5.7m,  "09038C", "白蛋白"),
        new("肌酸磷化酶","CK",   "U/L",             "其他",    30m,   223m,  "09032C", "肌酸磷化?"),
    };
}

public record LabItemPreset(
    string ItemName, string ItemCode, string Unit, string Category,
    decimal? NormalMin, decimal? NormalMax,
    string? NhiCode, string? NhiItemName);
```

---

## NHI 匯入策略（新蓋舊）

```
1. 解析 JSON，計算 date_range_start / date_range_end
2. Transaction 內：
   a. 刪除日期範圍內 source='nhi_import' 的舊資料：
      DELETE HealthRecords     WHERE user_id=? AND source='nhi_import' AND visited_at BETWEEN ...
      DELETE BloodPressureDetails  WHERE user_id=? AND source='nhi_import' AND recorded_at BETWEEN ...
      DELETE LabResultDetails  WHERE user_id=? AND source='nhi_import' AND recorded_at BETWEEN ...
      DELETE MedicationDetails WHERE user_id=? AND source='nhi_import' AND recorded_at BETWEEN ...
   b. r1  → HealthRecords（含 nhi_import_log_id）
   c. r1_1→ MedicationDetails（health_record_id = 對應 HealthRecord.id，nhi_import_log_id）
   d. r7  → LabResultDetails（health_record_id = 對應 HealthRecord.id 或 null，nhi_import_log_id）
   e. 寫入 NhiImportLog
   source='manual' 的資料完全不受影響
```

## NHI 撤銷策略

```
DELETE HealthRecords     WHERE nhi_import_log_id = logId
DELETE BloodPressureDetails  WHERE nhi_import_log_id = logId
DELETE LabResultDetails  WHERE nhi_import_log_id = logId
DELETE MedicationDetails WHERE nhi_import_log_id = logId
DELETE NhiImportLogs     WHERE id = logId
不需要 join，每張表直接查自己的 nhi_import_log_id
```

---

## NHI r1_1 藥品類型判斷

```
drug_type 規則：
  nhi_drug_code 以字母開頭（AB57103100, BC230161G0）→ 'medication'（真正的藥品）
  nhi_drug_code 為 00xxx 或 05xxx                   → 'service'（診察費/藥事費）
  其餘純數字開頭                                      → 'exam'（檢驗申報代碼）
```

---

## API 端點清單

### Phase 1

```
POST   /auth/register
POST   /auth/login
POST   /auth/refresh

GET    /profile
PUT    /profile
GET    /profile/emergency-contacts
POST   /profile/emergency-contacts
PUT    /profile/emergency-contacts/{id}
DELETE /profile/emergency-contacts/{id}

-- 血壓
GET    /blood-pressure
POST   /blood-pressure
GET    /blood-pressure/{id}
PUT    /blood-pressure/{id}              -- 僅 source='manual'
DELETE /blood-pressure/{id}              -- 僅 source='manual'
GET    /blood-pressure/stats
GET    /blood-pressure/chart-data

-- 檢驗
GET    /lab-results
POST   /lab-results                      -- 手動新增，多筆同時送出
GET    /lab-results/{id}
PUT    /lab-results/{id}                 -- 僅 source='manual'
DELETE /lab-results/{id}                 -- 僅 source='manual'
GET    /lab-results/by-date              -- 依日期分組
GET    /lab-results/trend                -- 趨勢圖（by item_code, is_numeric=true）

-- 看診（Phase 1：NHI 匯入 + GET + DELETE）
GET    /health-records
GET    /health-records/{id}                      -- 含同 health_record_id 的 medications + lab_results
DELETE /health-records/{id}                      -- 僅 source='nhi_import'

-- 用藥（Phase 1：NHI 匯入 + GET + DELETE）
GET    /medications
GET    /medications/current              -- 最近一次就診的藥品（drug_type='medication'）
DELETE /medications/{id}                 -- 僅 source='nhi_import'

-- NHI 匯入
POST   /nhi/import                       -- 新蓋舊，r1 + r1_1 + r7
GET    /nhi/import/logs
DELETE /nhi/import/{logId}               -- 撤銷，依 nhi_import_log_id 清除
```

### Phase 2 新增

```
POST   /health-records
PUT    /health-records/{id}
DELETE /health-records/{id}                      -- 開放 source='manual'
GET    /health-records/{id}/summary

POST   /medications
PUT    /medications/{id}
DELETE /medications/{id}                 -- 開放 source='manual'
POST   /medications/{id}/reminders
```

---

## 目錄結構

```
HealthRecord.API/
├── Controllers/
│   ├── AuthController.cs
│   ├── ProfileController.cs
│   ├── BloodPressureController.cs
│   ├── LabController.cs
│   ├── HealthRecordController.cs         ← Phase 1: GET + DELETE
│   ├── MedicationController.cs    ← Phase 1: GET + DELETE
│   └── NhiController.cs
├── Services/
│   ├── Interfaces/
│   ├── AuthService.cs
│   ├── ProfileService.cs
│   ├── BloodPressureService.cs
│   ├── LabService.cs
│   ├── HealthRecordService.cs
│   ├── MedicationService.cs
│   └── NhiImportService.cs
├── Models/
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── EmergencyContact.cs
│   │   ├── HealthRecord.cs        ← 看診主表（含診斷欄位）
│   │   ├── BloodPressureDetail.cs
│   │   ├── LabResultDetail.cs     ← 含項目定義欄位
│   │   ├── MedicationDetail.cs
│   │   └── NhiImportLog.cs
│   └── DTOs/
│       ├── Auth/ Profile/ BloodPressure/ Lab/ HealthRecord/ Medication/ Nhi/
│       └── Common/ApiResponse.cs
├── Infrastructure/Data/
│   ├── AppDbContext.cs
│   └── Migrations/
└── Common/
    ├── Constants/LabItemPresets.cs
    ├── Middleware/ExceptionMiddleware.cs
    └── Helpers/
        ├── JwtHelper.cs
        └── NhiJsonParser.cs
```

---

## 常見查詢範例

```csharp
// 查某使用者的所有血壓（不需要任何 join）
var bp = await context.BloodPressureDetails
    .Where(b => b.UserId == userId)
    .OrderByDescending(b => b.RecordedAt)
    .ToListAsync();

// 查某次看診的所有用藥
var meds = await context.MedicationDetails
    .Where(m => m.HealthRecordId == healthRecordId && m.DrugType == "medication")
    .ToListAsync();

// 查某個指標的趨勢（不需要 join LabItemDefinitions）
var trend = await context.LabResultDetails
    .Where(l => l.UserId == userId
             && l.ItemCode == "Cr"
             && l.IsNumeric
             && l.ValueNumeric != null)
    .OrderBy(l => l.RecordedAt)
    .ToListAsync();

// 查某次 NHI 匯入的所有資料（三張表各自查，不 join）
var healthRecords = await context.HealthRecords
    .Where(h => h.NhiImportLogId == logId).ToListAsync();
var labs = await context.LabResultDetails
    .Where(l => l.NhiImportLogId == logId).ToListAsync();
var meds = await context.MedicationDetails
    .Where(m => m.NhiImportLogId == logId).ToListAsync();
```

---

## 程式碼慣例
- C#，async/await 全面非同步
- 無全域 LabItemDefinitions 表，項目定義存在 LabResultDetails 每筆資料中
- 前端常用項目清單從 `LabItemPresets` 常數取得，不需要 API
- NHI 匯入整批 Transaction，新蓋舊，失敗完整 rollback
- PUT / DELETE 血壓與檢驗只允許 `source='manual'`
- DELETE 看診 / 用藥：Phase 1 只允許 `source='nhi_import'`
