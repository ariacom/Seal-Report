using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Linq;
using Azure.Core;

namespace Seal.Helpers
{
    /// <summary>
    /// Static helper methods for Azure: client principal parsing and Blob storage, available in Razor scripts
    /// </summary>
    public static class AzureHelper
    {
        #region Client Principal Claim
        private class ClientPrincipalClaim
        {
            [JsonPropertyName("typ")]
            public string Type { get; set; }
            [JsonPropertyName("val")]
            public string Value { get; set; }
        }

        private class ClientPrincipal
        {
            [JsonPropertyName("auth_typ")]
            public string IdentityProvider { get; set; }
            [JsonPropertyName("name_typ")]
            public string NameClaimType { get; set; }
            [JsonPropertyName("role_typ")]
            public string RoleClaimType { get; set; }
            [JsonPropertyName("claims")]
            public IEnumerable<ClientPrincipalClaim> Claims { get; set; }
        }

        /// <summary>
        /// Parse the 'x-ms-client-principal' header of an Azure App Service request into a ClaimsPrincipal
        /// </summary>
        public static ClaimsPrincipal ClaimsPrincipalParse(HttpRequest req)
        {
            var principal = new ClientPrincipal();

            if (req.Headers.TryGetValue("x-ms-client-principal", out var header))
            {
                var data = header[0];
                var decoded = Convert.FromBase64String(data);
                var json = System.Text.Encoding.UTF8.GetString(decoded);
                principal = JsonSerializer.Deserialize<ClientPrincipal>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            /*
             *  At this point, the code can iterate through `principal.Claims` to
             *  check claims as part of validation. Alternatively, we can convert
             *  it into a standard object with which to perform those checks later
             *  in the request pipeline. That object can also be leveraged for 
             *  associating user data, etc. The rest of this function performs such
             *  a conversion to create a `ClaimsPrincipal` as might be used in 
             *  other .NET code.
             */

            var identity = new ClaimsIdentity(principal.IdentityProvider, principal.NameClaimType, principal.RoleClaimType);
            identity.AddClaims(principal.Claims.Select(c => new Claim(c.Type, c.Value)));

            return new ClaimsPrincipal(identity);
        }
        #endregion

        #region Blob container
        /// <summary>
        /// Returns a BlobContainerClient for a container of an Azure storage account using a token credential
        /// </summary>
        public static BlobContainerClient BlobGetContainerClient(string storageAccountName, TokenCredential credential, string ContainerName)
        {
            return new BlobServiceClient(new Uri($"https://{storageAccountName}.blob.core.windows.net"), credential).GetBlobContainerClient(ContainerName);
        }

        /// <summary>
        /// Upload a byte array to a blob in the container
        /// </summary>
        public static void BlobSaveTo(byte[] bytes, string uri, bool overwrite, BlobContainerClient containerClient)
        {

            BlobClient blobClient = containerClient.GetBlobClient(uri);
            using (var stream = new MemoryStream(bytes, writable: false))
            {
                blobClient.Upload(stream, overwrite);
            }
        }

        /// <summary>
        /// Download a blob from the container as a byte array (throws an exception if the blob does not exist)
        /// </summary>
        public static byte[] BlobDownloadFrom(string uri, BlobContainerClient containerClient)
        {

            BlobClient blobClient = containerClient.GetBlobClient(uri);
            if (blobClient.Exists())
            {
                using (var stream = new MemoryStream())
                {
                    blobClient.DownloadTo(stream);
                    return stream.ToArray();
                }
            }
            throw new Exception($"No blob found at '{uri}'");
        }

        #endregion
    }
}
