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
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Seal.Helpers
{
    /// <summary>
    /// Static helper methods for hashing, AES, Triple DES and RSA encryption, available in Razor scripts
    /// </summary>
    public class CryptoHelper
    {
        /// <summary>
        /// AES key used to decrypt license files
        /// </summary>
        public const string AESLicenseKey = "sjedk*+$àWE¨€*2*%ssddè";
        /// <summary>
        /// RSA public key used to verify license file signatures
        /// </summary>
        public const string RSALicensePublicKey = "<RSAKeyValue><Modulus>tOiIckJ3yCHhEHuGFmz2OrumK/GYc49bQNzBrc2aTvQSUWynKD3BiaYlP0biQrCCHxjYcKHvuEcmIug6OELsnQasrId/FzXXtGNH7UnYoZHqOI9xUW57Ycd4eg7VEv8kBPP/q+6YdS2BhTav1JApDgKluGbRpfZtuCCCsIZmhw0=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        /// <summary>
        /// Hash a text with a salt using PBKDF2 (HMACSHA256, 100000 iterations), returned as a base64 string
        /// </summary>
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

        /// <summary>
        /// Encrypt a text with a key using AES, returned as a base64 string
        /// </summary>
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

        /// <summary>
        /// Decrypt a base64 text with a key using AES
        /// </summary>
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

        /// <summary>
        /// Encrypt a text with a key using Triple DES, returned as a base64 string
        /// </summary>
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

        /// <summary>
        /// Decrypt a base64 text with a key using Triple DES
        /// </summary>
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
        /// <summary>
        /// Encrypt a text using the RSA key pair stored in a key container, returned as a base64 string
        /// </summary>
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

        /// <summary>
        /// Decrypt a base64 text using the RSA key pair stored in a key container
        /// </summary>
        public static string DecryptWithRSAContainer(string text, string containerName, bool useMachineKeyStore)
        {
            CspParameters csp = new CspParameters();
            csp.KeyContainerName = containerName;
            //Existing key only: opening a missing container must not create a new empty key (the decryption would fail with an unclear error)
            csp.Flags = CspProviderFlags.UseExistingKey | (useMachineKeyStore ? CspProviderFlags.UseMachineKeyStore : CspProviderFlags.NoFlags);
            RSACryptoServiceProvider rsa;
            try
            {
                rsa = new RSACryptoServiceProvider(csp);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException($"The {(useMachineKeyStore ? "machine" : "user")} RSA key container '{containerName}' does not exist or cannot be read by the account '{Environment.UserDomainName}\\{Environment.UserName}': {ex.Message}", ex);
            }

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

        /// <summary>
        /// Prefix of a value encrypted with a certificate (see <see cref="EncryptWithCertificate"/>)
        /// </summary>
        public const string CertificatePrefix = "cert1:";

        const byte CertificateFormatVersion = 1;
        const int CertificateThumbprintLength = 20, CertificateNonceLength = 12, CertificateTagLength = 16;

        /// <summary>
        /// True if the value has been encrypted with a certificate
        /// </summary>
        public static bool IsCertificateValue(string value)
        {
            return value != null && value.StartsWith(CertificatePrefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Encrypt a text with the public key of a certificate: the text is encrypted with a random AES-256-GCM key, itself encrypted with the RSA key of the certificate (OAEP SHA-256).
        /// The result is 'cert1:' followed by the base64 of: version, SHA-1 thumbprint of the certificate, length and value of the encrypted key, nonce, tag and encrypted text.
        /// The key name is authenticated with the value: a value cannot be moved to another kind of secret. Only the public key is required.
        /// </summary>
        public static string EncryptWithCertificate(string text, string keyName, X509Certificate2 certificate)
        {
            using var rsa = certificate.GetRSAPublicKey();
            if (rsa == null) throw new CryptographicException($"The certificate '{certificate.Subject}' ({certificate.Thumbprint}) has no RSA key.");

            byte[] key = RandomNumberGenerator.GetBytes(32);
            byte[] nonce = RandomNumberGenerator.GetBytes(CertificateNonceLength);
            byte[] plain = Encoding.UTF8.GetBytes(text ?? "");
            byte[] cipher = new byte[plain.Length];
            byte[] tag = new byte[CertificateTagLength];
            byte[] thumbprint = Convert.FromHexString(certificate.Thumbprint);
            using (var aes = new AesGcm(key, CertificateTagLength))
            {
                aes.Encrypt(nonce, plain, cipher, tag, getCertificateAssociatedData(keyName, thumbprint));
            }
            byte[] wrappedKey = rsa.Encrypt(key, RSAEncryptionPadding.OaepSHA256);
            CryptographicOperations.ZeroMemory(key);

            using var result = new MemoryStream();
            result.WriteByte(CertificateFormatVersion);
            result.Write(thumbprint, 0, thumbprint.Length);
            result.WriteByte((byte)(wrappedKey.Length >> 8));
            result.WriteByte((byte)(wrappedKey.Length & 0xFF));
            result.Write(wrappedKey, 0, wrappedKey.Length);
            result.Write(nonce, 0, nonce.Length);
            result.Write(tag, 0, tag.Length);
            result.Write(cipher, 0, cipher.Length);
            return CertificatePrefix + Convert.ToBase64String(result.ToArray());
        }

        /// <summary>
        /// Thumbprint (upper case hexadecimal) of the certificate used to encrypt a value, null if the value is not encrypted with a certificate
        /// </summary>
        public static string GetCertificateThumbprint(string value)
        {
            if (!IsCertificateValue(value)) return null;
            var bytes = Convert.FromBase64String(value.Substring(CertificatePrefix.Length));
            if (bytes.Length < 1 + CertificateThumbprintLength || bytes[0] != CertificateFormatVersion) throw new CryptographicException("Invalid certificate encrypted value.");
            return Convert.ToHexString(bytes, 1, CertificateThumbprintLength);
        }

        /// <summary>
        /// Decrypt a value encrypted by <see cref="EncryptWithCertificate"/> with the private key of the certificate
        /// </summary>
        public static string DecryptWithCertificate(string value, string keyName, X509Certificate2 certificate)
        {
            var bytes = Convert.FromBase64String(value.Substring(CertificatePrefix.Length));
            int pos = 0;
            if (bytes.Length < 1 + CertificateThumbprintLength + 2 || bytes[pos++] != CertificateFormatVersion) throw new CryptographicException("Invalid certificate encrypted value.");
            byte[] thumbprint = bytes.AsSpan(pos, CertificateThumbprintLength).ToArray();
            pos += CertificateThumbprintLength;
            if (!Convert.ToHexString(thumbprint).Equals(certificate.Thumbprint, StringComparison.OrdinalIgnoreCase)) throw new CryptographicException($"The value has been encrypted with the certificate {Convert.ToHexString(thumbprint)}, not with '{certificate.Subject}' ({certificate.Thumbprint}).");
            int wrappedLength = (bytes[pos] << 8) | bytes[pos + 1];
            pos += 2;
            if (bytes.Length < pos + wrappedLength + CertificateNonceLength + CertificateTagLength) throw new CryptographicException("Invalid certificate encrypted value.");
            byte[] wrappedKey = bytes.AsSpan(pos, wrappedLength).ToArray();
            pos += wrappedLength;
            byte[] nonce = bytes.AsSpan(pos, CertificateNonceLength).ToArray();
            pos += CertificateNonceLength;
            byte[] tag = bytes.AsSpan(pos, CertificateTagLength).ToArray();
            pos += CertificateTagLength;
            byte[] cipher = bytes.AsSpan(pos).ToArray();

            RSA rsa;
            try
            {
                rsa = certificate.GetRSAPrivateKey();
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException($"The private key of the certificate '{certificate.Subject}' ({certificate.Thumbprint}) cannot be read by the account '{Environment.UserDomainName}\\{Environment.UserName}': {ex.Message}", ex);
            }
            if (rsa == null) throw new CryptographicException($"The certificate '{certificate.Subject}' ({certificate.Thumbprint}) has no private key available for the account '{Environment.UserDomainName}\\{Environment.UserName}'.");

            byte[] key = null;
            try
            {
                using (rsa)
                {
                    key = rsa.Decrypt(wrappedKey, RSAEncryptionPadding.OaepSHA256);
                }
                byte[] plain = new byte[cipher.Length];
                using (var aes = new AesGcm(key, CertificateTagLength))
                {
                    aes.Decrypt(nonce, cipher, tag, plain, getCertificateAssociatedData(keyName, thumbprint));
                }
                return Encoding.UTF8.GetString(plain);
            }
            finally
            {
                if (key != null) CryptographicOperations.ZeroMemory(key);
            }
        }

        static byte[] getCertificateAssociatedData(string keyName, byte[] thumbprint)
        {
            return Encoding.UTF8.GetBytes((keyName ?? "") + "|" + Convert.ToHexString(thumbprint));
        }


        /// <summary>
        /// Encrypt a text with an RSA public key (XML format), returned as a base64 string
        /// </summary>
        public static string RSAEncrypt(string publicKey, string textToEncrypt)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKey);

                byte[] plainTextBytes = Encoding.UTF8.GetBytes(textToEncrypt);
                //Use OAEP padding (true) instead of the weaker PKCS#1 v1.5 (false); paired with RSADecrypt below
                byte[] cipherTextBytes = rsa.Encrypt(plainTextBytes, true);

                return Convert.ToBase64String(cipherTextBytes);
            }
        }

        /// <summary>
        /// Decrypt a base64 text with an RSA private key (XML format)
        /// </summary>
        public static string RSADecrypt(string privateKey, string textToDecrypt)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKey);

                byte[] cipherTextBytes = Convert.FromBase64String(textToDecrypt);
                //Use OAEP padding (true) to match RSAEncrypt above
                byte[] decryptedBytes = rsa.Decrypt(cipherTextBytes, true);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }

        /// <summary>
        /// Sign a data string with an RSA private key (XML format) using SHA256, returned as a base64 string
        /// </summary>
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

        /// <summary>
        /// Verify the RSA signature of a data string using a public key (XML format) and SHA256
        /// </summary>
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
