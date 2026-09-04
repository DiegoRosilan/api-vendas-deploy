using GestorPDV.Application.Common;

namespace GestorPDV.Application.Seguranca;

// Caso de uso de login/troca de senha (RN-SEG-001). Depende apenas das
// abstrações IUsuarioRepository/IPasswordHasher — a implementação concreta
// de persistência/hash fica em GestorPDV.Data.Postgres/GestorPDV.Infrastructure.
public class AutenticacaoService : IAutenticacaoService
{
    private const int TamanhoMinimoSenha = 6;

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AutenticacaoService(IUsuarioRepository usuarioRepository, IPasswordHasher passwordHasher)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<SessaoUsuario>> AutenticarAsync(
        string login, string senha, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorLoginAsync(login, cancellationToken);
        if (usuario is null || !_passwordHasher.Verifica(senha, usuario.SenhaHash))
        {
            return Result<SessaoUsuario>.Falha("Usuário ou senha inválidos.");
        }

        if (!usuario.Ativo)
        {
            return Result<SessaoUsuario>.Falha("Usuário inativo. Procure o administrador do sistema.");
        }

        if (usuario.Bloqueado)
        {
            return Result<SessaoUsuario>.Falha("Usuário bloqueado. Procure o administrador do sistema.");
        }

        var permissoes = await _usuarioRepository.ObterCodigosPermissaoAsync(usuario.Id, cancellationToken);
        await _usuarioRepository.AtualizarUltimoAcessoAsync(usuario.Id, DateTimeOffset.Now, cancellationToken);

        var sessao = new SessaoUsuario
        {
            UsuarioId = usuario.Id,
            Login = usuario.Login,
            Nome = usuario.Nome,
            PerfilId = usuario.PerfilId,
            FilialId = usuario.FilialId,
            ExigeTrocaSenha = usuario.ExigeTrocaSenha,
            Permissoes = permissoes
        };

        return Result<SessaoUsuario>.Ok(sessao);
    }

    public async Task<Result> AlterarSenhaAsync(
        long usuarioId, string senhaAtual, string novaSenha, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
        {
            return Result.Falha("Usuário não encontrado.");
        }

        if (!_passwordHasher.Verifica(senhaAtual, usuario.SenhaHash))
        {
            return Result.Falha("Senha atual incorreta.");
        }

        if (string.IsNullOrWhiteSpace(novaSenha) || novaSenha.Length < TamanhoMinimoSenha)
        {
            return Result.Falha($"A nova senha deve ter ao menos {TamanhoMinimoSenha} caracteres.");
        }

        var novoHash = _passwordHasher.Hash(novaSenha);
        await _usuarioRepository.AtualizarSenhaAsync(usuarioId, novoHash, exigeTrocaSenha: false, cancellationToken);

        return Result.Ok();
    }
}
