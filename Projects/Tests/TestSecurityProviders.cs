//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Helpers;
using Seal.Model;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    /// <summary>
    /// Security Providers of the repository: syntax of the scripts and authentication from the user name only (user.ForSchedule: MCP server, web schedules, scheduled outputs)
    /// </summary>
    [TestClass]
    public class TestSecurityProviders
    {
        const string LoginId = "provider_test_user";

        static Repository _repository;
        static SecurityLogin _login;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _repository = Repository.Instance;
            _login = new SecurityLogin() { Id = LoginId, GroupIds = new List<string>() { _repository.Security.Groups.First().GUID } };
            _repository.Security.Logins.Add(_login);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _repository.Security.Logins.Remove(_login);
        }

        static SecurityProvider getProvider(string name)
        {
            var provider = _repository.Security.Providers.FirstOrDefault(i => i.Name == name);
            Assert.IsNotNull(provider, $"provider '{name}' not found");
            return provider;
        }

        [TestMethod]
        public void ScriptsCompile()
        {
            Assert.IsTrue(_repository.Security.Providers.Count >= 9);
            foreach (var provider in _repository.Security.Providers)
            {
                Assert.IsTrue(string.IsNullOrEmpty(provider.Error), $"{provider.Name}: {provider.Error}");
                try
                {
                    RazorCoreEngine.CheckSyntax(RazorHelper.GetFullScript(provider.Script), out _);
                }
                catch (System.Exception ex)
                {
                    Assert.Fail($"{provider.Name}: {ex.Message}");
                }
            }
        }

        [TestMethod]
        [DataRow("Basic Authentication")]
        [DataRow("JWT")]
        [DataRow("OpenId")]
        [DataRow("Claims Principal")]
        public void AuthenticationFromUserNameOnly(string providerName)
        {
            var script = getProvider(providerName).Script;

            var user = new SecurityUser(_repository.Security) { WebUserName = LoginId, ForSchedule = true };
            RazorHelper.CompileExecute(script, user);
            Assert.IsTrue(string.IsNullOrEmpty(user.Error), user.Error);
            Assert.AreEqual(1, user.SecurityGroups.Count);
            Assert.AreEqual(LoginId, user.Name);

            //Unknown user: refused
            user = new SecurityUser(_repository.Security) { WebUserName = "provider_unknown_user", ForSchedule = true };
            try
            {
                RazorHelper.CompileExecute(script, user);
            }
            catch { }
            Assert.AreEqual(0, user.SecurityGroups.Count);
        }
    }
}
