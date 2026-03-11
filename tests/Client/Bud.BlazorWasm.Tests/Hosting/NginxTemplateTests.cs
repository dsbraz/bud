using FluentAssertions;
using Xunit;

namespace Bud.BlazorWasm.Tests.Hosting;

public sealed class NginxTemplateTests
{
    [Fact]
    public void Template_ShouldEnableSniForHttpsApiProxy()
    {
        var templatePath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../../../src/Client/Bud.BlazorWasm/Hosting/nginx.conf.template"));

        File.Exists(templatePath).Should().BeTrue($"template should exist at {templatePath}");

        var template = File.ReadAllText(templatePath);

        template.Should().Contain("proxy_ssl_server_name on;");
        template.Should().NotContain("proxy_set_header Host $host;");
    }
}
