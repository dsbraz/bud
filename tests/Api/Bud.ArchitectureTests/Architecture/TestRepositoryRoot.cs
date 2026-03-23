namespace Bud.ArchitectureTests.Architecture;

internal static class TestRepositoryRoot
{
    public static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Bud.sln"))
                || File.Exists(Path.Combine(directory.FullName, "Bud.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Não foi possível localizar a raiz do repositório.");
    }
}
