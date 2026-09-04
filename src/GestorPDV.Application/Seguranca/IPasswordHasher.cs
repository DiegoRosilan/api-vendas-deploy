namespace GestorPDV.Application.Seguranca;

// Implementado em GestorPDV.Infrastructure.Security (BCrypt), compatível com
// os hashes gerados pela extensão pgcrypto usada no seed do banco.
public interface IPasswordHasher
{
    string Hash(string senha);
    bool Verifica(string senha, string hash);
}
