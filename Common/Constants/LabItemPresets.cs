using HealthRecord.API.Models.Entities;

namespace HealthRecord.API.Common.Constants;

/// <summary>
/// v2: 預設檢驗項目定義。item_code(=r7.8) + item_name(=r7.10) 與 NHI 完全一致。
/// 註冊時自動初始化到 UserLabItems，is_preset=true。
/// </summary>
public static class LabItemDefaults
{
    public static List<UserLabItem> CreatePresetsForUser(int userId)
    {
        var now = DateTime.UtcNow;
        var order = 0;
        return Items.Select(i => new UserLabItem
        {
            UserId = userId,
            ItemCode = i.ItemCode,
            ItemName = i.ItemName,
            DisplayName = i.DisplayName,
            Unit = i.Unit,
            Category = i.Category,
            NormalMin = i.NormalMin,
            NormalMax = i.NormalMax,
            SortOrder = order++,
            IsPreset = true,
            CreatedAt = now,
            UpdatedAt = now,
        }).ToList();
    }

    private static readonly List<PresetItem> Items =
    [
        // 腎功能
        new("09015C", "CRE(肌酸酐)", "肌酸酐", "mg/dL", "腎功能", 0.7m, 1.3m),
        new("09015C", "eGFR", "eGFR", "mL/min/1.73m²", "腎功能", 60m, null),
        new("09002C", "血中尿素氮", "BUN", "mg/dL", "腎功能", 7m, 25m),
        new("09013C", "尿酸", "尿酸", "mg/dL", "腎功能", 4.4m, 7.6m),
        new("12111C", "Microalbumin", "微量白蛋白", "mg/L", "腎功能", null, 1.9m),
        new("12111C", "UACR", "UACR", "mg/g", "腎功能", null, 30m),
        new("09011C", "CA(鈣)", "鈣", "mg/dL", "腎功能", 8.6m, 10.3m),
        new("09012C", "磷", "磷", "mg/dL", "腎功能", 2.5m, 5.0m),
        // 免疫
        new("12034B", "C3", "補體C3", "mg/dL", "免疫", 87m, 200m),
        new("12038B", "C4", "補體C4", "mg/dL", "免疫", 19m, 52m),
        new("12060C", "Anti ds-DNA Ab", "抗dsDNA抗體", "IU/mL", "免疫", null, 10m),
        new("12053C", "ANA", "抗核抗體", "–", "免疫", null, null),
        new("12025B", "IgG", "IgG", "mg/dL", "免疫", 610m, 1616m),
        new("12027B", "IgA", "IgA", "mg/dL", "免疫", 84.5m, 499m),
        new("12029B", "IgM", "IgM", "mg/dL", "免疫", 35m, 242m),
        new("12015C", "Ｃ反應性蛋白試驗－免疫比濁法", "CRP", "mg/dL", "免疫", null, 1.0m),
        // 血液
        new("08011C", "WBC 白血球", "WBC", "10³/μL", "血液", 4.1m, 10.5m),
        new("08011C", "RBC 紅血球", "RBC", "10⁶/μL", "血液", 4.3m, 6.0m),
        new("08011C", "Hemoglobin 血色素", "Hb", "g/dL", "血液", 13.4m, 17.2m),
        new("08011C", "Hematocrit 血球比容值", "Hct", "%", "血液", 39.8m, 50.7m),
        new("08011C", "Platelet 血小板", "PLT", "10³/μL", "血液", 160m, 370m),
        new("08011C", "MCV 平均血球容積", "MCV", "fL", "血液", 83.4m, 98.5m),
        new("08005C", "血球沉降率1小時", "ESR", "mm/hr", "血液", 2m, 10m),
        // 肝功能
        new("09025C", "血清麩胺酸苯醋酸轉氨基酶", "AST", "U/L", "肝功能", 13m, 39m),
        new("09026C", "血清麩胺酸丙酮酸轉氨基酶", "ALT", "U/L", "肝功能", 0m, 40m),
        new("09038C", "白蛋白", "Albumin", "g/dL", "肝功能", 3.5m, 5.7m),
        new("09040C", "總蛋白", "T-Protein", "g/dL", "肝功能", 6.0m, 8.3m),
        // 血脂
        new("09001C", "總膽固醇", "T-Chol", "mg/dL", "血脂", null, 200m),
        new("09004C", "三酸甘油脂 TG", "TG", "mg/dL", "血脂", null, 150m),
        new("09043C", "高密度脂蛋白－膽固醇", "HDL-C", "mg/dL", "血脂", 40m, null),
        new("09044C", "低密度脂蛋白 LDL", "LDL-C", "mg/dL", "血脂", null, 130m),
        // 血糖
        new("09005C", "飯前血糖 Glucose", "飯前血糖", "mg/dL", "血糖", 70m, 100m),
        new("09006C", "醣化血色素", "HbA1c", "%", "血糖", null, 5.7m),
        // 電解質
        new("09021C", "鈉", "Na", "mEq/L", "電解質", 136m, 146m),
        new("09022C", "鉀", "K", "mEq/L", "電解質", 3.5m, 5.1m),
        new("09023C", "氯", "Cl", "mEq/L", "電解質", 101m, 109m),
        // 尿液
        new("06013C", "蛋白", "尿蛋白", "–", "尿液", null, null),
        new("06012C", "RBC", "尿液RBC", "/HPF", "尿液", 0m, 2m),
        new("06012C", "WBC＆PUS CELL", "尿液WBC", "/HPF", "尿液", 0m, 5m),
        new("06013C", "比重", "尿比重", "–", "尿液", 1.005m, 1.030m),
    ];

    private record PresetItem(
        string ItemCode, string ItemName, string DisplayName,
        string Unit, string Category, decimal? NormalMin, decimal? NormalMax);
}
