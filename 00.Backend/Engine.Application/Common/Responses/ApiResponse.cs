namespace Engine.Application.Common.Responses;

/// <summary>
/// 공통 API 응답 래퍼
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Code { get; set; } = "OK";
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<ApiError>? Errors { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public static ApiResponse<T> Ok(T data, string message = "처리되었습니다.")
        => new() { Success = true, Code = "OK", Message = message, Data = data };

    public static ApiResponse<T> Fail(string code, string message, List<ApiError>? errors = null)
        => new() { Success = false, Code = code, Message = message, Errors = errors };

    public static ApiResponse<T> ValidationFail(List<ApiError> errors)
        => new() { Success = false, Code = "VALIDATION_ERROR", Message = "입력값을 확인해주세요.", Errors = errors };
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse OkEmpty(string message = "처리되었습니다.")
        => new() { Success = true, Code = "OK", Message = message };
}

public class ApiError
{
    public string? Field { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>페이징 결과</summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}
