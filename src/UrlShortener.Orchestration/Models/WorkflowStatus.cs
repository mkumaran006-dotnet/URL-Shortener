namespace UrlShortener.Orchestration.Models;

public enum WorkflowStatus { Pending, Running, AwaitingApproval, Completed, Failed, RolledBack, SafeStopped }

public enum StageStatus { Pending, Running, AwaitingApproval, Completed, Failed, RolledBack, Skipped }

public enum StageName
{
    Requirements,
    Architecture,
    Implementation,
    Testing,
    Documentation,
    ReleaseReadiness
}

public enum ApprovalDecision { Approved, Rejected, Deferred }

public enum Severity { Info, Warning, Error, Critical }

public enum ScenarioType { Greenfield, Brownfield, Ambiguous }
