using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.Visit;
using HealthRecord.API.Models.Entities;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthRecord.API.Services;

public class VisitRelationService(AppDbContext db) : IVisitRelationService
{
    // ── Default key lab items when no abnormal values found ──────────
    private static readonly (string Code, string Name)[] DefaultKeyLabs =
    [
        ("09015C", "CRE(肌酸酐)"),
        ("09015C", "eGFR"),
        ("12034B", "C3"),
    ];

    public async Task<VisitRelatedResponse> GetVisitRelatedAsync(int userId, int visitId)
    {
        var visit = await db.VisitDetails
            .Include(v => v.HealthRecord)
            .Include(v => v.Medications).ThenInclude(m => m.HealthRecord)
            .FirstOrDefaultAsync(v => v.Id == visitId && v.HealthRecord.UserId == userId)
            ?? throw new KeyNotFoundException("Visit not found.");

        var visitDate = visit.HealthRecord.RecordedAt;
        var instCode = visit.HealthRecord.NhiInstitutionCode;

        var labs = await GetRelatedLabsInternal(userId, visitDate, instCode);
        var bps = await GetBpAroundDateInternal(userId, visitDate, 3);

        var visitInfo = MapVisitInfo(visit);
        var medications = visit.Medications.Select(MapMedication).ToList();

        var abnormalCount = labs.Count(l => l.Status is "high" or "low");

        var summary = new VisitSummaryDto(
            LabCount: labs.Count,
            LabAbnormalCount: abnormalCount,
            MedicationCount: medications.Count,
            BpCount: bps.Count,
            BpAvgSystolic: bps.Count > 0 ? (int)Math.Round(bps.Average(b => b.Systolic)) : null,
            BpAvgDiastolic: bps.Count > 0 ? (int)Math.Round(bps.Average(b => b.Diastolic)) : null);

        return new VisitRelatedResponse(visitInfo, medications, labs, bps, summary);
    }

    public async Task<List<VisitTimelineItemDto>> GetTimelineAsync(
        int userId, DateTime? startDate, DateTime? endDate)
    {
        var query = db.VisitDetails
            .Include(v => v.HealthRecord)
            .Include(v => v.Medications)
            .Where(v => v.HealthRecord.UserId == userId && v.HealthRecord.RecordType == "visit");

        if (startDate.HasValue) query = query.Where(v => v.HealthRecord.RecordedAt >= startDate.Value);
        if (endDate.HasValue) query = query.Where(v => v.HealthRecord.RecordedAt <= endDate.Value);

        var visits = await query.OrderByDescending(v => v.HealthRecord.RecordedAt).ToListAsync();

        // Pre-load UserLabItems for the user (for display names + normal ranges)
        var userLabItems = await db.UserLabItems
            .Where(i => i.UserId == userId)
            .ToDictionaryAsync(i => (i.ItemCode, i.ItemName));

        var result = new List<VisitTimelineItemDto>();

        foreach (var v in visits)
        {
            var visitDate = v.HealthRecord.RecordedAt;
            var instCode = v.HealthRecord.NhiInstitutionCode;

            // Related labs (same date + same institution)
            var labs = await GetRelatedLabsInternal(userId, visitDate, instCode);

            // BP on the visit day only
            var bpOnDay = await db.BloodPressureDetails
                .Include(b => b.HealthRecord)
                .Where(b => b.HealthRecord.UserId == userId
                    && b.HealthRecord.RecordedAt.Date == visitDate.Date)
                .OrderByDescending(b => b.HealthRecord.RecordedAt)
                .FirstOrDefaultAsync();

            var keyLabs = PickKeyLabs(labs, userLabItems);

            result.Add(new VisitTimelineItemDto(
                VisitId: v.Id,
                RecordedAt: visitDate,
                Institution: v.HealthRecord.NhiInstitution,
                PrimaryDiagnosis: v.DiagnosisName1 ?? v.DiagnosisName2,
                MedicationCount: v.Medications.Count,
                LabCount: labs.Count,
                LabAbnormalCount: labs.Count(l => l.Status is "high" or "low"),
                BpOnDay: bpOnDay != null
                    ? new BloodPressureSimpleDto(bpOnDay.Systolic, bpOnDay.Diastolic, bpOnDay.Pulse)
                    : null,
                KeyLabs: keyLabs));
        }

        return result;
    }

    public async Task<List<LabResultWithStatusDto>> GetLabsByVisitAsync(int userId, int visitId)
    {
        var visit = await db.VisitDetails
            .Include(v => v.HealthRecord)
            .FirstOrDefaultAsync(v => v.Id == visitId && v.HealthRecord.UserId == userId)
            ?? throw new KeyNotFoundException("Visit not found.");

        return await GetRelatedLabsInternal(userId, visit.HealthRecord.RecordedAt, visit.HealthRecord.NhiInstitutionCode);
    }

    public async Task<List<BloodPressureWithDateDto>> GetBpAroundDateAsync(
        int userId, DateTime date, int dayRange = 3)
    {
        return await GetBpAroundDateInternal(userId, date, dayRange);
    }

    // ── Internal helpers ─────────────────────────────────────────────

