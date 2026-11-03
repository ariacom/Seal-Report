//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.Model;
using System.IO;

namespace Test
{
    /// <summary>
    /// Guest user (login without account, SealSecurity.AllowGuest): set by the Security Provider script, profile never saved
    /// </summary>
    [TestClass]
    public class TestGuestUser
    {
        SealSecurity newSecurity()
        {
            var repository = Repository.Create();
            var security = new SealSecurity() { Repository = repository, AllowGuest = true, UseCustomScript = true };
            security.Groups.Add(new SecurityGroup() { Name = "Visiteur", PersFolderRight = PersonalFolderRight.Reports, PersonalFolderUpload = true });
            security.Groups.Add(new SecurityGroup() { Name = "Membres" });
            //Empty login = guest, otherwise a member
            security.Script = @"@{
    Seal.Model.SecurityUser user = Model;
    if (string.IsNullOrEmpty(user.WebUserName)) { user.IsGuest = true; user.AddSecurityGroup(""Visiteur""); }
    else user.AddSecurityGroup(""Membres"");
}";
            return security;
        }

        [TestMethod]
        public void GuestIsSetByTheProvider()
        {
            var user = new SecurityUser(newSecurity()) { WebUserName = "" };
            user.Authenticate();
            Assert.IsTrue(user.IsAuthenticated);
            Assert.IsTrue(user.IsGuest);
            Assert.AreEqual("Visiteur", user.DefaultGroup.Name);
            //No personal folder for a guest, even if his group gives one: the guests would share it
            Assert.AreEqual(PersonalFolderRight.None, user.PersonalFolderRight);
            Assert.IsFalse(user.PersonalFolderUpload);

            //A new authentication clears the flag
            user.WebUserName = "membre@test";
            user.SecurityGroups.Clear();
            user.Authenticate();
            Assert.IsFalse(user.IsGuest);
            Assert.AreEqual("Membres", user.DefaultGroup.Name);
        }

        [TestMethod]
        public void GuestByDefaultWhenAllowed()
        {
            //A script that does not know IsGuest: a login without user name is a guest when the guests are allowed
            var security = newSecurity();
            security.Script = @"@{
    Seal.Model.SecurityUser user = Model;
    user.AddSecurityGroup(string.IsNullOrEmpty(user.WebUserName) ? ""Visiteur"" : ""Membres"");
}";
            var user = new SecurityUser(security) { WebUserName = "" };
            user.Authenticate();
            Assert.IsTrue(user.IsAuthenticated);
            Assert.IsTrue(user.IsGuest);

            //Not allowed: a user without name, not a guest
            security.AllowGuest = false;
            user = new SecurityUser(security) { WebUserName = "" };
            user.Authenticate();
            Assert.IsFalse(user.IsGuest);

            //The script may set it back to false (e.g. Windows authentication)
            security.AllowGuest = true;
            security.Script = @"@{
    Seal.Model.SecurityUser user = Model;
    user.IsGuest = false;
    user.AddSecurityGroup(""Membres"");
}";
            user = new SecurityUser(security) { WebUserName = "" };
            user.Authenticate();
            Assert.IsFalse(user.IsGuest);

            //Never for a scheduled output or the MCP server
            security.Script = @"@{
    Seal.Model.SecurityUser user = Model;
    user.AddSecurityGroup(""Visiteur"");
}";
            user = new SecurityUser(security) { WebUserName = "", ForSchedule = true };
            user.Authenticate();
            Assert.IsFalse(user.IsGuest);
        }

        [TestMethod]
        public void GuestProfileIsNotSaved()
        {
            var path = Path.Combine(Path.GetTempPath(), "TestGuestUser_" + Path.GetRandomFileName() + ".xml");
            try
            {
                var guest = new SecurityUser(newSecurity()) { WebUserName = "" };
                guest.Authenticate();
                guest.Profile.Path = path;
                Assert.IsTrue(guest.SaveProfile());
                Assert.IsFalse(File.Exists(path));

                var member = new SecurityUser(newSecurity()) { WebUserName = "membre@test" };
                member.Authenticate();
                member.Profile.Path = path;
                Assert.IsTrue(member.SaveProfile());
                Assert.IsTrue(File.Exists(path));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
