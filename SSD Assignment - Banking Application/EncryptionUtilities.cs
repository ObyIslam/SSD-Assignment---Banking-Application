using Banking_Application;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Banking_Application
{
    internal sealed class EncryptionUtilities
    {
        internal readonly Aes aes;

        private static readonly byte[] masterKey =
            Encoding.UTF8.GetBytes("OBYS_SECRET_KEY_32B!");

        public EncryptionUtilities()
        {
            aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Derive AES key from master key
            aes.Key = SHA256.HashData(masterKey);
        }

        public byte[] Encrypt(string plaintextData)
        {
            byte[] byteString = Encoding.UTF8.GetBytes(plaintextData);

            // Generate random IV
            byte[] iv = new byte[aes.BlockSize / 8];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(iv);
            }

            using var encryptor = aes.CreateEncryptor(aes.Key, iv);
            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                csEncrypt.Write(byteString, 0, byteString.Length);
            }

            byte[] ciphertextData = msEncrypt.ToArray();

            // Combine IV + ciphertext
            byte[] combinedOutput = new byte[iv.Length + ciphertextData.Length];
            Buffer.BlockCopy(iv, 0, combinedOutput, 0, iv.Length);
            Buffer.BlockCopy(ciphertextData, 0, combinedOutput, iv.Length, ciphertextData.Length);

            return combinedOutput;
        }

        public byte[] Decrypt(byte[] combinedData)
        {
            int ivLength = aes.BlockSize / 8;

            byte[] iv = new byte[ivLength];
            byte[] ciphertextData = new byte[combinedData.Length - ivLength];

            Buffer.BlockCopy(combinedData, 0, iv, 0, ivLength);
            Buffer.BlockCopy(combinedData, ivLength, ciphertextData, 0, ciphertextData.Length);

            using var decryptor = aes.CreateDecryptor(aes.Key, iv);
            using var msDecrypt = new MemoryStream();
            using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
            {
                csDecrypt.Write(ciphertextData, 0, ciphertextData.Length);
            }

            return msDecrypt.ToArray();
        }
    }
}
