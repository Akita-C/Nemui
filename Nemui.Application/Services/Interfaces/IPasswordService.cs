﻿namespace Nemui.Application.Services.Interfaces;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    bool IsPasswordStrong(string password);
}