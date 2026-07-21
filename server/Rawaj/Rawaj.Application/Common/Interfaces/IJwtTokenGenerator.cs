using Rawaj.Application.Common.Models;

namespace Rawaj.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(ApplicationUserDto user);
}
