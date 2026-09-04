using GestorPDV.Application.Common;

namespace GestorPDV.Application.Seguranca;

public interface IAutenticacaoService
{
    Task<Result<SessaoUsuario>> AutenticarAsync(
        string login, string senha, CancellationToken cancellationToken = default);

    Task<Result> AlterarSenhaAsync(
        long usuarioId, string senhaAtual, string novaSenha, CancellationToken cancellationToken = default);
}
