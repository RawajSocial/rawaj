namespace Rawaj.Application.Common.Interfaces;

/// <summary>
/// Config-bound feature toggles — enough to disable a bad rollout via config without a redeploy.
/// Not a flag-management vendor (LaunchDarkly etc.), just booleans read from appsettings.
/// </summary>
public interface IFeatureFlags
{
    bool VideoGeneration { get; }
    bool ExperimentalAi { get; }
    bool CoinLedger { get; }
}
