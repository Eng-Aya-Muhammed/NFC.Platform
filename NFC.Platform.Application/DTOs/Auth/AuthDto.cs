using System;
using System.Collections.Generic;

namespace NFC.Platform.Application.DTOs.Auth;

public class AuthDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public List<string> Roles { get; set; } = [];
}

