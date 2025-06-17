using System;
using System.Security.Cryptography;
using System.Text;

namespace TextToImageCrypto.Services.Cryptography;
// шифрование / дешифровка текста
public class AesGcmService : IDisposable
{
    private readonly AesGcm _aesGcm;

    public AesGcmService(byte[] key)
    {
        if (key.Length != 32)
            throw new ArgumentException("Ключ должен быть 256-bit (32 b)");
        
        _aesGcm = new AesGcm(key);
    }
    public (byte[] Ciphertext, byte[] Nonce, byte[] Tag) Encrypt(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; 
        RandomNumberGenerator.Fill(nonce);
        
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; 
        
        _aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        return (ciphertext, nonce, tag);
    }
    
    public string Decrypt(byte[] ciphertext, byte[] nonce, byte[] tag)
    {
        var plaintextBytes = new byte[ciphertext.Length];
        _aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);
        return Encoding.UTF8.GetString(plaintextBytes);
    }

    public void Dispose() => _aesGcm.Dispose();
    
}