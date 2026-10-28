//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Seal.Helpers;
using Seal.Model;

namespace Seal.AI
{
    /// <summary>
    /// OAuth client of the MCP server: registered dynamically (RFC 7591) or described by a Client ID Metadata Document (its client_id is the HTTPS URL of the document)
    /// </summary>
    public class McpOAuthClient
    {
        /// <summary>Identifier of the client</summary>
        public string ClientId;

        /// <summary>Name of the client shown on the consent page</summary>
        public string ClientName;

        /// <summary>Redirect URIs of the client</summary>
        public List<string> RedirectUris = new List<string>();

        /// <summary>True if the client is described by a Client ID Metadata Document (not stored)</summary>
        public bool IsMetadataDocument;

        /// <summary>Registration date</summary>
        public DateTime Creation;

        /// <summary>Last authorization of the client (null if never authorized)</summary>
        public DateTime? LastUse;
    }

    /// <summary>
    /// Authorization given by a user to an OAuth client for an agent of the MCP server: it holds the hashes of the current access token and refresh token.
    /// Revoking the grant revokes its tokens.
    /// </summary>
    public class McpOAuthGrant
    {
        /// <summary>Identifier of the grant, part of its tokens</summary>
        public string Id;

        /// <summary>Login name of the user (WebUserName)</summary>
        public string UserName;

        /// <summary>Identifier of the OAuth client</summary>
        public string ClientId;

        /// <summary>Name of the OAuth client</summary>
        public string ClientName;

        /// <summary>Redirect URI used for the authorization</summary>
        public string RedirectUri;

        /// <summary>Identifier of the agent (audience of the tokens)</summary>
        public string AgentSlug;

        /// <summary>URL of the MCP server of the agent (resource of RFC 8707)</summary>
        public string Resource;

        /// <summary>Scope granted</summary>
        public string Scope;

        /// <summary>Date of the consent</summary>
        public DateTime Creation;

        /// <summary>End of the grant: the user must authorize the client again (null for no limit)</summary>
        public DateTime? Expiration;

        /// <summary>Last use of the grant (request or refresh)</summary>
        public DateTime? LastUse;

        /// <summary>SHA-256 hash of the current access token</summary>
        public string AccessHash;

        /// <summary>Expiration of the current access token</summary>
        public DateTime AccessExpiration;

        /// <summary>SHA-256 hash of the current refresh token</summary>
        public string RefreshHash;

        /// <summary>SHA-256 hash of the previous refresh token, to detect its reuse</summary>
        public string PreviousRefreshHash;

        /// <summary>Date of the last rotation of the refresh token</summary>
        public DateTime? RefreshRotation;

        /// <summary>
        /// Returns null if the grant is valid at <paramref name="now"/>, otherwise the reason of the refusal.
        /// </summary>
        public string CheckValidity(DateTime now, int idleDays)
        {
            if (Expiration != null && now >= Expiration.Value) return $"the authorization of '{ClientName}' expired on {Expiration.Value:yyyy-MM-dd HH:mm}";
            if (idleDays > 0 && now >= (LastUse ?? Creation).AddDays(idleDays)) return $"the authorization of '{ClientName}' has not been used for more than {idleDays} days";
            return null;
        }
    }

    /// <summary>
    /// Store of the OAuth clients registered dynamically and of the grants of the MCP server
    /// </summary>
    public interface IMcpOAuthStore
    {
        /// <summary>Saves a client</summary>
        void SaveClient(McpOAuthClient client);

        /// <summary>Returns a client from its identifier, null if it does not exist</summary>
        McpOAuthClient FindClient(string clientId);

        /// <summary>Returns the number of clients registered</summary>
        int GetClientCount();

        /// <summary>Deletes the clients registered for more than one day and never authorized, or not authorized for <paramref name="unusedDays"/> days</summary>
        void PurgeClients(DateTime now, int unusedDays);

        /// <summary>Saves a grant</summary>
        void SaveGrant(McpOAuthGrant grant);

        /// <summary>Returns a grant from its identifier, null if it does not exist</summary>
        McpOAuthGrant FindGrant(string id);

        /// <summary>Returns the grants of a user</summary>
        List<McpOAuthGrant> GetUserGrants(string userName);

        /// <summary>Returns the grants of all the users</summary>
        List<McpOAuthGrant> GetAllGrants();

        /// <summary>Deletes a grant and its tokens, returns false if it does not exist</summary>
        bool DeleteGrant(string id);
    }

    /// <summary>
    /// Store of the OAuth clients and grants in the folders Security\McpTokens\OAuthClients and Security\McpTokens\OAuthGrants of the repository: one JSON file per client (named by the hash of its identifier) and per grant.
    /// </summary>
    public class McpFileOAuthStore : IMcpOAuthStore
    {
        readonly string _clientsFolder, _grantsFolder;
        readonly object _lock = new object();

        /// <summary>
        /// Creates a store in <paramref name="folder"/>
        /// </summary>
        public McpFileOAuthStore(string folder)
        {
            _folder = folder;
            _clientsFolder = Path.Combine(folder, "OAuthClients");
            _grantsFolder = Path.Combine(folder, "OAuthGrants");
        }

        /// <summary>
        /// The store of the current repository (Security\McpTokens)
        /// </summary>
        public static McpFileOAuthStore Instance
        {
            get
            {
                var folder = Path.Combine(Repository.Instance.SecurityFolder, "McpTokens");
                lock (_instanceLock)
                {
                    if (_instance == null || _instance._folder != folder) _instance = new McpFileOAuthStore(folder);
                    return _instance;
                }
            }
        }
        static McpFileOAuthStore _instance = null;
        static readonly object _instanceLock = new object();
        readonly string _folder;

