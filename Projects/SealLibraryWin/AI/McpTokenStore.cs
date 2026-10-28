//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Seal.Helpers;
using Seal.Model;

namespace Seal.AI
{
    /// <summary>
    /// Personal access token of the MCP server. Only the hash of the token is stored, the token itself is shown once to the user when it is created.
    /// </summary>
    public class McpAccessToken
    {
        /// <summary>Identifier of the token (shown to the user to revoke it)</summary>
        public string Id;

        /// <summary>Name given by the user (e.g. the AI client using it)</summary>
        public string Name;

        /// <summary>Login name of the user (WebUserName), used to authenticate the user with the Security Provider</summary>
        public string UserName;

        /// <summary>SHA-256 hash of the token (hexadecimal)</summary>
        public string Hash;

        /// <summary>Creation date</summary>
        public DateTime Creation;

        /// <summary>Expiration date (null for no expiration)</summary>
        public DateTime? Expiration;

        /// <summary>Last use of the token (null if never used)</summary>
        public DateTime? LastUse;

        /// <summary>
        /// Returns null if the token is valid at <paramref name="now"/>, otherwise the reason of the refusal.
        /// </summary>
        /// <param name="now">Current date</param>
        /// <param name="idleDays">Maximum inactivity in days (0 for no limit), see <see cref="AIServerConfiguration.McpTokenIdleDays"/></param>
        public string CheckValidity(DateTime now, int idleDays)
        {
            if (Expiration != null && now >= Expiration.Value) return $"the access token '{Name}' expired on {Expiration.Value:yyyy-MM-dd HH:mm}";
            if (idleDays > 0 && now >= (LastUse ?? Creation).AddDays(idleDays)) return $"the access token '{Name}' has not been used for more than {idleDays} days";
            return null;
        }
    }

    /// <summary>
    /// Store of the personal access tokens of the MCP server
    /// </summary>
    public interface IMcpTokenStore
    {
        /// <summary>
        /// Creates a token for a user and returns it with its clear value <paramref name="clearToken"/>, the only time it is available.
        /// </summary>
        /// <param name="userName">Login name of the user</param>
        /// <param name="name">Name of the token</param>
        /// <param name="expiration">Expiration date, null for no expiration</param>
        /// <param name="clearToken">The value of the token to give to the user</param>
        McpAccessToken Create(string userName, string name, DateTime? expiration, out string clearToken);

        /// <summary>Returns the token having the clear value <paramref name="clearToken"/>, null if it does not exist</summary>
        McpAccessToken Find(string clearToken);

        /// <summary>Returns the tokens of a user</summary>
        List<McpAccessToken> GetUserTokens(string userName);

        /// <summary>Deletes a token of a user, returns false if it does not exist</summary>
        bool Revoke(string userName, string id);

        /// <summary>Records the use of a token</summary>
        void Touch(McpAccessToken token, DateTime now);
    }

    /// <summary>
    /// Store of the MCP access tokens in the folder Security\McpTokens of the repository: one JSON file per token, named by the hash of the token (direct lookup, no scan).
    /// The last use of a token is kept in memory and written at most every <see cref="LastUseWriteInterval"/>: a request does not write a file.
    /// </summary>
    public class McpFileTokenStore : IMcpTokenStore
    {
        /// <summary>Prefix of the tokens, to recognize them (e.g. in a secret scanner)</summary>
        public const string TokenPrefix = "seal_mcp_";

        /// <summary>Minimum interval between two writes of the last use of a token</summary>
        public static TimeSpan LastUseWriteInterval = TimeSpan.FromMinutes(5);

        readonly string _folder;
        readonly object _lock = new object();
        readonly ConcurrentDictionary<string, DateTime> _lastWrites = new ConcurrentDictionary<string, DateTime>();

        /// <summary>
        /// Creates a store of the tokens in <paramref name="folder"/>
        /// </summary>
        public McpFileTokenStore(string folder)
        {
            _folder = folder;
        }

        /// <summary>
        /// The store of the current repository (Security\McpTokens)
        /// </summary>
        public static McpFileTokenStore Instance
        {
            get
            {
                var folder = Path.Combine(Repository.Instance.SecurityFolder, "McpTokens");
                lock (_instanceLock)
                {
                    if (_instance == null || _instance._folder != folder) _instance = new McpFileTokenStore(folder);
                    return _instance;
                }
            }
        }
        static McpFileTokenStore _instance = null;
        static readonly object _instanceLock = new object();

        /// <summary>Returns the SHA-256 hash (hexadecimal) of a clear token</summary>
        public static string GetHash(string clearToken)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clearToken ?? ""))).ToLowerInvariant();
        }

        string getPath(string hash) => Path.Combine(_folder, hash + ".json");

        McpAccessToken read(string path)
        {
            try
            {
                return JsonConvert.DeserializeObject<McpAccessToken>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Helper.WriteLogException("McpFileTokenStore", ex);
                return null;
            }
        }

        void write(McpAccessToken token)
        {
            if (!Directory.Exists(_folder)) Directory.CreateDirectory(_folder);
            File.WriteAllText(getPath(token.Hash), JsonConvert.SerializeObject(token, Formatting.Indented));
        }

        /// <inheritdoc/>
        public McpAccessToken Create(string userName, string name, DateTime? expiration, out string clearToken)
        {
            if (string.IsNullOrWhiteSpace(userName)) throw new Exception("No user name for the access token");
            clearToken = TokenPrefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var token = new McpAccessToken()
            {
                Id = Guid.NewGuid().ToString("N").Substring(0, 12),
                Name = string.IsNullOrWhiteSpace(name) ? "MCP" : name.Trim(),
                UserName = userName,
                Hash = GetHash(clearToken),
                Creation = DateTime.Now,
                Expiration = expiration
            };
            lock (_lock) write(token);
            return token;
        }

        /// <inheritdoc/>
        public McpAccessToken Find(string clearToken)
        {
            if (string.IsNullOrEmpty(clearToken) || !clearToken.StartsWith(TokenPrefix)) return null;
            var path = getPath(GetHash(clearToken));
            return File.Exists(path) ? read(path) : null;
        }

        /// <inheritdoc/>
        public List<McpAccessToken> GetUserTokens(string userName)
        {
            if (!Directory.Exists(_folder)) return new List<McpAccessToken>();
            return Directory.GetFiles(_folder, "*.json")
                .Select(read)
                .Where(t => t != null && string.Equals(t.UserName, userName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Creation)
                .ToList();
        }

        /// <inheritdoc/>
        public bool Revoke(string userName, string id)
        {
            var token = GetUserTokens(userName).FirstOrDefault(t => t.Id == id);
            if (token == null) return false;
            lock (_lock) File.Delete(getPath(token.Hash));
            _lastWrites.TryRemove(token.Hash, out _);
            return true;
        }

        /// <inheritdoc/>
        public void Touch(McpAccessToken token, DateTime now)
        {
            token.LastUse = now;
            if (_lastWrites.TryGetValue(token.Hash, out var lastWrite) && now - lastWrite < LastUseWriteInterval) return;
            _lastWrites[token.Hash] = now;
            lock (_lock)
            {
                //Revoked meanwhile: do not create the file again
                if (File.Exists(getPath(token.Hash))) write(token);
            }
        }
    }
}
