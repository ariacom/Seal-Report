//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Seal.Helpers
{
    public class CryptoHelper
    {
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
            byte[] encryptedAsBytes = rsa.Encrypt(Encoding.UTF8.GetBytes(text), true);
            string encryptedAsBase64 = Convert.ToBase64String(encryptedAsBytes);
            return encryptedAsBase64;
        }

        public static string DecryptWithRSAContainer(string text, string containerName, bool useMachineKeyStore)

        {
            CspParameters csp = new CspParameters();
            csp.KeyContainerName = containerName;
            if (useMachineKeyStore) csp.Flags = CspProviderFlags.UseMachineKeyStore;
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(csp);
            rsa.PersistKeyInCsp = true;
            
            byte[] decryptBytes = rsa.Decrypt(Convert.FromBase64String(text), true);
            string secretMessage = Encoding.Default.GetString(decryptBytes);
            return secretMessage;
        }
    }
}
