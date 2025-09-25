using EsgAsAService.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EsgAsAService.Api.Filters;

internal class ApiExceptionFilter(IDiagnosticService diag) : IActionFilter
{
    private readonly IDiagnosticService _diag = diag;

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (context.Exception is null) return;

        var ex = context.Exception;
        var path = context.HttpContext.Request.Path.ToString();
        var method = context.HttpContext.Request.Method;
        var traceId = context.HttpContext.TraceIdentifier;

        // Map common exceptions to HTTP status codes
        int status = 500;
        string title = "Internal Server Error";
        string code = "internal_error";
        string? detail = null;

        switch (ex)
        {
            case UnauthorizedAccessException:
                status = 403; title = "Forbidden"; code = "forbidden"; break;
            case KeyNotFoundException:
                status = 404; title = "Not Found"; code = "not_found"; break;
            case ArgumentException:
            case FormatException:
                status = 400; title = "Bad Request"; code = "invalid_argument"; detail = ex.Message; break;
            case InvalidOperationException ioe:
                if (ioe.Message.EndsWith("_not_found", StringComparison.OrdinalIgnoreCase))
                { status = 404; title = "Not Found"; code = ioe.Message; }
                else { status = 400; title = "Invalid Operation"; code = "invalid_operation"; detail = ioe.Message; }
                break;
        }

        // Always capture diagnostic code for 5xx (and optionally for 4xx with unknown code)
        var diagCode = _diag.Capture(ex, context.ActionDescriptor.DisplayName ?? "API", new { path, method, status });
        context.HttpContext.Response.Headers["X-Diagnostic-Code"] = diagCode;

        var problem = new ProblemDetails
        {
            Title = title,
            Status = status,
            Detail = status >= 500 ? "An unexpected error occurred. Share X-Diagnostic-Code with support." : detail,
            Instance = path,
            Type = status switch
            {
                400 => "https://httpstatuses.com/400",
                403 => "https://httpstatuses.com/403",
                404 => "https://httpstatuses.com/404",
                _ => "https://httpstatuses.com/500"
            }
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = traceId;

        context.Result = new ObjectResult(problem) { StatusCode = status };
        context.ExceptionHandled = true;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(kvp => kvp.Value is not null)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value" : e.ErrorMessage).ToArray()
                );
            var problem = new ValidationProblemDetails(errors)
            {
                Title = "Validation error",
                Status = 400,
                Type = "https://httpstatuses.com/400"
            };
            problem.Extensions["code"] = "validation_error";
            context.Result = new BadRequestObjectResult(problem);
        }
    }
}
