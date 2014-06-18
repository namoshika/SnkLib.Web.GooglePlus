using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.TestDataGenerator
{
    using SunokoLibrary.Web.GooglePlus;
    using SunokoLibrary.Web.GooglePlus.Primitive;
    using SunokoLibrary.Web.GooglePlus.Utility;

    class Program
    {
        static void Main(string[] args)
        {
            var clientStabA = new PlatformClientStub(new System.Net.CookieContainer());
            clientStabA.AtValue = "initDt_AtValue";
            clientStabA.EjxValue = "initDt_EjxValue";
            clientStabA.PvtValue = "initDt_PvtValue";
            var apiWrapper = new ApiWrapperWithLogger();
            var api = new DefaultAccessor(apiWrapper);

            var hmInit = api.GetInitDataAsync(clientStabA).Result;
        }
    }
}
