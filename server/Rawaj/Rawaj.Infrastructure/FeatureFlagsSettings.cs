using Rawaj.Application.Common.Interfaces;

namespace Rawaj.Infrastructure;

public class FeatureFlagsSettings : IFeatureFlags
{
    public const string SectionName = "FeatureFlags";

    public bool VideoGeneration { get; set; }
    public bool ExperimentalAi { get; set; }
    public bool CoinLedger { get; set; } = true;
}
