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
        public AttachedInteractiveLink(ContentType type,
            string title, string summary, Uri linkUrl, string providerName, Uri providerUrl, Uri providerLogoUrl,
            Uri actionUrl, LabelType label, Uri faviconUrl, Uri originalThumbnailUrl, string thumbnailUrl,
            int thumbnailWidth, int thumbnailHeight, Uri plusBaseUrl)
            : base(type, title, summary, linkUrl, faviconUrl, originalThumbnailUrl, thumbnailUrl, thumbnailWidth, thumbnailHeight)
        {
            ProviderName = providerName;
            ProviderUrl = providerUrl;
            ProviderLogoUrl = providerLogoUrl;
            ActionUrl = actionUrl;
            Label = label;
        }
        public readonly string ProviderName;
        public readonly Uri ProviderUrl;
        public readonly Uri ProviderLogoUrl;
        public readonly Uri ActionUrl;
        public readonly LabelType Label;
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
