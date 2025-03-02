using System;
using System.Data.Common;
using System.Net;

using Microsoft.EntityFrameworkCore;
using MySqlConnector;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (MySqlException ex)
        {
            await HandleExceptionAsync(context, ex, HttpStatusCode.ServiceUnavailable, "Erro ao conectar ao banco de dados.");
        }
        catch (DbUpdateException ex)
        {
            await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest, "Erro ao atualizar o banco de dados.");
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError, "Erro inesperado.");
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex, HttpStatusCode statusCode, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(message);
    }
}
