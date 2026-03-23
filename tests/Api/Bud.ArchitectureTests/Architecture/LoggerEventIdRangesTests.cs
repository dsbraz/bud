using System.Text.RegularExpressions;
using System.Globalization;
using FluentAssertions;
using Xunit;

namespace Bud.ArchitectureTests.Architecture;

public sealed class LoggerEventIdRangesTests
{
    private static readonly Regex LoggerEventIdRegex = new(@"\[LoggerMessage\(EventId = (?<id>\d+),", RegexOptions.Compiled);

    [Fact]
    public void UseCases_ShouldUseReservedEventIdRangesPerDomain()
    {
        var repositoryRoot = TestRepositoryRoot.Find();
        var featuresRoot = Path.Combine(repositoryRoot, "src", "Server", "Bud.Application", "Features");

        var ranges = new Dictionary<string, Func<string, (int Min, int Max)?>>
        {
            ["Goals"] = static _ => (4000, 4009),
            ["Organizations"] = static _ => (4010, 4019),
            ["Workspaces"] = static _ => (4020, 4029),
            ["Teams"] = static _ => (4030, 4039),
            ["Collaborators"] = static _ => (4040, 4049),
            ["Indicators"] = static fileName => fileName.Contains("Checkin", StringComparison.Ordinal)
                ? (4060, 4069)
                : (4050, 4059),
            ["Templates"] = static _ => (4070, 4079),
            ["Tasks"] = static _ => (4080, 4089),
            ["Sessions"] = static _ => (4090, 4099),
            ["Notifications"] = static _ => (4090, 4099)
        };

        var violations = new List<string>();

        foreach (var (feature, rangeResolver) in ranges)
        {
            var folderPath = Path.Combine(featuresRoot, feature, "UseCases");
            if (!Directory.Exists(folderPath))
            {
                continue;
            }

            var files = Directory.EnumerateFiles(folderPath, "*.cs", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var range = rangeResolver(Path.GetFileName(file));
                if (!range.HasValue)
                {
                    continue;
                }

                var content = File.ReadAllText(file);
                var matches = LoggerEventIdRegex.Matches(content);
                foreach (Match match in matches)
                {
                    var id = int.Parse(match.Groups["id"].Value, CultureInfo.InvariantCulture);
                    if (id < range.Value.Min || id > range.Value.Max)
                    {
                        var relativePath = Path.GetRelativePath(repositoryRoot, file);
                        violations.Add($"{relativePath}: EventId {id} fora da faixa {range.Value.Min}-{range.Value.Max}.");
                    }
                }
            }
        }

        violations.Should().BeEmpty("EventId dos use cases deve respeitar a faixa reservada por domínio.");
    }
}
