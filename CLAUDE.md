# HealthRecord.API — CLAUDE.md v2

## 專案概述

個人身體紀錄後端 API。以慢性病（狼瘡腎炎、高血壓、糖尿病等）日常管理為核心設計場景。
支援手動輸入、健保存摺 JSON 匯入、HealthKit 同步（`source` 欄位區分來源）。

部署目標：Zeabur（Docker 容器）

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
- `HealthRecords`（record_type）+ 各 Detail 子表，支援無痛擴展新紀錄類型
- `UserLabItems` 動態維護檢驗項目，取代硬編碼常數

---

## 開發路線圖

### ✅ Phase 1（基礎功能）

- 帳號系統（註冊、登入、JWT、Profile、緊急聯絡人）
- 血壓紀錄（手動 CRUD、統計、圖表）
- 檢驗數值（手動 CRUD、趨勢圖）
- 檢驗項目動態維護（UserLabItems CRUD、註冊初始化、匯入自動新增）
- 回診紀錄（NHI 匯入 + 手動 CRUD）
- 用藥紀錄（NHI 匯入 + 手動 CRUD）
- 健保存摺 JSON 匯入（r7 檢驗 + r1 回診 + r1_1 醫令）
- NHI 匯入去重（SHA256 hash + r7 dedup）
- 測試資料 Seed（多帳號、多情境）

### 🔜 Phase 2（進階功能）

- 服藥提醒通知（MedicationReminders）
- 症狀日誌（SymptomLogs）
- 回診前彙整 / 摘要 PDF 匯出
- 回診 ↔ 檢驗 ↔ 血壓 關聯查詢

### 🔮 未來擴充（Schema 預留）

- 體重 / 體組成紀錄（record_type='body_composition'）
- 運動紀錄（record_type='exercise'）
- 疫苗接種（匯入 NHI r6）
- 中醫紀錄（匯入 NHI r9）
- 影像檢查（匯入 NHI r8）
- 資料匯出 / FHIR 格式相容

---

## 資料庫 Schema

### 帳號

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
  blood_type         VARCHAR(5) NULL          -- 'A+' | 'B-' | 'O+' | 'AB+' ...
  chronic_conditions TEXT NULL                -- JSON array: ["SLE", "Lupus Nephritis"]
  allergies          TEXT NULL                -- JSON array: ["Penicillin"]
  created_at         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
  updated_at         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP

EmergencyContacts
  id           INT PK AUTO_INCREMENT
  user_id      INT FK → Users(id) ON DELETE CASCADE
  name         VARCHAR(100) NOT NULL
  relationship VARCHAR(50) NOT NULL
  phone        VARCHAR(30) NOT NULL
  note         VARCHAR(255) NULL
  sort_order   INT NOT NULL DEFAULT 0
  created_at   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
```

### 檢驗項目維護檔

```sql
UserLabItems
  id            INT PK AUTO_INCREMENT
  user_id       INT NOT NULL FK → Users(id) ON DELETE CASCADE
  item_code     VARCHAR(50) NOT NULL      -- r7.8 NHI 申報代碼，如 '09015C'
  item_name     VARCHAR(100) NOT NULL     -- r7.10 子項目名稱，如 'CRE(肌酸酐)'
  display_name  VARCHAR(100) NULL         -- 使用者自訂顯示名，如 '肌酸酐'
  unit          VARCHAR(30) NOT NULL DEFAULT ''
  category      VARCHAR(50) NOT NULL DEFAULT '其他'
  normal_min    DECIMAL(10,4) NULL
  normal_max    DECIMAL(10,4) NULL
  sort_order    INT NOT NULL DEFAULT 0
  is_preset     BOOLEAN NOT NULL DEFAULT FALSE
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
  updated_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP

  UNIQUE INDEX uq_user_item (user_id, item_code, item_name)
