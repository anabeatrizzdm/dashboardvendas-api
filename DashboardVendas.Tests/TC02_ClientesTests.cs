using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace DashboardVendas.Tests;

/// <summary>
/// CASO DE TESTE 2: Clientes — CRUD completo com autenticação JWT
///
/// Cenários cobertos:
/// - Criar cliente retorna 201 Created
/// - Buscar cliente por ID retorna dados corretos
/// - Atualizar cliente retorna 204 NoContent
/// - Deletar cliente sem vendas retorna 204 NoContent
/// - Endpoint exige token (sem token retorna 401)
/// </summary>
public class ClientesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ClientesTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Registra usuário, faz login e configura o header Authorization no cliente HTTP.
    /// </summary>
    private async Task AutenticarAsync()
    {
        var email = $"auth_{Guid.NewGuid()}@teste.com";
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            nome = "Auth User",
            email,
            senha = "Senha@123"
        });

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new { email, senha = "Senha@123" });
        var loginBody = await loginResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var token = loginBody!["token"]!.ToString();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact(DisplayName = "TC02.1 - Criar cliente autenticado deve retornar 201 Created")]
    public async Task CriarCliente_Autenticado_DeveRetornar201()
    {
        await AutenticarAsync();

        var payload = new { nome = "Ana Silva", email = "ana@email.com", telefone = "11999990000" };

        var response = await _client.PostAsJsonAsync("/api/clientes", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var cliente = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(cliente);
        Assert.Equal("Ana Silva", cliente!["nome"]?.ToString());
    }

    [Fact(DisplayName = "TC02.2 - Buscar cliente por ID deve retornar os dados corretos")]
    public async Task BuscarClientePorId_DeveRetornarDadosCorretos()
    {
        await AutenticarAsync();

        // Cria o cliente
        var createResp = await _client.PostAsJsonAsync("/api/clientes", new
        {
            nome = "Carlos Busca",
            email = "carlos@email.com",
            telefone = "11888880000"
        });
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var id = created!["id"];

        // Busca pelo ID
        var getResp = await _client.GetAsync($"/api/clientes/{id}");

        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        var cliente = await getResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("Carlos Busca", cliente!["nome"]?.ToString());
        Assert.Equal("carlos@email.com", cliente["email"]?.ToString());
    }

    [Fact(DisplayName = "TC02.3 - Atualizar cliente existente deve retornar 204 NoContent")]
    public async Task AtualizarCliente_DeveRetornar204()
    {
        await AutenticarAsync();

        var createResp = await _client.PostAsJsonAsync("/api/clientes", new
        {
            nome = "Pedro Antigo",
            email = "pedro@email.com",
            telefone = "11777770000"
        });
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var id = created!["id"];

        var updateResp = await _client.PutAsJsonAsync($"/api/clientes/{id}", new
        {
            nome = "Pedro Atualizado",
            email = "pedro.novo@email.com",
            telefone = "11666660000"
        });

        Assert.Equal(HttpStatusCode.NoContent, updateResp.StatusCode);
    }

    [Fact(DisplayName = "TC02.4 - Deletar cliente sem vendas deve retornar 204 NoContent")]
    public async Task DeletarCliente_SemVendas_DeveRetornar204()
    {
        await AutenticarAsync();

        var createResp = await _client.PostAsJsonAsync("/api/clientes", new
        {
            nome = "Cliente Deletável",
            email = "deletavel@email.com",
            telefone = "11555550000"
        });
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var id = created!["id"];

        var deleteResp = await _client.DeleteAsync($"/api/clientes/{id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }

    [Fact(DisplayName = "TC02.5 - Acessar clientes sem token deve retornar 401 Unauthorized")]
    public async Task AcessarClientes_SemToken_DeveRetornar401()
    {
        // Garante que não há token no header
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/clientes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
