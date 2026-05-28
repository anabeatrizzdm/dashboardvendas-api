using DashboardVendas.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DashboardVendas.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Define o ambiente como Testing para o Program.cs poder verificar
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove TODOS os DbContext registrados
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            // Remove o DbConnection do PostgreSQL se existir
            var descriptors = services.Where(d =>
                d.ServiceType.FullName != null &&
                (d.ServiceType.FullName.Contains("Npgsql") ||
                 d.ServiceType.FullName.Contains("Postgres"))).ToList();

            foreach (var d in descriptors)
                services.Remove(d);

            // Registra o DbContext com InMemory
            var dbName = $"TestDb_{Guid.NewGuid()}";
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
        });

        // Configurações JWT para os testes
        builder.UseSetting("Jwt:Key", "chave-super-secreta-para-testes-unitarios-123!");
        builder.UseSetting("Jwt:Issuer", "DashboardVendas");
        builder.UseSetting("Jwt:Audience", "DashboardVendas");
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);

        // Garante que o banco foi criado (substitui o Migrate() do Program.cs)
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }
}