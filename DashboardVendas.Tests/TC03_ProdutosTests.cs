using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace DashboardVendas.Tests;

/// <summary>
/// CASO DE TESTE 3: Produtos — criação, listagem e validações
///
/// Cenários cobertos:
/// - Criar produto válido retorna 201 Created
/// - Listar produtos retorna array (mesmo que vazio)
/// - Buscar produto inexistente retorna 404 NotFound
/// - Atualizar produto retorna 204 NoContent e dados são persistidos
/// - Deletar produto retorna 204 NoContent
/// </summary>
public class ProdutosTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProdutosTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AutenticarAsync()
    {
        var email = $"prod_{Guid.NewGuid()}@teste.com";
        await _client.PostAsJsonAsync("/api/auth/register", new { nome = "Prod User", email, senha = "Senha@123" });
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new { email, senha = "Senha@123" });
        var body = await loginResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!["token"]!.ToString());
    }

    [Fact(DisplayName = "TC03.1 - Criar produto válido deve retornar 201 Created")]
    public async Task CriarProduto_Valido_DeveRetornar201()
    {
        await AutenticarAsync();

        var payload = new { nome = "Notebook Pro", preco = 4500.00m, estoque = 10 };

        var response = await _client.PostAsJsonAsync("/api/produtos", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var produto = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("Notebook Pro", produto!["nome"]?.ToString());
    }

    [Fact(DisplayName = "TC03.2 - Listar produtos deve retornar status 200 OK")]
    public async Task ListarProdutos_DeveRetornar200()
    {
        await AutenticarAsync();

        var response = await _client.GetAsync("/api/produtos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "TC03.3 - Buscar produto com ID inexistente deve retornar 404 NotFound")]
    public async Task BuscarProduto_IdInexistente_DeveRetornar404()
    {
        await AutenticarAsync();

        var response = await _client.GetAsync("/api/produtos/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "TC03.4 - Atualizar produto deve persistir os novos dados")]
    public async Task AtualizarProduto_DeveAlterarDados()
    {
        await AutenticarAsync();

        // Cria o produto
        var createResp = await _client.PostAsJsonAsync("/api/produtos", new
        {
            nome = "Mouse Antigo",
            preco = 50.00m,
            estoque = 100
        });
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var id = created!["id"];

        // Atualiza
        var updateResp = await _client.PutAsJsonAsync($"/api/produtos/{id}", new
        {
            nome = "Mouse Gamer RGB",
            preco = 150.00m,
            estoque = 80
        });
        Assert.Equal(HttpStatusCode.NoContent, updateResp.StatusCode);

        // Verifica se os dados foram alterados
        var getResp = await _client.GetAsync($"/api/produtos/{id}");
        var produto = await getResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        Assert.Equal("Mouse Gamer RGB", produto!["nome"]?.ToString());
    }

    [Fact(DisplayName = "TC03.5 - Deletar produto existente deve retornar 204 NoContent")]
    public async Task DeletarProduto_Existente_DeveRetornar204()
    {
        await AutenticarAsync();

        var createResp = await _client.PostAsJsonAsync("/api/produtos", new
        {
            nome = "Produto Para Deletar",
            preco = 10.00m,
            estoque = 5
        });
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var id = created!["id"];

        var deleteResp = await _client.DeleteAsync($"/api/produtos/{id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }
}
