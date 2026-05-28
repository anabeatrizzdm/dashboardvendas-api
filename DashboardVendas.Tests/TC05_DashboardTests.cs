using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DashboardVendas.Tests;

/// <summary>
/// CASO DE TESTE 5: Dashboard — consistência dos dados agregados
///
/// Cenários cobertos:
/// - Dashboard sem dados retorna totais zerados
/// - Dashboard reflete corretamente o total de clientes cadastrados
/// - Dashboard reflete corretamente o total de produtos cadastrados
/// - Dashboard reflete quantidadeVendas e totalVendas após uma venda
/// - Acessar dashboard sem token retorna 401 Unauthorized
/// </summary>
public class DashboardTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DashboardTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AutenticarAsync()
    {
        var email = $"dash_{Guid.NewGuid()}@teste.com";
        await _client.PostAsJsonAsync("/api/auth/register", new { nome = "Dash User", email, senha = "Senha@123" });
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new { email, senha = "Senha@123" });
        var body = await loginResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!["token"]!.ToString());
    }

    private async Task<int> CriarClienteAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/clientes", new
        {
            nome = "Cliente Dashboard",
            email = $"{Guid.NewGuid()}@email.com",
            telefone = "11900000000"
        });
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return int.Parse(body!["id"]!.ToString()!);
    }

    private async Task<int> CriarProdutoAsync(decimal preco = 200.00m, int estoque = 50)
    {
        var resp = await _client.PostAsJsonAsync("/api/produtos", new
        {
            nome = $"Produto_{Guid.NewGuid()}",
            preco,
            estoque
        });
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return int.Parse(body!["id"]!.ToString()!);
    }

    [Fact(DisplayName = "TC05.1 - Dashboard sem dados deve retornar todos os totais zerados")]
    public async Task Dashboard_SemDados_DeveTerTotaisZerados()
    {
        await AutenticarAsync();

        var response = await _client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dashboard = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0, dashboard.GetProperty("totalVendas").GetDecimal());
        Assert.Equal(0, dashboard.GetProperty("quantidadeVendas").GetInt32());
        Assert.Equal(0, dashboard.GetProperty("totalClientes").GetInt32());
        Assert.Equal(0, dashboard.GetProperty("totalProdutos").GetInt32());
    }

    [Fact(DisplayName = "TC05.2 - Dashboard deve refletir o total de clientes cadastrados")]
    public async Task Dashboard_DeveContarClientesCorretamente()
    {
        await AutenticarAsync();

        // Cria 2 clientes
        await CriarClienteAsync();
        await CriarClienteAsync();

        var response = await _client.GetAsync("/api/dashboard");
        var dashboard = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(2, dashboard.GetProperty("totalClientes").GetInt32());
    }

    [Fact(DisplayName = "TC05.3 - Dashboard deve refletir o total de produtos cadastrados")]
    public async Task Dashboard_DeveContarProdutosCorretamente()
    {
        await AutenticarAsync();

        await CriarProdutoAsync();
        await CriarProdutoAsync();
        await CriarProdutoAsync();

        var response = await _client.GetAsync("/api/dashboard");
        var dashboard = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(3, dashboard.GetProperty("totalProdutos").GetInt32());
    }

    [Fact(DisplayName = "TC05.4 - Dashboard deve refletir quantidadeVendas e totalVendas após uma venda")]
    public async Task Dashboard_DeveReflectirVendaRealizada()
    {
        await AutenticarAsync();

        var clienteId = await CriarClienteAsync();
        // Produto com preço R$ 300,00 e estoque 10
        var produtoId = await CriarProdutoAsync(preco: 300.00m, estoque: 10);

        // Realiza uma venda de 2 unidades → total esperado: R$ 600,00
        await _client.PostAsJsonAsync("/api/vendas", new
        {
            clienteId,
            itens = new[] { new { produtoId, quantidade = 2 } }
        });

        var response = await _client.GetAsync("/api/dashboard");
        var dashboard = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, dashboard.GetProperty("quantidadeVendas").GetInt32());
        Assert.Equal(600.00m, dashboard.GetProperty("totalVendas").GetDecimal());
    }

    [Fact(DisplayName = "TC05.5 - Acessar dashboard sem token deve retornar 401 Unauthorized")]
    public async Task Dashboard_SemToken_DeveRetornar401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
