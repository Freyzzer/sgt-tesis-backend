namespace SGT.Tesis.ArchitectureTests;

/// <summary>
/// ADR-003: MediatR y AutoMapper (desde v13 y v15) requieren licencia comercial. No se usan ni se declaran.
/// </summary>
public class LicenciasTests
{
    private static readonly string[] Prohibidos = ["MediatR", "AutoMapper"];

    [Fact]
    public void Ningun_ensamblado_del_sistema_referencia_paquetes_de_licencia_comercial()
    {
        var ensamblados = new[]
        {
            typeof(SGT.Tesis.Domain.Common.BaseEntity).Assembly,
            typeof(SGT.Tesis.Application.ApplicationAssemblyMarker).Assembly,
            typeof(SGT.Tesis.Infrastructure.DependencyInjection).Assembly,
            typeof(SGT.Tesis.API.Middleware.GlobalExceptionHandler).Assembly,
        };

        var referencias = ensamblados
            .SelectMany(a => a.GetReferencedAssemblies())
            .Select(r => r.Name ?? string.Empty)
            .Where(n => Prohibidos.Any(p => n.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            .Distinct()
            .ToList();

        Assert.True(referencias.Count == 0, "Referencias prohibidas por ADR-003: " + string.Join(", ", referencias));
    }

    [Fact]
    public void Ningun_proyecto_declara_esos_paquetes()
    {
        var raiz = RaizDelRepositorio();
        var archivos = Directory
            .EnumerateFiles(raiz, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".csproj", StringComparison.Ordinal) || f.EndsWith("Directory.Packages.props", StringComparison.Ordinal) || f.EndsWith("Directory.Build.props", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

        var infractores = archivos
            .Where(f => Prohibidos.Any(p => File.ReadAllText(f).Contains($"Include=\"{p}", StringComparison.OrdinalIgnoreCase)))
            .Select(f => Path.GetRelativePath(raiz, f))
            .ToList();

        Assert.True(infractores.Count == 0, "Paquetes prohibidos por ADR-003 declarados en: " + string.Join(", ", infractores));
    }

    private static string RaizDelRepositorio()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SGT.Tesis.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("No se encontró la raíz del repositorio (SGT.Tesis.slnx).");
    }
}
