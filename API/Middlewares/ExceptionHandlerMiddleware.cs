using System.Net;
using Core.Exceptions;
using System.Text.Json;
using Data.Response;

namespace API.Middlewares;

public class ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception error)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            response.StatusCode = error switch
            {
                NotFoundException => (int)HttpStatusCode.NotFound,
                BadRequestException => (int)HttpStatusCode.BadRequest,
                _ => (int)HttpStatusCode.InternalServerError
            };

            logger.LogError(error, "An error occurred while processing the request.");
            context.Response.StatusCode = response.StatusCode;

            var errors = error switch
            {
                NotFoundException => [error.Message],
                BadRequestException => [error.Message],
                _ => new List<string> { "An unexpected error occurred." }
            };

            var result = new ResponseSchema
            {
                Message = "An error occurred while processing the request.",
                Success = false,
                Errors = errors
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(result));
        }
    }
}

public static class ExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandlerMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlerMiddleware>();
    }
}