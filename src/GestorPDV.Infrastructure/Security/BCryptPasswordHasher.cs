using GestorPDV.Application.Seguranca;

namespace GestorPDV.Infrastructure.Security;

// Compatível com os hashes bcrypt gerados por crypt(senha, gen_salt('bf'))
// no seed do banco (database/seed/seed_inicial.sql).
public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha);

    public bool Verifica(string senha, string hash) => BCrypt.Net.BCrypt.Verify(senha, hash);
}
