using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SGT.Tesis.Application.Common.Abstractions;
using SGT.Tesis.Domain.Identidad;
using SGT.Tesis.Infrastructure.Persistence;

namespace SGT.Tesis.IntegrationTests.Support;

public static class TestHelpers
{
    public const string PasswordPrueba = "Clave#Segura123";

    /// <summary>Inserta un usuario directamente en la base de datos de la fábrica y devuelve su correo.</summary>
    public static async Task<(Guid Id, string Correo)> SembrarUsuarioAsync(this ApiFactory factory, Rol rol = Rol.Estudiante, bool activo = true)
    {
        var correo = $"{Guid.NewGuid():N}@institucion.edu";

        using var scope = factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var usuario = Usuario.Crear("Ana", "Pérez", correo, hasher.Hash(PasswordPrueba), rol);

        if (!activo)
        {
            usuario.Desactivar();
        }

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        return (usuario.Id, correo);
    }

    public static async Task<JsonElement> LeerJsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    public static async Task<JsonElement> IniciarSesionAsync(this ApiFactory factory, string correo, string password)
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { correo, password });
        response.EnsureSuccessStatusCode();

        return await response.LeerJsonAsync();
    }

    /// <summary>Cliente HTTP autenticado con el access token del usuario indicado.</summary>
    public static async Task<HttpClient> ClienteComoAsync(this ApiFactory factory, string correo, string password = PasswordPrueba)
    {
        var tokens = await factory.IniciarSesionAsync(correo, password);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.GetProperty("accessToken").GetString());

        return client;
    }

    public static Task<HttpClient> ClienteAdministradorAsync(this ApiFactory factory) =>
        factory.ClienteComoAsync(ApiFactory.AdminCorreo, ApiFactory.AdminPassword);
}
