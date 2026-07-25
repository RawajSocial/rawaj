using System.Net;
using System.Text.Json;
using Rawaj.Application.Common.Exceptions;
using Rawaj.Common;
using ValidationException = Rawaj.Application.Common.Exceptions.ValidationException;

namespace Rawaj.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await WriteResponseAsync(context, HttpStatusCode.BadRequest,
                ApiResponse<object>.Fail("Validation failed.", ex.Errors));
        }
        catch (ForbiddenAccessException ex)
        {
            await WriteResponseAsync(context, HttpStatusCode.Forbidden,
                ApiResponse<object>.Fail(ex.Message));
        }
        catch (ConcurrencyConflictException ex)
        {
            await WriteResponseAsync(context, HttpStatusCode.Conflict,
                ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred while processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteResponseAsync(context, HttpStatusCode.InternalServerError,
                ApiResponse<object>.Error("An unexpected error occurred."));
        }
    }

    private static Task WriteResponseAsync(HttpContext context, HttpStatusCode statusCode, object response)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
