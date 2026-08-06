using Rawaj.Domain.Enums;

namespace Rawaj.Application.Common.Interfaces;

/// <summary>How a stage's model call should be made.</summary>
/// <param name="TaskName">Config key for the per-task model lookup. Everything currently runs on one
/// model — <c>llama-3.3-70b-versatile</c> writes both the cheap onboarding questions and the
/// 12,000-coin strategy — and this is what lets that stop being true without touching stage code.</param>
/// <param name="JsonMode">Whether to ask the provider for guaranteed JSON. False for prompts whose
/// output is prose.</param>
/// <param name="Temperature">Null leaves the provider default. Lower for structural work where
/// deviation is a defect, higher for copywriting where sameness is.</param>
public sealed record PromptTemplate(string TaskName, bool JsonMode, double? Temperature);

/// <summary>
/// Maps a stage to its generation settings — the one place that knows a strategy deserves a
/// different model or temperature from an image caption. Kept out of the executors so changing how a
/// stage is generated never means editing what it generates.
/// </summary>
public interface IPromptTemplateProvider
{
    PromptTemplate For(AiPipelineStageKind kind);
}
