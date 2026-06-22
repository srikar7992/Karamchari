namespace Karamchari.Capability.Domain.LearningPaths;

public sealed record LearningPathStep(int StepOrder, Guid ModuleId, bool IsOptional);
