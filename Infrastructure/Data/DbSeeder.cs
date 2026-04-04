using HealthRecord.API.Common.Constants;
using HealthRecord.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using HealthRecordEntity = HealthRecord.API.Models.Entities.HealthRecord;

namespace HealthRecord.API.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedUser(db, "sle@test.com", "陳小明（SLE）", "SLE、狼瘡腎炎", new DateOnly(1988, 5, 12), SeedSle);
        await SeedUser(db, "htn@test.com", "林高壓（HTN）", "高血壓", new DateOnly(1965, 11, 20), SeedHtn);
        await SeedUser(db, "dm@test.com", "王糖友（DM）", "第二型糖尿病", new DateOnly(1970, 3, 8), SeedDm);
        await SeedUser(db, "healthy@test.com", "張健康", null, new DateOnly(1995, 8, 25), SeedHealthy);
    }

    private static async Task SeedUser(AppDbContext db, string email, string name, string? chronic,
        DateOnly? birth, Func<AppDbContext, int, Task> seedData)
    {
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existing != null) return; // idempotent

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test1234!"),
            DisplayName = name,
            BirthDate = birth,
            ChronicConditions = chronic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.UserLabItems.AddRange(LabItemDefaults.CreatePresetsForUser(user.Id));
        await db.SaveChangesAsync();

        await seedData(db, user.Id);
    }

    // ── Batch helper: create HealthRecords first, then add details referencing them ──

    private static async Task AddBpBatch(AppDbContext db, int userId, List<(DateTime at, int sys, int dia, int pulse)> items)
    {
        // Insert all HealthRecords in one SaveChanges to get IDs
        var records = items.Select(i => new HealthRecordEntity
        {
            UserId = userId, RecordType = "blood_pressure", RecordedAt = i.at,
            Source = "manual", CreatedAt = i.at, UpdatedAt = i.at
        }).ToList();
        db.HealthRecords.AddRange(records);
        await db.SaveChangesAsync();

        // Now add details with the generated IDs
        for (int i = 0; i < records.Count; i++)
        {
            db.BloodPressureDetails.Add(new BloodPressureDetail
            {
                HealthRecordId = records[i].Id,
                Systolic = items[i].sys,
                Diastolic = items[i].dia,
                Pulse = items[i].pulse
            });
        }
        await db.SaveChangesAsync();
    }

    // ─── 1. SLE 腎炎 ────────────────────────────────────────────────

    private static async Task SeedSle(AppDbContext db, int userId)
    {
        var rng = new Random(42);
        var now = DateTime.UtcNow;

        // BP: 2/day for 6 months (~360)
        var bpItems = new List<(DateTime, int, int, int)>();
        for (int d = 180; d >= 0; d--)
        {
            var day = now.AddDays(-d);
            var baseSys = 135 - (180 - d) / 20;
            for (int t = 0; t < 2; t++)
            {
                var at = day.Date.AddHours(t == 0 ? 7 : 20).AddMinutes(rng.Next(0, 30));
                bpItems.Add((at, baseSys + rng.Next(-8, 12), 82 + rng.Next(-5, 10), 68 + rng.Next(-5, 10)));
            }
        }
        await AddBpBatch(db, userId, bpItems);

        // Labs: 8 dates, each with 5 items
        var labDays = new[] { -720, -540, -360, -270, -180, -120, -60, -14 };
        var crVals = new[] { 1.8m, 1.6m, 1.5m, 1.42m, 1.38m, 1.32m, 1.28m, 1.25m };
        var egfrVals = new[] { 42m, 48m, 50m, 53m, 55m, 58m, 60m, 62m };

        for (int i = 0; i < labDays.Length; i++)
        {
            var at = now.AddDays(labDays[i]).Date.AddHours(9);
            var rec = new HealthRecordEntity
            {
                UserId = userId, RecordType = "lab_result", RecordedAt = at,
                Source = "manual", NhiInstitution = "高雄榮總", NhiInstitutionCode = "0602030026",
                CreatedAt = at, UpdatedAt = at
            };
            db.HealthRecords.Add(rec);
            await db.SaveChangesAsync();

            db.LabResultDetails.AddRange([
                new() { HealthRecordId = rec.Id, ItemCode = "09015C", ItemName = "CRE(肌酸酐)", IsNumeric = true, ValueNumeric = crVals[i], Unit = "mg/dL" },
                new() { HealthRecordId = rec.Id, ItemCode = "09015C", ItemName = "eGFR", IsNumeric = true, ValueNumeric = egfrVals[i], Unit = "mL/min/1.73m²" },
                new() { HealthRecordId = rec.Id, ItemCode = "12034B", ItemName = "C3", IsNumeric = true, ValueNumeric = 65m + i * 4, Unit = "mg/dL" },
                new() { HealthRecordId = rec.Id, ItemCode = "12038B", ItemName = "C4", IsNumeric = true, ValueNumeric = 10m + i * 2, Unit = "mg/dL" },
                new() { HealthRecordId = rec.Id, ItemCode = "08011C", ItemName = "Hemoglobin 血色素", IsNumeric = true, ValueNumeric = 10.5m + i * 0.2m, Unit = "g/dL" },
            ]);
        }
        await db.SaveChangesAsync();

        // Visits + Medications: 8 visits
        for (int i = 0; i < labDays.Length; i++)
        {
            var at = now.AddDays(labDays[i]).Date.AddHours(10);
            var vRec = new HealthRecordEntity
            {
                UserId = userId, RecordType = "visit", RecordedAt = at,
                Source = "manual", NhiInstitution = "高雄榮總", NhiInstitutionCode = "0602030026",
                CreatedAt = at, UpdatedAt = at
            };
            db.HealthRecords.Add(vRec);
            await db.SaveChangesAsync();

            var vd = new VisitDetail
            {
                HealthRecordId = vRec.Id, DiagnosisCode1 = "M32", DiagnosisName1 = "系統性紅斑狼瘡",
                DiagnosisCode2 = "N03.2", DiagnosisName2 = "慢性腎絲球腎炎"
            };
            db.VisitDetails.Add(vd);
            await db.SaveChangesAsync();

            var meds = new[] { "羥氯奎寧錠200mg", "黴芬諾酸酯500mg", "樂納腎錠50mg", "普賴鬆5mg", "鈣片" };
            var medRecs = meds.Take(3 + i % 3).Select(m => new HealthRecordEntity
            {
                UserId = userId, RecordType = "medication", RecordedAt = at,
                Source = "manual", CreatedAt = at, UpdatedAt = at
            }).ToList();
            db.HealthRecords.AddRange(medRecs);
            await db.SaveChangesAsync();

            for (int j = 0; j < medRecs.Count; j++)
            {
                db.MedicationDetails.Add(new MedicationDetail
                {
                    HealthRecordId = medRecs[j].Id, VisitDetailId = vd.Id,
                    MedicationName = meds[j], Quantity = 28, DurationDays = 28
                });
            }
        }
        await db.SaveChangesAsync();
    }

    // ─── 2. 高血壓 ──────────────────────────────────────────────────

    private static async Task SeedHtn(AppDbContext db, int userId)
    {
        var rng = new Random(43);
        var now = DateTime.UtcNow;

        var bpItems = Enumerable.Range(0, 91).Select(d =>
        {
            var at = now.AddDays(-90 + d).Date.AddHours(7).AddMinutes(rng.Next(0, 30));
            return (at, 145 + rng.Next(-10, 15), 92 + rng.Next(-5, 8), 75 + rng.Next(-5, 5));
        }).ToList();
        await AddBpBatch(db, userId, bpItems);

        foreach (var daysAgo in new[] { -365, -270, -180, -30 })
        {
            var at = now.AddDays(daysAgo).Date.AddHours(10);
            var vRec = new HealthRecordEntity
            {
                UserId = userId, RecordType = "visit", RecordedAt = at,
                Source = "manual", NhiInstitution = "台北長庚", CreatedAt = at, UpdatedAt = at
            };
            db.HealthRecords.Add(vRec);
            await db.SaveChangesAsync();

            var vd = new VisitDetail { HealthRecordId = vRec.Id, DiagnosisCode1 = "I10", DiagnosisName1 = "原發性高血壓" };
            db.VisitDetails.Add(vd);
            await db.SaveChangesAsync();

            var medNames = daysAgo < -200 ? new[] { "脈優錠5mg" } : new[] { "脈優錠5mg", "得安穩10mg" };
            var mRecs = medNames.Select(m => new HealthRecordEntity
            {
                UserId = userId, RecordType = "medication", RecordedAt = at,
                Source = "manual", CreatedAt = at, UpdatedAt = at
            }).ToList();
            db.HealthRecords.AddRange(mRecs);
            await db.SaveChangesAsync();

            for (int j = 0; j < mRecs.Count; j++)
                db.MedicationDetails.Add(new MedicationDetail
                {
                    HealthRecordId = mRecs[j].Id, VisitDetailId = vd.Id,
                    MedicationName = medNames[j], DurationDays = 28
                });
        }
        await db.SaveChangesAsync();
    }

    // ─── 3. 糖尿病 ──────────────────────────────────────────────────

    private static async Task SeedDm(AppDbContext db, int userId)
    {
        var rng = new Random(44);
        var now = DateTime.UtcNow;

        var bpItems = Enumerable.Range(0, 61).Select(d =>
        {
            var at = now.AddDays(-60 + d).Date.AddHours(7).AddMinutes(rng.Next(0, 30));
            return (at, 128 + rng.Next(-8, 8), 78 + rng.Next(-5, 5), 72 + rng.Next(-5, 5));
        }).ToList();
        await AddBpBatch(db, userId, bpItems);

        var hba1cVals = new[] { 8.2m, 7.5m, 7.1m, 6.8m };
        var days = new[] { -270, -180, -90, -7 };

        for (int i = 0; i < days.Length; i++)
        {
            var at = now.AddDays(days[i]).Date.AddHours(8);

            // Labs
            var labRec = new HealthRecordEntity
            {
                UserId = userId, RecordType = "lab_result", RecordedAt = at,
                Source = "manual", CreatedAt = at, UpdatedAt = at
            };
            db.HealthRecords.Add(labRec);
            await db.SaveChangesAsync();
            db.LabResultDetails.AddRange([
                new() { HealthRecordId = labRec.Id, ItemCode = "09006C", ItemName = "醣化血色素", IsNumeric = true, ValueNumeric = hba1cVals[i], Unit = "%" },
                new() { HealthRecordId = labRec.Id, ItemCode = "09005C", ItemName = "飯前血糖 Glucose", IsNumeric = true, ValueNumeric = 130m - i * 10, Unit = "mg/dL" },
            ]);

            // Visit
            var vRec = new HealthRecordEntity
            {
                UserId = userId, RecordType = "visit", RecordedAt = at,
                Source = "manual", NhiInstitution = "成大醫院", CreatedAt = at, UpdatedAt = at
            };
            db.HealthRecords.Add(vRec);
            await db.SaveChangesAsync();
            var vd = new VisitDetail { HealthRecordId = vRec.Id, DiagnosisCode1 = "E11", DiagnosisName1 = "第二型糖尿病" };
            db.VisitDetails.Add(vd);
            await db.SaveChangesAsync();

            // Meds
            var mRecs = new[] { "Metformin 500mg", "Glimepiride 2mg" }.Select(m => new HealthRecordEntity
            {
                UserId = userId, RecordType = "medication", RecordedAt = at,
                Source = "manual", CreatedAt = at, UpdatedAt = at
            }).ToList();
            db.HealthRecords.AddRange(mRecs);
            await db.SaveChangesAsync();
            for (int j = 0; j < mRecs.Count; j++)
                db.MedicationDetails.Add(new MedicationDetail
                {
                    HealthRecordId = mRecs[j].Id, VisitDetailId = vd.Id,
                    MedicationName = j == 0 ? "Metformin 500mg" : "Glimepiride 2mg", DurationDays = 28
                });
        }
        await db.SaveChangesAsync();
    }

    // ─── 4. 健康體檢 ────────────────────────────────────────────────

    private static async Task SeedHealthy(AppDbContext db, int userId)
    {
        var rng = new Random(45);
        var now = DateTime.UtcNow;

        var bpItems = Enumerable.Range(0, 5).Select(i =>
        {
            var at = now.AddDays(-i * 7).Date.AddHours(8);
            return (at, 118 + rng.Next(-5, 5), 74 + rng.Next(-4, 4), 68 + rng.Next(-3, 3));
        }).ToList();
        await AddBpBatch(db, userId, bpItems);

        var labAt = now.AddDays(-30).Date.AddHours(8);
        var labRec = new HealthRecordEntity
        {
            UserId = userId, RecordType = "lab_result", RecordedAt = labAt,
            Source = "manual", NhiInstitution = "家庭診所", CreatedAt = labAt, UpdatedAt = labAt
        };
        db.HealthRecords.Add(labRec);
        await db.SaveChangesAsync();
        db.LabResultDetails.AddRange([
            new() { HealthRecordId = labRec.Id, ItemCode = "09015C", ItemName = "CRE(肌酸酐)", IsNumeric = true, ValueNumeric = 0.9m, Unit = "mg/dL" },
            new() { HealthRecordId = labRec.Id, ItemCode = "09015C", ItemName = "eGFR", IsNumeric = true, ValueNumeric = 105m, Unit = "mL/min/1.73m²" },
            new() { HealthRecordId = labRec.Id, ItemCode = "09005C", ItemName = "飯前血糖 Glucose", IsNumeric = true, ValueNumeric = 88m, Unit = "mg/dL" },
            new() { HealthRecordId = labRec.Id, ItemCode = "09001C", ItemName = "總膽固醇", IsNumeric = true, ValueNumeric = 185m, Unit = "mg/dL" },
        ]);
        await db.SaveChangesAsync();
    }
}
