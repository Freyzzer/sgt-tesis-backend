using NetArchTest.Rules;
using SGT.Tesis.API.Middleware;
using SGT.Tesis.Application;
using SGT.Tesis.Domain.Common;
using SGT.Tesis.Infrastructure;

namespace SGT.Tesis.ArchitectureTests;

/// <summary>
/// Regla de dependencias de Clean Architecture: API → Infrastructure → Application → Domain.
/// </summary>
public class LayerDependencyTests
{
    private const string DomainNs = "SGT.Tesis.Domain";
    private const string ApplicationNs = "SGT.Tesis.Application";
    private const string InfrastructureNs = "SGT.Tesis.Infrastructure";
    private const string ApiNs = "SGT.Tesis.API";

    private static readonly System.Reflection.Assembly Domain = typeof(BaseEntity).Assembly;
    private static readonly System.Reflection.Assembly Application = typeof(ApplicationAssemblyMarker).Assembly;
    private static readonly System.Reflection.Assembly Infrastructure = typeof(SGT.Tesis.Infrastructure.DependencyInjection).Assembly;
    private static readonly System.Reflection.Assembly Api = typeof(GlobalExceptionHandler).Assembly;

    private static void AssertNoDependencies(System.Reflection.Assembly assembly, params string[] forbidden)
    {
        var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();

        Assert.True(
            result.IsSuccessful,
            "Dependencias prohibidas en: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_no_depende_de_otras_capas_ni_de_frameworks()
    {
        AssertNoDependencies(
            Domain,
            ApplicationNs,
            InfrastructureNs,
            ApiNs,
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "Serilog",
            "FluentValidation");
    }

    [Fact]
    public void Application_no_depende_de_Infrastructure_API_ni_de_EF_Core()
    {
        AssertNoDependencies(
            Application,
            InfrastructureNs,
            ApiNs,
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore");
    }

    [Fact]
    public void Infrastructure_no_depende_de_la_API()
    {
        AssertNoDependencies(Infrastructure, ApiNs);
    }

    [Fact]
    public void Las_entidades_del_dominio_son_selladas_o_abstractas_y_heredan_de_BaseEntity_si_son_persistidas()
    {
        var entidades = Types.InAssembly(Domain)
            .That().Inherit(typeof(BaseEntity))
            .And().AreNotAbstract()
            .GetTypes();

        Assert.All(entidades, tipo => Assert.True(tipo.IsSealed, $"{tipo.Name} debe ser sealed."));
    }

    [Fact]
    public void Las_excepciones_de_dominio_heredan_de_DomainException()
    {
        var infractoras = Types.InAssembly(Domain)
            .That().HaveNameEndingWith("Exception")
            .And().AreNotAbstract()
            .And().DoNotHaveName(nameof(DomainException))
            .Should().Inherit(typeof(DomainException))
            .GetResult();

        Assert.True(infractoras.IsSuccessful);
    }
}
