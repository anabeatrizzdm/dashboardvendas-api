using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DashboardVendas.Tests;

/// <summary>
/// CASO DE TESTE 4: Vendas — regras de negócio críticas
///
/// Cenários cobertos:
/// - Criar venda válida retorna 200 OK com vendaId
/// - Criar venda com cliente inexistente retorna 400 BadRequest
/// - Criar venda sem itens retorna 400 BadRequest
/// - Criar venda com estoque insuficiente retorna 400 BadRequest
/// - Criar venda deduz o estoque do produto corretamente
/// </summary>
public class VendasTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public VendasTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AutenticarAsync()
    {
        var email = $"venda_{Guid.NewGuid()}@teste.com";
        await _client.PostAsJsonAsync("/api/auth/register", new { nome = "Venda User", email, senha = "Senha@123" });
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new { email, senha = "Senha@123" });
        var body = await loginResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!["token"]!.ToString());
    }

    private async Task<int> CriarClienteAsync(string nome = "Cliente Venda")
    {
        var resp = await _client.PostAsJsonAsync("/api/clientes", new
        {
            nome,
            email = $"{Guid.NewGuid()}@email.com",
            telefone = "11900000000"
        });
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return int.Parse(body!["id"]!.ToString()!);
    }

    private async Task<int> CriarProdutoAsync(string nome = "Produto Teste", int estoque = 50)
    {
        var resp = await _client.PostAsJsonAsync("/api/produtos", new
        {
            nome,
            preco = 100.00m,
            estoque
        });
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return int.Parse(body!["id"]!.ToString()!);
    }

    [Fact(DisplayName = "TC04.1 - Criar venda válida deve retornar 200 OK com vendaId")]
    public async Task CriarVenda_Valida_DeveRetornarVendaId()
    {
        await AutenticarAsync();

        var clienteId = await CriarClienteAsync();
        var produtoId = await CriarProdutoAsync();

        var payload = new
        {
            clienteId,
            itens = new[] { new { produtoId, quantidade = 2 } }
        };

        var response = await _client.PostAsJsonAsync("/api/vendas", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(body);
        Assert.True(body!.ContainsKey("vendaId"), "Resposta deve conter 'vendaId'");
    }

    [Fact(DisplayName = "TC04.2 - Criar venda com cliente inexistente deve retornar 400 BadRequest")]
    public async Task CriarVenda_ClienteInexistente_DeveRetornar400()
    {
        await AutenticarAsync();

        var produtoId = await CriarProdutoAsync();

        var payload = new
        {
            clienteId = 99999, // não existe
            itens = new[] { new { produtoId, quantidade = 1 } }
        };

        var response = await _client.PostAsJsonAsync("/api/vendas", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "TC04.3 - Criar venda sem itens deve retornar 400 BadRequest")]
    public async Task CriarVenda_SemItens_DeveRetornar400()
    {
        await AutenticarAsync();

        var clienteId = await CriarClienteAsync();

        var payload = new
        {
            clienteId,
            itens = Array.Empty<object>() // lista vazia
        };

        var response = await _client.PostAsJsonAsync("/api/vendas", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "TC04.4 - Criar venda com estoque insuficiente deve retornar 400 BadRequest")]
    public async Task CriarVenda_EstoqueInsuficiente_DeveRetornar400()
    {
        await AutenticarAsync();

        var clienteId = await CriarClienteAsync();
        var produtoId = await CriarProdutoAsync(estoque: 3); // apenas 3 no estoque

        var payload = new
        {
            clienteId,
            itens = new[] { new { produtoId, quantidade = 10 } } // pede 10
        };

        var response = await _client.PostAsJsonAsync("/api/vendas", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Contains("estoque", body!["message"]!.ToString()!.ToLower());
    }

    [Fact(DisplayName = "TC04.5 - Criar venda deve deduzir o estoque do produto corretamente")]
    public async Task CriarVenda_DeveDeduziEstoque()
    {
        await AutenticarAsync();

        var clienteId = await CriarClienteAsync();
        var produtoId = await CriarProdutoAsync(estoque: 20);

        // Cria venda com 5 unidades
        await _client.PostAsJsonAsync("/api/vendas", new
        {
            clienteId,
            itens = new[] { new { produtoId, quantidade = 5 } }
        });

        // Verifica se estoque foi reduzido de 20 para 15
        var getResp = await _client.GetAsync($"/api/produtos/{produtoId}");
        var produto = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        var estoqueAtual = produto.GetProperty("estoque").GetInt32();

        Assert.Equal(15, estoqueAtual);
    }
}
