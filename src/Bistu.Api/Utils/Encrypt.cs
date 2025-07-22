using System;
using System.Collections.Generic;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Bistu.Api.Utils;

internal static class Encrypt
{
    public static string Password(string plain, string salt)
    { // CBCEncrypt AES/CBC/PKCS7Padding 加密
        SecureRandom random = new();
        byte[] iv = new byte[16]; // AES 块大小 = 16字节
        random.NextBytes(iv);
        var aes = new AesEngine();
        var cbc = new CbcBlockCipher(aes);
        var paddedCipher = new PaddedBufferedBlockCipher(cbc, new Pkcs7Padding());
        paddedCipher.Init(
            true,
            new ParametersWithIV(new KeyParameter(System.Text.Encoding.UTF8.GetBytes(salt)), iv)
        );
        byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(plain);
        byte[] outputBytes = new byte[paddedCipher.GetOutputSize(inputBytes.Length)];
        int length = paddedCipher.ProcessBytes(inputBytes, 0, inputBytes.Length, outputBytes, 0);
        length += paddedCipher.DoFinal(outputBytes, length);
        var encryptedPassword = Convert.ToBase64String(outputBytes, 0, length);
        return encryptedPassword;
    }
}