        static T read<T>(string path) where T : class
        {
            try
            {
                return File.Exists(path) ? JsonConvert.DeserializeObject<T>(File.ReadAllText(path)) : null;
            }
            catch (Exception ex)
            {
                Helper.WriteLogException("McpFileOAuthStore", ex);
                return null;
            }
        }

        static void write(string folder, string path, object value)
        {
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            File.WriteAllText(path, JsonConvert.SerializeObject(value, Formatting.Indented));
        }

        string getClientPath(string clientId) => Path.Combine(_clientsFolder, McpFileTokenStore.GetHash(clientId) + ".json");

        string getGrantPath(string id) => Path.Combine(_grantsFolder, id + ".json");

        static bool isValidId(string id) => !string.IsNullOrEmpty(id) && id.Length <= 64 && id.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));

        /// <inheritdoc/>
        public void SaveClient(McpOAuthClient client)
        {
            lock (_lock) write(_clientsFolder, getClientPath(client.ClientId), client);
        }

        /// <inheritdoc/>
        public McpOAuthClient FindClient(string clientId)
        {
            if (string.IsNullOrEmpty(clientId)) return null;
            return read<McpOAuthClient>(getClientPath(clientId));
        }

        /// <inheritdoc/>
        public int GetClientCount()
        {
            return Directory.Exists(_clientsFolder) ? Directory.GetFiles(_clientsFolder, "*.json").Length : 0;
        }

        /// <inheritdoc/>
        public void PurgeClients(DateTime now, int unusedDays)
        {
            if (!Directory.Exists(_clientsFolder)) return;
            var grantClients = new HashSet<string>(GetAllGrants().Select(g => g.ClientId));
            foreach (var path in Directory.GetFiles(_clientsFolder, "*.json"))
            {
                var client = read<McpOAuthClient>(path);
                if (client == null) continue;
                var obsolete = client.LastUse == null ? now >= client.Creation.AddDays(1) : now >= client.LastUse.Value.AddDays(unusedDays);
                if (obsolete && !grantClients.Contains(client.ClientId))
                {
                    lock (_lock) File.Delete(path);
                }
            }
        }

        /// <inheritdoc/>
        public void SaveGrant(McpOAuthGrant grant)
        {
            if (!isValidId(grant.Id)) throw new Exception("Invalid grant identifier");
            lock (_lock) write(_grantsFolder, getGrantPath(grant.Id), grant);
        }

        /// <inheritdoc/>
        public McpOAuthGrant FindGrant(string id)
        {
            if (!isValidId(id)) return null;
            return read<McpOAuthGrant>(getGrantPath(id));
        }

        /// <inheritdoc/>
        public List<McpOAuthGrant> GetUserGrants(string userName)
        {
            return GetAllGrants().Where(g => string.Equals(g.UserName, userName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <inheritdoc/>
        public List<McpOAuthGrant> GetAllGrants()
        {
            if (!Directory.Exists(_grantsFolder)) return new List<McpOAuthGrant>();
            return Directory.GetFiles(_grantsFolder, "*.json").Select(read<McpOAuthGrant>).Where(g => g != null).OrderBy(g => g.Creation).ToList();
        }

        /// <inheritdoc/>
        public bool DeleteGrant(string id)
        {
            if (!isValidId(id)) return false;
            lock (_lock)
            {
                var path = getGrantPath(id);
                if (!File.Exists(path)) return false;
                File.Delete(path);
                return true;
            }
        }
    }

    /// <summary>
    /// Error of the OAuth authorization server (RFC 6749): <see cref="Error"/> is the OAuth error code
    /// </summary>
    public class McpOAuthException : Exception
    {
        /// <summary>OAuth error code (invalid_request, invalid_client, invalid_grant...)</summary>
        public string Error;

        /// <summary>True if the error can be sent to the redirect URI of the client (the client and its redirect URI are valid)</summary>
        public bool CanRedirect;

        /// <summary>
        /// Creates an OAuth error
        /// </summary>
        public McpOAuthException(string error, string description, bool canRedirect = false) : base(description)
        {
            Error = error;
            CanRedirect = canRedirect;
        }
    }

    /// <summary>
    /// Authorization request validated, waiting for the login and the consent of the user
    /// </summary>
    public class McpAuthorizationRequest
    {
        /// <summary>Identifier of the request in the consent page</summary>
        public string Id;

        /// <summary>The OAuth client</summary>
        public McpOAuthClient Client;

        /// <summary>Redirect URI of the request</summary>
        public string RedirectUri;

        /// <summary>State of the client, returned with the code</summary>
        public string State;

        /// <summary>PKCE code challenge (S256)</summary>
        public string CodeChallenge;

        /// <summary>URL of the MCP server of the agent</summary>
        public string Resource;

        /// <summary>Identifier of the agent</summary>
        public string AgentSlug;

        /// <summary>Scope requested</summary>
        public string Scope;

        /// <summary>Date of the request</summary>
        public DateTime Creation;

        /// <summary>Web session of the browser having opened the request: the consent is accepted only from this session</summary>
        public string SessionId;

        /// <summary>Host of the redirect URI, shown on the consent page</summary>
        public string RedirectHost => Uri.TryCreate(RedirectUri, UriKind.Absolute, out var uri) ? uri.Host : RedirectUri;

        /// <summary>True if the redirect URI is a loopback address (a local application of the computer of the user)</summary>
        public bool IsLoopbackRedirect => McpOAuth.IsLoopback(RedirectUri);
    }

    /// <summary>
    /// OAuth 2.1 authorization server of the MCP server (the external AI clients like claude.ai, Claude Desktop or Claude Code connect without a personal access token):
    /// metadata (RFC 8414, RFC 9728), clients registered dynamically (RFC 7591) or described by a Client ID Metadata Document, authorization code with PKCE S256,
    /// tokens bound to an agent (RFC 8707), rotation of the refresh tokens with detection of their reuse, revocation (RFC 7009).
    /// The user is authenticated by the login of the Web Report Server: this class is independent of the transport.
    /// </summary>
    public static class McpOAuth
    {
        /// <summary>Path of the endpoints of the authorization server</summary>
        public const string OAuthPath = "/mcp-oauth";

        /// <summary>Prefix of the access tokens</summary>
        public const string AccessTokenPrefix = "seal_mcp_at_";

        /// <summary>Prefix of the refresh tokens</summary>
        public const string RefreshTokenPrefix = "seal_mcp_rt_";

        /// <summary>Scope of the MCP server</summary>
        public const string McpScope = "mcp";

        /// <summary>Validity of an authorization code</summary>
        public static TimeSpan CodeLifetime = TimeSpan.FromMinutes(2);

        /// <summary>Validity of an authorization request waiting for the consent</summary>
        public static TimeSpan RequestLifetime = TimeSpan.FromMinutes(15);

        /// <summary>A previous refresh token reused within this delay after its rotation is refused without revoking the grant (concurrent refreshes of a client)</summary>
        public static TimeSpan RefreshReuseGracePeriod = TimeSpan.FromSeconds(60);

        /// <summary>Validity of a Client ID Metadata Document in the cache</summary>
        public static TimeSpan MetadataDocumentCacheLifetime = TimeSpan.FromHours(1);

        /// <summary>Clients registered dynamically and not used for this number of days are deleted</summary>
        public const int UnusedClientDays = 90;

        /// <summary>Maximum number of clients registered dynamically</summary>
        public const int MaxRegisteredClients = 1000;

        /// <summary>Store of the clients and grants</summary>
        public static IMcpOAuthStore Store
        {
            get { return _store ?? McpFileOAuthStore.Instance; }
            set { _store = value; }
        }
        static IMcpOAuthStore _store = null;

        /// <summary>Downloads a Client ID Metadata Document, null for the default download (replaced in the tests)</summary>
        public static Func<string, string> MetadataDocumentFetcher = null;

        static AIServerConfiguration Configuration => Repository.Instance.AIConfiguration;

        #region Metadata

        /// <summary>
        /// Returns the public base URL of the server (issuer of the authorization server): <see cref="AIServerConfiguration.McpPublicUrl"/> if set, otherwise the base URL of the request
        /// </summary>
        public static string GetPublicBaseUrl(string requestBaseUrl)
        {
            var url = Configuration.McpPublicUrl;
            return (string.IsNullOrWhiteSpace(url) ? requestBaseUrl ?? "" : url.Trim()).TrimEnd('/');
        }

        /// <summary>Returns the URL of the MCP server of an agent</summary>
        public static string GetResourceUrl(string publicBaseUrl, string agentSlug) => publicBaseUrl + "/mcp/" + agentSlug;

        /// <summary>Returns the URL of the protected resource metadata of an agent (RFC 9728), given in the header WWW-Authenticate of the 401 responses</summary>
        public static string GetResourceMetadataUrl(string publicBaseUrl, string agentSlug) => publicBaseUrl + "/.well-known/oauth-protected-resource/mcp/" + agentSlug;

        /// <summary>
        /// Returns the metadata of the authorization server (RFC 8414)
        /// </summary>
        /// <param name="publicBaseUrl">Public base URL of the server (issuer)</param>
        /// <param name="openIdConfiguration">True for the document served as OpenID Connect discovery: the fields required by its schema are added (the MCP clients validate it strictly, e.g. for an issuer in a virtual directory found at {path}/.well-known/openid-configuration), no ID token is issued (empty key set)</param>
        public static Dictionary<string, object> GetAuthorizationServerMetadata(string publicBaseUrl, bool openIdConfiguration = false)
        {
            var result = new Dictionary<string, object>()
            {
                { "issuer", publicBaseUrl },
                { "authorization_endpoint", publicBaseUrl + OAuthPath + "/authorize" },
                { "token_endpoint", publicBaseUrl + OAuthPath + "/token" },
                { "revocation_endpoint", publicBaseUrl + OAuthPath + "/revoke" },
                { "response_types_supported", new[] { "code" } },
                { "response_modes_supported", new[] { "query" } },
                { "grant_types_supported", new[] { "authorization_code", "refresh_token" } },
                { "code_challenge_methods_supported", new[] { "S256" } },
                { "token_endpoint_auth_methods_supported", new[] { "none" } },
                { "revocation_endpoint_auth_methods_supported", new[] { "none" } },
                { "scopes_supported", new[] { McpScope, "offline_access" } },
                { "client_id_metadata_document_supported", true },
                { "authorization_response_iss_parameter_supported", true }
            };
            if (Configuration.McpOAuthDynamicRegistration) result.Add("registration_endpoint", publicBaseUrl + OAuthPath + "/register");
            if (openIdConfiguration)
            {
                result.Add("jwks_uri", publicBaseUrl + OAuthPath + "/jwks");
                result.Add("subject_types_supported", new[] { "public" });
                result.Add("id_token_signing_alg_values_supported", new[] { "RS256" });
            }
            return result;
        }

        /// <summary>
        /// Returns the protected resource metadata of the MCP server of an agent (RFC 9728)
        /// </summary>
        public static Dictionary<string, object> GetProtectedResourceMetadata(string publicBaseUrl, string agentSlug, string agentName)
        {
            return new Dictionary<string, object>()
            {
                { "resource", GetResourceUrl(publicBaseUrl, agentSlug) },
                { "authorization_servers", new[] { publicBaseUrl } },
                { "scopes_supported", new[] { McpScope } },
                { "bearer_methods_supported", new[] { "header" } },
                { "resource_name", "Seal Report - " + agentName }
            };
        }

        /// <summary>Returns the agent of the MCP server having the slug given, whatever the user (null if none)</summary>
        public static AIAgentConfiguration FindMcpAgent(string agentSlug)
        {
            if (string.IsNullOrEmpty(agentSlug)) return null;
            return Configuration.AIAgents.FirstOrDefault(a => a.IsEnabled && a.IsMcpAgent && McpGateway.GetAgentSlug(a) == agentSlug);
        }

        #endregion

        #region Redirect URIs and clients

        /// <summary>True if the URI is an HTTP loopback address (localhost, 127.0.0.1, [::1])</summary>
        public static bool IsLoopback(string uri)
        {
            return Uri.TryCreate(uri, UriKind.Absolute, out var u) && u.Scheme == Uri.UriSchemeHttp && (u.Host == "localhost" || u.Host == "127.0.0.1" || u.Host == "[::1]" || u.Host == "::1");
        }

        /// <summary>
        /// True if a redirect URI requested matches a registered one: same URI, the port is ignored for a loopback address (RFC 8252: the native clients use an ephemeral port).
        /// The pattern '*' matches any HTTPS or loopback URI.
        /// </summary>
        public static bool RedirectMatches(string registered, string requested)
        {
            if (string.IsNullOrWhiteSpace(registered) || string.IsNullOrWhiteSpace(requested)) return false;
            registered = registered.Trim();
            if (!Uri.TryCreate(requested, UriKind.Absolute, out var req) || !string.IsNullOrEmpty(req.Fragment)) return false;
            if (registered == "*") return req.Scheme == Uri.UriSchemeHttps || IsLoopback(requested);
            if (IsLoopback(registered) && IsLoopback(requested))
            {
                var reg = new Uri(registered);
                return reg.Host == req.Host && reg.AbsolutePath == req.AbsolutePath && reg.Query == req.Query;
            }
            return string.Equals(registered, requested, StringComparison.Ordinal);
        }

        /// <summary>Returns the redirect URIs allowed by <see cref="AIServerConfiguration.McpOAuthRedirectUris"/></summary>
        public static List<string> GetAllowedRedirectUris()
        {
            var value = string.IsNullOrWhiteSpace(Configuration.McpOAuthRedirectUris) ? AIServerConfiguration.DefaultMcpOAuthRedirectUris : Configuration.McpOAuthRedirectUris;
            return value.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        }

        /// <summary>True if a redirect URI is allowed by the configuration of the server</summary>
        public static bool IsRedirectAllowed(string redirectUri)
        {
            return GetAllowedRedirectUris().Any(p => RedirectMatches(p, redirectUri));
        }

        static readonly ConcurrentDictionary<string, (McpOAuthClient client, DateTime fetched)> _metadataDocuments = new ConcurrentDictionary<string, (McpOAuthClient, DateTime)>();

        /// <summary>True if the client identifier is the URL of a Client ID Metadata Document</summary>
        public static bool IsMetadataDocumentId(string clientId)
        {
            return Uri.TryCreate(clientId, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && uri.AbsolutePath.Length > 1 && string.IsNullOrEmpty(uri.Fragment);
        }

        /// <summary>
        /// Returns the client of an authorization request: described by its Client ID Metadata Document (client_id = HTTPS URL of the document) or registered dynamically.
        /// </summary>
        public static McpOAuthClient ResolveClient(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId)) throw new McpOAuthException("invalid_request", "client_id is missing");
            if (IsMetadataDocumentId(clientId))
            {
                if (_metadataDocuments.TryGetValue(clientId, out var cached) && DateTime.Now - cached.fetched < MetadataDocumentCacheLifetime) return cached.client;
                string json;
                try
                {
                    json = (MetadataDocumentFetcher ?? fetchMetadataDocument)(clientId);
                }
                catch (Exception ex)
                {
                    throw new McpOAuthException("invalid_client", $"the client metadata document '{clientId}' cannot be read: {ex.Message}");
                }
                var client = ParseMetadataDocument(clientId, json);
                _metadataDocuments[clientId] = (client, DateTime.Now);
                return client;
            }
            var registered = Store.FindClient(clientId);
            if (registered == null) throw new McpOAuthException("invalid_client", $"unknown client '{clientId}'");
            return registered;
        }

        /// <summary>Clears the cache of the Client ID Metadata Documents</summary>
        public static void ClearMetadataDocumentCache() => _metadataDocuments.Clear();

        /// <summary>
        /// Parses and validates a Client ID Metadata Document: its client_id must be the URL of the document, it must have redirect URIs and be a public client.
        /// </summary>
        public static McpOAuthClient ParseMetadataDocument(string clientId, string json)
        {
            JObject doc;
            try
            {
                doc = JObject.Parse(json ?? "");
            }
            catch
            {
                throw new McpOAuthException("invalid_client", "the client metadata document is not a valid JSON object");
            }
            if ((string)doc["client_id"] != clientId) throw new McpOAuthException("invalid_client", "the client_id of the client metadata document is not the URL of the document");
            var method = (string)doc["token_endpoint_auth_method"];
            if (!string.IsNullOrEmpty(method) && method != "none") throw new McpOAuthException("invalid_client", $"the token endpoint authentication method '{method}' is not supported (public clients only)");
            var redirectUris = (doc["redirect_uris"] as JArray)?.Select(t => (string)t).Where(u => !string.IsNullOrEmpty(u)).ToList();
            if (redirectUris == null || redirectUris.Count == 0) throw new McpOAuthException("invalid_client", "the client metadata document has no redirect_uris");
            var name = (string)doc["client_name"];
            return new McpOAuthClient()
            {
                ClientId = clientId,
                ClientName = truncate(string.IsNullOrWhiteSpace(name) ? new Uri(clientId).Host : name.Trim(), 100),
                RedirectUris = redirectUris,
                IsMetadataDocument = true,
                Creation = DateTime.Now
            };
        }

        /// <summary>
        /// Downloads a Client ID Metadata Document: HTTPS only, no private or loopback address (server-side request forgery), 5 seconds and 64 KB at most.
        /// </summary>
        static string fetchMetadataDocument(string url)
        {
            var uri = new Uri(url);
            var addresses = Dns.GetHostAddresses(uri.DnsSafeHost);
            if (addresses.Length == 0 || addresses.Any(IsPrivateAddress)) throw new Exception("the host of the document is not a public address");
            using var handler = new SocketsHttpHandler()
            {
                AllowAutoRedirect = false,
                ConnectTimeout = TimeSpan.FromSeconds(5),
                //Connect to the address checked (no second DNS resolution)
                ConnectCallback = async (context, cancellationToken) =>
                {
                    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                    try
                    {
                        await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken);
                        return new NetworkStream(socket, true);
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                }
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5), MaxResponseContentBufferSize = 64 * 1024 };
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            var response = client.GetAsync(uri).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode) throw new Exception($"HTTP status {(int)response.StatusCode}");
            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        /// <summary>True for a loopback, private, link-local or unspecified address</summary>
        public static bool IsPrivateAddress(IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
            if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)) return true;
            if (address.AddressFamily == AddressFamily.InterNetworkV6) return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal || address.IsIPv6Multicast;
            var b = address.GetAddressBytes();
            return b[0] == 10 || b[0] == 0 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254) || (b[0] == 100 && b[1] >= 64 && b[1] <= 127) || b[0] >= 224;
        }

        /// <summary>
        /// Registers a client dynamically (RFC 7591): a public client whose redirect URIs are allowed by the configuration. Returns the registration response.
        /// </summary>
        public static Dictionary<string, object> RegisterClient(string json, DateTime now)
        {
            if (!Configuration.McpOAuthDynamicRegistration) throw new McpOAuthException("access_denied", "the dynamic client registration is not enabled");
            JObject doc;
            try
            {
                doc = JObject.Parse(json ?? "");
            }
            catch
            {
                throw new McpOAuthException("invalid_client_metadata", "the registration request is not a valid JSON object");
            }
            var redirectUris = (doc["redirect_uris"] as JArray)?.Select(t => (string)t).ToList();
            if (redirectUris == null || redirectUris.Count == 0) throw new McpOAuthException("invalid_redirect_uri", "redirect_uris is missing");
            foreach (var uri in redirectUris)
            {
                if (!Uri.TryCreate(uri, UriKind.Absolute, out var u) || !string.IsNullOrEmpty(u.Fragment) || (u.Scheme != Uri.UriSchemeHttps && !IsLoopback(uri)))
                    throw new McpOAuthException("invalid_redirect_uri", $"the redirect URI '{uri}' must be an HTTPS or loopback URI");
                if (!IsRedirectAllowed(uri)) throw new McpOAuthException("invalid_redirect_uri", $"the redirect URI '{uri}' is not allowed by the MCP server");
            }
            var grantTypes = (doc["grant_types"] as JArray)?.Select(t => (string)t).ToList();
            if (grantTypes != null && !grantTypes.Contains("authorization_code")) throw new McpOAuthException("invalid_client_metadata", "the grant type 'authorization_code' is required");

            Store.PurgeClients(now, UnusedClientDays);
            if (Store.GetClientCount() >= MaxRegisteredClients) throw new McpOAuthException("access_denied", "too many clients are registered");

            var name = (string)doc["client_name"];
            var client = new McpOAuthClient()
            {
                ClientId = "seal_client_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant(),
                ClientName = truncate(string.IsNullOrWhiteSpace(name) ? "MCP client" : name.Trim(), 100),
                RedirectUris = redirectUris,
                Creation = now
            };
            Store.SaveClient(client);
            return new Dictionary<string, object>()
            {
                { "client_id", client.ClientId },
                { "client_id_issued_at", new DateTimeOffset(now).ToUnixTimeSeconds() },
                { "client_name", client.ClientName },
                { "redirect_uris", client.RedirectUris },
                { "grant_types", new[] { "authorization_code", "refresh_token" } },
                { "response_types", new[] { "code" } },
                { "token_endpoint_auth_method", "none" }
            };
        }

        #endregion

        #region Authorization

        static readonly ConcurrentDictionary<string, McpAuthorizationRequest> _requests = new ConcurrentDictionary<string, McpAuthorizationRequest>();

        class AuthorizationCode
        {
            public McpAuthorizationRequest Request;
            public string UserName;
            public DateTime Expiration;
            public string GrantId;
        }
        static readonly ConcurrentDictionary<string, AuthorizationCode> _codes = new ConcurrentDictionary<string, AuthorizationCode>();

        static void purgeMemory(DateTime now)
        {
            foreach (var item in _requests.Where(i => now - i.Value.Creation > RequestLifetime).ToList()) _requests.TryRemove(item.Key, out _);
            //A code used is kept until its expiration to detect its reuse
            foreach (var item in _codes.Where(i => now > i.Value.Expiration.AddMinutes(10)).ToList()) _codes.TryRemove(item.Key, out _);
        }

        static string get(IDictionary<string, string> parameters, string name) => parameters != null && parameters.TryGetValue(name, out var value) ? value : null;

        /// <summary>
        /// Returns the identifier of the agent of a resource (URL of the MCP server of an agent of this server), throws an error if the resource is not one of this server.
        /// </summary>
        public static string GetAgentSlugFromResource(string resource, string publicBaseUrl, bool canRedirect)
        {
            if (string.IsNullOrWhiteSpace(resource)) throw new McpOAuthException("invalid_target", "the parameter 'resource' (URL of the MCP server of the agent) is missing", canRedirect);
            var prefix = normalizeUrl(publicBaseUrl + "/mcp/");
            var value = normalizeUrl(resource.Trim());
            if (prefix == null || value == null || !value.StartsWith(prefix, StringComparison.Ordinal)) throw new McpOAuthException("invalid_target", $"the resource '{resource}' is not a MCP server of {publicBaseUrl}", canRedirect);
            var slug = value.Substring(prefix.Length).TrimEnd('/');
            if (slug.Length == 0 || slug.Contains('/') || FindMcpAgent(slug) == null) throw new McpOAuthException("invalid_target", $"the resource '{resource}' is not a MCP server of {publicBaseUrl}", canRedirect);
            return slug;
        }

        static string normalizeUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
            var port = uri.IsDefaultPort ? "" : ":" + uri.Port;
            return uri.Scheme.ToLowerInvariant() + "://" + uri.Host.ToLowerInvariant() + port + uri.AbsolutePath;
        }

        /// <summary>
        /// Validates an authorization request (parameters of the authorization endpoint) and keeps it until the consent of the user.
        /// An error with <see cref="McpOAuthException.CanRedirect"/> false must be shown to the user (unknown client or redirect URI not allowed), the others are sent to the redirect URI.
        /// </summary>
        public static McpAuthorizationRequest ValidateAuthorizationRequest(IDictionary<string, string> parameters, string publicBaseUrl, DateTime now)
        {
            purgeMemory(now);
            var client = ResolveClient(get(parameters, "client_id"));
            var redirectUri = get(parameters, "redirect_uri");
            if (string.IsNullOrEmpty(redirectUri)) throw new McpOAuthException("invalid_request", "redirect_uri is missing");
            if (!client.RedirectUris.Any(r => RedirectMatches(r, redirectUri))) throw new McpOAuthException("invalid_request", $"the redirect URI '{redirectUri}' is not registered for the client");
            if (!IsRedirectAllowed(redirectUri)) throw new McpOAuthException("invalid_request", $"the redirect URI '{redirectUri}' is not allowed by the MCP server (see 'MCP OAuth redirect URIs' in the AI configuration)");

            var state = get(parameters, "state");
            if (get(parameters, "response_type") != "code") throw new McpOAuthException("unsupported_response_type", "the response type must be 'code'", true);
            var challenge = get(parameters, "code_challenge");
            if (string.IsNullOrEmpty(challenge) || get(parameters, "code_challenge_method") != "S256") throw new McpOAuthException("invalid_request", "PKCE is required with the method S256", true);
            if (challenge.Length < 43 || challenge.Length > 128) throw new McpOAuthException("invalid_request", "invalid code_challenge", true);
            var resource = get(parameters, "resource");
            var slug = GetAgentSlugFromResource(resource, publicBaseUrl, true);

            var request = new McpAuthorizationRequest()
            {
                Id = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant(),
                Client = client,
                RedirectUri = redirectUri,
                State = state,
                CodeChallenge = challenge,
                Resource = GetResourceUrl(publicBaseUrl, slug),
                AgentSlug = slug,
                Scope = McpScope,
                Creation = now
            };
            _requests[request.Id] = request;
            return request;
        }

        /// <summary>Returns an authorization request waiting for the consent, null if it does not exist or has expired</summary>
        public static McpAuthorizationRequest GetAuthorizationRequest(string id, DateTime now)
        {
            if (string.IsNullOrEmpty(id) || !_requests.TryGetValue(id, out var request)) return null;
            return now - request.Creation > RequestLifetime ? null : request;
        }

        /// <summary>
        /// Returns the URL redirecting to the client with the result of an authorization request (code or error), with the state and the issuer (RFC 9207)
        /// </summary>
        public static string GetRedirectUrl(string redirectUri, string state, string publicBaseUrl, string code = null, string error = null, string errorDescription = null)
        {
            var sb = new StringBuilder(redirectUri);
            var separator = redirectUri.Contains('?') ? '&' : '?';
            void add(string name, string value)
            {
                if (value == null) return;
                sb.Append(separator).Append(name).Append('=').Append(Uri.EscapeDataString(value));
                separator = '&';
            }
            add("code", code);
            add("error", error);
            add("error_description", errorDescription);
            add("state", state);
            add("iss", publicBaseUrl);
            return sb.ToString();
        }

        /// <summary>
        /// Consent of the user: removes the authorization request and returns an authorization code for the client (or null if the user refused).
        /// The caller has checked that the agent is one of the MCP agents of the user.
        /// </summary>
        public static string Authorize(McpAuthorizationRequest request, string userName, bool allowed, DateTime now)
        {
            _requests.TryRemove(request.Id, out _);
            if (!allowed) return null;
            if (string.IsNullOrEmpty(userName)) throw new Exception("No user name for the authorization");
            var code = Base64Url(RandomNumberGenerator.GetBytes(32));
            _codes[McpFileTokenStore.GetHash(code)] = new AuthorizationCode() { Request = request, UserName = userName, Expiration = now.Add(CodeLifetime) };
            if (!request.Client.IsMetadataDocument)
            {
                request.Client.LastUse = now;
                Store.SaveClient(request.Client);
            }
            return code;
        }

        #endregion

        #region Tokens

        /// <summary>Returns the base64url encoding (without padding) of bytes</summary>
        public static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        /// <summary>Returns the PKCE S256 challenge of a code verifier</summary>
        public static string GetCodeChallenge(string verifier) => Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier ?? "")));

        static bool equalHashes(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(a), Encoding.ASCII.GetBytes(b));
        }

        /// <summary>
        /// Executes a request of the token endpoint (grant types authorization_code and refresh_token) and returns the token response (RFC 6749 section 5.1).
        /// </summary>
        public static Dictionary<string, object> ExecuteTokenRequest(IDictionary<string, string> parameters, DateTime now)
        {
            var grantType = get(parameters, "grant_type");
            if (grantType == "authorization_code") return exchangeCode(parameters, now);
            if (grantType == "refresh_token") return refresh(parameters, now);
            throw new McpOAuthException("unsupported_grant_type", $"the grant type '{grantType}' is not supported");
        }

        static Dictionary<string, object> exchangeCode(IDictionary<string, string> parameters, DateTime now)
        {
            purgeMemory(now);
            var code = get(parameters, "code");
            if (string.IsNullOrEmpty(code) || !_codes.TryGetValue(McpFileTokenStore.GetHash(code), out var entry)) throw new McpOAuthException("invalid_grant", "invalid authorization code");
            lock (entry)
            {
                if (entry.GrantId != null)
                {
                    //Code used twice: the tokens issued with it are revoked (OAuth 2.1 section 4.1.2)
                    Store.DeleteGrant(entry.GrantId);
                    throw new McpOAuthException("invalid_grant", "the authorization code has already been used");
                }
                if (now > entry.Expiration) throw new McpOAuthException("invalid_grant", "the authorization code has expired");
                var request = entry.Request;
                if (get(parameters, "client_id") != request.Client.ClientId) throw new McpOAuthException("invalid_grant", "the authorization code was not issued to this client");
                if (get(parameters, "redirect_uri") != request.RedirectUri) throw new McpOAuthException("invalid_grant", "the redirect URI is not the one of the authorization request");
                var verifier = get(parameters, "code_verifier");
                if (string.IsNullOrEmpty(verifier) || verifier.Length < 43 || verifier.Length > 128 || !equalHashes(GetCodeChallenge(verifier), request.CodeChallenge)) throw new McpOAuthException("invalid_grant", "invalid PKCE code verifier");
                var resource = get(parameters, "resource");
                if (!string.IsNullOrEmpty(resource) && normalizeUrl(resource)?.TrimEnd('/') != normalizeUrl(request.Resource)?.TrimEnd('/')) throw new McpOAuthException("invalid_target", "the resource is not the one of the authorization request");

                var maxDays = getGrantMaxDays();
                var grant = new McpOAuthGrant()
                {
                    Id = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant(),
                    UserName = entry.UserName,
                    ClientId = request.Client.ClientId,
                    ClientName = request.Client.ClientName,
                    RedirectUri = request.RedirectUri,
                    AgentSlug = request.AgentSlug,
                    Resource = request.Resource,
                    Scope = request.Scope,
                    Creation = now,
                    LastUse = now,
                    Expiration = maxDays > 0 ? now.AddDays(maxDays) : null
                };
                entry.GrantId = grant.Id;
                return issueTokens(grant, now);
            }
        }

        /// <summary>Maximum duration of a grant in days: the smallest of <see cref="AIServerConfiguration.McpOAuthGrantMaxDays"/> and <see cref="AIServerConfiguration.McpTokenMaxDays"/> (0 for no limit)</summary>
        static int getGrantMaxDays()
        {
            var values = new[] { Configuration.McpOAuthGrantMaxDays, Configuration.McpTokenMaxDays }.Where(v => v > 0).ToList();
            return values.Count == 0 ? 0 : values.Min();
        }

        static Dictionary<string, object> refresh(IDictionary<string, string> parameters, DateTime now)
        {
            var refreshToken = get(parameters, "refresh_token");
            var grant = findGrant(refreshToken, RefreshTokenPrefix);
            if (grant == null) throw new McpOAuthException("invalid_grant", "invalid refresh token");
            lock (getGrantLock(grant.Id))
            {
                //Read again under the lock: a concurrent refresh may have rotated the token
                grant = Store.FindGrant(grant.Id);
                if (grant == null) throw new McpOAuthException("invalid_grant", "invalid refresh token");
                var hash = McpFileTokenStore.GetHash(refreshToken);
                if (!equalHashes(hash, grant.RefreshHash))
                {
                    if (equalHashes(hash, grant.PreviousRefreshHash))
                    {
                        if (grant.RefreshRotation != null && now - grant.RefreshRotation.Value < RefreshReuseGracePeriod) throw new McpOAuthException("invalid_grant", "the refresh token has already been used");
                        //Reuse of a refresh token: stolen token, the grant is revoked (OAuth 2.1 section 4.3.1)
                        Store.DeleteGrant(grant.Id);
                        Helper.WriteLogEntry("Seal MCP", System.Diagnostics.EventLogEntryType.Warning, $"MCP OAuth: reuse of a refresh token of '{grant.ClientName}' for '{grant.UserName}', the authorization is revoked");
                    }
                    throw new McpOAuthException("invalid_grant", "invalid refresh token");
                }
                if (get(parameters, "client_id") != grant.ClientId) throw new McpOAuthException("invalid_grant", "the refresh token was not issued to this client");
                var resource = get(parameters, "resource");
                if (!string.IsNullOrEmpty(resource) && normalizeUrl(resource)?.TrimEnd('/') != normalizeUrl(grant.Resource)?.TrimEnd('/')) throw new McpOAuthException("invalid_target", "the resource is not the one of the authorization");
                var error = grant.CheckValidity(now, Configuration.McpTokenIdleDays);
                if (error != null)
                {
                    Store.DeleteGrant(grant.Id);
                    throw new McpOAuthException("invalid_grant", error);
                }
                grant.PreviousRefreshHash = grant.RefreshHash;
                grant.RefreshRotation = now;
                grant.LastUse = now;
                return issueTokens(grant, now);
            }
        }

        static readonly ConcurrentDictionary<string, object> _grantLocks = new ConcurrentDictionary<string, object>();
        static object getGrantLock(string id) => _grantLocks.GetOrAdd(id, _ => new object());

        static Dictionary<string, object> issueTokens(McpOAuthGrant grant, DateTime now)
        {
            var minutes = Math.Max(1, Configuration.McpOAuthAccessTokenMinutes);
            var accessToken = AccessTokenPrefix + grant.Id + "_" + Base64Url(RandomNumberGenerator.GetBytes(32));
            var refreshToken = RefreshTokenPrefix + grant.Id + "_" + Base64Url(RandomNumberGenerator.GetBytes(32));
            grant.AccessHash = McpFileTokenStore.GetHash(accessToken);
            grant.AccessExpiration = now.AddMinutes(minutes);
            //The access token does not outlive the grant
            if (grant.Expiration != null && grant.AccessExpiration > grant.Expiration.Value) grant.AccessExpiration = grant.Expiration.Value;
            grant.RefreshHash = McpFileTokenStore.GetHash(refreshToken);
            Store.SaveGrant(grant);
            return new Dictionary<string, object>()
            {
                { "access_token", accessToken },
                { "token_type", "Bearer" },
                { "expires_in", Math.Max(0, (int)(grant.AccessExpiration - now).TotalSeconds) },
                { "refresh_token", refreshToken },
                { "scope", grant.Scope }
            };
        }

        /// <summary>Returns the identifier of the grant of a token (access or refresh), null if the token has not the prefix given</summary>
        static McpOAuthGrant findGrant(string token, string prefix)
        {
            if (string.IsNullOrEmpty(token) || !token.StartsWith(prefix, StringComparison.Ordinal)) return null;
            var rest = token.Substring(prefix.Length);
            var index = rest.IndexOf('_');
            return index <= 0 ? null : Store.FindGrant(rest.Substring(0, index));
        }

        /// <summary>True if the token is an OAuth access token of the MCP server</summary>
        public static bool IsAccessToken(string token) => token != null && token.StartsWith(AccessTokenPrefix, StringComparison.Ordinal);

        static readonly ConcurrentDictionary<string, DateTime> _lastWrites = new ConcurrentDictionary<string, DateTime>();

        /// <summary>
        /// Authenticates an OAuth access token for the agent <paramref name="agentSlug"/>: returns its grant, or null with the reason in <paramref name="error"/> (invalid, expired, revoked, issued for another agent).
        /// </summary>
        public static McpOAuthGrant AuthenticateAccessToken(string accessToken, string agentSlug, DateTime now, out string error)
        {
            error = null;
            var grant = findGrant(accessToken, AccessTokenPrefix);
            if (grant == null || !equalHashes(McpFileTokenStore.GetHash(accessToken), grant.AccessHash))
            {
                error = "invalid or revoked access token";
                return null;
            }
            if (now >= grant.AccessExpiration)
            {
                error = "the access token has expired";
                return null;
            }
            error = grant.CheckValidity(now, Configuration.McpTokenIdleDays);
            if (error != null) return null;
            if (grant.AgentSlug != agentSlug)
            {
                error = $"the access token was issued for the MCP server of the agent '{grant.AgentSlug}'";
                return null;
            }
            //Last use written at most every 5 minutes
            grant.LastUse = now;
            if (!_lastWrites.TryGetValue(grant.Id, out var lastWrite) || now - lastWrite >= McpFileTokenStore.LastUseWriteInterval)
            {
                _lastWrites[grant.Id] = now;
                lock (getGrantLock(grant.Id))
                {
                    var current = Store.FindGrant(grant.Id);
                    if (current != null)
                    {
                        current.LastUse = now;
                        Store.SaveGrant(current);
                    }
                }
            }
            return grant;
        }

        /// <summary>
        /// Revokes a token (RFC 7009): a refresh token revokes its grant, an access token only itself. An unknown token is ignored.
        /// </summary>
        public static void RevokeToken(string token, string clientId)
        {
            var isRefresh = token != null && token.StartsWith(RefreshTokenPrefix, StringComparison.Ordinal);
            var grant = findGrant(token, isRefresh ? RefreshTokenPrefix : AccessTokenPrefix);
            if (grant == null || (!string.IsNullOrEmpty(clientId) && clientId != grant.ClientId)) return;
            var hash = McpFileTokenStore.GetHash(token);
            lock (getGrantLock(grant.Id))
            {
                if (isRefresh && equalHashes(hash, grant.RefreshHash)) Store.DeleteGrant(grant.Id);
                else if (!isRefresh && equalHashes(hash, grant.AccessHash))
                {
                    grant.AccessHash = null;
                    Store.SaveGrant(grant);
                }
            }
        }

        /// <summary>Revokes a grant of a user (from his profile), returns false if it does not exist</summary>
        public static bool RevokeGrant(string userName, string id)
        {
            var grant = Store.FindGrant(id);
            if (grant == null || !string.Equals(grant.UserName, userName, StringComparison.OrdinalIgnoreCase)) return false;
            return Store.DeleteGrant(id);
        }

        #endregion

        static string truncate(string text, int length) => text == null || text.Length <= length ? text : text.Substring(0, length);
    }
}
