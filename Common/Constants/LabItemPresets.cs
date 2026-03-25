namespace HealthRecord.API.Common.Constants;

public static class LabItemPresets
{
    public static readonly List<LabItemPreset> Items =
    [
        // 腎功能
        new("肌酸酐",     "Cr",         "mg/dL",           "腎功能",   0.7m,   1.3m,   "09015C", "CRE(肌酸酐)"),
        new("腎絲球過濾率","eGFR",       "mL/min/1.73m²",   "腎功能",   60m,    null,   "09015C", "eGFR"),
        new("尿素氮",     "BUN",        "mg/dL",           "腎功能",   7m,     25m,    "09002C", "血中尿素氮"),
        new("尿肌酐",     "uCr",        "mg/dL",           "腎功能",   null,   null,   "09016C", "CRE(肌酸酐)"),
        new("微白蛋白",   "Microalbumin","mg/dL",          "腎功能",   null,   1.9m,   "12111C", "Microalbumin"),
        new("UACR",      "UACR",       "mg/g",            "腎功能",   null,   30m,    "12111C", "UACR"),
        new("UPCR",      "UPCR",       "mg/g",            "腎功能",   null,   200m,   null,     null),
        // 免疫指標
        new("抗雙股DNA抗體","anti-dsDNA","IU/mL",          "免疫指標", 0m,     15m,    "12060C", "Anti ds-DNA Ab"),
        new("抗核抗體",   "ANA",        "–",               "免疫指標", null,   null,   "12053C", "ANA"),
        new("補體C3",     "C3",         "mg/dL",           "免疫指標", 87m,    200m,   "12034B", "C3"),
        new("補體C4",     "C4",         "mg/dL",           "免疫指標", 19m,    52m,    "12038B", "C4"),
        // 血球
        new("白血球",     "WBC",        "10³/μL",          "血球",     4.1m,   10.5m,  "08011C", "WBC 白血球"),
        new("紅血球",     "RBC",        "10⁶/μL",          "血球",     4.3m,   6.0m,   "08011C", "RBC 紅血球"),
        new("血色素",     "Hb",         "g/dL",            "血球",     13.4m,  17.2m,  "08011C", "Hemoglobin 血色素"),
        new("血球比容值", "Hct",        "%",               "血球",     39.8m,  50.7m,  "08011C", "Hematocrit 血球比容值"),
        new("血小板",     "PLT",        "10³/μL",          "血球",     160m,   370m,   "08011C", "Platelet 血小板"),
        new("平均血球容積","MCV",        "fL",              "血球",     83.4m,  98.5m,  "08011C", "MCV 平均血球容積"),
        new("嗜中性球",   "Neutrophil", "%",               "血球",     41.8m,  70.8m,  "08013C", "Neutrophil Seg.嗜中性"),
        new("淋巴球",     "Lymphocyte", "%",               "血球",     20.7m,  49.2m,  "08013C", "Lymphocyte 淋巴球"),
        // 發炎指標
        new("C反應蛋白",  "CRP",        "mg/L",            "發炎指標", 0m,     1.0m,   "12015C", "Ｃ反應性蛋白試驗－免疫比濁法"),
        new("紅血球沈降", "ESR",        "mm/hr",           "發炎指標", 2m,     10m,    "08005C", "血球沉降率1小時"),
        // 肝功能
        new("丙胺酸轉胺酶","ALT",       "U/L",             "肝功能",   0m,     40m,    "09026C", "血清麩胺酸丙酮酸轉氨基"),
        new("天門冬胺酸", "AST",        "U/L",             "肝功能",   13m,    39m,    "09025C", "血清麩胺酸苯醋酸轉氨基"),
        // 血脂
        new("總膽固醇",   "Cholesterol","mg/dL",           "血脂",     null,   200m,   "09001C", "總膽固醇"),
        new("三酸甘油脂", "TG",         "mg/dL",           "血脂",     null,   150m,   "09004C", "三酸甘油脂 TG"),
        new("高密度脂蛋白","HDL",       "mg/dL",           "血脂",     40m,    null,   "09043C", "高密度脂蛋白－膽固醇"),
        new("低密度脂蛋白","LDL",       "mg/dL",           "血脂",     null,   130m,   "09044C", "低密度脂蛋白 LDL"),
        // 血糖
        new("空腹血糖",   "Glucose",    "mg/dL",           "血糖",     70m,    100m,   "09005C", "飯前血糖 Glucose"),
        new("醣化血色素", "HbA1c",      "%",               "血糖",     null,   5.7m,   "09006C", "醣化血色素"),
        // 電解質
        new("鈉",         "Na",         "mEq/L",           "電解質",   136m,   146m,   "09021C", "鈉"),
        new("鉀",         "K",          "mEq/L",           "電解質",   3.5m,   5.1m,   "09022C", "鉀"),
        new("氯",         "Cl",         "mEq/L",           "電解質",   101m,   109m,   "09023C", "氯"),
        new("鈣",         "Ca",         "mg/dL",           "電解質",   8.6m,   10.3m,  "09011C", "CA(鈣)"),
        // 其他
        new("尿酸",       "Uric_acid",  "mg/dL",           "其他",     4.4m,   7.6m,   "09013C", "尿酸"),
        new("白蛋白",     "Albumin",    "g/dL",            "其他",     3.5m,   5.7m,   "09038C", "白蛋白"),
        new("肌酸磷化酶", "CK",         "U/L",             "其他",     30m,    223m,   "09032C", "肌酸磷化?"),
    ];

    public static LabItemPreset? FindByNhiCode(string? nhiCode, string? nhiItemName)
    {
        if (nhiCode is null) return null;
        return Items.FirstOrDefault(i =>
            i.NhiCode == nhiCode &&
            (nhiItemName is null || i.NhiItemName == nhiItemName));
    }
}

public record LabItemPreset(
    string ItemName,
    string ItemCode,
    string Unit,
    string Category,
    decimal? NormalMin,
    decimal? NormalMax,
    string? NhiCode,
    string? NhiItemName);
