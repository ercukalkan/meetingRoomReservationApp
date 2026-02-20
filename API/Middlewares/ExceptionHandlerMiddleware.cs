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

            var result = new
            {
                message = error?.Message,
                statusCode = response.StatusCode
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