```

### 健康紀錄主表

```sql
HealthRecords
  id               INT PK AUTO_INCREMENT
  user_id          INT FK → Users(id) ON DELETE CASCADE
  record_type      VARCHAR(50) NOT NULL
                   -- 'blood_pressure' | 'lab_result' | 'visit' | 'medication'
                   -- 未來擴充：'body_composition' | 'exercise' | 'symptom'
  recorded_at      DATETIME NOT NULL
  note             TEXT NULL
  source           VARCHAR(20) NOT NULL DEFAULT 'manual'
                   -- 'manual' | 'nhi_import' | 'healthkit'
  nhi_import_log_id     INT NULL FK → NhiImportLogs(id) ON DELETE SET NULL
  nhi_institution       VARCHAR(100) NULL   -- r1.4 / r7.4
  nhi_institution_code  VARCHAR(20) NULL    -- r1.3 / r7.3
  nhi_visit_seq         VARCHAR(20) NULL    -- r1.7
  nhi_result_date       DATE NULL           -- r7.6
  created_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
  updated_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP

  INDEX idx_user_type (user_id, record_type)
  INDEX idx_user_date (user_id, recorded_at)
  INDEX idx_import_log (nhi_import_log_id)
```

### Detail 子表

```sql
BloodPressureDetails
  id                 INT PK AUTO_INCREMENT
  health_record_id   INT UNIQUE FK → HealthRecords(id) ON DELETE CASCADE
  systolic           INT NOT NULL
  diastolic          INT NOT NULL
  pulse              INT NULL
  measurement_position VARCHAR(20) NULL   -- 'sitting' | 'standing' | 'lying'
  arm                VARCHAR(10) NULL     -- 'left' | 'right'

LabResultDetails
  id                 INT PK AUTO_INCREMENT
  health_record_id   INT FK → HealthRecords(id) ON DELETE CASCADE
  user_lab_item_id   INT NULL FK → UserLabItems(id) ON DELETE SET NULL
  item_code          VARCHAR(50) NOT NULL    -- r7.8
  item_name          VARCHAR(100) NOT NULL   -- r7.10
  is_numeric         BOOLEAN NOT NULL DEFAULT TRUE
  value_numeric      DECIMAL(10,4) NULL
  value_text         VARCHAR(100) NULL       -- '-', '1+', 'Pale yellow', '1:40 SP'
  unit               VARCHAR(30) NULL
  reference_range    VARCHAR(200) NULL       -- r7.12 原始格式
  nhi_order_name     VARCHAR(200) NULL       -- r7.9 申報項目全名
  nhi_raw_value      VARCHAR(100) NULL       -- r7.11 原始值

  INDEX idx_record (health_record_id)
  INDEX idx_item (item_code, item_name)

VisitDetails
  id                 INT PK AUTO_INCREMENT
  health_record_id   INT UNIQUE FK → HealthRecords(id) ON DELETE CASCADE
  visit_type         VARCHAR(50) NULL
  visit_type_code    VARCHAR(10) NULL        -- r1.1
  department         VARCHAR(100) NULL
  doctor_name        VARCHAR(100) NULL
  diagnosis_code_1   VARCHAR(20) NULL        -- r1.8
  diagnosis_name_1   VARCHAR(200) NULL       -- r1.9
  diagnosis_code_2   VARCHAR(20) NULL        -- r1.10
  diagnosis_name_2   VARCHAR(200) NULL       -- r1.11
  diagnosis_code_3   VARCHAR(20) NULL        -- r1.14（注意跳過 r1.12/13）
  diagnosis_name_3   VARCHAR(200) NULL       -- r1.15
  diagnosis_code_4   VARCHAR(20) NULL        -- r1.16
  diagnosis_name_4   VARCHAR(200) NULL       -- r1.17
  diagnosis_code_5   VARCHAR(20) NULL        -- r1.18
  diagnosis_name_5   VARCHAR(200) NULL       -- r1.19
  copayment_code     VARCHAR(10) NULL        -- r1.12
  medical_cost       DECIMAL(10,2) NULL      -- r1.13
  nhi_raw_data       JSON NULL

