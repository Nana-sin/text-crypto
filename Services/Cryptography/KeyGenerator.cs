using System.Security.Cryptography;
using System.Text;
// класс генерации ключа
namespace TextToImageCrypto.Services.Cryptography;

public class KeyGenerator
{
    public static byte[] DeriveKeyFromPassword(string password, byte[] salt)
    {
        using var deriveBytes = new Rfc2898DeriveBytes(
            password: Encoding.UTF8.GetBytes(password),
            salt: salt,
            // 600000 итераций для защиты от брутфорса
            iterations: 600000,
            hashAlgorithm: HashAlgorithmName.SHA512);
        
        return deriveBytes.GetBytes(32); // 256-bit key
    }

    public static byte[] GenerateRandomSalt() 
    {
        var salt = new byte[16]; 
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }
}