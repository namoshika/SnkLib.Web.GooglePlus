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

            //platform.Relation.UpdateCirclesAndBlockAsync(false, CircleUpdateLevel.LoadedWithMembers).Wait();
            //var aaa = platform.Relation.Circles[0];
            //var bbb = aaa.GetMembers();

            //reshare - youtube
            var activityA = platform.Activity.GetActivityInfo("z13vshnrmob4c1czh04cc1tj3pi3d5sxhi4");
            activityA.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            //link
            var activityB = platform.Activity.GetActivityInfo("z13dfjpo0tvpevopz04cc1tj3pi3d5sxhi4");
            activityB.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            //youtube
            var activityC = platform.Activity.GetActivityInfo("z12ztlcxux2gtfnt1220ifsxfkudibffl");
            activityC.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            //image
            var activityD = platform.Activity.GetActivityInfo("z13sy1gpykybvdjmf220ifsxfkudibffl");
            activityD.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            //album
            var activityE = platform.Activity.GetActivityInfo("z125gfbr2rr5y5iv0220ifsxfkudibffl");
            activityE.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            //images
            var activityF = platform.Activity.GetActivityInfo("z13pxp1pszzdj52xy04cc1tj3pi3d5sxhi4");
            activityF.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();
    
            //geo
            var activityG = platform.Activity.GetActivityInfo("z130sl5a3s3hghcki22byxaz3rmkctxik04");
            activityG.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            //community
            var activityH = platform.Activity.GetActivityInfo("z13tcvyjnzrmgzkej22rudghttbbsp1ns");
            activityH.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            //interactive post
            var activityI = platform.Activity.GetActivityInfo("z13dthk54wu4y1kg304cefhheyaatzz5szs0k");
            activityI.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            Console.ReadLine();
        }
    }
}
