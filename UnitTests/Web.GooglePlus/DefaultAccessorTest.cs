using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace UnitTests.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus;
    using SunokoLibrary.Web.GooglePlus.Primitive;
    using SunokoLibrary.Web.GooglePlus.Utility;

    [TestClass]
    public class DefaultAccessorTest
    {
        private TestContext testContextInstance;
        static PlatformClientStub clientStabA;

        /// <summary>
        ///現在のテストの実行についての情報および機能を
        ///提供するテスト コンテキストを取得または設定します。
        ///</summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        [ClassInitialize]
        public static void PlatformClientInitialize(TestContext testContext)
        {
            clientStabA = new PlatformClientStub(new System.Net.CookieContainer());
            clientStabA.AtValue = "initDt_AtValue";
            clientStabA.EjxValue = "initDt_EjxValue";
            clientStabA.PvtValue = "initDt_PvtValue";
        }

        [TestMethod, TestCategory("DefaultAccessor")]
        public async Task GetInitDataAsyncTest()
        {
            var target = new DefaultAccessor(new ApiWrapperStub());
            var res = await target.GetInitDataAsync(clientStabA);
            Assert.Fail();
        }
    }
}
