using Engine.Application.Admin;
using Engine.Application.Common.DTOs;
using Engine.Application.Common.Exceptions;
using Engine.Application.Common.Interfaces;
using Engine.Application.Common.Responses;
using Engine.Domain.Security;
using Engine.Infrastructure.Persistence.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Engine.Infrastructure.Services;

// ─── SystemCode ───

public class SystemCodeServiceImpl : ISystemCodeService
{
    private readonly GroupwareDbContext _db;
    private readonly IAuditLogService _audit;

    public SystemCodeServiceImpl(GroupwareDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<PagedResult<SystemCodeDto>> GetCodesAsync(string? groupCode, PagedRequest request, CancellationToken ct = default)
    {
        var filter = Builders<SystemCode>.Filter.And(
            Builders<SystemCode>.Filter.Eq(x => x.IsDeleted, false),
            groupCode != null
                ? Builders<SystemCode>.Filter.Eq(x => x.GroupCode, groupCode)
                : Builders<SystemCode>.Filter.Empty
        );

        var total = await _db.SystemCodes.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _db.SystemCodes.Find(filter)
            .SortBy(x => x.GroupCode).ThenBy(x => x.SortOrder)
            .Skip((request.Page - 1) * request.PageSize).Limit(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<SystemCodeDto> { Items = items.Select(ToDto).ToList(), TotalCount = (int)total, Page = request.Page, PageSize = request.PageSize };
    }

    public async Task<List<SystemCodeDto>> GetCodesByGroupAsync(string groupCode, CancellationToken ct = default)
    {
        var items = await _db.SystemCodes
            .Find(x => x.GroupCode == groupCode && x.IsActive && !x.IsDeleted)
            .SortBy(x => x.SortOrder)
            .ToListAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<SystemCodeDto> GetCodeByIdAsync(string id, CancellationToken ct = default)
    {
        var code = await _db.SystemCodes.Find(x => x.Id == id && !x.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("SystemCode", id);
        return ToDto(code);
    }

    public async Task<SystemCodeDto> UpsertCodeAsync(UpsertSystemCodeRequest request, CancellationToken ct = default)
    {
        var existing = await _db.SystemCodes
            .Find(x => x.GroupCode == request.GroupCode && x.Code == request.Code && !x.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (existing != null)
        {
            existing.Name = request.Name;
            existing.GroupName = request.GroupName;
            existing.Description = request.Description;
            existing.SortOrder = request.SortOrder;
            existing.IsActive = request.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SystemCodes.ReplaceOneAsync(x => x.Id == existing.Id, existing, cancellationToken: ct);
            return ToDto(existing);
        }

        var code = new SystemCode
        {
            GroupCode = request.GroupCode,
            GroupName = request.GroupName,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
        };
        await _db.SystemCodes.InsertOneAsync(code, cancellationToken: ct);
        return ToDto(code);
    }

    public async Task DeleteCodeAsync(string id, CancellationToken ct = default)
    {
        var code = await _db.SystemCodes.Find(x => x.Id == id && !x.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("SystemCode", id);
        var update = Builders<SystemCode>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.DeletedAt, DateTime.UtcNow);
        await _db.SystemCodes.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        await _audit.LogAsync("SYSTEM_CODE_DELETE", "SystemCode", id, ct: ct);
    }

    private static SystemCodeDto ToDto(SystemCode x) => new()
    {
        Id = x.Id,
        GroupCode = x.GroupCode,
        GroupName = x.GroupName,
        Code = x.Code,
        Name = x.Name,
        Description = x.Description,
        SortOrder = x.SortOrder,
        IsActive = x.IsActive,
    };
}

// ─── Menu ───

public class MenuServiceImpl : IMenuService
{
    private readonly GroupwareDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public MenuServiceImpl(GroupwareDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<MenuDto>> GetMenuTreeAsync(CancellationToken ct = default)
    {
        var all = await _db.Menus.Find(x => !x.IsDeleted).SortBy(x => x.SortOrder).ToListAsync(ct);
        return BuildTree(all, null);
    }

    public async Task<List<MenuDto>> GetMyMenusAsync(CancellationToken ct = default)
    {
        var all = await _db.Menus.Find(x => x.IsActive && !x.IsDeleted).SortBy(x => x.SortOrder).ToListAsync(ct);
        var filtered = all.Where(m =>
            m.RequiredRoles.Count == 0 || m.RequiredRoles.Any(r => _currentUser.IsInRole(r))).ToList();
        return BuildTree(filtered, null);
    }

    public async Task<MenuDto> GetMenuByIdAsync(string id, CancellationToken ct = default)
    {
        var menu = await _db.Menus.Find(x => x.Id == id && !x.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Menu", id);
        return ToDto(menu);
    }

    public async Task<MenuDto> CreateMenuAsync(UpsertMenuRequest request, CancellationToken ct = default)
    {
        var menu = new Engine.Domain.Security.Menu
        {
            ParentId = request.ParentId,
            Name = request.Name,
            Icon = request.Icon,
            Route = request.Route,
            Url = request.Url,
            SortOrder = request.SortOrder,
            IsVisible = request.IsVisible,
            IsActive = true,
            RequiredRoles = request.RequiredRoles,
            RequiredPermissions = request.RequiredPermissions,
        };
        await _db.Menus.InsertOneAsync(menu, cancellationToken: ct);
        return ToDto(menu);
    }

    public async Task<MenuDto> UpdateMenuAsync(string id, UpsertMenuRequest request, CancellationToken ct = default)
    {
        var menu = await _db.Menus.Find(x => x.Id == id && !x.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Menu", id);
        menu.ParentId = request.ParentId;
        menu.Name = request.Name;
        menu.Icon = request.Icon;
        menu.Route = request.Route;
        menu.Url = request.Url;
        menu.SortOrder = request.SortOrder;
        menu.IsVisible = request.IsVisible;
        menu.RequiredRoles = request.RequiredRoles;
        menu.RequiredPermissions = request.RequiredPermissions;
        menu.UpdatedAt = DateTime.UtcNow;
        await _db.Menus.ReplaceOneAsync(x => x.Id == id, menu, cancellationToken: ct);
        return ToDto(menu);
    }

    public async Task DeleteMenuAsync(string id, CancellationToken ct = default)
    {
        var update = Builders<Engine.Domain.Security.Menu>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.DeletedAt, DateTime.UtcNow);
        await _db.Menus.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
    }

    private static List<MenuDto> BuildTree(List<Engine.Domain.Security.Menu> all, string? parentId)
        => all.Where(m => m.ParentId == parentId)
              .Select(m => { var dto = ToDto(m); dto.Children = BuildTree(all, m.Id); return dto; })
              .ToList();

    private static MenuDto ToDto(Engine.Domain.Security.Menu x) => new()
    {
        Id = x.Id,
        ParentId = x.ParentId,
        Name = x.Name,
        Icon = x.Icon,
        Route = x.Route,
        Url = x.Url,
        SortOrder = x.SortOrder,
        IsVisible = x.IsVisible,
        IsActive = x.IsActive,
        RequiredRoles = x.RequiredRoles,
        RequiredPermissions = x.RequiredPermissions,
    };
}

// ─── SystemSetting ───

public class SystemSettingServiceImpl : ISystemSettingService
{
    private readonly GroupwareDbContext _db;

    public SystemSettingServiceImpl(GroupwareDbContext db) => _db = db;

    public async Task<List<SystemSettingDto>> GetAllSettingsAsync(string? category, CancellationToken ct = default)
    {
        var filter = category != null
            ? Builders<SystemSetting>.Filter.And(
                Builders<SystemSetting>.Filter.Eq(x => x.IsDeleted, false),
                Builders<SystemSetting>.Filter.Eq(x => x.Category, category))
            : Builders<SystemSetting>.Filter.Eq(x => x.IsDeleted, false);

        var items = await _db.SystemSettings.Find(filter).SortBy(x => x.Category).ThenBy(x => x.Key).ToListAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<SystemSettingDto> GetSettingByKeyAsync(string key, CancellationToken ct = default)
    {
        var setting = await _db.SystemSettings.Find(x => x.Key == key && !x.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("SystemSetting", key);
        return ToDto(setting);
    }

    public async Task<SystemSettingDto> UpdateSettingAsync(string key, UpdateSystemSettingRequest request, CancellationToken ct = default)
    {
        var setting = await _db.SystemSettings.Find(x => x.Key == key && !x.IsDeleted).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("SystemSetting", key);
        setting.Value = request.Value;
        setting.UpdatedAt = DateTime.UtcNow;
        await _db.SystemSettings.ReplaceOneAsync(x => x.Id == setting.Id, setting, cancellationToken: ct);
        return ToDto(setting);
    }

    private static SystemSettingDto ToDto(SystemSetting x) => new()
    {
        Id = x.Id,
        Key = x.Key,
        Value = x.IsSecret ? "***" : x.Value,
        Description = x.Description,
        IsSecret = x.IsSecret,
        Category = x.Category,
    };
}

// ─── AuditLog Query ───

public class AuditLogQueryServiceImpl : IAuditLogQueryService
{
    private readonly GroupwareDbContext _db;

    public AuditLogQueryServiceImpl(GroupwareDbContext db) => _db = db;

    public async Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        var filters = new List<FilterDefinition<AuditLog>>();

        if (!string.IsNullOrWhiteSpace(query.Action))
            filters.Add(Builders<AuditLog>.Filter.Regex(x => x.Action, new MongoDB.Bson.BsonRegularExpression(query.Action, "i")));

        if (!string.IsNullOrWhiteSpace(query.ActorUserId))
            filters.Add(Builders<AuditLog>.Filter.Eq(x => x.ActorUserId, query.ActorUserId));

        if (!string.IsNullOrWhiteSpace(query.ResourceType))
            filters.Add(Builders<AuditLog>.Filter.Eq(x => x.ResourceType, query.ResourceType));

        if (query.From.HasValue)
            filters.Add(Builders<AuditLog>.Filter.Gte(x => x.CreatedAt, query.From.Value));

        if (query.To.HasValue)
            filters.Add(Builders<AuditLog>.Filter.Lte(x => x.CreatedAt, query.To.Value));

        var filter = filters.Count > 0 ? Builders<AuditLog>.Filter.And(filters) : Builders<AuditLog>.Filter.Empty;

        var total = await _db.AuditLogs.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _db.AuditLogs.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Limit(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto> { Items = items.Select(ToDto).ToList(), TotalCount = (int)total, Page = query.Page, PageSize = query.PageSize };
    }

    private static AuditLogDto ToDto(AuditLog x) => new()
    {
        Id = x.Id,
        TenantId = x.TenantId,
        ActorUserId = x.ActorUserId,
        ActorName = x.ActorName,
        Action = x.Action,
        ResourceType = x.ResourceType,
        ResourceId = x.ResourceId,
        IpAddress = x.IpAddress,
        UserAgent = x.UserAgent,
        Before = x.Before,
        After = x.After,
        CreatedAt = x.CreatedAt,
    };
}
