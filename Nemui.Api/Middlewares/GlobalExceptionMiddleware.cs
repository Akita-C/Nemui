﻿using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using Nemui.Shared.DTOs.Common;

namespace Nemui.Api.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred");
            await HandleExceptionAsync(context, exception);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
    
        var response = exception switch
        {
            FluentValidation.ValidationException validationEx => new
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Response = ErrorResponse.FromValidation(validationEx.Errors.Select(e => e.ErrorMessage).ToList())
            },
            System.ComponentModel.DataAnnotations.ValidationException dataAnnotationEx => new
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Response = ErrorResponse.FromValidation([dataAnnotationEx.Message])
            },
            UnauthorizedAccessException unauthorizedEx => new
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Response = ErrorResponse.Create(unauthorizedEx.Message)
            },
            InvalidOperationException invalidOpEx => new
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Response = ErrorResponse.Create("Invalid operation")
            },
            ArgumentException argEx => new
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Response = ErrorResponse.Create("Invalid request")
            },
            _ => new
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Response = ErrorResponse.Create("An internal server error occurred")
            }
        };
    
        context.Response.StatusCode = response.StatusCode;
        
        var jsonResponse = JsonSerializer.Serialize(response.Response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await context.Response.WriteAsync(jsonResponse);
    }
}