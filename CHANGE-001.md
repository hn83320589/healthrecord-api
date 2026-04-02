# CHANGE-001：檢驗項目維護檔（UserLabItems）

## 背景

Phase 1 的檢驗項目定義寫死在 `LabItemPresets` 常數中，帶來兩個問題：
1. NHI 匯入遇到不認識的項目會 skipped，資料遺漏
2. 使用者無法自訂項目，難以維護與擴充

## 變更內容

### 1. 新增 UserLabItems 表

```sql
UserLabItems
  id            INT PK AUTO_INCREMENT
  user_id       INT NOT NULL FK → Users(id) ON DELETE CASCADE
  item_code     VARCHAR(50) NOT NULL     -- 統一識別碼（對應 NHI 的 r7.8，如 '09015C'）
  item_name     VARCHAR(100) NOT NULL    -- 項目名稱（對應 NHI 的 r7.10，如 'CRE(肌酸酐)'）
  unit          VARCHAR(30) NOT NULL DEFAULT ''
  category      VARCHAR(50) NOT NULL DEFAULT '其他'
  normal_min    DECIMAL(10,4) NULL
  normal_max    DECIMAL(10,4) NULL
  is_preset     BOOLEAN NOT NULL DEFAULT FALSE   -- 來自系統預設（LabItemPresets 常數）
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
  updated_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP

  UNIQUE INDEX uq_user_item (user_id, item_code, item_name)
```

### 2. LabResultDetails 欄位命名統一

Phase 1 的 `LabResultDetails` 有四個欄位命名不一致：
- `item_code`（'Cr'，人類易讀碼）
- `item_name`（'肌酸酐'，中文名稱）
- `nhi_code`（'09015C'，NHI 申報代碼）
- `nhi_item_name`（'CRE(肌酸酐)'，NHI 子項目名稱）

**統一為兩個欄位**（Migration 更新）：

| 舊欄位 | 新欄位 | 說明 |
|--------|--------|------|
| `nhi_code` | `item_code` | 統一識別碼，與 UserLabItems.item_code 對應 |
| `nhi_item_name` | `item_name` | 項目名稱，與 UserLabItems.item_name 對應 |
| `item_code`（舊的 'Cr'） | 移除 | 不再需要，從 UserLabItems 查詢即可 |
| `item_name`（舊的 '肌酸酐'） | 移除 | 不再需要，從 UserLabItems 查詢即可 |
| `nhi_raw_value` | 保留 | 原始值，debug 用 |
| `nhi_raw_range` | 保留 | 原始範圍，debug 用 |

### 3. 新增 Migration

```bash
dotnet ef migrations add AddUserLabItems
```

Migration 內容：
- 新增 `UserLabItems` 表
- `LabResultDetails`：移除 `item_code`（舊）、`item_name`（舊），新增 `item_code`（原 nhi_code）、`item_name`（原 nhi_item_name）

### 4. 使用者初始化

新使用者**註冊時**，自動將 `LabItemPresets` 常數的 37 項寫入 `UserLabItems`（`is_preset = true`）：

```csharp
// AuthService.RegisterAsync 完成後執行
var presets = LabItemPresets.Items.Select(p => new UserLabItem
{
    UserId    = newUser.Id,
    ItemCode  = p.NhiCode ?? p.ItemCode,
    ItemName  = p.NhiItemName ?? p.ItemName,
    Unit      = p.Unit,
    Category  = p.Category,
    NormalMin = p.NormalMin,
    NormalMax = p.NormalMax,
    IsPreset  = true,
});
context.UserLabItems.AddRange(presets);
await context.SaveChangesAsync();
```

### 5. NHI 匯入邏輯變更

原本：比對 `LabItemPresets` 常數，找不到 → skipped
現在：比對 `UserLabItems`，找不到 → **自動新增**後繼續匯入

```csharp
// NhiImportService 中的比對邏輯
var userItem = await context.UserLabItems
    .FirstOrDefaultAsync(i =>
        i.UserId    == userId &&
        i.ItemCode  == item.NhiCode &&
        i.ItemName  == item.NhiItemName);

if (userItem == null)
{
    // 自動建立新項目，不再 skipped
    userItem = new UserLabItem
    {
        UserId   = userId,
        ItemCode = item.NhiCode,
        ItemName = item.NhiItemName,
        Unit     = "",          // 待使用者補充
        Category = "其他",
        IsPreset = false,
    };
    context.UserLabItems.Add(userItem);
    await context.SaveChangesAsync();
    // newItemCount++ 記錄自動新增的數量
}

// 接續寫入 LabResultDetail，item_code / item_name 從 userItem 取
context.LabResultDetails.Add(new LabResultDetail
{
    ...
    ItemCode = userItem.ItemCode,
    ItemName = userItem.ItemName,
    Unit     = userItem.Unit,
    Category = userItem.Category,
    NormalMin = userItem.NormalMin,
    NormalMax = userItem.NormalMax,
    ...
});
```

`NhiImportResultResponse` 新增 `NewItemCount` 欄位，回傳本次自動新增的項目數。

### 6. 新增 API 端點

```
GET    /user-lab-items              -- 取得使用者所有檢驗項目（預設 + 自訂）
POST   /user-lab-items              -- 手動新增自訂項目
PUT    /user-lab-items/{id}         -- 更新（調整 unit、normal_min/max、category 等）
DELETE /user-lab-items/{id}         -- 刪除（建議 is_preset=true 不可刪）
```

### 7. LabResultDetails 趨勢查詢變更

原本用 `item_code = 'Cr'` 查詢，現在改用 `item_code = '09015C'` + `item_name = 'CRE(肌酸酐)'`：

```csharp
// GET /lab-results/trend?itemCode=09015C&itemName=CRE(肌酸酐)
var points = await context.LabResultDetails
    .Where(l => l.UserId   == userId
             && l.ItemCode == itemCode
             && l.ItemName == itemName
             && l.IsNumeric
             && l.ValueNumeric.HasValue)
    .OrderBy(l => l.RecordedAt)
    .ToListAsync();
```

前端改為從 `GET /user-lab-items` 選擇項目後帶入。

---

## 影響範圍

| 模組 | 影響 |
|------|------|
| AuthService | 新增：註冊時初始化 UserLabItems |
| NhiImportService | 修改：比對 UserLabItems，找不到自動新增 |
| LabController | 修改：趨勢查詢參數改為 itemCode + itemName |
| UserLabItemController | **新增** |
| LabResultDetails | Migration：欄位重新命名 |

## 開發 Prompt

```
請依照 CHANGES/CHANGE-001.md 進行以下變更：

1. 建立 UserLabItem Entity 與 AppDbContext 設定
2. 建立 Migration：AddUserLabItems
3. 修改 AuthService.RegisterAsync：新使用者自動初始化 37 個預設項目
4. 修改 NhiImportService：改為查 UserLabItems，找不到自動新增
5. 新增 UserLabItemController（CRUD）
6. 修改 LabController /trend：查詢參數改為 itemCode + itemName
7. NhiImportResultResponse 新增 NewItemCount 欄位

注意：
- LabResultDetails 的 item_code / item_name 欄位對應 nhi_code / nhi_item_name
- 舊的 LabItemPresets 比對邏輯完整移除，改為查 DB
- is_preset=true 的項目，DELETE 時回傳 400（不可刪除系統預設）
```
