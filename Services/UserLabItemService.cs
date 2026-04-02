using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Models.DTOs.UserLabItem;
using HealthRecord.API.Models.Entities;
using HealthRecord.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HealthRecord.API.Services;

public class UserLabItemService(AppDbContext db) : IUserLabItemService
{
    public async Task<List<UserLabItemResponse>> GetAllAsync(int userId)
    {
        return await db.UserLabItems
            .Where(i => i.UserId == userId)
            .OrderBy(i => i.Category)
            .ThenBy(i => i.ItemName)
            .Select(i => Map(i))
            .ToListAsync();
    }

    public async Task<UserLabItemResponse> CreateAsync(int userId, CreateUserLabItemRequest request)
    {
        var exists = await db.UserLabItems.AnyAsync(i =>
            i.UserId == userId && i.ItemCode == request.ItemCode && i.ItemName == request.ItemName);
        if (exists)
            throw new ArgumentException("An item with the same ItemCode and ItemName already exists.");

        var item = new UserLabItem
        {
            UserId = userId,
            ItemCode = request.ItemCode,
            ItemName = request.ItemName,
            Unit = request.Unit,
            Category = request.Category,
            NormalMin = request.NormalMin,
            NormalMax = request.NormalMax,
            IsPreset = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.UserLabItems.Add(item);
        await db.SaveChangesAsync();
        return Map(item);
    }

    public async Task<UserLabItemResponse> UpdateAsync(int userId, int id, UpdateUserLabItemRequest request)
    {
        var item = await db.UserLabItems
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId)
            ?? throw new KeyNotFoundException("Lab item not found.");

        item.Unit = request.Unit;
        item.Category = request.Category;
        item.NormalMin = request.NormalMin;
        item.NormalMax = request.NormalMax;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Map(item);
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var item = await db.UserLabItems
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId)
            ?? throw new KeyNotFoundException("Lab item not found.");

        if (item.IsPreset)
            throw new InvalidOperationException("Cannot delete preset items.");

        db.UserLabItems.Remove(item);
        await db.SaveChangesAsync();
    }

    private static UserLabItemResponse Map(UserLabItem i) => new(
        i.Id, i.ItemCode, i.ItemName, i.Unit, i.Category,
        i.NormalMin, i.NormalMax, i.IsPreset, i.CreatedAt, i.UpdatedAt);
}
