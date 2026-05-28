using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace DashboardVendas.Tests;

/// <summary>
/// CASO DE TESTE 1: Autenticação — Registro e Login de usuário
///
/// Cenários cobertos:
/// - Registrar novo usuário retorna 200 OK
/// - Tentar registrar e-mail duplicado retorna 400 BadRequest
/// - Login com credenciais válidas retorna 200 OK com token JWT
/// - Login com senha errada retorna 401 Unauthorized
/// </summary>
public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(DisplayName = "TC01.1 - Registro com dados válidos deve retornar 200 OK")]
    public async Task Register_ComDadosValidos_DeveRetornar200()
    {
        // Arrange
        var payload = new
        {
            nome = "João Teste",
            email = "joao@teste.com",
            senha = "Senha@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body);
        Assert.Contains("sucesso", body!["message"].ToLower());
    }

    [Fact(DisplayName = "TC01.2 - Registro com e-mail duplicado deve retornar 400 BadRequest")]
    public async Task Register_ComEmailDuplicado_DeveRetornar400()
    {
        // Arrange — registra o usuário uma primeira vez
        var payload = new
        {
            nome = "Maria Duplicada",
            email = "duplicado@teste.com",
            senha = "Senha@123"
        };
        await _client.PostAsJsonAsync("/api/auth/register", payload);

        // Act — tenta registrar novamente com o mesmo e-mail
        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "TC01.3 - Login com credenciais válidas deve retornar token JWT")]
    public async Task Login_ComCredenciaisValidas_DeveRetornarToken()
    {
        // Arrange — cria usuário antes de logar
        var email = "login@teste.com";
        var senha = "Senha@123";

        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            nome = "Login User",
            email,
            senha
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, senha });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(body);
        Assert.True(body!.ContainsKey("token"), "Resposta deve conter o campo 'token'");
        Assert.False(string.IsNullOrWhiteSpace(body["token"]?.ToString()));
    }

    [Fact(DisplayName = "TC01.4 - Login com senha errada deve retornar 401 Unauthorized")]
    public async Task Login_ComSenhaErrada_DeveRetornar401()
    {
        // Arrange
        var email = "senhaerrada@teste.com";
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            nome = "Senha Errada",
            email,
            senha = "SenhaCorreta@123"
        });

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            senha = "SenhaErrada@999"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
