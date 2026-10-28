//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.AI;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Test
{
    /// <summary>
    /// OAuth 2.1 authorization server of the MCP server: redirect URIs, clients (dynamic registration, Client ID Metadata Document), authorization code with PKCE, tokens bound to an agent, rotation and reuse of the refresh tokens, revocation
    /// </summary>
    [TestClass]
    public class TestMcpOAuth
    {
        const string BaseUrl = "https://seal.test/seal";
        const string ClaudeCallback = "https://claude.ai/api/mcp/auth_callback";

        static Repository _repository;
        static string _folder;
        static SecurityLogin _login;
        static AIAgentConfiguration _agent;
        static AIAgentChannel _channel;
        static string _slug;
        static bool _serverEnabled, _oauthEnabled;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _repository = Repository.Instance;
            _folder = Path.Combine(Path.GetTempPath(), "seal_test_mcp_oauth_" + Guid.NewGuid().ToString("N"));
            McpGateway.TokenStore = new McpFileTokenStore(_folder);
            McpOAuth.Store = new McpFileOAuthStore(_folder);
            var group = _repository.Security.Groups.First(g => g.AgentGUIDs.Count > 0);
            _login = new SecurityLogin() { Id = "mcp_oauth_test_user", GroupIds = new List<string>() { group.GUID } };
            _repository.Security.Logins.Add(_login);
            _agent = _repository.AIConfiguration.AIAgents.First(a => group.AgentGUIDs.Contains(a.GUID) && a.IsEnabled);
            _channel = _agent.Channel;
            _agent.Channel = AIAgentChannel.WebAndMcp;
            _slug = McpGateway.GetAgentSlug(_agent);
            _serverEnabled = _repository.AIConfiguration.McpServerEnabled;
            _oauthEnabled = _repository.AIConfiguration.McpOAuthEnabled;
            _repository.AIConfiguration.McpServerEnabled = true;
            _repository.AIConfiguration.McpOAuthEnabled = true;
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _repository.Security.Logins.Remove(_login);
            _agent.Channel = _channel;
            _repository.AIConfiguration.McpServerEnabled = _serverEnabled;
            _repository.AIConfiguration.McpOAuthEnabled = _oauthEnabled;
            McpGateway.TokenStore = null;
            McpOAuth.Store = null;
            McpOAuth.MetadataDocumentFetcher = null;
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        static string Resource => McpOAuth.GetResourceUrl(BaseUrl, _slug);

        static McpOAuthClient register(params string[] redirectUris)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new { client_name = "Claude", redirect_uris = redirectUris, grant_types = new[] { "authorization_code", "refresh_token" }, token_endpoint_auth_method = "none" });
            var result = McpOAuth.RegisterClient(json, DateTime.Now);
            return McpOAuth.Store.FindClient((string)result["client_id"]);
        }

        static Dictionary<string, string> authorizeParameters(string clientId, string verifier, string redirectUri = ClaudeCallback, string resource = null)
        {
            return new Dictionary<string, string>()
            {
                { "response_type", "code" },
                { "client_id", clientId },
                { "redirect_uri", redirectUri },
                { "state", "xyz" },
                { "code_challenge", McpOAuth.GetCodeChallenge(verifier) },
                { "code_challenge_method", "S256" },
                { "resource", resource ?? Resource },
                { "scope", "mcp offline_access" }
            };
        }

        static string newVerifier() => McpOAuth.Base64Url(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        /// <summary>Authorization by the test user and exchange of the code: returns the token response</summary>
        static Dictionary<string, object> getTokens(McpOAuthClient client, out string code, string redirectUri = ClaudeCallback)
        {
            var verifier = newVerifier();
            var request = McpOAuth.ValidateAuthorizationRequest(authorizeParameters(client.ClientId, verifier, redirectUri), BaseUrl, DateTime.Now);
            code = McpOAuth.Authorize(request, _login.Id, true, DateTime.Now);
            return McpOAuth.ExecuteTokenRequest(new Dictionary<string, string>()
            {
                { "grant_type", "authorization_code" }, { "code", code }, { "client_id", client.ClientId }, { "redirect_uri", redirectUri }, { "code_verifier", verifier }, { "resource", Resource }
            }, DateTime.Now);
        }

        static void assertOAuthError(string error, Action action, bool? canRedirect = null)
        {
            try
            {
                action();
                Assert.Fail($"OAuth error '{error}' expected");
            }
            catch (McpOAuthException ex)
            {
                Assert.AreEqual(error, ex.Error, ex.Message);
                if (canRedirect != null) Assert.AreEqual(canRedirect.Value, ex.CanRedirect, ex.Message);
            }
        }

        [TestMethod]
        public void RedirectUris()
        {
            //Loopback: the port is ignored (RFC 8252), not the host or the path
            Assert.IsTrue(McpOAuth.RedirectMatches("http://localhost/callback", "http://localhost:3118/callback"));
            Assert.IsTrue(McpOAuth.RedirectMatches("http://127.0.0.1/callback", "http://127.0.0.1:50000/callback"));
            Assert.IsFalse(McpOAuth.RedirectMatches("http://localhost/callback", "http://127.0.0.1:3118/callback"));
            Assert.IsFalse(McpOAuth.RedirectMatches("http://localhost/callback", "http://localhost:3118/other"));
            //Other URIs: exact match
            Assert.IsTrue(McpOAuth.RedirectMatches(ClaudeCallback, ClaudeCallback));
            Assert.IsFalse(McpOAuth.RedirectMatches(ClaudeCallback, ClaudeCallback + "x"));
            Assert.IsFalse(McpOAuth.RedirectMatches(ClaudeCallback, "https://claude.ai:8443/api/mcp/auth_callback"));
            Assert.IsFalse(McpOAuth.RedirectMatches(ClaudeCallback, ClaudeCallback + "#frag"));
            //'*': any HTTPS or loopback URI
            Assert.IsTrue(McpOAuth.RedirectMatches("*", "https://cursor.sh/callback"));
            Assert.IsFalse(McpOAuth.RedirectMatches("*", "http://evil.example.com/callback"));

            //Default configuration: Claude only
            Assert.IsTrue(McpOAuth.IsRedirectAllowed(ClaudeCallback));
            Assert.IsTrue(McpOAuth.IsRedirectAllowed("http://localhost:4567/callback"));
            Assert.IsFalse(McpOAuth.IsRedirectAllowed("https://evil.example.com/callback"));

            //Server-side request forgery of the Client ID Metadata Documents
            Assert.IsTrue(McpOAuth.IsPrivateAddress(IPAddress.Parse("127.0.0.1")));
            Assert.IsTrue(McpOAuth.IsPrivateAddress(IPAddress.Parse("10.1.2.3")));
            Assert.IsTrue(McpOAuth.IsPrivateAddress(IPAddress.Parse("192.168.1.1")));
            Assert.IsTrue(McpOAuth.IsPrivateAddress(IPAddress.Parse("169.254.169.254")));
            Assert.IsTrue(McpOAuth.IsPrivateAddress(IPAddress.Parse("::1")));
            Assert.IsTrue(McpOAuth.IsPrivateAddress(IPAddress.Parse("::ffff:10.0.0.1")));
            Assert.IsFalse(McpOAuth.IsPrivateAddress(IPAddress.Parse("160.79.104.10")));
        }

        [TestMethod]
        public void Metadata()
        {
            var server = McpOAuth.GetAuthorizationServerMetadata(BaseUrl);
            Assert.AreEqual(BaseUrl, server["issuer"]);
            Assert.AreEqual(BaseUrl + "/mcp-oauth/token", server["token_endpoint"]);
            CollectionAssert.Contains((string[])server["code_challenge_methods_supported"], "S256");
            CollectionAssert.Contains((string[])server["token_endpoint_auth_methods_supported"], "none");
            Assert.AreEqual(true, server["client_id_metadata_document_supported"], "Claude uses a Client ID Metadata Document only if advertised");
            Assert.IsTrue(server.ContainsKey("registration_endpoint"));

            var resource = McpOAuth.GetProtectedResourceMetadata(BaseUrl, _slug, _agent.Name);
            Assert.AreEqual(BaseUrl + "/mcp/" + _slug, resource["resource"], "the resource must be the URL entered in the client");
            CollectionAssert.AreEqual(new[] { BaseUrl }, (string[])resource["authorization_servers"]);
            Assert.AreEqual(BaseUrl + "/.well-known/oauth-protected-resource/mcp/" + _slug, McpOAuth.GetResourceMetadataUrl(BaseUrl, _slug));
        }

        [TestMethod]
        public void AuthorizationRequest()
        {
            var client = register(ClaudeCallback);
            var verifier = newVerifier();

            //Errors shown to the user: unknown client, redirect URI not registered or not allowed
            assertOAuthError("invalid_client", () => McpOAuth.ValidateAuthorizationRequest(authorizeParameters("unknown", verifier), BaseUrl, DateTime.Now), false);
            assertOAuthError("invalid_request", () => McpOAuth.ValidateAuthorizationRequest(authorizeParameters(client.ClientId, verifier, "https://claude.ai/other"), BaseUrl, DateTime.Now), false);
            //Errors sent to the client: no PKCE, resource of another server or unknown agent
            var parameters = authorizeParameters(client.ClientId, verifier);
            parameters.Remove("code_challenge_method");
            assertOAuthError("invalid_request", () => McpOAuth.ValidateAuthorizationRequest(parameters, BaseUrl, DateTime.Now), true);
            assertOAuthError("invalid_target", () => McpOAuth.ValidateAuthorizationRequest(authorizeParameters(client.ClientId, verifier, resource: "https://other.test/mcp/" + _slug), BaseUrl, DateTime.Now), true);
            assertOAuthError("invalid_target", () => McpOAuth.ValidateAuthorizationRequest(authorizeParameters(client.ClientId, verifier, resource: BaseUrl + "/mcp/no-such-agent"), BaseUrl, DateTime.Now), true);

            //Valid request: host and trailing slash normalized
            var request = McpOAuth.ValidateAuthorizationRequest(authorizeParameters(client.ClientId, verifier, resource: "https://SEAL.test/seal/mcp/" + _slug + "/"), BaseUrl, DateTime.Now);
            Assert.AreEqual(_slug, request.AgentSlug);
            Assert.AreEqual("claude.ai", request.RedirectHost);
            Assert.IsFalse(request.IsLoopbackRedirect);
            Assert.AreSame(request, McpOAuth.GetAuthorizationRequest(request.Id, DateTime.Now));
            Assert.IsNull(McpOAuth.GetAuthorizationRequest(request.Id, DateTime.Now.AddHours(1)), "expired");

            //Refusal of the user: no code
            Assert.IsNull(McpOAuth.Authorize(request, _login.Id, false, DateTime.Now));
            Assert.IsNull(McpOAuth.GetAuthorizationRequest(request.Id, DateTime.Now), "the request is used once");

            var url = McpOAuth.GetRedirectUrl(ClaudeCallback, "a b", BaseUrl, "c0de");
            Assert.AreEqual(ClaudeCallback + "?code=c0de&state=a%20b&iss=" + Uri.EscapeDataString(BaseUrl), url);

            //Dynamic registration: only the redirect URIs allowed
            assertOAuthError("invalid_redirect_uri", () => register("https://evil.example.com/callback"));
            assertOAuthError("invalid_redirect_uri", () => register("http://claude.ai/api/mcp/auth_callback"));
        }

        [TestMethod]
        public void TokensAndRefresh()
        {
            var client = register(ClaudeCallback);
            var tokens = getTokens(client, out var code);
            var accessToken = (string)tokens["access_token"];
            var refreshToken = (string)tokens["refresh_token"];
            StringAssert.StartsWith(accessToken, McpOAuth.AccessTokenPrefix);
            Assert.AreEqual("Bearer", tokens["token_type"]);
            Assert.IsTrue((int)tokens["expires_in"] > 0);
            var grant = McpOAuth.Store.GetUserGrants(_login.Id).Single(g => g.ClientId == client.ClientId);
            Assert.IsFalse(File.ReadAllText(Path.Combine(_folder, "OAuthGrants", grant.Id + ".json")).Contains(accessToken.Substring(20)), "the clear tokens must not be stored");
            Assert.AreEqual(_repository.AIConfiguration.McpOAuthGrantMaxDays, (int)Math.Round((grant.Expiration.Value - grant.Creation).TotalDays));

            //The access token authenticates the user for the agent of the authorization only
            var context = McpGateway.Authenticate("Bearer " + accessToken, _slug, "10.0.0.1", out var error, out var unauthorized);
            Assert.IsNotNull(context?.Agent, error);
            Assert.AreEqual(grant.Id, context.Grant.Id);
            Assert.IsTrue(context.User.ForSchedule);
            Assert.IsNull(McpGateway.Authenticate("Bearer " + accessToken, "other-agent", null, out error, out unauthorized));
            Assert.IsTrue(unauthorized, "token of another resource");
            Assert.IsNull(McpGateway.Authenticate("Bearer " + accessToken + "x", _slug, null, out error, out unauthorized));
            Assert.IsNull(McpOAuth.AuthenticateAccessToken(accessToken, _slug, DateTime.Now.AddHours(2), out error), "expired access token");

            //The code is used once: its reuse revokes the authorization
            assertOAuthError("invalid_grant", () => McpOAuth.ExecuteTokenRequest(new Dictionary<string, string>() { { "grant_type", "authorization_code" }, { "code", code }, { "client_id", client.ClientId }, { "redirect_uri", ClaudeCallback }, { "code_verifier", newVerifier() } }, DateTime.Now));
            Assert.IsNull(McpOAuth.Store.FindGrant(grant.Id));
            Assert.IsNull(McpGateway.Authenticate("Bearer " + accessToken, _slug, null, out error, out unauthorized));

            //New authorization: refresh with rotation
            tokens = getTokens(client, out _);
            accessToken = (string)tokens["access_token"];
            refreshToken = (string)tokens["refresh_token"];
            Dictionary<string, string> refreshRequest(string token) => new Dictionary<string, string>() { { "grant_type", "refresh_token" }, { "refresh_token", token }, { "client_id", client.ClientId } };
            assertOAuthError("invalid_grant", () => McpOAuth.ExecuteTokenRequest(new Dictionary<string, string>() { { "grant_type", "refresh_token" }, { "refresh_token", refreshToken }, { "client_id", "other" } }, DateTime.Now));
            var refreshed = McpOAuth.ExecuteTokenRequest(refreshRequest(refreshToken), DateTime.Now);
            Assert.AreNotEqual(refreshToken, refreshed["refresh_token"]);
            Assert.IsNull(McpOAuth.AuthenticateAccessToken(accessToken, _slug, DateTime.Now, out error), "the previous access token is replaced");
            Assert.IsNotNull(McpOAuth.AuthenticateAccessToken((string)refreshed["access_token"], _slug, DateTime.Now, out error), error);

            //Reuse of the previous refresh token: refused, within the grace period the authorization is kept, after it is revoked
            assertOAuthError("invalid_grant", () => McpOAuth.ExecuteTokenRequest(refreshRequest(refreshToken), DateTime.Now));
            Assert.IsNotNull(McpOAuth.AuthenticateAccessToken((string)refreshed["access_token"], _slug, DateTime.Now, out error), "concurrent refresh");
            assertOAuthError("invalid_grant", () => McpOAuth.ExecuteTokenRequest(refreshRequest(refreshToken), DateTime.Now.AddMinutes(5)));
            Assert.IsNull(McpOAuth.AuthenticateAccessToken((string)refreshed["access_token"], _slug, DateTime.Now, out error), "stolen refresh token: authorization revoked");
            assertOAuthError("invalid_grant", () => McpOAuth.ExecuteTokenRequest(refreshRequest((string)refreshed["refresh_token"]), DateTime.Now));

            //End of the authorization: the refresh is refused
            tokens = getTokens(client, out _);
            assertOAuthError("invalid_grant", () => McpOAuth.ExecuteTokenRequest(refreshRequest((string)tokens["refresh_token"]), DateTime.Now.AddDays(_repository.AIConfiguration.McpOAuthGrantMaxDays + 1)));

            //Revocation: refresh token (RFC 7009) and by the user in his profile
            tokens = getTokens(client, out _);
            McpOAuth.RevokeToken((string)tokens["refresh_token"], client.ClientId);
            Assert.IsNull(McpOAuth.AuthenticateAccessToken((string)tokens["access_token"], _slug, DateTime.Now, out error));
            tokens = getTokens(client, out _);
            var id = McpOAuth.Store.GetUserGrants(_login.Id).Single(g => g.ClientId == client.ClientId).Id;
            Assert.IsFalse(McpOAuth.RevokeGrant("someone else", id));
            Assert.IsTrue(McpOAuth.RevokeGrant(_login.Id.ToUpper(), id));
            Assert.IsNull(McpGateway.Authenticate("Bearer " + tokens["access_token"], _slug, null, out error, out unauthorized));

            //OAuth disabled: the access tokens are refused
            tokens = getTokens(client, out _);
            _repository.AIConfiguration.McpOAuthEnabled = false;
            try
            {
                Assert.IsNull(McpGateway.Authenticate("Bearer " + tokens["access_token"], _slug, null, out error, out unauthorized));
                Assert.IsTrue(unauthorized);
            }
            finally
            {
                _repository.AIConfiguration.McpOAuthEnabled = true;
            }
        }

        [TestMethod]
        public void PkceAndCode()
        {
            var client = register("http://127.0.0.1/callback");
            var verifier = newVerifier();
            //Claude Code: loopback redirect with an ephemeral port
            var redirectUri = "http://127.0.0.1:51234/callback";
            var request = McpOAuth.ValidateAuthorizationRequest(authorizeParameters(client.ClientId, verifier, redirectUri), BaseUrl, DateTime.Now);
            Assert.IsTrue(request.IsLoopbackRedirect);
            var code = McpOAuth.Authorize(request, _login.Id, true, DateTime.Now);
            Dictionary<string, string> exchange(string v, string r = null, string c = null) => new Dictionary<string, string>() { { "grant_type", "authorization_code" }, { "code", code }, { "client_id", c ?? client.ClientId }, { "redirect_uri", r ?? redirectUri }, { "code_verifier", v } };
            assertOAuthError("invalid_grant", () => McpOAuth.ExecuteTokenRequest(exchange(newVerifier()), DateTime.Now));
            assertOAuthError("invalid_grant", () => McpOAuth.ExecuteTokenRequest(exchange(verifier, "http://127.0.0.1:9999/callback"), DateTime.Now));
            assertOAuthError("invalid_grant", () => McpOAuth.ExecuteTokenRequest(exchange(verifier, null, "other"), DateTime.Now));
            assertOAuthError("invalid_grant", () => McpOAuth.ExecuteTokenRequest(exchange(verifier), DateTime.Now.AddMinutes(5)), null);
            assertOAuthError("unsupported_grant_type", () => McpOAuth.ExecuteTokenRequest(new Dictionary<string, string>() { { "grant_type", "client_credentials" } }, DateTime.Now));
            Assert.IsNotNull(McpOAuth.ExecuteTokenRequest(exchange(verifier), DateTime.Now)["access_token"]);
        }

        [TestMethod]
        public void MetadataDocument()
        {
            const string clientId = "https://claude.ai/oauth/claude-code-client-metadata";
            var document = "{\"client_id\":\"" + clientId + "\",\"client_name\":\"Claude Code\",\"redirect_uris\":[\"http://localhost/callback\",\"http://127.0.0.1/callback\"],\"token_endpoint_auth_method\":\"none\"}";
            var fetches = 0;
            McpOAuth.MetadataDocumentFetcher = url => { fetches++; return url == clientId ? document : throw new Exception("not found"); };
            McpOAuth.ClearMetadataDocumentCache();
            try
            {
                var client = McpOAuth.ResolveClient(clientId);
                Assert.AreEqual("Claude Code", client.ClientName);
                Assert.IsTrue(client.IsMetadataDocument);
                McpOAuth.ResolveClient(clientId);
                Assert.AreEqual(1, fetches, "document cached");

                var verifier = newVerifier();
                var request = McpOAuth.ValidateAuthorizationRequest(authorizeParameters(clientId, verifier, "http://localhost:3118/callback"), BaseUrl, DateTime.Now);
                var code = McpOAuth.Authorize(request, _login.Id, true, DateTime.Now);
                var tokens = McpOAuth.ExecuteTokenRequest(new Dictionary<string, string>() { { "grant_type", "authorization_code" }, { "code", code }, { "client_id", clientId }, { "redirect_uri", "http://localhost:3118/callback" }, { "code_verifier", verifier } }, DateTime.Now);
                Assert.IsNotNull(McpOAuth.AuthenticateAccessToken((string)tokens["access_token"], _slug, DateTime.Now, out var error), error);

                //Invalid documents
                assertOAuthError("invalid_client", () => McpOAuth.ParseMetadataDocument("https://evil.example.com/client", document));
                assertOAuthError("invalid_client", () => McpOAuth.ParseMetadataDocument(clientId, document.Replace("\"none\"", "\"private_key_jwt\"")));
                assertOAuthError("invalid_client", () => McpOAuth.ParseMetadataDocument(clientId, "{\"client_id\":\"" + clientId + "\"}"));
                assertOAuthError("invalid_client", () => McpOAuth.ResolveClient("https://unknown.example.com/client"));
                Assert.IsFalse(McpOAuth.IsMetadataDocumentId("http://claude.ai/client"), "HTTPS only");
                Assert.IsFalse(McpOAuth.IsMetadataDocumentId("https://claude.ai/"), "a path is required");
            }
            finally
            {
                McpOAuth.MetadataDocumentFetcher = null;
                McpOAuth.ClearMetadataDocumentCache();
            }
        }
    }
}
