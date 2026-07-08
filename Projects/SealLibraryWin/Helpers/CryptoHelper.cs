//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using QuestPDF.Infrastructure;
using Seal.Model;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Seal.Helpers
{
    public class CryptoHelper
    {
        public const string AESLicenseKey = "sjedk*+$àWE¨€*2*%ssddè";
        public const string RSALicensePublicKey = "<RSAKeyValue><Modulus>tOiIckJ3yCHhEHuGFmz2OrumK/GYc49bQNzBrc2aTvQSUWynKD3BiaYlP0biQrCCHxjYcKHvuEcmIug6OELsnQasrId/FzXXtGNH7UnYoZHqOI9xUW57Ycd4eg7VEv8kBPP/q+6YdS2BhTav1JApDgKluGbRpfZtuCCCsIZmhw0=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        public static string Hash(string text, string salt)
        {
            var bSalt = Encoding.GetEncoding(1252).GetBytes(salt + "47042ebf6b91akdjrdskjwk34dkf3241aa59ceca119ad").Take(128 / 8).ToArray();

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: text,
            salt: bSalt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));

            return hashed;
        }

        private static byte[] AESIV = { 22, 32, 44, 21, 34, 65, 98, 19, 11, 13, 46, 24, 36, 56, 96, 10 };

        public static string EncryptAES(string text, string key)
        {
            string result = "";
            if (string.IsNullOrEmpty(text)) return result;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.GetEncoding(1252).GetBytes(key + "akdjrdskeporndjwk34dkf3241aa59ceca119ad47042ebf6b91").Take(aesAlg.KeySize / 8).ToArray();
                aesAlg.IV = AESIV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(text);
                        }
                        result = Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }

            return result;
        }

        public static string DecryptAES(string text, string key)
        {
            string result = "";
            if (string.IsNullOrEmpty(text)) return result;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.GetEncoding(1252).GetBytes(key + "akdjrdskeporndjwk34dkf3241aa59ceca119ad47042ebf6b91").Take(aesAlg.KeySize / 8).ToArray();
                aesAlg.IV = AESIV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(text)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            result = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return result;
        }

        private static string TripleDESVector = "qOiä+-?$"; //8 chars

        static TripleDES getCryptoServiceProvider(string key)
        {
            TripleDES crypto = TripleDES.Create();
            crypto.Key = Encoding.GetEncoding(1252).GetBytes(key + "123456789").Take(crypto.KeySize / 8).ToArray();
            crypto.IV = Encoding.GetEncoding(1252).GetBytes(TripleDESVector + "123456789").Take(crypto.BlockSize / 8).ToArray();
            return crypto;
        }

        public static string EncryptTripleDES(string text, string key)
        {
            //Expect an ANSI key of 24 chars...
            string result = "";
            if (string.IsNullOrEmpty(text)) return result;

            var crypto = getCryptoServiceProvider(key);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, crypto.CreateEncryptor(), CryptoStreamMode.Write);
            StreamWriter sw = new StreamWriter(cs);
            sw.Write(text);
            sw.Close();
            cs.Close();
            result = Convert.ToBase64String(ms.ToArray());

            return result;
        }

        public static string DecryptTripleDES(string text, string key)
        {
            string result = "";
            if (string.IsNullOrEmpty(text)) return result;

            var crypto = getCryptoServiceProvider(key);
            MemoryStream ms = new MemoryStream(System.Convert.FromBase64String(text));
            CryptoStream cs = new CryptoStream(ms, crypto.CreateDecryptor(), CryptoStreamMode.Read);
            StreamReader sr = new StreamReader(cs);
            result = sr.ReadToEnd();
            sr.Close();
            cs.Close();
            return result;
        }


        //RSA Container base helpers
        public static string EncryptWithRSAContainer(string text, string containerName, bool useMachineKeyStore)
        {
            CspParameters csp = new CspParameters();
            csp.KeyContainerName = containerName;
            if (useMachineKeyStore) csp.Flags = CspProviderFlags.UseMachineKeyStore;
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(csp);
            rsa.PersistKeyInCsp = true;
            //RSA-OAEP(SHA-1) can only encrypt KeySize/8 - 42 bytes at once: process by blocks
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            int maxBlockSize = rsa.KeySize / 8 - 42;
            using (var result = new MemoryStream())
            {
                int offset = 0;
                do
                {
                    int count = Math.Min(maxBlockSize, bytes.Length - offset);
                    byte[] block = new byte[count];
                    Array.Copy(bytes, offset, block, 0, count);
                    byte[] encryptedBlock = rsa.Encrypt(block, true);
                    result.Write(encryptedBlock, 0, encryptedBlock.Length);
                    offset += count;
                }
                while (offset < bytes.Length);
                return Convert.ToBase64String(result.ToArray());
            }
        }

        public static string DecryptWithRSAContainer(string text, string containerName, bool useMachineKeyStore)

        {
            CspParameters csp = new CspParameters();
            csp.KeyContainerName = containerName;
            if (useMachineKeyStore) csp.Flags = CspProviderFlags.UseMachineKeyStore;
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(csp);
            rsa.PersistKeyInCsp = true;

            //Decrypt by blocks of KeySize/8 bytes (single block for values encrypted before the block support)
            byte[] bytes = Convert.FromBase64String(text);
            int blockSize = rsa.KeySize / 8;
            using (var result = new MemoryStream())
            {
                for (int offset = 0; offset < bytes.Length; offset += blockSize)
                {
                    int count = Math.Min(blockSize, bytes.Length - offset);
                    byte[] block = new byte[count];
                    Array.Copy(bytes, offset, block, 0, count);
                    byte[] decryptedBlock = rsa.Decrypt(block, true);
                    result.Write(decryptedBlock, 0, decryptedBlock.Length);
                }
                return Encoding.UTF8.GetString(result.ToArray());
            }
        }


        public static string RSAEncrypt(string publicKey, string textToEncrypt)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKey);

                byte[] plainTextBytes = Encoding.UTF8.GetBytes(textToEncrypt);
                byte[] cipherTextBytes = rsa.Encrypt(plainTextBytes, false);

                return Convert.ToBase64String(cipherTextBytes);
            }
        }

        public static string RSADecrypt(string privateKey, string textToDecrypt)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKey);

                byte[] cipherTextBytes = Convert.FromBase64String(textToDecrypt);
                byte[] decryptedBytes = rsa.Decrypt(cipherTextBytes, false);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }

        public static string RSASignData(string privateKey, string data)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKey);

                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] signatureBytes = rsa.SignData(dataBytes, SHA256.Create());

                return Convert.ToBase64String(signatureBytes);
            }
        }

        public static bool RSAVerifySignature(string publicKey, string data, string signature)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKey);

                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] signatureBytes = Convert.FromBase64String(signature);

                return rsa.VerifyData(dataBytes, SHA256.Create(), signatureBytes);
            }
        }
    }
}
