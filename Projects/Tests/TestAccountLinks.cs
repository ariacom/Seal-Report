//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Model;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Test
{
    /// <summary>
    /// Account links of the security (login page, profile) and the ticket signed for the application of the accounts
    /// </summary>
    [TestClass]
    public class TestAccountLinks
    {
        static byte[] fromBase64Url(string value)
        {
            var s = value.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
        }

        SealSecurity newSecurity()
        {
            var repository = Repository.Create();
            var security = new SealSecurity() { Repository = repository };
            security.AccountLinks = "Créer un compte|https://comptes.test/inscription|login\r\n" +
                                    "Mon compte|https://comptes.test/entree|profile\r\n" +
                                    "bad line\r\n" +
                                    "Équipe|https://comptes.test/entree?page=equipe|profile|Admin abonné; Administration";
            return security;
        }

        [TestMethod]
        public void LinksPerLocationAndGroup()
        {
            var security = newSecurity();
            var login = security.GetAccountLinks("login");
            Assert.AreEqual(1, login.Count);
            Assert.AreEqual("Créer un compte", login[0].Label);

            var member = new SecurityUser(security) { WebUserName = "membre@test" };
            member.SecurityGroups.Add(new SecurityGroup() { Name = "Abonnés" });
            Assert.AreEqual("Mon compte", string.Join(",", security.GetAccountLinks("profile", member).Select(i => i.Label)));

            var admin = new SecurityUser(security) { WebUserName = "admin@test" };
            admin.SecurityGroups.Add(new SecurityGroup() { Name = "Admin abonné" });
            var links = security.GetAccountLinks("profile", admin);
            Assert.AreEqual("Mon compte,Équipe", string.Join(",", links.Select(i => i.Label)));
            //The index is the line of the link: the url is found again from the index
            Assert.AreEqual(3, links[1].Index);
            Assert.IsNull(security.GetAccountLinkUrl(3, member));
            Assert.AreEqual("https://comptes.test/entree?page=equipe", security.GetAccountLinkUrl(3, admin));
        }

        [TestMethod]
        public void FindAccountLinkAfterLogin()
        {
            var security = newSecurity();
            var member = new SecurityUser(security) { WebUserName = "membre@test" };
            var admin = new SecurityUser(security) { WebUserName = "admin@test" };
            admin.SecurityGroups.Add(new SecurityGroup() { Name = "Admin abonné" });
            //Exact url (case and final slash ignored), allowed to the user
            Assert.AreEqual(3, security.FindAccountLink("https://comptes.test/entree?page=equipe", admin));
            Assert.AreEqual(1, security.FindAccountLink("HTTPS://comptes.test/entree/", member));
            //Link not allowed to the user, or another page of the portal: first link of the same site
            Assert.AreEqual(1, security.FindAccountLink("https://comptes.test/entree?page=equipe", member));
            Assert.AreEqual(1, security.FindAccountLink("https://comptes.test/abonnement", member));
            //Another site, a link of the login only, a bad address, a guest: nothing
            Assert.AreEqual(-1, security.FindAccountLink("https://evil.test/entree", member));
            Assert.AreEqual(-1, security.FindAccountLink("https://comptes.test:8443/entree", member));
            Assert.AreEqual(-1, security.FindAccountLink("/entree", member));
            Assert.AreEqual(-1, security.FindAccountLink("", member));
            var guest = new SecurityUser(security) { IsGuest = true };
            Assert.AreEqual(-1, security.FindAccountLink("https://comptes.test/entree", guest));
        }

        [TestMethod]
        public void AccountLinkAddressAfterLogout()
        {
            var security = newSecurity();
            //Home/AccountLogout: back to the page of the portal itself (any link, any group), never a ticket
            Assert.AreEqual("https://comptes.test/", security.FindAccountLinkAddress("https://comptes.test/"));
            Assert.AreEqual("https://comptes.test/abonnement?x=1", security.FindAccountLinkAddress("https://comptes.test/abonnement?x=1"));
            security.ClearAccountLinkSecret = "le secret partagé";
            Assert.IsFalse(security.FindAccountLinkAddress("https://comptes.test/entree").Contains("ticket"));
            //Another site, port or scheme, a relative or bad address: nothing (no open redirect)
            Assert.IsNull(security.FindAccountLinkAddress("https://evil.test/"));
            Assert.IsNull(security.FindAccountLinkAddress("https://comptes.test.evil.test/"));
            Assert.IsNull(security.FindAccountLinkAddress("https://comptes.test:8443/"));
            Assert.IsNull(security.FindAccountLinkAddress("http://comptes.test/"));
            Assert.IsNull(security.FindAccountLinkAddress("javascript:alert(1)"));
            Assert.IsNull(security.FindAccountLinkAddress("/entree"));
            Assert.IsNull(security.FindAccountLinkAddress(null));
            Assert.IsNull(new SealSecurity().FindAccountLinkAddress("https://comptes.test/"));
        }

        [TestMethod]
        public void TicketIsSigned()
        {
            var security = newSecurity();
            security.ClearAccountLinkSecret = "le secret partagé";
            Assert.AreNotEqual("le secret partagé", security.AccountLinkSecret);
            Assert.AreEqual("le secret partagé", security.ClearAccountLinkSecret);

            var user = new SecurityUser(security) { WebUserName = "membre@test" };
            var url = security.GetAccountLinkUrl(1, user);
            StringAssert.StartsWith(url, "https://comptes.test/entree?ticket=");
            var url2 = security.GetAccountLinkUrl(3, new SecurityUser(security) { WebUserName = "x", SecurityGroups = { new SecurityGroup() { Name = "Administration" } } });
            StringAssert.StartsWith(url2, "https://comptes.test/entree?page=equipe&ticket=");

            var ticket = Uri.UnescapeDataString(url.Substring(url.IndexOf("ticket=") + 7));
            var parts = ticket.Split('.');
            Assert.AreEqual(2, parts.Length);
            var payload = fromBase64Url(parts[0]);
            CollectionAssert.AreEqual(HMACSHA256.HashData(Encoding.UTF8.GetBytes("le secret partagé"), payload), fromBase64Url(parts[1]));
            var values = Encoding.UTF8.GetString(payload).Split('\n');
            Assert.AreEqual(4, values.Length);
            Assert.AreEqual("v1", values[0]);
            Assert.AreEqual("membre@test", values[1]);
            var expiry = DateTimeOffset.FromUnixTimeSeconds(long.Parse(values[2]));
            Assert.IsTrue(expiry > DateTimeOffset.UtcNow.AddSeconds(50) && expiry <= DateTimeOffset.UtcNow.AddSeconds(61));
            Assert.AreEqual(32, values[3].Length);
            //A new ticket each time (nonce)
            Assert.AreNotEqual(url, security.GetAccountLinkUrl(1, user));
        }

        [TestMethod]
        public void NoSecretNoTicket()
        {
            var security = newSecurity();
            Assert.AreEqual("https://comptes.test/entree", security.GetAccountLinkUrl(1, new SecurityUser(security) { WebUserName = "membre@test" }));
            Assert.AreEqual(0, new SealSecurity().GetAccountLinks("login").Count);
        }
    }
}
