using Microsoft.Extensions.Options;
using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Infrastructure;

public class FrontendUrlProvider(IOptions<FrontendSettings> settings) : IFrontendUrlProvider
{
    public string BaseUrl => settings.Value.BaseUrl.TrimEnd('/');
}
