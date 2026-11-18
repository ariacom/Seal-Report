//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Helpers;
using Seal.Model;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Test
{
    /// <summary>
    /// Encryption mode Certificate of the server configuration: values encrypted with the public key of a certificate (PFX or PEM file),
    /// decrypted with its private key, previous certificates for a rotation, legacy values still readable and re-encryption of the secrets of a file
    /// </summary>
    [TestClass]
    public class TestCertificateEncryption
    {
        const string PasswordVariable = "SEAL_TEST_CERTIFICATE_PASSWORD";
        static string _folder, _pfxA, _pemB, _publicB, _thumbprintA, _thumbprintB;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            //Code page 1252 used by the Triple DES keys (registered at startup by the applications)
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            _folder = Path.Combine(Path.GetTempPath(), "SealTestCertificate_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable(PasswordVariable, "Test-pfx-password!");

            //Certificate A: PFX file protected by a password given by an environment variable
            using (var certificateA = createCertificate("CN=Seal Test Secrets A"))
            {
                _thumbprintA = certificateA.Thumbprint;
                _pfxA = Path.Combine(_folder, "secretsA.pfx");
                File.WriteAllBytes(_pfxA, certificateA.Export(X509ContentType.Pfx, "Test-pfx-password!"));
            }
            //Certificate B: PEM certificate with its private key in a .key file, and the certificate alone (public key, encryption only)
            using (var certificateB = createCertificate("CN=Seal Test Secrets B"))
            {
                _thumbprintB = certificateB.Thumbprint;
                _pemB = Path.Combine(_folder, "secretsB.crt");
                File.WriteAllText(_pemB, certificateB.ExportCertificatePem());
                using (var rsa = certificateB.GetRSAPrivateKey()) File.WriteAllText(Path.ChangeExtension(_pemB, ".key"), rsa.ExportPkcs8PrivateKeyPem());
                _publicB = Path.Combine(_folder, "public", "secretsB.cer");
                Directory.CreateDirectory(Path.GetDirectoryName(_publicB));
                File.WriteAllText(_publicB, certificateB.ExportCertificatePem());
            }
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            Environment.SetEnvironmentVariable(PasswordVariable, null);
            try { Directory.Delete(_folder, true); } catch { }
        }

        static X509Certificate2 createCertificate(string subject)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(10));
        }

        static SealServerConfiguration configuration(EncryptionMode mode, string certificatePath = "", string previous = "")
        {
            var result = new SealServerConfiguration() { EncryptionMode = mode, CertificatePath = certificatePath, CertificatePasswordVariable = PasswordVariable, PreviousCertificates = previous };
            result.InitDefaultKeyValues();
            return result;
        }

        [TestMethod]
        public void RoundTripWithPfx()
        {
            var config = configuration(EncryptionMode.Certificate, _pfxA);
            var encrypted = config.EncryptValue("Pass word é€ with a long text " + new string('x', 500), MetaConnection.PasswordKeyName);
            Assert.IsTrue(encrypted.StartsWith(CryptoHelper.CertificatePrefix));
            Assert.AreEqual(_thumbprintA, CryptoHelper.GetCertificateThumbprint(encrypted));
            Assert.AreEqual("Pass word é€ with a long text " + new string('x', 500), config.DecryptValue(encrypted, MetaConnection.PasswordKeyName));
            //Random key and nonce: two encryptions of the same value differ
            Assert.AreNotEqual(encrypted, config.EncryptValue("Pass word é€ with a long text " + new string('x', 500), MetaConnection.PasswordKeyName));
        }

        [TestMethod]
        public void PublicKeyEncryptsPemDecrypts()
        {
            //A workstation having only the public certificate can encrypt, the server having the private key decrypts
            var encrypted = configuration(EncryptionMode.Certificate, _publicB).EncryptValue("smtp-secret", OutputEmailDevice.PasswordKeyName);
            Assert.AreEqual(_thumbprintB, CryptoHelper.GetCertificateThumbprint(encrypted));
            Assert.AreEqual("smtp-secret", configuration(EncryptionMode.Certificate, _pemB).DecryptValue(encrypted, OutputEmailDevice.PasswordKeyName));
            var ex = Assert.Throws<CryptographicException>(() => configuration(EncryptionMode.Certificate, _publicB).DecryptValue(encrypted, OutputEmailDevice.PasswordKeyName));
            StringAssert.Contains(ex.Message, "private key");
        }

        [TestMethod]
        public void ValueBoundToItsKeyName()
        {
            var config = configuration(EncryptionMode.Certificate, _pfxA);
            var encrypted = config.EncryptValue("db-password", MetaConnection.PasswordKeyName);
            Assert.Throws<AuthenticationTagMismatchException>(() => config.DecryptValue(encrypted, OutputEmailDevice.PasswordKeyName));
        }

        [TestMethod]
        public void RotationWithPreviousCertificates()
        {
            var encryptedA = configuration(EncryptionMode.Certificate, _pfxA).EncryptValue("old secret", MetaConnection.PasswordKeyName);

            //New current certificate B: A is still declared to decrypt the values not re-encrypted yet
            var config = configuration(EncryptionMode.Certificate, _pemB, _pfxA);
            Assert.AreEqual("old secret", config.DecryptValue(encryptedA, MetaConnection.PasswordKeyName));
            Assert.AreEqual(_thumbprintB, CryptoHelper.GetCertificateThumbprint(config.EncryptValue("new secret", MetaConnection.PasswordKeyName)));

            //Without the previous certificate, the error names the missing certificate
            var ex = Assert.Throws<CryptographicException>(() => configuration(EncryptionMode.Certificate, _pemB).DecryptValue(encryptedA, MetaConnection.PasswordKeyName));
            StringAssert.Contains(ex.Message, _thumbprintA);
        }

        [TestMethod]
        public void LegacyValuesStillReadable()
        {
            var tripleDES = configuration(EncryptionMode.Default).EncryptValue("legacy password", MetaConnection.PasswordKeyName);
            var aes = configuration(EncryptionMode.Default).EncryptValue("legacy key", OutputEmailDevice.SendGridKeyName, true);
            var config = configuration(EncryptionMode.Certificate, _pfxA);
            Assert.AreEqual("legacy password", config.DecryptValue(tripleDES, MetaConnection.PasswordKeyName));
            Assert.AreEqual("legacy key", config.DecryptValue(aes, OutputEmailDevice.SendGridKeyName, true));

            //Back to the Default mode: a certificate value is still decrypted with its certificate
            var encrypted = config.EncryptValue("cert password", MetaConnection.PasswordKeyName);
            Assert.AreEqual("cert password", configuration(EncryptionMode.Default, _pfxA).DecryptValue(encrypted, MetaConnection.PasswordKeyName));
        }

        /// <summary>
        /// Certificate of the Windows store identified by its thumbprint: executed only if the environment variable SEAL_TEST_CERTIFICATE_THUMBPRINT
        /// gives the thumbprint of a certificate of 'Local Machine\Personal' or 'Current User\Personal' (e.g. created by New-SelfSignedCertificate)
        /// </summary>
        [TestMethod]
        public void WindowsStoreCertificate()
        {
            var thumbprint = Environment.GetEnvironmentVariable("SEAL_TEST_CERTIFICATE_THUMBPRINT");
            if (string.IsNullOrEmpty(thumbprint) || !OperatingSystem.IsWindows()) Assert.Inconclusive("SEAL_TEST_CERTIFICATE_THUMBPRINT is not set");

            var config = configuration(EncryptionMode.Certificate);
            config.CertificateThumbprint = thumbprint.ToLowerInvariant();
            var encrypted = config.EncryptValue("store secret", MetaConnection.PasswordKeyName);
            Assert.AreEqual(thumbprint.ToUpperInvariant(), CryptoHelper.GetCertificateThumbprint(encrypted));
            Assert.AreEqual("store secret", config.DecryptValue(encrypted, MetaConnection.PasswordKeyName));
        }

        [TestMethod]
        public void MissingCertificateFile()
        {
            var config = configuration(EncryptionMode.Certificate, Path.Combine(_folder, "missing.pfx"));
            var ex = Assert.Throws<CryptographicException>(() => config.EncryptValue("x", MetaConnection.PasswordKeyName));
            StringAssert.Contains(ex.Message, "does not exist");
            ex = Assert.Throws<CryptographicException>(() => configuration(EncryptionMode.Certificate).EncryptValue("x", MetaConnection.PasswordKeyName));
            StringAssert.Contains(ex.Message, "no certificate is defined");
        }

        [TestMethod]
        public void ReEncryptFile()
        {
            var legacy = configuration(EncryptionMode.Default).EncryptValue("db pass", MetaConnection.PasswordKeyName);
            var path = Path.Combine(_folder, "Source.scfx");
            var xml = "<?xml version=\"1.0\"?>\r\n<MetaSource><Connections><MetaConnection><Name>A</Name><Password>" + legacy + "</Password></MetaConnection>"
                + "<MetaConnection><Name>B</Name><Password>not-a-valid-value</Password></MetaConnection><MetaConnection><Name>C</Name><Password /></MetaConnection></Connections></MetaSource>";
            File.WriteAllText(path, xml);

            var config = configuration(EncryptionMode.Certificate, _pfxA);
            var secrets = new[] { new SecretsHelper.SecretElement() { Element = "Password", KeyName = MetaConnection.PasswordKeyName } };

            //Simulation: nothing written
            var result = new SecretsHelper.Result();
            SecretsHelper.ReEncryptFile(path, config, secrets, true, result);
            Assert.AreEqual(1, result.Values);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.AreEqual(xml, File.ReadAllText(path));

            result = new SecretsHelper.Result();
            SecretsHelper.ReEncryptFile(path, config, secrets, false, result);
            Assert.AreEqual(1, result.Files);
            Assert.AreEqual(1, result.Values);
            StringAssert.Contains(result.Errors[0], "<Password>");
            var newXml = File.ReadAllText(path);
            Assert.IsFalse(newXml.Contains(legacy));
            Assert.IsTrue(newXml.Contains("<Password>not-a-valid-value</Password>"), "a value that cannot be decrypted is left unchanged");
            var start = newXml.IndexOf("<Password>" + CryptoHelper.CertificatePrefix) + "<Password>".Length;
            var encrypted = newXml.Substring(start, newXml.IndexOf("</Password>", start) - start);
            Assert.AreEqual("db pass", config.DecryptValue(encrypted, MetaConnection.PasswordKeyName));

            //Second run: already encrypted with the current certificate
            result = new SecretsHelper.Result();
            SecretsHelper.ReEncryptFile(path, config, secrets, false, result);
            Assert.AreEqual(0, result.Values);
            Assert.AreEqual(1, result.UpToDate);
        }
    }
}
