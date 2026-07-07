using System.Threading.Tasks;
using NFC.Platform.Application.DTOs;
using NFC.Platform.BuildingBlocks.Results;

namespace NFC.Platform.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<ServiceResult<AuthDto>> LoginAsync(LoginRequest request);
        Task<ServiceResult<AuthDto>> RegisterAsync(RegisterRequest request);
        Task<ServiceResult<AuthDto>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<ServiceResult> RevokeTokenAsync(RefreshTokenRequest request);
        Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request);
        Task<ServiceResult<UserDto>> CreateUserByAdminAsync(AdminCreateUserRequest request);
    }
}