MedicationDetails
  id                 INT PK AUTO_INCREMENT
  health_record_id   INT FK → HealthRecords(id) ON DELETE CASCADE
  visit_detail_id    INT NULL FK → VisitDetails(id) ON DELETE SET NULL
  medication_name    VARCHAR(200) NOT NULL   -- r1_1.2
  generic_name       VARCHAR(200) NULL
  nhi_drug_code      VARCHAR(20) NULL        -- r1_1.1（10碼藥品代碼）
  quantity           DECIMAL(10,2) NULL      -- r1_1.3
  copayment          DECIMAL(10,2) NULL      -- r1_1.4
  dosage             VARCHAR(100) NULL       -- 手動輸入
  frequency          VARCHAR(100) NULL       -- 手動：'QD' | 'BID' | 'TID'
  route              VARCHAR(50) NULL        -- 手動：'PO' | 'IV'
  duration_days      INT NULL
  is_active          BOOLEAN NOT NULL DEFAULT FALSE  -- Phase 2
  start_date         DATE NULL
  end_date           DATE NULL

  INDEX idx_record (health_record_id)
```

### Phase 2 預留表

```sql
MedicationReminders
  id                   INT PK AUTO_INCREMENT
  user_id              INT FK → Users(id) ON DELETE CASCADE
  medication_detail_id INT FK → MedicationDetails(id) ON DELETE CASCADE
  remind_time          TIME NOT NULL
  days_of_week         VARCHAR(20) NOT NULL
  is_enabled           BOOLEAN NOT NULL DEFAULT TRUE
  created_at           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP

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
```

### 匯入紀錄

```sql
NhiImportLogs
  id               INT PK AUTO_INCREMENT
  user_id          INT FK → Users(id) ON DELETE CASCADE
  file_hash        VARCHAR(64) NOT NULL
  file_name        VARCHAR(255) NULL
  data_date        DATE NULL                 -- b1.2
  record_count     INT NOT NULL DEFAULT 0
  lab_count        INT NOT NULL DEFAULT 0
  visit_count      INT NOT NULL DEFAULT 0
  medication_count INT NOT NULL DEFAULT 0
  new_item_count   INT NOT NULL DEFAULT 0
  skipped_lab_count    INT NOT NULL DEFAULT 0
  duplicate_lab_count  INT NOT NULL DEFAULT 0
  date_range_start DATE NULL
  date_range_end   DATE NULL
  imported_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP

  UNIQUE INDEX uq_user_hash (user_id, file_hash)
