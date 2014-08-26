using SunokoLibrary.Application;
using SunokoLibrary.Application.Browsers;
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
            var url = new Uri("https://plus.google.com/");
            var getterA = new GoogleChromeBrowserManager().CreateCookieImporters().First();
            var getterB = new IEBrowserManager().CreateIEPMCookieGetter();
            var cookieA = new CookieContainer();
            var cookieB = new CookieContainer();
            getterA.GetCookies(url, cookieA);
            getterB.GetCookies(url, cookieA);

            var platform = PlatformClient.Factory.ImportFrom(getterA).Result[0].Build(null).Result;
            var atVal = platform.AtValue;
            var pvtVal = platform.PvtValue;

            //platform.People.UpdateCirclesAndBlockAsync(false, CircleUpdateLevel.LoadedWithMembers).Wait();
            //var aaa = platform.People.Circles[0];
            //var bbb = aaa.GetMembers();

            //reshare - youtube
            //https://plus.google.com/u/0/114508593546569263679/posts/ggrJ4Tevi3g
            var activityA = platform.Activity.GetActivityInfo("z13vshnrmob4c1czh04cc1tj3pi3d5sxhi4");
            activityA.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            //link
            //https://plus.google.com/u/0/114508593546569263679/posts/Z4BWkxGjex9
            var activityB = platform.Activity.GetActivityInfo("z13dfjpo0tvpevopz04cc1tj3pi3d5sxhi4");
            activityB.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            //youtube
            //https://plus.google.com/u/0/114508593546569263679/posts/6d9CcZsNx4n
            var activityC = platform.Activity.GetActivityInfo("z12ztlcxux2gtfnt1220ifsxfkudibffl");
            activityC.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            //image
            //https://plus.google.com/u/0/114508593546569263679/posts/DdKnSaLwSDv
            var activityD = platform.Activity.GetActivityInfo("z13sy1gpykybvdjmf220ifsxfkudibffl");
            activityD.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            //album
            //https://plus.google.com/u/0/114508593546569263679/posts/H8rKRBnJBLV
            var activityE = platform.Activity.GetActivityInfo("z125gfbr2rr5y5iv0220ifsxfkudibffl");
            activityE.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            //images
            //https://plus.google.com/u/0/114508593546569263679/posts/Yo96am7poqU
            var activityF = platform.Activity.GetActivityInfo("z13pxp1pszzdj52xy04cc1tj3pi3d5sxhi4");
            activityF.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();
    
            //geo
            //https://plus.google.com/u/0/114894856840323351046/posts/3PSJiDZ4LXJ
            var activityG = platform.Activity.GetActivityInfo("z130sl5a3s3hghcki22byxaz3rmkctxik04");
            activityG.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            //community
            //https://plus.google.com/u/0/102375446456224109853/posts/EJNcVsNf7hB
            var activityH = platform.Activity.GetActivityInfo("z13tcvyjnzrmgzkej22rudghttbbsp1ns");
            activityH.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            //interactive post
            //https://plus.google.com/u/0/102170436514097583222/posts/Nzr9fYLJZEi
            var activityI = platform.Activity.GetActivityInfo("z13dthk54wu4y1kg304cefhheyaatzz5szs0k");
            activityI.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            //interactive post (google play)
            //https://plus.google.com/u/0/110215502808202870856/posts/L8gwMev3gr4
            var activityJ = platform.Activity.GetActivityInfo("z13gu5gq4ru3vhgfm224sxkbkuuwfj3tu");
            activityJ.UpdateGetActivityAsync(true, ActivityUpdateApiFlag.GetActivity).Wait();

            Console.ReadLine();
        }
    }
}
