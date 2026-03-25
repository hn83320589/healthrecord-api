# HealthRecord API

個人身體紀錄後端 API。以狼瘡腎炎等慢性病日常管理為核心設計場景。

## Tech Stack

- .NET 10 Web API
- Entity Framework Core 10 + `MySql.EntityFrameworkCore`
- MySQL 8.x
- JWT Authentication
- FluentValidation / Serilog

## 快速啟動

### 使用 Docker Compose

```bash
docker compose up -d
```

API 會在 http://localhost:8080 啟動，Swagger UI 在開發模式下可訪問。

### 本地開發

1. 啟動 MySQL（或使用 Docker）：
```bash
docker run -d -p 3306:3306 -e MYSQL_ROOT_PASSWORD=password -e MYSQL_DATABASE=healthrecord mysql:8.0
```

2. 設定環境變數或修改 `appsettings.Development.json`

3. 啟動 API：
```bash
cd HealthRecord.API
dotnet run
```

Migration 會在啟動時自動套用。

## API 端點

### Auth
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`

### Profile
- `GET/PUT /api/v1/profile`
- `GET/POST /api/v1/profile/emergency-contacts`
- `PUT/DELETE /api/v1/profile/emergency-contacts/{id}`

### 血壓
- `GET/POST /api/v1/blood-pressure`
- `GET/PUT/DELETE /api/v1/blood-pressure/{id}`
- `GET /api/v1/blood-pressure/stats`
- `GET /api/v1/blood-pressure/chart-data?period=7d|30d|all`

### 檢驗
- `GET/POST /api/v1/lab-results`
- `GET/PUT/DELETE /api/v1/lab-results/{id}`
- `GET /api/v1/lab-results/by-date`
- `GET /api/v1/lab-results/trend?itemCode=Cr`

### 看診（Phase 1：NHI 匯入 + 查詢）
- `GET /api/v1/visits`
- `GET /api/v1/visits/{id}`
- `DELETE /api/v1/visits/{id}`

### 用藥（Phase 1：NHI 匯入 + 查詢）
- `GET /api/v1/medications`
- `GET /api/v1/medications/current`
- `DELETE /api/v1/medications/{id}`

### NHI 匯入
- `POST /api/v1/nhi/import`（上傳健保存摺 JSON）
- `GET /api/v1/nhi/import/logs`
- `DELETE /api/v1/nhi/import/{logId}`（撤銷）

## 設定

所有設定透過環境變數覆蓋 `appsettings.json`：

| 變數 | 說明 |
|------|------|
| `ConnectionStrings__DefaultConnection` | MySQL 連線字串 |
| `Jwt__Secret` | JWT 簽名密鑰（至少 32 字元） |
| `Jwt__Issuer` | JWT Issuer |
| `Jwt__Audience` | JWT Audience |
| `Jwt__ExpiryMinutes` | Access token 有效時間（分鐘） |

## Zeabur 部署

1. 將 repo push 到 GitHub
2. 在 Zeabur 新增 MySQL 服務
3. 新增 .NET 服務，指向此 repo
4. 設定環境變數（ConnectionStrings 等）
5. Migration 會在啟動時自動套用