    private async Task<List<LabResultWithStatusDto>> GetRelatedLabsInternal(
        int userId, DateTime visitDate, string? institutionCode)
    {
        // Same recorded_at date + same nhi_institution_code
        var query = db.LabResultDetails
            .Include(l => l.HealthRecord)
            .Include(l => l.UserLabItem)
            .Where(l => l.HealthRecord.UserId == userId
                && l.HealthRecord.RecordType == "lab_result"
                && l.HealthRecord.RecordedAt.Date == visitDate.Date);

        if (!string.IsNullOrEmpty(institutionCode))
            query = query.Where(l => l.HealthRecord.NhiInstitutionCode == institutionCode);

        var labs = await query.ToListAsync();

        // Fallback lookup: when UserLabItemId is null, match by (itemCode, itemName)
        var userItems = await db.UserLabItems
            .Where(i => i.UserId == userId)
            .ToDictionaryAsync(i => (i.ItemCode, i.ItemName));

        return labs.Select(l =>
        {
            var item = l.UserLabItem;
            if (item == null)
                userItems.TryGetValue((l.ItemCode, l.ItemName), out item);

            var normalMin = item?.NormalMin;
            var normalMax = item?.NormalMax;
            var status = GetLabStatus(l.ValueNumeric, normalMin, normalMax, l.IsNumeric);

            return new LabResultWithStatusDto(
                l.Id, l.ItemCode, l.ItemName,
                item?.DisplayName,
                item?.Category,
                l.ValueNumeric, l.ValueText, l.IsNumeric,
                l.Unit, l.ReferenceRange,
                normalMin, normalMax, status);
        }).ToList();
    }

    private async Task<List<BloodPressureWithDateDto>> GetBpAroundDateInternal(
        int userId, DateTime visitDate, int dayRange)
    {
        var from = visitDate.Date.AddDays(-dayRange);
        var to = visitDate.Date.AddDays(dayRange + 1); // +1 to include end of day

        var bps = await db.BloodPressureDetails
            .Include(b => b.HealthRecord)
            .Where(b => b.HealthRecord.UserId == userId
                && b.HealthRecord.RecordedAt >= from
                && b.HealthRecord.RecordedAt < to)
            .OrderBy(b => b.HealthRecord.RecordedAt)
            .ToListAsync();

        return bps.Select(b => new BloodPressureWithDateDto(
            b.Id,
            b.HealthRecord.RecordedAt,
            b.Systolic, b.Diastolic, b.Pulse,
            DaysFromVisit: (b.HealthRecord.RecordedAt.Date - visitDate.Date).Days)).ToList();
    }

    private static List<KeyLabDto> PickKeyLabs(
        List<LabResultWithStatusDto> labs,
        Dictionary<(string, string), UserLabItem> userLabItems)
    {
        if (labs.Count == 0) return [];

        // 1. Abnormal values first
        var abnormal = labs
            .Where(l => l.Status is "high" or "low")
            .Take(3)
            .Select(l => new KeyLabDto(l.DisplayName ?? l.ItemName, l.Value, l.Unit, l.Status))
            .ToList();

        if (abnormal.Count >= 3) return abnormal;

        // 2. Fill remaining with UserLabItems by sort_order, or defaults
        var usedNames = abnormal.Select(a => a.DisplayName).ToHashSet();
        var remaining = 3 - abnormal.Count;

        var sorted = labs
            .Where(l => !usedNames.Contains(l.DisplayName ?? l.ItemName))
            .OrderBy(l =>
            {
                if (userLabItems.TryGetValue((l.ItemCode, l.ItemName), out var item))
                    return item.SortOrder;
                // Default items get priority
                var idx = Array.FindIndex(DefaultKeyLabs, d => d.Code == l.ItemCode && d.Name == l.ItemName);
                return idx >= 0 ? idx : 999;
            })
            .Take(remaining)
            .Select(l => new KeyLabDto(l.DisplayName ?? l.ItemName, l.Value, l.Unit, l.Status));

        abnormal.AddRange(sorted);
        return abnormal;
    }

    private static string GetLabStatus(decimal? value, decimal? normalMin, decimal? normalMax, bool isNumeric)
    {
        if (!isNumeric || value == null) return "unknown";
        if (normalMin.HasValue && value < normalMin) return "low";
        if (normalMax.HasValue && value > normalMax) return "high";
        if (normalMin.HasValue || normalMax.HasValue) return "normal";
        return "unknown";
    }

    private static VisitInfoDto MapVisitInfo(VisitDetail v)
    {
        var diagnoses = new List<DiagnosisDto>();
        AddDiagnosis(diagnoses, v.DiagnosisCode1, v.DiagnosisName1);
        AddDiagnosis(diagnoses, v.DiagnosisCode2, v.DiagnosisName2);
        AddDiagnosis(diagnoses, v.DiagnosisCode3, v.DiagnosisName3);
        AddDiagnosis(diagnoses, v.DiagnosisCode4, v.DiagnosisName4);
        AddDiagnosis(diagnoses, v.DiagnosisCode5, v.DiagnosisName5);

        return new VisitInfoDto(
            v.Id, v.HealthRecord.RecordedAt,
            v.HealthRecord.NhiInstitution, v.HealthRecord.NhiInstitutionCode,
            v.VisitTypeCode, diagnoses,
            v.CopaymentCode, v.MedicalCost, v.HealthRecord.Source);
    }

    private static void AddDiagnosis(List<DiagnosisDto> list, string? code, string? name)
    {
        if (!string.IsNullOrWhiteSpace(code) || !string.IsNullOrWhiteSpace(name))
            list.Add(new DiagnosisDto(code ?? "", name ?? ""));
    }

    private static VisitMedicationDto MapMedication(MedicationDetail m) => new(
        m.Id, m.MedicationName, m.GenericName, m.NhiDrugCode,
        m.Quantity, m.Copayment, m.Dosage, m.Frequency, m.DurationDays);
}
