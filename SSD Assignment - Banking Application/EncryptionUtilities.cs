using Banking_Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Banking_Application
{
    internal sealed class EncryptionUtilities
    {
        internal readonly Aes aes;
        private readonly HMACSHA256 hmac;
        private const string CryptoKeyName = "BankingAppKey";

        public EncryptionUtilities()
        {
            CngProvider keyStorageProvider = CngProvider.MicrosoftSoftwareKeyStorageProvider;

            // Check if the key exists; if not, create it
            if (!CngKey.Exists(CryptoKeyName, keyStorageProvider))
            {
                CngKeyCreationParameters keyCreationParameters = new CngKeyCreationParameters
                {
                    Provider = keyStorageProvider
                };

                // Create the named key
                CngKey.Create(new CngAlgorithm("AES"), CryptoKeyName, keyCreationParameters);
            }

            aes = new AesCng(CryptoKeyName, keyStorageProvider)
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };

            byte[] aesKey = aes.Key;
            byte[] hmacKey = DeriveKey(aesKey, "HMAC");

            hmac = new HMACSHA256(hmacKey);
        }

        private byte[] DeriveKey(byte[] masterKey, string purpose)
        {
            using (var hmac = new HMACSHA256(masterKey))
            {
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(purpose));
            }
        }

        public byte[] Encrypt(string plaintextData)
        {
            byte[] byteString = Encoding.UTF8.GetBytes(plaintextData);

            byte[] iv = new byte[aes.BlockSize / 8]; 
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(iv); 
            }

            using var encryptor = aes.CreateEncryptor(aes.Key, iv);
            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                csEncrypt.Write(byteString, 0, plaintextData.Length);
            }

            byte[] ciphertextData = msEncrypt.ToArray();
       
            byte[] combinedOutput = new byte[iv.Length + ciphertextData.Length];
            Buffer.BlockCopy(iv, 0, combinedOutput, 0, iv.Length);
            Buffer.BlockCopy(ciphertextData, 0, combinedOutput, iv.Length, ciphertextData.Length); 

            return combinedOutput; 
        }


        public byte[] Decrypt(byte[] combinedData)
        {
            // Separate IV and ciphertext
            int ivLength = aes.BlockSize / 8;
            byte[] iv = new byte[ivLength];
            byte[] ciphertextData = new byte[combinedData.Length - ivLength];

            Buffer.BlockCopy(combinedData, 0, iv, 0, ivLength); 
            Console.WriteLine(iv);
            Buffer.BlockCopy(combinedData, ivLength, ciphertextData, 0, ciphertextData.Length); 

            // Decrypt the ciphertext using the extracted IV
            using var decryptor = aes.CreateDecryptor(aes.Key, iv); // Use AES with the key and IV
            using var msDecrypt = new MemoryStream();
            using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
            {
                csDecrypt.Write(ciphertextData, 0, ciphertextData.Length);
            }

            return msDecrypt.ToArray(); // Return the decrypted data
        }


        public byte[] ComputeHash(byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException(nameof(inputData));

            return hmac.ComputeHash(inputData);
        }

        public byte[] ComputeHash(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            byte[] inputData = Encoding.UTF8.GetBytes(input);
            return ComputeHash(inputData);
        }


        public bool CompareHashes(byte[] hash1, byte[] hash2)
        {
            if (hash1.Length != hash2.Length) return false;
            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i]) return false;
            }
            return true;
        }

        public byte[] CombineByteArrays(params byte[][] arrays)
        {
            int totalLength = arrays.Where(a => a != null).Sum(a => a.Length);
            byte[] combined = new byte[totalLength];

            int offset = 0;
            foreach (var array in arrays)
            {
                if (array != null)
                {
                    Buffer.BlockCopy(array, 0, combined, offset, array.Length);
                    offset += array.Length;
                }
            }

            return combined;
        }

        public byte[] GenerateHashForSendingToDB(Bank_Account account)
        {
            // Compute the hash value directly using plaintext fields
            byte[] updatedHashValue = ComputeHash(
                CombineByteArrays(
                    Encoding.UTF8.GetBytes(account.accountNo),
                    Encoding.UTF8.GetBytes(account.name),
                    account.address_line_1 != null ? Encoding.UTF8.GetBytes(account.address_line_1) : new byte[0],
                    account.address_line_2 != null ? Encoding.UTF8.GetBytes(account.address_line_2) : new byte[0],
                    account.address_line_3 != null ? Encoding.UTF8.GetBytes(account.address_line_3) : new byte[0],
                    Encoding.UTF8.GetBytes(account.town),
                    BitConverter.GetBytes(account.balance),
                    BitConverter.GetBytes(account.GetType() == typeof(Current_Account) ? 1 : 2),
                    BitConverter.GetBytes((account is Current_Account ca) ? ca.overdraftAmount : 0.0), 
                    BitConverter.GetBytes((account is Savings_Account sa) ? sa.interestRate : 0.0),
                    ComputeHash(account.accountNo) 
                )
            );
            return updatedHashValue;
        }

    }
}