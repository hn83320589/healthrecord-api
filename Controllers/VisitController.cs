using HealthRecord.API.Models.DTOs.Common;
using HealthRecord.API.Models.DTOs.Visit;
using HealthRecord.API.Models.DTOs.VisitSummary;
using HealthRecord.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthRecord.API.Controllers;

[Authorize]
[Route("visits")]
public class VisitController(
    IVisitService service,
    IVisitRelationService relation,
    IVisitSummaryService summary) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<VisitResponse>>>> GetList(
        int page = 1, int pageSize = 20, DateTime? from = null, DateTime? to = null)
    {
        var result = await service.GetListAsync(CurrentUserId, page, pageSize, from, to);
        return Ok(ApiResponse<PagedResult<VisitResponse>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<VisitDetailResponse>>> GetById(int id)
    {
        var result = await service.GetByIdAsync(CurrentUserId, id);
        return Ok(ApiResponse<VisitDetailResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<VisitResponse>>> Create(CreateVisitRequest request)
    {
        var result = await service.CreateAsync(CurrentUserId, request);
        return StatusCode(201, ApiResponse<VisitResponse>.Ok(result));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<VisitResponse>>> Update(int id, UpdateVisitRequest request)
    {
        var result = await service.UpdateAsync(CurrentUserId, id, request);
        return Ok(ApiResponse<VisitResponse>.Ok(result));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await service.DeleteAsync(CurrentUserId, id);
        return Ok(ApiResponse<object>.Ok(null!, "Deleted."));
    }

    [HttpGet("{id}/related")]
    public async Task<ActionResult<ApiResponse<VisitRelatedResponse>>> GetRelated(int id)
    {
        var result = await relation.GetVisitRelatedAsync(CurrentUserId, id);
        return Ok(ApiResponse<VisitRelatedResponse>.Ok(result));
    }

    [HttpGet("timeline")]
    public async Task<ActionResult<ApiResponse<List<VisitTimelineItemDto>>>> GetTimeline(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var result = await relation.GetTimelineAsync(CurrentUserId, startDate, endDate);
        return Ok(ApiResponse<List<VisitTimelineItemDto>>.Ok(result));
    }

    [HttpGet("{id}/summary-pdf")]
    public async Task<IActionResult> GetSummaryPdf(int id)
    {
        var pdfBytes = await summary.GeneratePdfAsync(CurrentUserId, id);
        return File(pdfBytes, "application/pdf", $"visit-summary-{id}.pdf");
    }

    [HttpGet("latest-summary-pdf")]
    public async Task<IActionResult> GetLatestSummaryPdf()
    {
        var visitId = await summary.GetLatestVisitIdAsync(CurrentUserId)
            ?? throw new KeyNotFoundException("No visits found.");
        var pdfBytes = await summary.GeneratePdfAsync(CurrentUserId, visitId);
        return File(pdfBytes, "application/pdf", "visit-summary-latest.pdf");
    }

    [HttpGet("{id}/summary-json")]
    public async Task<ActionResult<ApiResponse<VisitSummaryJsonResponse>>> GetSummaryJson(int id)
    {
        var result = await summary.GetSummaryJsonAsync(CurrentUserId, id);
        return Ok(ApiResponse<VisitSummaryJsonResponse>.Ok(result));
    }
}