```

---

## NHI 健保存摺 JSON 結構參考

### 頂層結構

```
myhealthbank.bdata
├── b1.1    身分證字號（部分遮蔽）
├── b1.2    資料日期（西元 YYYYMMDD）
├── r0      聲明書（忽略）
├── r1[]    西醫門診/住院紀錄 ← 匯入
│   └── r1_1[]  醫令明細（混合：藥品處方 + 檢驗醫令 + 診察費）
├── r3[]    牙醫（未來擴充）
├── r6[]    疫苗接種（未來擴充）
├── r7[]    檢驗結果 ← 匯入
├── r8[]    影像/病理檢查（未來擴充）
├── r9[]    中醫（未來擴充）
└── r10~R14 其他（通常無資料）
```

### r1 欄位對應

| 欄位 | 說明 | DB 欄位 |
|------|------|---------|
| r1.1 | 類型代碼（5=醫學中心, 4=診所） | visit_type_code |
| r1.3 | 醫事機構代碼 | nhi_institution_code |
| r1.4 | 醫事機構名稱 | nhi_institution |
| r1.5 | 就醫日期 YYYYMMDD | recorded_at |
| r1.7 | 就醫序號 | nhi_visit_seq |
| r1.8/r1.9 | 主診斷 ICD + 名稱 | diagnosis_code_1/name_1 |
| r1.10/r1.11 | 次診斷 2 | diagnosis_code_2/name_2 |
| **r1.12** | **部分負擔代碼（非診斷！）** | copayment_code |
| **r1.13** | **醫療費用點數（非診斷！）** | medical_cost |
| r1.14/r1.15 | 診斷 3 | diagnosis_code_3/name_3 |
| r1.16/r1.17 | 診斷 4 | diagnosis_code_4/name_4 |
| r1.18/r1.19 | 診斷 5 | diagnosis_code_5/name_5 |

### r1_1 欄位對應（醫令明細 — 混合內容）

| 欄位 | 說明 | 範例 |
|------|------|------|
| r1_1.1 | 醫令代碼 | '09015C'（檢驗）/ 'AB57103100'（藥品）/ '00156A'（診察費） |
| r1_1.2 | 醫令名稱 | '肌酸酐、血' / '那寶膜衣錠50毫克' |
| r1_1.3 | 數量 | '56.00' |
| r1_1.4 | 自付額 | '28' |

**分類邏輯**：
- 藥品：代碼 10 碼，首字 A/B/C/N → MedicationDetails
- 檢驗醫令：代碼 ≤6 碼，首兩碼 06~12 → 忽略（r7 有結果）
- 診察費/藥事費：首兩碼 00~05 → 忽略

### r7 欄位對應

| 欄位 | 說明 | DB 欄位 | 範例 |
|------|------|---------|------|
| r7.3 | 機構代碼 | nhi_institution_code | '0602030026' |
| r7.4 | 機構名稱 | nhi_institution | '高雄榮總' |
| r7.5 | 就醫日期 | recorded_at | '20230131' |
| r7.6 | 報告日期 | nhi_result_date | '20230324' |
| **r7.8** | **申報代碼** | **item_code** | '09015C' |
| r7.9 | 申報項目全名 | nhi_order_name | '肌酸酐、血' |
| **r7.10** | **子項目名稱** | **item_name** | 'CRE(肌酸酐)' |
| **r7.11** | **檢驗值** | value | '1.20' / '-' / 'Pale yellow' |
| **r7.12** | **參考範圍** | reference_range | '[0.7][1.3]' / '[無][]' |

### r7 重要注意事項

1. **r7 有重複**：同日同項目可出現 2~4 筆，dedup by (r7.5+r7.8+r7.10+r7.11)
2. **全形/半形混用**：'WBC＆PUS CELL' vs 'WBC&PUS CELL'，比對時正規化
3. **過濾非臨床項目**：r7.10 = 'Appearance' / '顏色' / '混濁度' / 'GENERAL URINE EXAMINATION' → 跳過
4. **r7.12 格式**：[low][high]、[無][]、[無][＜1.0]、[(0-5)][] 等多種

---

## Seed Data — 系統預設檢驗項目

item_code/item_name 必須與 NHI r7.8/r7.10 完全一致：

**腎功能**
| item_code | item_name | display_name | unit | min | max |
|-----------|-----------|--------------|------|-----|-----|
| 09015C | CRE(肌酸酐) | 肌酸酐 | mg/dL | 0.7 | 1.3 |
| 09015C | eGFR | eGFR | mL/min/1.73m² | 60 | - |
| 09002C | 血中尿素氮 | BUN | mg/dL | 7 | 25 |
| 09013C | 尿酸 | 尿酸 | mg/dL | 4.4 | 7.6 |
| 12111C | Microalbumin | 微量白蛋白 | mg/L | - | 1.9 |
| 12111C | UACR | UACR | mg/g | - | 30 |
| 09011C | CA(鈣) | 鈣 | mg/dL | 8.6 | 10.3 |
| 09012C | 磷 | 磷 | mg/dL | 2.5 | 5.0 |

**免疫**
| item_code | item_name | display_name | unit | min | max |
|-----------|-----------|--------------|------|-----|-----|
| 12034B | C3 | 補體C3 | mg/dL | 87 | 200 |
| 12038B | C4 | 補體C4 | mg/dL | 19 | 52 |
| 12060C | Anti ds-DNA Ab | 抗dsDNA抗體 | IU/mL | - | 10 |
| 12053C | ANA | 抗核抗體 | -- | - | - |
| 12025B | IgG | IgG | mg/dL | 610 | 1616 |
| 12027B | IgA | IgA | mg/dL | 84.5 | 499 |
| 12029B | IgM | IgM | mg/dL | 35 | 242 |
| 12015C | Ｃ反應性蛋白試驗－免疫比濁法 | CRP | mg/dL | - | 1.0 |

**血液**
| item_code | item_name | display_name | unit | min | max |
|-----------|-----------|--------------|------|-----|-----|
| 08011C | WBC 白血球 | WBC | 10³/μL | 4.1 | 10.5 |
| 08011C | RBC 紅血球 | RBC | 10⁶/μL | 4.3 | 6.0 |
| 08011C | Hemoglobin 血色素 | Hb | g/dL | 13.4 | 17.2 |
| 08011C | Hematocrit 血球比容值 | Hct | % | 39.8 | 50.7 |
| 08011C | Platelet 血小板 | PLT | 10³/μL | 160 | 370 |
| 08011C | MCV 平均血球容積 | MCV | fL | 83.4 | 98.5 |
| 08005C | 血球沉降率1小時 | ESR | mm/hr | 2 | 10 |

**肝功能**
| item_code | item_name | display_name | unit | min | max |
|-----------|-----------|--------------|------|-----|-----|
| 09025C | 血清麩胺酸苯醋酸轉氨基酶 | AST | U/L | 13 | 39 |
| 09026C | 血清麩胺酸丙酮酸轉氨基酶 | ALT | U/L | 0 | 40 |
| 09038C | 白蛋白 | Albumin | g/dL | 3.5 | 5.7 |
| 09040C | 總蛋白 | T-Protein | g/dL | 6.0 | 8.3 |

**血脂**
| item_code | item_name | display_name | unit | min | max |
|-----------|-----------|--------------|------|-----|-----|
| 09001C | 總膽固醇 | T-Chol | mg/dL | - | 200 |
| 09004C | 三酸甘油脂 TG | TG | mg/dL | - | 150 |
| 09043C | 高密度脂蛋白－膽固醇 | HDL-C | mg/dL | 40 | - |
| 09044C | 低密度脂蛋白 LDL | LDL-C | mg/dL | - | 130 |

**血糖**
| item_code | item_name | display_name | unit | min | max |
|-----------|-----------|--------------|------|-----|-----|
| 09005C | 飯前血糖 Glucose | 飯前血糖 | mg/dL | 70 | 100 |
| 09006C | 醣化血色素 | HbA1c | % | - | 5.7 |

**電解質**
| item_code | item_name | display_name | unit | min | max |
|-----------|-----------|--------------|------|-----|-----|
| 09021C | 鈉 | Na | mEq/L | 136 | 146 |
| 09022C | 鉀 | K | mEq/L | 3.5 | 5.1 |
| 09023C | 氯 | Cl | mEq/L | 101 | 109 |

**尿液**
| item_code | item_name | display_name | unit | min | max |
|-----------|-----------|--------------|------|-----|-----|
| 06013C | 蛋白 | 尿蛋白 | -- | - | - |
| 06012C | RBC | 尿液RBC | /HPF | 0 | 2 |
| 06012C | WBC＆PUS CELL | 尿液WBC | /HPF | 0 | 5 |
| 06013C | 比重 | 尿比重 | -- | 1.005 | 1.030 |

---

## 測試帳號

| # | email | 情境 | 血壓 | 檢驗 | 回診 | 用藥 |
|---|-------|------|------|------|------|------|
| 1 | sle@test.com | SLE腎炎 | ~360筆/6月 | 8次/2年 | 8筆 | 5-8種/次 |
| 2 | htn@test.com | 高血壓 | ~90筆/3月 | 2次/年 | 4筆 | 1-2種 |
| 3 | dm@test.com | 糖尿病 | ~60筆/2月 | 4次/年 | 4筆 | 2種 |
| 4 | healthy@test.com | 健康體檢 | 5筆 | 1次 | 0筆 | 0種 |

密碼統一 `Test1234!`。Seed 冪等：以 email 判斷已存在則跳過。僅 `IsDevelopment()` 執行。

---

## 給 Claude Code 的指示

1. **卡住三次就停**：Maximum 3 attempts per issue, then STOP and report.
2. **每次任務完成後更新 CLAUDE.md**。
3. **Migration 命名**：`YYYYMMDD_DescriptiveName`。
4. **不允許無 issue 的 TODO**。
5. **API 回應一律 `ApiResponse<T>`**。
6. **NHI 匯入**：JSON 編碼使用 `utf-8-sig`（含 BOM）。
7. **item_name 比對**：全形/半形正規化後比對。

---

## NHI 匯入流程

### 整體步驟

1. 計算 JSON 的 SHA256 hash → 查 NhiImportLogs 防重複
2. 解析 r1 → 建立 HealthRecords(visit) + VisitDetails
3. 解析 r1_1 → 分類處理：
   - **藥品**（代碼 10 碼，首字 A/B/C/N）→ HealthRecords(medication) + MedicationDetails，關聯到對應 VisitDetails
   - **檢驗醫令**（≤6碼，首兩碼 06~12）→ 忽略（r7 有實際結果）
   - **診察費/藥事費**（首兩碼 00~05）→ 忽略
4. 解析 r7 → 每筆建立 HealthRecords(lab_result) + LabResultDetails
   - 查 UserLabItems(item_code + item_name)：找到 → 填入 user_lab_item_id；找不到 → 自動新增
   - r7 去重：(r7.5 + r7.8 + r7.10 + r7.11) 相同的只取第一筆
   - 過濾：r7.10 含 'Appearance' / '顏色' / '混濁度' / 'GENERAL URINE EXAMINATION' → 跳過
   - 定量/定性判斷：`decimal.TryParse(r7.11)` 成功 → is_numeric=true
5. 記錄 NhiImportLogs

### 同日多筆 r1 合併

同一天同機構可有 2~3 筆 r1（檢驗醫令、藥品處方分開），
用 (r1.5 + r1.3 + r1.7) 組合識別同一次就醫，合併為同一個 VisitDetails。
多筆 r1 的診斷取第一筆非空的。

### 撤銷匯入

按 `nhi_import_log_id` 查詢所有關聯 HealthRecords → CASCADE 刪除 Details → 刪除 NhiImportLogs。
自動新增的 UserLabItems 不刪除。

---

## API 路由總覽

### 認證
```
POST   /auth/register
POST   /auth/login
POST   /auth/refresh
```

### 個人資料
```
GET    /profile
PUT    /profile
GET    /profile/emergency-contacts
POST   /profile/emergency-contacts
PUT    /profile/emergency-contacts/{id}
DELETE /profile/emergency-contacts/{id}
```

### 血壓紀錄
```
GET    /blood-pressure                    -- 分頁、日期範圍
GET    /blood-pressure/{id}
POST   /blood-pressure
PUT    /blood-pressure/{id}
DELETE /blood-pressure/{id}
GET    /blood-pressure/statistics
```

### 檢驗數值
```
GET    /lab-results
GET    /lab-results/{id}
POST   /lab-results
PUT    /lab-results/{id}
DELETE /lab-results/{id}
GET    /lab-results/trend?itemCode=09015C&itemName=CRE(肌酸酐)
```

### 檢驗項目維護
```
GET    /user-lab-items                    -- 依 category 分組
POST   /user-lab-items
PUT    /user-lab-items/{id}               -- is_preset=true 僅改 normal_min/max
DELETE /user-lab-items/{id}               -- is_preset=true 回傳 400
```

### 回診紀錄
```
GET    /visits
GET    /visits/{id}                       -- 含關聯用藥
POST   /visits
PUT    /visits/{id}
DELETE /visits/{id}
```

### 用藥紀錄
```
GET    /medications
GET    /medications/{id}
POST   /medications
PUT    /medications/{id}
DELETE /medications/{id}
GET    /medications/active                -- Phase 2
```

### NHI 匯入
```
POST   /nhi/import
GET    /nhi/import-logs
DELETE /nhi/import-logs/{id}              -- 撤銷匯入
```

### Phase 2（預留）
```
GET/POST/PUT/DELETE /symptoms
GET    /symptoms/summary
GET/POST/PUT/DELETE /medication-reminders
GET    /visits/{id}/summary
```

---

## 關鍵設計決策

1. **HealthRecords + Detail 子表**：統一時間軸、統一 source 追蹤，新類型只加子表。
2. **item_code(r7.8) + item_name(r7.10) 聯合唯一**：同一 09015C 下有 CRE(肌酸酐) 和 eGFR。
3. **定量/定性分離**：is_numeric + value_numeric + value_text 三欄位。
4. **r1_1 是混合醫令**：藥品 + 檢驗 + 診察費，依代碼格式分類。
5. **r7 有重複**：NHI 系統問題，匯入 dedup by (date+code+name+value)。
6. **r1.12/r1.13 不是診斷**：是部分負擔和費用，診斷 3 從 r1.14 開始。
7. **全形/半形混用**：＆/& 混用，比對時正規化。
8. **nhi_import_log_id FK**：HealthRecords 直接關聯匯入批次，撤銷時批次查詢。

---

## 重構 Prompt（給 Claude Code 使用）

從既有的 Phase 1 專案重構到 v2 架構。不是從頭建，是調整既有程式碼。

```
我已將 CLAUDE.md 更新為 v2 版本。請閱讀完整內容後進行以下重構：

