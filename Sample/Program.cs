using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Web;

namespace Sample
{
    using System.Reactive;
    using System.Reactive.Linq;
    using SunokoLibrary.Web.GooglePlus;
    using SunokoLibrary.Web.GooglePlus.Primitive;

    class Program
    {
        static void Main(string[] args)
        {
            var generator = PlatformClient.Factory.ImportFromChrome().Result;
            var platform = generator[0].Build().Result;
            var atVal = platform.AtValue;
            var pvtVal = platform.PvtValue;

            platform.Relation.UpdateCirclesAndBlockAsync(false, CircleUpdateLevel.LoadedWithMembers).Wait();
            var aaa = platform.Relation.Circles[0];
            var bbb = aaa.GetMembers();

            //platform.Activity.GetStream().Subscribe(Console.WriteLine);
            var aa = platform.Activity.GetActivityInfo("z130sl5a3s3hghcki22byxaz3rmkctxik04");
            aa.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();
            Console.ReadLine();
        }
    }
}
