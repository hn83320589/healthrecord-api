# HealthRecord.API — Claude Code Prompt

## 🚀 Phase 1 初始化

```
請依照 CLAUDE.md 建立 HealthRecord.API 專案（.NET 9 Web API）。

【專案建立】
1. dotnet new webapi -n HealthRecord.API
2. 安裝套件：
   Pomelo.EntityFrameworkCore.MySql
   Microsoft.EntityFrameworkCore.Design
   Microsoft.AspNetCore.Authentication.JwtBearer
   FluentValidation.AspNetCore
   AutoMapper.Extensions.Microsoft.DependencyInjection
   BCrypt.Net-Next
   Serilog.AspNetCore

【資料層】
3. 依照 CLAUDE.md Schema 與 .claude/skills/ef-migration.md 建立所有 Entity：
   - User、EmergencyContact
   - HealthRecord（看診主表，含診斷欄位）
   - BloodPressureDetail（user_id + health_record_id nullable + nhi_import_log_id nullable）
   - LabResultDetail（user_id + health_record_id nullable + nhi_import_log_id nullable，自帶項目定義欄位）
   - MedicationDetail（user_id + health_record_id nullable + nhi_import_log_id nullable）
   - NhiImportLog
4. 建立 AppDbContext（參照 .claude/skills/ef-migration.md）：
   - health_record_id / nhi_import_log_id 全部設 ON DELETE SET NULL
   - LabResultDetails 加 (user_id, item_code) 複合索引
5. 建立 InitialCreate Migration 並套用

【共用基礎】
6. ApiResponse<T>、PagedResult<T>（參照 .claude/skills/api-response-pattern.md）
7. BaseController（從 Claims 取 UserId）
8. JWT 設定（appsettings.json 的 Jwt 區段）
9. ExceptionMiddleware（全域錯誤 → ApiResponse.Fail）
10. FluentValidation 全域攔截
11. Serilog 設定

【常數】
12. 建立 Common/Constants/LabItemPresets.cs（參照 CLAUDE.md 的 37 項 Seed 清單）
    不需要 DB Seed，這是純 C# 常數清單

【Auth 模組】
13. Register / Login / RefreshToken（BCrypt 密碼雜湊）

【Profile 模組】
14. GET/PUT /profile（含身體數據）
15. CRUD /profile/emergency-contacts

【血壓模組】
16. 參照 .claude/skills/api-response-pattern.md：
    - GET/POST/GET{id}/PUT{id}/DELETE{id}（PUT/DELETE 僅 source='manual'）
    - GET /stats（平均/最高最低/BP 分類分佈）
    - GET /chart-data（?period=7d|30d|all）

【檢驗模組】
17. 參照 .claude/skills/lab-results.md：
    - POST /lab-results（多筆，每筆含完整項目定義，可帶 health_record_id）
    - GET/PUT{id}/DELETE{id}（PUT/DELETE 僅 source='manual'）
    - GET /by-date（依日期分組）
    - GET /trend（?itemCode=Cr）

【看診模組 — Phase 1：GET + DELETE】
18. GET /health-records（列表，分頁 + 日期篩選）
19. GET /health-records/{id}（詳情 + 同 health_record_id 的 medications + lab_results）
20. DELETE /health-records/{id}（僅 source='nhi_import'）

【用藥模組 — Phase 1：GET + DELETE】
21. GET /medications（列表，可篩 drug_type='medication'）
22. GET /medications/current（最近一次看診的藥品清單）
23. DELETE /medications/{id}（僅 source='nhi_import'）

【NHI 匯入模組】
24. 建立 Common/Helpers/NhiJsonParser.cs
25. 建立 Services/NhiImportService.cs（參照 .claude/skills/nhi-import.md）：
    - 計算 date_range_start / date_range_end
    - 先刪日期範圍內四張表的 source='nhi_import' 舊資料
    - 建立 NhiImportLog（先建，取得 id）
    - r1 → HealthRecords（含診斷）
    - r1_1 → MedicationDetails（health_record_id 關聯）
    - r7 → LabResultDetails（比對 LabItemPresets 常數，填入項目定義）
    - 整批 Transaction，失敗完整 rollback
26. POST /nhi/import（multipart/form-data）
27. GET /nhi/import/logs
28. DELETE /nhi/import/{logId}（各表 WHERE nhi_import_log_id = logId 直接刪）

【收尾】
29. Program.cs 設定自動套用 Migration（Zeabur 部署用）
30. 建立 Dockerfile（multi-stage）+ docker-compose.yml（API + MySQL 8）
31. 建立 README.md
```

---

## 🔧 後續 Prompt

### Swagger + JWT 測試
```
安裝 Swashbuckle.AspNetCore，設定 Bearer Token 輸入。
NhiController 加 [Consumes("multipart/form-data")]。
```

### Phase 2
```
新增看診 POST/PUT（手動 CRUD），開放 DELETE source='manual'。
新增用藥 POST/PUT，MedicationController 加服藥提醒端點。
新增症狀日誌（SymptomLogs 表 + CRUD）。
建立新 Migration：AddPhase2Features。
```
