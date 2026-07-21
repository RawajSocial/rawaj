namespace Rawaj.Domain.Enums;

public enum AiJobType
{
    MarketAnalysis,
    PlanGeneration,
    ContentGeneration,
    ImageGeneration,
    RagIndex,
    Scheduling
}

public enum AiJobStatus
{
    Queued,
    Running,
    Completed,
    Failed
}
