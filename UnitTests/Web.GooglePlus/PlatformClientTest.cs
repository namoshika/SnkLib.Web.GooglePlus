using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Web.GooglePlus
{
    using SunokoLibrary.Collections.Generic;
    using SunokoLibrary.Web.GooglePlus;
    using SunokoLibrary.Web.GooglePlus.Primitive;
    using SunokoLibrary.Web.GooglePlus.Utility;

    [TestClass]
    public class PlatformClientTest
    {
        static PlatformClient client;
        static DefaultAccessorStub stub = new DefaultAccessorStub();
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            client = new PlatformClient(
                new Uri("https://plus.google.com"), new Uri("https://talkgadget.google.com"), new System.Net.CookieContainer(), stub,
                new CacheDictionary<string, ProfileCache, ProfileData>(1200, 400, true, dt => new ProfileCache() { Value = dt }),
                new CacheDictionary<string, ActivityCache, ActivityData>(1200, 400, true, dt => new ActivityCache() { Value = dt }));
        }
        [TestCleanup]
        public void TestCleanup()
        {
            client.Dispose();
        }

        [TestMethod, TestCategory("PlatformClient")]
        public async Task UpdateHomeInitDataAsyncTest()
        {
            Assert.AreEqual(false, client.IsLoadedHomeInitData);
            Utility.AssertException<InvalidOperationException>(() => client.AtValue);
            Utility.AssertException<InvalidOperationException>(() => client.EjxValue);
            Utility.AssertException<InvalidOperationException>(() => client.PvtValue);
            await client.UpdateHomeInitDataAsync(false);
            Assert.AreEqual("AtValue00_GetInitDataAsync", client.AtValue);
            Assert.AreEqual("EjxValue00_GetInitDataAsync", client.EjxValue);
            Assert.AreEqual("PvtValue00_GetInitDataAsync", client.PvtValue);
            Assert.AreEqual(true, client.IsLoadedHomeInitData);
        }
        [TestMethod, TestCategory("PeopleContainer")]
        public async Task UpdateCirclesAndBlockAsyncTest()
        {
            await client.UpdateHomeInitDataAsync(false);

            //Loaded
            await client.People.UpdateCirclesAndBlockAsync(false, CircleUpdateLevel.Loaded);
            Assert.AreEqual(5, client.People.Circles.Count);
            for (var i = 0; i < client.People.Circles.Count; i++)
            {
                var item = client.People.Circles[i];
                Assert.AreEqual(string.Format("Id{0:00}", i), item.Id);
                Assert.AreEqual(string.Format("Name{0:00}_GetInitDataAsync", i), item.Name);
                Assert.AreEqual(false, item.IsLoadedMember);
                Utility.AssertException<InvalidOperationException>(() => item.GetMembers());
            }

            //LoadedWithMember
            await client.People.UpdateCirclesAndBlockAsync(true, CircleUpdateLevel.LoadedWithMembers);
            Assert.AreEqual(3, client.People.Circles.Count);
            foreach (var item in client.People.Circles.Zip(
                new[]{
                    new { cid = "Id01", cname = "Name01_GetInitDataAsync", cmem = new string[] { "Id00", "Id01" }},
                    new { cid = "Id02", cname = "Name02_GetInitDataAsync", cmem = new string[] { "Id01", "Id02" }},
                    new { cid = "Id03", cname = "Name03_GetInitDataAsync", cmem = new string[] { }}
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
            foreach(var item in client.People.YourCircle.GetMembers().Select((inf, idx) => new { Info = inf, Index = idx }))
            {
                Assert.AreEqual(string.Format("Id{0:00}", item.Index), item.Info.Id);
                Assert.AreEqual(string.Format("Name{0:00}_GetCircleDatasAsync", item.Index), item.Info.Name);
                Assert.AreEqual(string.Format("IconImageUrl{0:00}_GetCircleDatasAsync", item.Index), item.Info.IconImageUrl);
            }
        }
        [TestMethod, TestCategory("PeopleContainer")]
        public async Task UpdateFollowerAsyncTest()
        {
            await client.UpdateHomeInitDataAsync(false);
            await client.People.UpdateFollowerAsync(false);
            Assert.AreEqual(3, client.People.FollowerList.GetMembers().Count());
            foreach (var item in client.People.FollowerList.GetMembers().Zip(
                new[]{
                    new { cid = "Id01", cname = "Name01_GetFollowingMeProfilesAsync" },
                    new { cid = "Id02", cname = "Name02_GetFollowingMeProfilesAsync" },
                    new { cid = "Id03", cname = "Name03_GetFollowingMeProfilesAsync" }
                },
                (inf, expecteds) => new { Info = inf, Expecteds = expecteds }))
            {
                Assert.AreEqual(item.Expecteds.cid, item.Info.Id);
                Assert.AreEqual(item.Expecteds.cname, item.Info.Name);
            }
        }
        [TestMethod, TestCategory("PeopleContainer")]
        public async Task UpdateIgnoreAsyncTest()
        {
            await client.UpdateHomeInitDataAsync(false);
            await client.People.UpdateIgnoreAsync(false);

            Assert.AreEqual(3, client.People.IgnoreList.GetMembers().Count());
            foreach (var item in client.People.IgnoreList.GetMembers().Zip(
                new[]{
                    new { cid = "Id04", cname = "Name04_GetIgnoredProfilesAsync" },
                    new { cid = "Id05", cname = "Name05_GetIgnoredProfilesAsync" },
                    new { cid = "Id06", cname = "Name06_GetIgnoredProfilesAsync" }
                },
                (inf, expecteds) => new { Info = inf, Expecteds = expecteds }))
            {
                Assert.AreEqual(item.Expecteds.cid, item.Info.Id);
                Assert.AreEqual(item.Expecteds.cname, item.Info.Name);
            }
        }
        [TestMethod, TestCategory("PeopleContainer")]
        public async Task GetProfileOfMeAsyncTest()
        {
            await client.UpdateHomeInitDataAsync(false);
            var profile = await client.People.GetProfileOfMeAsync(false);

            Assert.AreEqual("Id00", profile.Id);
            Assert.AreEqual("Name00_GetProfileAboutMeAsync", profile.Name);
            Assert.AreEqual("GreetingText00_GetProfileAboutMeAsync", profile.GreetingText);
            Assert.AreEqual("IconImageUrl00_GetProfileAboutMeAsync", profile.IconImageUrl);
            Assert.AreEqual(ProfileUpdateApiFlag.ProfileGet, profile.LoadedApiTypes & ProfileUpdateApiFlag.ProfileGet);
        }
        [TestMethod, TestCategory("PeopleContainer"), Priority(0)]
        public async Task GetProfileOfTest()
        {
            var profile = client.People.GetProfileOf("Id99");
            Assert.AreEqual("Id99", profile.Id);
            Assert.AreEqual(ProfileUpdateApiFlag.Unloaded, profile.LoadedApiTypes);

            await profile.UpdateProfileGetAsync(false);
            Assert.AreEqual("Id99", profile.Id);
            Assert.AreEqual("Name99_GetProfileFullAsync", profile.Name);
            Assert.AreEqual("FirstName99_GetProfileFullAsync", profile.FirstName);
            Assert.AreEqual("LastName99_GetProfileFullAsync", profile.LastName);
            Assert.AreEqual("Introduction99_GetProfileFullAsync", profile.Introduction);
            Assert.AreEqual("BraggingRights99_GetProfileFullAsync", profile.BraggingRights);
            Assert.AreEqual("Occupation99_GetProfileFullAsync", profile.Occupation);
            Assert.AreEqual("GreetingText99_GetProfileFullAsync", profile.GreetingText);
            Assert.AreEqual("NickName99_GetProfileFullAsync", profile.NickName);
            Assert.AreEqual("IconImageUrl99_GetProfileFullAsync", profile.IconImageUrl);
            Assert.AreEqual(RelationType.Engaged, profile.Relationship);
            Assert.AreEqual(GenderType.Other, profile.GenderType);
            Assert.AreEqual(ProfileUpdateApiFlag.ProfileGet, profile.LoadedApiTypes);
        }
        [TestMethod, TestCategory("NotificationContainer"), Priority(0)]
        public async Task GetNotificationsTest()
        {
            var target = client.Notification.GetNotifications(true);
            await target.UpdateAsync(15);
            await target.UpdateAsync(15);
        }
    }
}
