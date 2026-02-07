namespace Bud.Server.Application.Common.Pipeline;

public sealed record UseCaseExecutionContext(string UseCaseName, string OperationName);
