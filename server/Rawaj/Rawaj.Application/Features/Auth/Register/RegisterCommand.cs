using MediatR;
using Rawaj.Application.Common.Models;
using Rawaj.Domain.Enums;

namespace Rawaj.Application.Features.Auth.Register;

public record RegisterCommand(
    string Email,
    string Username,
    string Password,
    string FullName,
    Language PreferredLanguage) : IRequest<Result<RegisterResponse>>;
