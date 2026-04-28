namespace Engine.Application.Common.Exceptions;

/// <summary>도메인 및 애플리케이션 예외</summary>
public class AppException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }
    public List<(string Field, string Reason)>? Errors { get; }

    public AppException(string errorCode, string message, int statusCode = 400,
        List<(string, string)>? errors = null) : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        Errors = errors;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string resourceName, string id)
        : base($"{resourceName.ToUpper()}_NOT_FOUND", $"{resourceName}을(를) 찾을 수 없습니다. ID: {id}", 404) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "접근 권한이 없습니다.")
        : base("FORBIDDEN", message, 403) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "인증이 필요합니다.")
        : base("UNAUTHORIZED", message, 401) { }
}

public class ConflictException : AppException
{
    public ConflictException(string message)
        : base("CONFLICT", message, 409) { }
}

public class DomainException : AppException
{
    public DomainException(string message)
        : base("DOMAIN_RULE_VIOLATION", message, 422) { }
}
