using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus;
    using SunokoLibrary.Web.GooglePlus.Primitive;

    [TestClass]
    public class PlatformClientTest
    {
        static PlatformClient client;

        [ClassInitialize]
        public static void HardCodeMapperInitialize(TestContext testContext)
        {
            client = new PlatformClient(
                new Uri("https://plus.google.com"), new Uri("https://talkgadget.google.com"),
                new System.Net.CookieContainer(), new DefaultAccessorStab());
        }

        [TestMethod, TestCategory("PlatformClient")]
        public async Task UpdateHomeInitDataAsyncTest()
        {
            await client.UpdateHomeInitDataAsync(false);
            Assert.AreEqual("atValue_hmInit", client.AtValue);
            Assert.AreEqual("ejxValue_hmInit", client.EjxValue);
            Assert.AreEqual("pvtValue_hmInit", client.PvtValue);
        }
        [TestMethod, TestCategory("RelationContainer")]
        public async Task UpdateCirclesAndBlockAsyncTest()
        {
            await client.UpdateHomeInitDataAsync(false);
            await client.Relation.UpdateCirclesAndBlockAsync(false, CircleUpdateLevel.Loaded);

            Assert.AreEqual(2, client.Relation.Circles.Count);
            for (var i = 1; i <= client.Relation.Circles.Count; i++)
            {
                var item = client.Relation.Circles[i - 1];
                Assert.AreEqual(string.Format("cid{0:00}", i), item.Id);
                Assert.AreEqual(string.Format("cname{0:00}_hmInit", i), item.Name);
                Assert.AreEqual(false, item.IsLoadedMember);
            }

            await client.Relation.UpdateCirclesAndBlockAsync(true, CircleUpdateLevel.LoadedWithMembers);
            Assert.AreEqual(3, client.Relation.Circles.Count);
            foreach (var item in client.Relation.Circles.Zip(
                new[]{
                    new { cid = "cid02", cname = "cname02_hmInit", cmem = new string[] { "01", "02" }},
                    new { cid = "cid03", cname = "cname03", cmem = new string[] { "02", "03" }},
                    new { cid = "cid04", cname = "cname04", cmem = new string[] { }}
                },
                (inf, expecteds) => new { Info = inf, Expecteds = expecteds }))
            {
                Assert.AreEqual(item.Expecteds.cid, item.Info.Id);
                Assert.AreEqual(item.Expecteds.cname, item.Info.Name);
                Assert.AreEqual(true, item.Info.IsLoadedMember);
                Assert.AreEqual(item.Info.GetMembers().Count(), item.Expecteds.cmem.Length);
                foreach (var itemA in item.Info.GetMembers().Zip(item.Expecteds.cmem, (inf, expecteds) => new { Info = inf, Expecteds = expecteds }))
                {
                    Assert.AreEqual(itemA.Expecteds, itemA.Info.Id);
                }
            }
            foreach(var item in client.Relation.YourCircle.GetMembers().Select((inf, idx) => new { Info = inf, Index = idx }))
            {
                Assert.AreEqual(string.Format("{0:00}", item.Index + 1), item.Info.Id);
                Assert.AreEqual(string.Format("pname{0:00}_circle", item.Index + 1), item.Info.Name);
                Assert.AreEqual(string.Format("http://picon/s0/{0:00}_circle", item.Index + 1), item.Info.IconImageUrl);
            }
        }
        [TestMethod, TestCategory("RelationContainer")]
        public async Task UpdateFollowerAsyncTest()
        {
            await client.UpdateHomeInitDataAsync(false);
            await client.Relation.UpdateFollowerAsync(false);

            Assert.AreEqual(3, client.Relation.FollowerList.GetMembers().Count());
            foreach (var item in client.Relation.FollowerList.GetMembers().Zip(
                new[]{
                    new { cid = "02", cname = "pname02_follower" },
                    new { cid = "03", cname = "pname03_follower" },
                    new { cid = "04", cname = "pname04_follower" }
                },
                (inf, expecteds) => new { Info = inf, Expecteds = expecteds }))
            {
                Assert.AreEqual(item.Expecteds.cid, item.Info.Id);
                Assert.AreEqual(item.Expecteds.cname, item.Info.Name);
            }
        }
        [TestMethod, TestCategory("RelationContainer")]
        public async Task UpdateIgnoreAsyncTest()
        {
            await client.UpdateHomeInitDataAsync(false);
            await client.Relation.UpdateIgnoreAsync(false);

            Assert.AreEqual(3, client.Relation.IgnoreList.GetMembers().Count());
            foreach (var item in client.Relation.IgnoreList.GetMembers().Zip(
                new[]{
                    new { cid = "05", cname = "pname05_ignore" },
                    new { cid = "06", cname = "pname06_ignore" },
                    new { cid = "07", cname = "pname07_ignore" }
                },
                (inf, expecteds) => new { Info = inf, Expecteds = expecteds }))
            {
                Assert.AreEqual(item.Expecteds.cid, item.Info.Id);
                Assert.AreEqual(item.Expecteds.cname, item.Info.Name);
            }
        }
        [TestMethod, TestCategory("RelationContainer")]
        public async Task GetProfileOfMeAsyncTest()
        {
            await client.UpdateHomeInitDataAsync(false);
            var profile = await client.Relation.GetProfileOfMeAsync(false);

            Assert.AreEqual("00", profile.Id);
            Assert.AreEqual("pname00_profileMe", profile.Name);
            Assert.AreEqual("pgree00_profileMe", profile.GreetingText);
            Assert.AreEqual("http://picon/s0/00_profileMe", profile.IconImageUrl);
            Assert.AreEqual(ProfileUpdateApiFlag.ProfileGet, profile.LoadedApiTypes & ProfileUpdateApiFlag.ProfileGet);
        }
        [TestMethod, TestCategory("RelationContainer"), Priority(0)]
        public async Task GetProfileOfTest()
        {
            var profile = client.Relation.GetProfileOf("99");
            Assert.AreEqual("99", profile.Id);
            Assert.AreEqual(ProfileUpdateApiFlag.Unloaded, profile.LoadedApiTypes);

            await profile.UpdateProfileGetAsync(false);
            Assert.AreEqual("99", profile.Id);
            Assert.AreEqual("pname99_profileFull", profile.Name);
            Assert.AreEqual("pfnam99_profileFull", profile.FirstName);
            Assert.AreEqual("plnam99_profileFull", profile.LastName);
            Assert.AreEqual("pintr99_profileFull", profile.Introduction);
            Assert.AreEqual("pbrag99_profileFull", profile.BraggingRights);
            Assert.AreEqual("poccu99_profileFull", profile.Occupation);
            Assert.AreEqual("pgree99_profileFull", profile.GreetingText);
            Assert.AreEqual("pnick99_profileFull", profile.NickName);
            Assert.AreEqual("http://picon/s0/99_profileFull", profile.IconImageUrl);
            Assert.AreEqual(RelationType.Engaged, profile.Relationship);
            Assert.AreEqual(GenderType.Other, profile.GenderType);
            Assert.AreEqual(ProfileUpdateApiFlag.ProfileGet, profile.LoadedApiTypes);
        }
        [TestMethod, TestCategory("NotificationContainer"), Priority(0)]
        public async Task GetNotificationsTest()
        {
            var target = client.Notification.GetNotifications(NotificationsFilter.All);
            await target.UpdateAsync(15);
            await target.UpdateAsync(15);
        }
    }
}
