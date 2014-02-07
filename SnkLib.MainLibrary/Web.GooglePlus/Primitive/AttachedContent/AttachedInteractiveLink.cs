using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class AttachedInteractiveLink : AttachedLink
    {
        public AttachedInteractiveLink(JArray json, Uri plusBaseUrl) : base(json, plusBaseUrl) { }
        public override ContentType Type { get { return ContentType.InteractiveLink; } }
        public string ProviderName { get; private set; }
        public Uri ProviderUrl { get; private set; }
        public Uri ProviderLogoUrl { get; private set; }
        public Uri ActionUrl { get; private set; }
        public LabelType Label { get; private set; }

        protected override void ParseTemplate(JArray json)
        {
            var workJson = (JArray)json[75];
            ActionUrl = new Uri((string)workJson[0][3]);
            LabelType tmp;
            var labelTypeStr = string.Join(string.Empty, ((string)workJson[2]).Split(' ').Select(str => str[0].ToString().ToUpper() + str.Substring(1)));
            if (Enum.TryParse(labelTypeStr, out tmp) == false)
                tmp = LabelType.Unknown;
            Label = tmp;

            if (json[77].Type == JTokenType.Array)
            {
                workJson = (JArray)json[77];
                ProviderName = (string)workJson[0];
                ProviderUrl = new Uri((string)workJson[1]);
                ProviderLogoUrl = new Uri((string)workJson[2]);
            }

            base.ParseTemplate((JArray)GetContentBody((JArray)json[8]).Value);
        }
    }
    public enum LabelType
    {
        Accept, AcceptGift, Add, AddFriend, AddMe, AddToCart, AddToCalendar, AddToFavorites,
        AddToQueue, AddToWishList, Answer, AnswerQuiz, Apply, Ask, Attack, Beat, Bid, Book,
        Bookmark, Browse, Buy, Capture, Challenge, Change, Chat, CheckIn, Collect, Comment,
        Compare, Complain, Confirm, Connect, Contribute, Cook, Create, Defend, Dine, Discover,
        Discuss, Donate, Download, Earn, Eat, Explain, Find, FindATable, Follow, Get, Gift,
        Give, Go, Help, Identify, Install, InstallApp, Introduce, Invite, Join, JoinMe, Learn,
        LearnMore, Listen, Make, Match, Message, Open, OpenApp, Own, Pay, Pin, PinIt, Plan,
        Play, Purchase, Rate, Read, ReadMore, Recommend, Record, Redeem, Register, Reply,
        Reserve, Review, RSVP, Save, SaveOffer, SeeDemo, Sell, Send, SignIn, SignUp, Start,
        Stop, Subscribe, TakeQuiz, TakeTest, TryIt, Upvote, Use, View, ViewItem, ViewMenu,
        ViewProfile, Visit, Vote, Want, WantToSee, WantToSeeIt, Watch, WatchTrailer, Wish,
        Write, Unknown, 
    }
}