## 資料庫重構

1. 刪除 Migrations/ 資料夾下所有既有 migration 檔案
2. 依照新 CLAUDE.md Schema 調整所有 Entity：
   - 新增 UserLabItems Entity
   - LabResultDetails：移除舊的 item_code/item_name/nhi_code/nhi_item_name，
     改為新的 item_code(=r7.8) + item_name(=r7.10) + nhi_order_name(=r7.9) + nhi_raw_value(=r7.11)
   - VisitDetails：新增多診斷欄位(diagnosis_code_1~5/name_1~5)、
     copayment_code(r1.12)、medical_cost(r1.13)、visit_type_code(r1.1)
   - MedicationDetails：新增 nhi_drug_code、quantity、copayment、visit_detail_id FK
   - HealthRecords：新增 nhi_import_log_id FK
   - NhiImportLogs：新增 skipped_lab_count、duplicate_lab_count
3. 更新 AppDbContext（OnModelCreating 設定 indexes、relationships）
4. 重建初始 Migration：dotnet ef migrations add 20260403_InitialCreate

## 新增功能

5. 新增 UserLabItemController（CRUD）
   - GET /user-lab-items（依 category 分組）
   - POST/PUT/DELETE（is_preset=true 禁止刪除、僅改 normal_min/max）
6. 修改 AuthService.RegisterAsync：新使用者自動初始化預設 UserLabItems
7. 新增 DbSeeder：四個測試帳號 + 對應的測試資料（血壓、檢驗、回診、用藥）

## NHI 匯入重構

8. 修改 NhiImportService：
   - JSON 編碼改為 utf-8-sig
   - r1_1 分類邏輯：藥品(10碼A/B/C/N) vs 檢驗醫令(≤6碼06~12) vs 診察費(00~05)
   - r7 去重：(date+code+name+value) 重複的只取第一筆
   - r7 過濾：Appearance/顏色/混濁度/GENERAL URINE EXAMINATION → 跳過
   - item_name 比對全形/半形正規化
   - 查 UserLabItems 取代舊的 LabItemPresets 常數比對
   - 找不到的項目自動新增到 UserLabItems
   - HealthRecords 填入 nhi_import_log_id
   - 同日多筆 r1 合併為同一 VisitDetails
9. 移除 LabItemPresets 常數檔案

## 既有功能調整

10. LabController /trend 端點：查詢參數改為 itemCode + itemName
11. 移除舊的 LabItemPresets 相關引用

## 完成後

12. 確認 dotnet build 無錯誤
13. 確認 Migration 可正常 apply（dotnet ef database update）
14. 確認 Seed 可正常執行且冪等
15. 更新 CLAUDE.md 的進度 checkbox
```
