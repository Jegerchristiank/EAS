using EsgAsAService.Application.Models;

namespace EsgAsAService.Api.Models;

public record NarrativeUpsertRequest(
    C1StrategySection? C1,
    C2RiskSection? C2,
    C3HumanRightsSection? C3,
    C4GovernanceSection? C4,
    C5BoardDiversitySection? C5,
    C6StakeholderSection? C6,
    C7ValueChainSection? C7,
    C8AssuranceSection? C8,
    C9MethodologySection? C9
);

public record NarrativeResponse(
    Guid PeriodId,
    C1StrategySection? C1,
    C2RiskSection? C2,
    C3HumanRightsSection? C3,
    C4GovernanceSection? C4,
    C5BoardDiversitySection? C5,
    C6StakeholderSection? C6,
    C7ValueChainSection? C7,
    C8AssuranceSection? C8,
    C9MethodologySection? C9
);
