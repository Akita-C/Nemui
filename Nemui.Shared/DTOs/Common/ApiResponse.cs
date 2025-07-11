﻿namespace Nemui.Shared.DTOs.Common;

public class ApiResponse<T> : BaseApiResponse
{
    public T? Data { get; set; }
    
    public static ApiResponse<T> SuccessResult(T data, string message = "Success") 
    {
        return new ApiResponse<T> 
        { 
            Success = true, 
            Message = message, 
            Data = data 
        };
    }

    public static ApiResponse<T> ErrorResult(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? []
        };
    }
}