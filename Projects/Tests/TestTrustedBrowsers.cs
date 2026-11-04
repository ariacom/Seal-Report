//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Model;
using System;

namespace Test
{
    /// <summary>
    /// Trusted browsers of the Two-Factor Authentication: signed cookie that skips the security code (SealSecurity.TwoFATrustDays)
    /// </summary>
    [TestClass]
    public class TestTrustedBrowsers
    {
        const string Secret = "0123456789abcdef-secret";

        SealSecurity newSecurity()
        {
            var repository = Repository.Create();
            var security = new SealSecurity() { Repository = repository, UseCustomScript = true, AllowGuest = true, EnableTwoFA = true, TwoFATrustDays = 30 };
            security.ClearTrustedBrowserSecret = Secret;
            security.Groups.Add(new SecurityGroup() { Name = "Membres" });
            security.Groups.Add(new SecurityGroup() { Name = "Visiteur" });
            security.Script = @"@{
    Seal.Model.SecurityUser user = Model;
    if (string.IsNullOrEmpty(user.WebUserName)) { user.IsGuest = true; user.AddSecurityGroup(""Visiteur""); }
    else if (user.WebPassword == ""ok"") user.AddSecurityGroup(""Membres"");
}";
            //Code generated without e-mail
            security.TwoFAGenerationScript = @"@{
    Seal.Model.SecurityUser user = Model;
    if (user.IsGuest) return;
    user.SecurityCode = ""123456"";
    user.SecurityCodeGeneration = DateTime.Now;
}";
            return security;
        }

        [TestMethod]
        public void TokenSignatureExpiryAndUser()
        {
            var now = DateTime.UtcNow;
            var token = SealSecurity.GetTrustedBrowserToken("Alice", Secret, now.AddDays(30));
            Assert.IsTrue(SealSecurity.IsTrustedBrowserToken(token, "alice", Secret, now), "user name not case sensitive");
            Assert.IsTrue(SealSecurity.IsTrustedBrowserToken(token, " Alice ", Secret, now));
            Assert.IsFalse(SealSecurity.IsTrustedBrowserToken(token, "bob", Secret, now), "other user");
            Assert.IsFalse(SealSecurity.IsTrustedBrowserToken(token, "Alice", Secret, now.AddDays(31)), "expired");
            Assert.IsFalse(SealSecurity.IsTrustedBrowserToken(token, "Alice", "another-secret-of-the-instance", now), "secret changed");
            Assert.IsFalse(SealSecurity.IsTrustedBrowserToken(token + "x", "Alice", Secret, now), "signature altered");
            Assert.IsFalse(SealSecurity.IsTrustedBrowserToken("", "Alice", Secret, now));
            Assert.IsFalse(SealSecurity.IsTrustedBrowserToken("abc", "Alice", Secret, now));

            //Payload changed (other user) with the old signature
            var parts = token.Split('.');
            var forged = SealSecurity.GetTrustedBrowserToken("Bob", Secret, now.AddDays(30)).Split('.')[0] + "." + parts[1];
            Assert.IsFalse(SealSecurity.IsTrustedBrowserToken(forged, "Bob", Secret, now));

            //An account link ticket (same secret) is not a trusted browser token
            var ticket = SealSecurity.GetAccountTicket("Alice", Secret, now.AddDays(30));
            Assert.IsFalse(SealSecurity.IsTrustedBrowserToken(ticket, "Alice", Secret, now));
        }

        [TestMethod]
        public void TrustActiveOnlyWithDaysAndSecret()
        {
            var security = newSecurity();
            Assert.IsTrue(security.TwoFATrustActive);
            security.TwoFATrustDays = 0;
            Assert.IsFalse(security.TwoFATrustActive);
            security.TwoFATrustDays = 30;
            security.ClearTrustedBrowserSecret = "short";
            Assert.IsFalse(security.TwoFATrustActive, "secret of less than 16 characters");
            security.ClearTrustedBrowserSecret = Secret;
            security.EnableTwoFA = false;
            Assert.IsFalse(security.TwoFATrustActive);
        }

        [TestMethod]
        public void TrustedBrowserSkipsTheCodeNotThePassword()
        {
            var security = newSecurity();
            var token = SealSecurity.GetTrustedBrowserToken("alice", Secret, DateTime.UtcNow.AddDays(30));

            //No cookie: code required
            var user = new SecurityUser(security) { WebUserName = "alice", WebPassword = "ok" };
            user.Authenticate();
            Assert.AreEqual("123456", user.SecurityCode);
            Assert.IsFalse(user.IsAuthenticated);
            Assert.IsFalse(user.TrustedBrowser);

            //Trusted browser: no code, strong authentication
            user = new SecurityUser(security) { WebUserName = "alice", WebPassword = "ok", TrustedBrowserToken = token };
            user.Authenticate();
            Assert.IsTrue(string.IsNullOrEmpty(user.SecurityCode));
            Assert.IsTrue(user.IsAuthenticated);
            Assert.IsTrue(user.TrustedBrowser);
            Assert.IsTrue(user.StrongAuthentication);

            //Wrong password: the cookie does not help
            user = new SecurityUser(security) { WebUserName = "alice", WebPassword = "bad", TrustedBrowserToken = token };
            user.Authenticate();
            Assert.IsFalse(user.IsAuthenticated);
            Assert.IsFalse(user.TrustedBrowser);
            Assert.IsFalse(user.StrongAuthentication);

            //Cookie of another user: code required
            user = new SecurityUser(security) { WebUserName = "bob", WebPassword = "ok", TrustedBrowserToken = token };
            user.Authenticate();
            Assert.AreEqual("123456", user.SecurityCode);
            Assert.IsFalse(user.TrustedBrowser);

            //Feature disabled (0 day): the cookie is ignored
            security.TwoFATrustDays = 0;
            user = new SecurityUser(security) { WebUserName = "alice", WebPassword = "ok", TrustedBrowserToken = token };
            user.Authenticate();
            Assert.AreEqual("123456", user.SecurityCode);
            Assert.IsFalse(user.TrustedBrowser);
        }

        [TestMethod]
        public void GuestIsNeverATrustedBrowser()
        {
            var security = newSecurity();
            var user = new SecurityUser(security) { WebUserName = "", TrustedBrowserToken = SealSecurity.GetTrustedBrowserToken("", Secret, DateTime.UtcNow.AddDays(30)) };
            user.Authenticate();
            Assert.IsTrue(user.IsGuest);
            Assert.IsFalse(user.TrustedBrowser);
            Assert.IsFalse(user.StrongAuthentication);
        }
    }
}
