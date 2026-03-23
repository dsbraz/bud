namespace Bud.Mcp.Protocol;

public interface IMcpRequestProcessor
{
    Task<IResult> ProcessAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}
