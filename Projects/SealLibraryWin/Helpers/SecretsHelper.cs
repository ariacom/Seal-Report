//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Seal.AI;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace Seal.Helpers
{
    /// <summary>
    /// Re-encryption of the secrets of a repository (passwords, keys and secrets of the configuration, security, AI providers, data sources, devices and reports)
    /// with the current encryption mode of the server configuration, e.g. after a change of mode or of certificate.
    /// The values are replaced in the XML text of the files: nothing else is modified in the files.
    /// </summary>
    public class SecretsHelper
    {
        /// <summary>
        /// Encrypted element of a file: name of the XML element, key name used for its encryption, AES or Triple DES in the Default mode
        /// </summary>
        public class SecretElement
        {
            /// <summary>Name of the XML element holding the encrypted value</summary>
            public string Element;
            /// <summary>Key name used to encrypt the value</summary>
            public string KeyName;
            /// <summary>True if the value is encrypted with AES instead of Triple DES in the Default mode</summary>
            public bool UseAES;
            /// <summary>Optional parent element: only the elements inside it are secrets (e.g. the values of the application keys)</summary>
            public string Scope;
        }

        /// <summary>
        /// Result of a re-encryption
        /// </summary>
        public class Result
        {
            /// <summary>Files modified (or to modify when simulated)</summary>
            public int Files;
            /// <summary>Values re-encrypted (or to re-encrypt when simulated)</summary>
            public int Values;
            /// <summary>Values already encrypted with the current certificate</summary>
            public int UpToDate;
            /// <summary>Values that cannot be decrypted: file, element and error</summary>
            public List<string> Errors = new List<string>();
        }

        /// <summary>
        /// Re-encrypt all the secrets of the repository with the encryption mode of the configuration file of the repository.
        /// If simulate is true, the files are not modified (count of the values to re-encrypt and of the errors).
        /// </summary>
        public static Result ReEncryptRepository(Repository repository, bool simulate, ExecutionLogInterface log = null)
        {
            //The crypto engine is the configuration saved in the file: mode, certificates and key values
            var configuration = SealServerConfiguration.LoadFromFile(repository.ConfigurationPath, false);
            var result = new Result();
            log?.Log("{0}", $"Re-encryption of the secrets with the mode '{configuration.EncryptionMode}'{(simulate ? " (simulation, no file modified)" : "")}\r\n");

            void process(string path, params SecretElement[] secrets)
            {
                if (log != null && log.IsJobCancelled()) return;
                if (!File.Exists(path)) return;
                ReEncryptFile(path, configuration, secrets, simulate, result, log);
            }
            void processFolder(string folder, string pattern, params SecretElement[] secrets)
            {
                if (!Directory.Exists(folder)) return;
                foreach (var path in Directory.GetFiles(folder, pattern, SearchOption.AllDirectories).OrderBy(i => i, StringComparer.OrdinalIgnoreCase)) process(path, secrets);
            }

            var connectionPassword = new SecretElement() { Element = "Password", KeyName = MetaConnection.PasswordKeyName };

            process(repository.ConfigurationPath,
                new SecretElement() { Element = "SchedulerPassword", KeyName = SealServerConfiguration.SchedulerPasswordKeyName },
                new SecretElement() { Element = "Value", KeyName = SealServerConfiguration.ApplicationKeysKeyName, Scope = "ApplicationKeys" });
            process(repository.SecurityPath,
                new SecretElement() { Element = "TrustedBrowserSecret", KeyName = SealSecurity.TrustedBrowserSecretKeyName },
                new SecretElement() { Element = "AccountLinkSecret", KeyName = SealSecurity.AccountLinkSecretKeyName });
            var aiProvider = new SecretElement() { Element = "ProviderKey", KeyName = AIProviderConfiguration.AIProviderKeysKeyName };
            process(repository.AIConfigurationPath, aiProvider);
            process(AIServerConfiguration.GetAIProvidersFilePath(repository.AIConfigurationPath), aiProvider);

            processFolder(repository.SourcesFolder, "*." + Repository.SealConfigurationFileExtension, connectionPassword);
            processFolder(repository.DevicesEmailFolder, "*." + Repository.SealConfigurationFileExtension,
                new SecretElement() { Element = "Password", KeyName = OutputEmailDevice.PasswordKeyName },
                new SecretElement() { Element = "SendGridKey", KeyName = OutputEmailDevice.SendGridKeyName, UseAES = true },
                new SecretElement() { Element = "AzureSecret", KeyName = OutputEmailDevice.AzureSecretKeyName, UseAES = true });
            processFolder(repository.DevicesFileServerFolder, "*." + Repository.SealConfigurationFileExtension,
                new SecretElement() { Element = "Password", KeyName = OutputFileServerDevice.PasswordKeyName });
            processFolder(repository.DevicesSharePointFolder, "*." + Repository.SealConfigurationFileExtension,
                new SecretElement() { Element = "ClientSecret", KeyName = OutputSharePointDevice.SecretKeyName },
                new SecretElement() { Element = "CertificatePassword", KeyName = OutputSharePointDevice.SecretKeyName });

            //Reports having their own connections (passwords of the connections of their data sources)
            foreach (var folder in new[] { repository.ReportsFolder, repository.SubReportsFolder, repository.PersonalFolder }.Distinct())
            {
                processFolder(folder, "*." + Repository.SealReportFileExtension, connectionPassword);
            }

            log?.Log("{0}", $"\r\n{result.Values} value(s) {(simulate ? "to re-encrypt" : "re-encrypted")} in {result.Files} file(s), {result.UpToDate} value(s) already encrypted with the current certificate, {result.Errors.Count} error(s).");
            foreach (var error in result.Errors) log?.Log("{0}", "ERROR: " + error);
            return result;
        }

        /// <summary>
        /// Re-encrypt the secrets of a file
        /// </summary>
        public static void ReEncryptFile(string path, SealServerConfiguration configuration, IEnumerable<SecretElement> secrets, bool simulate, Result result, ExecutionLogInterface log = null)
        {
            var bytes = File.ReadAllBytes(path);
            bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var xml = new UTF8Encoding(false).GetString(bytes, hasBom ? 3 : 0, bytes.Length - (hasBom ? 3 : 0));

            int count = 0;
            foreach (var secret in secrets)
            {
                xml = ReEncryptXml(xml, configuration, secret, ref count, result, path);
            }
            if (count == 0) return;

            result.Files++;
            result.Values += count;
            log?.Log("{0}", $"{path}: {count} value(s)");
            if (!simulate)
            {
                using var stream = new MemoryStream();
                if (hasBom) stream.Write(new byte[] { 0xEF, 0xBB, 0xBF }, 0, 3);
                var content = new UTF8Encoding(false).GetBytes(xml);
                stream.Write(content, 0, content.Length);
                File.WriteAllBytes(path, stream.ToArray());
            }
        }

        /// <summary>
        /// Re-encrypt the values of an element in an XML text
        /// </summary>
        public static string ReEncryptXml(string xml, SealServerConfiguration configuration, SecretElement secret, ref int count, Result result, string label)
        {
            if (!string.IsNullOrEmpty(secret.Scope))
            {
                int localCount = count;
                var scoped = Regex.Replace(xml, $@"<{secret.Scope}>(.*?)</{secret.Scope}>", m =>
                {
                    var inner = ReEncryptXml(m.Groups[1].Value, configuration, new SecretElement() { Element = secret.Element, KeyName = secret.KeyName, UseAES = secret.UseAES }, ref localCount, result, label);
                    return $"<{secret.Scope}>{inner}</{secret.Scope}>";
                }, RegexOptions.Singleline);
                count = localCount;
                return scoped;
            }

            int changed = count;
            string currentThumbprint = null;
            if (configuration.EncryptionMode == EncryptionMode.Certificate)
            {
                try { currentThumbprint = configuration.CurrentCertificate.Thumbprint; } catch { }
            }

            var output = Regex.Replace(xml, $@"<{secret.Element}>([^<]*)</{secret.Element}>", m =>
            {
                var value = WebUtility.HtmlDecode(m.Groups[1].Value);
                if (string.IsNullOrEmpty(value)) return m.Value;
                //Already encrypted with the current certificate
                if (currentThumbprint != null && CryptoHelper.IsCertificateValue(value) && CryptoHelper.GetCertificateThumbprint(value) == currentThumbprint)
                {
                    result.UpToDate++;
                    return m.Value;
                }
                try
                {
                    var clear = configuration.DecryptValueAnyMode(value, secret.KeyName, secret.UseAES);
                    var encrypted = configuration.EncryptValue(clear, secret.KeyName, secret.UseAES);
                    if (string.IsNullOrEmpty(encrypted)) throw new Exception($"No key value '{secret.KeyName}' in the server configuration.");
                    if (encrypted == value) return m.Value;
                    changed++;
                    return $"<{secret.Element}>{SecurityElement.Escape(encrypted)}</{secret.Element}>";
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{label}: <{secret.Element}> ({secret.KeyName}): {ex.Message}");
                    return m.Value;
                }
            });
            count = changed;
            return output;
        }
    }
}
