using Engine.Application.Common.Exceptions;
using Engine.Application.Common.Responses;
using System.Net;
using System.Text.Json;

namespace Engine.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var traceId = context.TraceIdentifier;

        var (statusCode, code, message) = ex switch
        {
            NotFoundException e => (HttpStatusCode.NotFound, "NOT_FOUND", e.Message),
            ForbiddenException e => (HttpStatusCode.Forbidden, "FORBIDDEN", e.Message),
            UnauthorizedException e => (HttpStatusCode.Unauthorized, "UNAUTHORIZED", e.Message),
            ConflictException e => (HttpStatusCode.Conflict, "CONFLICT", e.Message),
            DomainException e => (HttpStatusCode.UnprocessableEntity, "DOMAIN_ERROR", e.Message),
            AppException e => (HttpStatusCode.BadRequest, "APP_ERROR", e.Message),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "서버 오류가 발생했습니다.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(ex, "Unhandled exception for request {TraceId}", traceId);
        else
            _logger.LogWarning(ex, "Handled exception {Code} for request {TraceId}", code, traceId);

        var response = new ApiResponse<object>
        {
            Success = false,
            Code = code,
            Message = message,
            TraceId = traceId,
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}

public class RequestIdMiddleware
{
    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey("X-Request-Id"))
            context.Request.Headers.Append("X-Request-Id", Guid.NewGuid().ToString("N")[..12]);
        context.Response.Headers.Append("X-Request-Id", context.TraceIdentifier);
        await _next(context);
    }
}
