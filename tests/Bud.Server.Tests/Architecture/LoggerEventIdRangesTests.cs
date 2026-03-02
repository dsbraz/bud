using System.Text.RegularExpressions;
using System.Globalization;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Architecture;

public sealed class LoggerEventIdRangesTests
{
    private static readonly Regex LoggerEventIdRegex = new(@"\[LoggerMessage\(EventId = (?<id>\d+),", RegexOptions.Compiled);

    [Fact]
    public void UseCases_ShouldUseReservedEventIdRangesPerDomain()
    {
        var repositoryRoot = FindRepositoryRoot();
        var useCasesRoot = Path.Combine(repositoryRoot, "src", "Bud.Server", "Application", "UseCases");

        var ranges = new Dictionary<string, (int Min, int Max)>
        {
            ["Goals"] = (4000, 4009),
            ["Organizations"] = (4010, 4019),
            ["Workspaces"] = (4020, 4029),
            ["Teams"] = (4030, 4039),
            ["Collaborators"] = (4040, 4049),
            ["Indicators"] = (4050, 4059),
            ["Checkins"] = (4060, 4069),
            ["Templates"] = (4070, 4079),
            ["Tasks"] = (4080, 4089),
            ["Sessions"] = (4090, 4099),
            ["Notifications"] = (4090, 4099)
        };

        var violations = new List<string>();

        foreach (var (folder, range) in ranges)
        {
            var folderPath = Path.Combine(useCasesRoot, folder);
            if (!Directory.Exists(folderPath))
            {
                continue;
            }

            var files = Directory.EnumerateFiles(folderPath, "*.cs", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var content = File.ReadAllText(file);
                var matches = LoggerEventIdRegex.Matches(content);
                foreach (Match match in matches)
                {
                    var id = int.Parse(match.Groups["id"].Value, CultureInfo.InvariantCulture);
                    if (id < range.Min || id > range.Max)
                    {
                        var relativePath = Path.GetRelativePath(repositoryRoot, file);
                        violations.Add($"{relativePath}: EventId {id} fora da faixa {range.Min}-{range.Max}.");
                    }
                }
            }
        }

        violations.Should().BeEmpty("EventId dos use cases deve respeitar a faixa reservada por domínio.");
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var candidate = Path.Combine(current, "Bud.slnx");
            if (File.Exists(candidate))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Não foi possível localizar a raiz do repositório.");
    }
}
