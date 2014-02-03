using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus;
    using SunokoLibrary.Web.GooglePlus.Primitive;

    [TestClass]
    public class DefaultAccessorTest
    {
        static DefaultAccessor target = new DefaultAccessor();
        static PlatformClientStab clientStabA;

        [ClassInitialize]
        public static void PlatformClientInitialize(TestContext testContext)
        {
            var cookies = PlatformClientFactoryEx.ImportCookiesFromChrome().Result;
            clientStabA = new PlatformClientStab(cookies);
            var initDt = target.GetInitDataAsync(clientStabA).Result;
            clientStabA.AtValue = initDt.AtValue;
            clientStabA.EjxValue = initDt.EjxValue;
            clientStabA.PvtValue = initDt.PvtValue;
        }

        //[TestMethod, TestCategory("DefaultAccessor")]
        //public async Task LoginTest()
        //{
        //    var target = new DefaultAccessor();
        //    var cookie = new System.Net.CookieContainer();
        //    var client = new PlatformClientStab(cookie);
        //    Assert.IsTrue(await target.LoginAsync(mail, pass, client));

        //    cookie = new System.Net.CookieContainer();
        //    client = new PlatformClientStab(cookie);
        //    Assert.IsFalse(await target.LoginAsync("test", "test", client));
        //}
        [TestMethod, TestCategory("DefaultAccessor")]
        public async Task GetInitDataTest()
        {
            var target = new DefaultAccessor();
            var initDt = await target.GetInitDataAsync(clientStabA);
            Assert.IsNotNull(initDt.AtValue);
            Assert.IsNotNull(initDt.CircleInfos);
            Assert.IsNotNull(initDt.EjxValue);
            Assert.IsNotNull(initDt.LatestActivities);
            Assert.IsNotNull(initDt.PvtValue);
        }
        [TestMethod, TestCategory("DefaultAccessor")]
        public async Task GetProfileDataFullTest()
        {
            var result = await target.GetProfileFullAsync("114508593546569263679", clientStabA);

            //自己紹介
            Assert.AreEqual("114508593546569263679", result.Id);
            Assert.AreEqual("Hideki Nishimura", result.Name);
            Assert.AreEqual("Hideki", result.FirstName);
            Assert.AreEqual("Nishimura", result.LastName);
            Assert.AreEqual("<span>test_self‐introduction</span>", result.Introduction);
            Assert.AreEqual("test_boast", result.BraggingRights);
            Assert.AreEqual("test_occupation", result.Occupation);
            Assert.AreEqual("test_catch-phrase", result.GreetingText);
            Assert.AreEqual("nick", result.NickName);
            Assert.AreEqual("https://lh4.googleusercontent.com/-TQ_A2CTpXVs/AAAAAAAAAAI/AAAAAAAAAAA/INNFU2-ceaU/$SIZE_SEGMENT/photo.jpg", result.IconImageUrl);
            Assert.AreEqual(RelationType.Widowed, result.Relationship);
            Assert.AreEqual(GenderType.Other, result.GenderType);
            Assert.AreEqual(true, result.LookingFor.Dating);
            Assert.AreEqual(false, result.LookingFor.Friends);
            Assert.AreEqual(true, result.LookingFor.Networking);
            Assert.AreEqual(false, result.LookingFor.Partner);

            //職歴
            Assert.AreEqual(2, result.Employments.Count());
            foreach (var item in new[]
                {
                    new{ Index = 0, Current = true, StartYear = -1, EndYear = 2013, Name = "test_company01", JobTitle = "" },
                    new{ Index = 1, Current = false, StartYear = 2005, EndYear = -1, Name = "", JobTitle = "test_position02" },
                })
            {
                Assert.AreEqual(item.Name, result.Employments[item.Index].Name);
                Assert.AreEqual(item.JobTitle, result.Employments[item.Index].JobTitle);
                Assert.AreEqual(item.StartYear, result.Employments[item.Index].StartYear);
                Assert.AreEqual(item.EndYear, result.Employments[item.Index].EndYear);
                Assert.AreEqual(item.Current, result.Employments[item.Index].Current);
            }

            //学歴
            Assert.AreEqual(2, result.Educations.Count());
            foreach (var item in new[]
                {
                    new{ Index = 0, Current = true, StartYear = -1, EndYear = 2013, Name = "", MajorOrFieldOfStudy = "test_cc03" },
                    new{ Index = 1, Current = false, StartYear = 2006, EndYear = -1, Name = "test_school02", MajorOrFieldOfStudy = "" },
                })
            {
                Assert.AreEqual(item.Name, result.Educations[item.Index].Name);
                Assert.AreEqual(item.MajorOrFieldOfStudy, result.Educations[item.Index].MajorOrFieldOfStudy);
                Assert.AreEqual(item.Current, result.Educations[item.Index].Current);
                Assert.AreEqual(item.StartYear, result.Educations[item.Index].StartYear);
                Assert.AreEqual(item.EndYear, result.Educations[item.Index].EndYear);
            }

            //家
            Assert.AreEqual(8, result.ContactsInHome.Count());
            foreach (var item in result.ContactsInHome.Zip(new[]
                {
                    new{ ContactType = ContactType.Phone, Info = "000-000-0000" },
                    new{ ContactType = ContactType.Phone, Info = "444-444-4444" },
                    new{ ContactType = ContactType.Mobile, Info = "111-111-1111" },
                    new{ ContactType = ContactType.Fax, Info = "222-222-2222" },
                    new{ ContactType = ContactType.Pager, Info = "333-333-3333" },
                    new{ ContactType = ContactType.Adress, Info = "the mars" },
                    new{ ContactType = ContactType.AIM, Info = "hage@hage.com" },
                    new{ ContactType = ContactType.Email, Info = "hoge@hoge.com" },
                }, (inf, expected) => new { Info = inf, Expected = expected }))
            {
                Assert.AreEqual(item.Expected.ContactType, item.Info.ContactInfoType);
                Assert.AreEqual(item.Expected.Info, item.Info.Info);
            }
            //職場
            Assert.AreEqual(1, result.ContactsInWork.Count());
            foreach (var item in result.ContactsInWork.Zip(new[]
                {
                    new{ ContactType = ContactType.Adress, Info = "the earth" },
                }, (inf, expected) => new { Info = inf, Expected = expected }))
            {
                Assert.AreEqual(item.Expected.ContactType, item.Info.ContactInfoType);
                Assert.AreEqual(item.Expected.Info, item.Info.Info);
            }
            //住んだ場所
            Assert.AreEqual(1, result.PlacesLived.Count());
            foreach (var item in result.PlacesLived.Zip(
                new[] { "tokyo" }, (inf, expected) => new { Info = inf, Expected = expected }))
                Assert.AreEqual(item.Expected, item.Info);
            //別名
            Assert.AreEqual(2, result.OtherNames.Count());
            foreach (var item in result.OtherNames.Zip(
                new[] { "test_otherName01", "test_otherName02" }, (inf, expected) => new { Info = inf, Expected = expected }))
                Assert.AreEqual(item.Expected, item.Info);
            //寄稿先
            Assert.AreEqual("test_label01", result.ContributeUrls[0].Title);
            Assert.AreEqual("http://hogehage.com/", result.ContributeUrls[0].Url.AbsoluteUri);
            //リンク
            Assert.AreEqual("test_label01", result.RecommendedUrls[0].Title);
            Assert.AreEqual("http://foohidebu.com/", result.RecommendedUrls[0].Url.AbsoluteUri);
        }
        [TestMethod, TestCategory("DefaultAccessor")]
        public async Task GetProfileDataLiteTest()
        {
            var target = new DefaultAccessor();
            var result = await target.GetProfileLiteAsync("114365312488191371601", clientStabA);
            Assert.AreEqual("114365312488191371601", result.Id);
            Assert.AreEqual("Hiroki Saito", result.Name);
            Assert.AreEqual("ゾウさんが好きです。でもパンダも良い", result.GreetingText);
            Assert.AreEqual("https://lh6.googleusercontent.com/-0zOCgk_an80/AAAAAAAAAAI/AAAAAAAAASI/q_HCz9fhnCA/$SIZE_SEGMENT/photo.jpg", result.IconImageUrl);
        }
        [TestMethod, TestCategory("DefaultAccessor")]
        public async Task GetProfileDataAboutMeTest()
        {
            var target = new DefaultAccessor();
            var result = await target.GetProfileAboutMeAsync(clientStabA);
            Assert.AreEqual("114365312488191371601", result.Id);
            Assert.AreEqual("Hiroki Saito", result.Name);
            Assert.AreEqual("ゾウさんが好きです。でもパンダも良い", result.GreetingText);
        }
        [TestMethod, TestCategory("DefaultAccessor")]
        public async Task GetCircleDatasTest()
        {
            var target = new DefaultAccessor();
            var result = await target.GetCircleDatasAsync(clientStabA);
        }
        [TestMethod, TestCategory("DefaultAccessor")]
        public async Task GetFollowingProfilesOfTest()
        {
            var target = new DefaultAccessor();
            var result = await target.GetFollowingProfilesAsync("114365312488191371601", clientStabA);
        }
        [TestMethod, TestCategory("DefaultAccessor")]
        public async Task GetFollowedProfilesOfTest()
        {
            var target = new DefaultAccessor();
            var expect = 10;
            var result = await target.GetFollowedProfilesAsync("114365312488191371601", expect, clientStabA);
            Assert.AreEqual(expect, result.Length);
        }
        [TestMethod, TestCategory("DefaultAccessor")]
        public async Task GetProfileDatasOfIgnoredTest()
        {
            var target = new DefaultAccessor();
            var result = await target.GetIgnoredProfilesAsync(clientStabA);
        }
        [TestMethod, TestCategory("DefaultAccessor")]
        public async Task GetProfileDatasOfFollowerTest()
        {
            var target = new DefaultAccessor();
            var result = await target.GetFollowingMeProfilesAsync(clientStabA);
        }
        [TestMethod, TestCategory("DefaultAccessor")]
        public async Task GetActivityDataOfTest()
        {
            var target = new DefaultAccessor();
            var result = await target.GetActivityAsync("z13njvfyswyrtnolr23vtrmrxyjpe1na504", clientStabA);
            Assert.AreEqual("テストアクティビティA", result.Text);
        }
        [TestMethod, TestCategory("DefaultAccessor")]
        public async Task GetActivityDatasOfTest()
        {
            var target = new DefaultAccessor();
            var result = await target.GetActivitiesAsync("66f85d300cca1dcc", null, null, 20, clientStabA);
            result = await target.GetActivitiesAsync("66f85d300cca1dcc", null, result.Item2, 20, clientStabA);
            result = await target.GetActivitiesAsync(null, "114365312488191371601", null, 20, clientStabA);
            result = await target.GetActivitiesAsync(null, "114365312488191371601", result.Item2, 20, clientStabA);
        }
        [TestMethod, TestCategory("DefaultAccessor")]
        public async Task GetNotificationDatasTest()
        {
            var target = new DefaultAccessor();
            var result = await target.GetNotificationsAsync(NotificationsFilter.All, 10, null, clientStabA);
        }
        //[TestMethod, TestCategory("DefaultAccessor")]
        //public async Task GetStreamAttacherTest()
        //{
        //    var target = new DefaultAccessor();
        //    var result = target.GetStreamAttacher(clientStabA);
        //    var aaa = await result.FirstAsync();
        //}
    }
}
