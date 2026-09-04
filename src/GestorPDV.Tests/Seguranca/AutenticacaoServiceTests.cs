using GestorPDV.Application.Seguranca;
using GestorPDV.Domain.Seguranca;
using Xunit;

namespace GestorPDV.Tests.Seguranca;

// Dublês de teste simples (sem banco/hash real) para exercitar as regras de
// RN-SEG-001 isoladamente: usuário inativo/bloqueado, senha incorreta,
// permissões efetivas e troca de senha.
file class UsuarioRepositoryFake : IUsuarioRepository
{
    public readonly Dictionary<string, Usuario> UsuariosPorLogin = new(StringComparer.OrdinalIgnoreCase);
    public readonly Dictionary<long, List<string>> PermissoesPorUsuario = new();
    public DateTimeOffset? UltimoAcessoRegistrado { get; private set; }
    public (long UsuarioId, string Hash, bool ExigeTrocaSenha)? UltimaAtualizacaoSenha { get; private set; }

    public Task<Usuario?> ObterPorLoginAsync(string login, CancellationToken cancellationToken = default) =>
        Task.FromResult(UsuariosPorLogin.GetValueOrDefault(login));

    public Task<Usuario?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(UsuariosPorLogin.Values.FirstOrDefault(u => u.Id == id));

    public Task<IReadOnlyList<string>> ObterCodigosPermissaoAsync(
        long usuarioId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(PermissoesPorUsuario.GetValueOrDefault(usuarioId) ?? new List<string>());

    public Task AtualizarUltimoAcessoAsync(
        long usuarioId, DateTimeOffset dataAcesso, CancellationToken cancellationToken = default)
    {
        UltimoAcessoRegistrado = dataAcesso;
        return Task.CompletedTask;
    }

    public Task AtualizarSenhaAsync(
        long usuarioId, string novaSenhaHash, bool exigeTrocaSenha, CancellationToken cancellationToken = default)
    {
        UltimaAtualizacaoSenha = (usuarioId, novaSenhaHash, exigeTrocaSenha);
        var usuario = UsuariosPorLogin.Values.First(u => u.Id == usuarioId);
        usuario.SenhaHash = novaSenhaHash;
        usuario.ExigeTrocaSenha = exigeTrocaSenha;
        return Task.CompletedTask;
    }
}

file class PasswordHasherFake : IPasswordHasher
{
    public string Hash(string senha) => $"HASH:{senha}";

    public bool Verifica(string senha, string hash) => hash == Hash(senha);
}

public class AutenticacaoServiceTests
{
    private static (AutenticacaoService Servico, UsuarioRepositoryFake Repositorio) CriarServico()
    {
        var repositorio = new UsuarioRepositoryFake();
        var hasher = new PasswordHasherFake();

        repositorio.UsuariosPorLogin["admin"] = new Usuario
        {
            Id = 1,
            Login = "admin",
            SenhaHash = hasher.Hash("admin123"),
            Nome = "Administrador",
            Ativo = true,
            Bloqueado = false,
            ExigeTrocaSenha = true
        };
        repositorio.PermissoesPorUsuario[1] = new List<string> { "CAIXA_ABRIR", "VENDA_INCLUIR" };

        repositorio.UsuariosPorLogin["inativo"] = new Usuario
        {
            Id = 2,
            Login = "inativo",
            SenhaHash = hasher.Hash("senha123"),
            Nome = "Usuário Inativo",
            Ativo = false
        };

        repositorio.UsuariosPorLogin["bloqueado"] = new Usuario
        {
            Id = 3,
            Login = "bloqueado",
            SenhaHash = hasher.Hash("senha123"),
            Nome = "Usuário Bloqueado",
            Ativo = true,
            Bloqueado = true
        };

        return (new AutenticacaoService(repositorio, hasher), repositorio);
    }

    [Fact]
    public async Task Autenticar_ComCredenciaisValidas_DeveRetornarSessaoComPermissoes()
    {
        var (servico, repositorio) = CriarServico();

        var resultado = await servico.AutenticarAsync("admin", "admin123");

        Assert.True(resultado.Sucesso);
        Assert.NotNull(resultado.Valor);
        Assert.Equal("Administrador", resultado.Valor!.Nome);
        Assert.True(resultado.Valor.ExigeTrocaSenha);
        Assert.Contains("VENDA_INCLUIR", resultado.Valor.Permissoes);
        Assert.True(resultado.Valor.TemPermissao("caixa_abrir"));
        Assert.NotNull(repositorio.UltimoAcessoRegistrado);
    }

    [Fact]
    public async Task Autenticar_ComSenhaIncorreta_DeveFalhar()
    {
        var (servico, _) = CriarServico();

        var resultado = await servico.AutenticarAsync("admin", "senha-errada");

        Assert.False(resultado.Sucesso);
        Assert.Null(resultado.Valor);
    }

    [Fact]
    public async Task Autenticar_ComUsuarioInativo_DeveFalhar()
    {
        var (servico, _) = CriarServico();

        var resultado = await servico.AutenticarAsync("inativo", "senha123");

        Assert.False(resultado.Sucesso);
        Assert.Contains("inativo", resultado.Erro, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Autenticar_ComUsuarioBloqueado_DeveFalhar()
    {
        var (servico, _) = CriarServico();

        var resultado = await servico.AutenticarAsync("bloqueado", "senha123");

        Assert.False(resultado.Sucesso);
        Assert.Contains("bloqueado", resultado.Erro, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AlterarSenha_ComSenhaAtualIncorreta_DeveFalhar()
    {
        var (servico, _) = CriarServico();

        var resultado = await servico.AlterarSenhaAsync(1, "senha-errada", "novaSenha123");

        Assert.False(resultado.Sucesso);
    }

    [Fact]
    public async Task AlterarSenha_ComDadosValidos_DeveAtualizarHashERemoverExigeTrocaSenha()
    {
        var (servico, repositorio) = CriarServico();

        var resultado = await servico.AlterarSenhaAsync(1, "admin123", "novaSenha123");

        Assert.True(resultado.Sucesso);
        Assert.NotNull(repositorio.UltimaAtualizacaoSenha);
        Assert.False(repositorio.UltimaAtualizacaoSenha!.Value.ExigeTrocaSenha);

        var loginComSenhaAntiga = await servico.AutenticarAsync("admin", "admin123");
        Assert.False(loginComSenhaAntiga.Sucesso);

        var loginComSenhaNova = await servico.AutenticarAsync("admin", "novaSenha123");
        Assert.True(loginComSenhaNova.Sucesso);
    }

    [Fact]
    public async Task AlterarSenha_ComSenhaCurta_DeveFalhar()
    {
        var (servico, _) = CriarServico();

        var resultado = await servico.AlterarSenhaAsync(1, "admin123", "123");

        Assert.False(resultado.Sucesso);
    }
}
