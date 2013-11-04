using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class ProfileData : DataBase
    {
        public ProfileData(
            ProfileUpdateApiFlag loadedApiTypes = ProfileUpdateApiFlag.Unloaded, AccountStatus? status = null,
            DateTime? lastUpdateLookupProfile = null, DateTime? lastUpdateProfileGet = null,
            string id = null, string name = null, string firstName = null, string lastName = null,
            string introduction = null, string braggingRights = null, string occupation = null,
            string greetingText = null, string nickName = null, string iconImageUrl = null,
            RelationType? relationship = null, GenderType? genderType = null, LookingFor lookingFor = null,
            EmploymentInfo[] employments = null, EducationInfo[] educations = null,
            ContactInfo[] contactsInHome = null, ContactInfo[] contactsInWork = null,
            UrlInfo[] otherProfileUrls = null, UrlInfo[] contributeUrls = null, UrlInfo[] recommendedUrls = null,
            string[] placesLived = null, string[] otherNames = null)
        {
            LoadedApiTypes = loadedApiTypes;
            Status = status;
            Id = id;
            Name = name;
            FirstName = firstName;
            LastName = lastName;
            Introduction = introduction;
            BraggingRights = braggingRights;
            Occupation = occupation;
            GreetingText = greetingText;
            NickName = nickName;
            IconImageUrl = iconImageUrl;
            Relationship = relationship;
            GenderType = genderType;
            LookingFor = lookingFor;
            Employments = employments;
            Educations = educations;
            ContactsInHome = contactsInHome;
            ContactsInWork = contactsInWork;
            OtherProfileUrls = otherProfileUrls;
            ContributeUrls = contributeUrls;
            RecommendedUrls = recommendedUrls;
            PlacesLived = placesLived;
            OtherNames = otherNames;

            LastUpdateLookupProfile = lastUpdateLookupProfile ?? DateTime.MinValue;
            LastUpdateProfileGet = lastUpdateProfileGet ?? DateTime.MinValue;

            if (Id == null)
                throw new ArgumentNullException("ProfileDataの引数idをnullにする事はできません。");
            if (System.Text.RegularExpressions.Regex.IsMatch(id, "^\\d+$") == false && status == AccountStatus.Active)
                throw new ArgumentException("ProfileDataの引数idがメアド状態で引数statusをActiveにする事はできません。");
        }

        public DateTime LastUpdateLookupProfile{get;private set;}
        public DateTime LastUpdateProfileGet { get; private set; }
        public ProfileUpdateApiFlag LoadedApiTypes { get; private set; }
        public AccountStatus? Status { get; private set; }
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Introduction { get; private set; }
        public string BraggingRights { get; private set; }
        public string Occupation { get; private set; }
        public string GreetingText { get; private set; }
        public string NickName { get; private set; }
        public string IconImageUrl { get; private set; }
        public RelationType? Relationship { get; private set; }
        public GenderType? GenderType { get; private set; }
        public LookingFor LookingFor { get; private set; }
        public EmploymentInfo[] Employments { get; private set; }
        public EducationInfo[] Educations { get; private set; }
        public ContactInfo[] ContactsInHome { get; private set; }
        public ContactInfo[] ContactsInWork { get; private set; }
        public UrlInfo[] OtherProfileUrls { get; private set; }
        public UrlInfo[] ContributeUrls { get; private set; }
        public UrlInfo[] RecommendedUrls { get; private set; }
        public string[] PlacesLived { get; private set; }
        public string[] OtherNames { get; private set; }

        public static ProfileData operator +(ProfileData value1, ProfileData value2)
        {
            var flg = 
                (value2 != null ? value2.LoadedApiTypes : ProfileUpdateApiFlag.Unloaded)
                | (value1 != null ? value1.LoadedApiTypes : ProfileUpdateApiFlag.Unloaded);
            return new ProfileData(
                flg,
                Merge(value1, value2, obj => obj.Status),
                Merge(value1, value2, obj => obj.LastUpdateLookupProfile),
                Merge(value1, value2, obj => obj.LastUpdateProfileGet),
                Merge(value1, value2, obj => obj.Id),
                Merge(value1, value2, obj => obj.Name),
                Merge(value1, value2, obj => obj.FirstName),
                Merge(value1, value2, obj => obj.LastName),
                Merge(value1, value2, obj => obj.Introduction),
                Merge(value1, value2, obj => obj.BraggingRights),
                Merge(value1, value2, obj => obj.Occupation),
                Merge(value1, value2, obj => obj.GreetingText),
                Merge(value1, value2, obj => obj.NickName),
                Merge(value1, value2, obj => obj.IconImageUrl),
                Merge(value1, value2, obj => obj.Relationship),
                Merge(value1, value2, obj => obj.GenderType),
                Merge(value1, value2, obj => obj.LookingFor),
                Merge(value1, value2, obj => obj.Employments),
                Merge(value1, value2, obj => obj.Educations),
                Merge(value1, value2, obj => obj.ContactsInHome),
                Merge(value1, value2, obj => obj.ContactsInWork),
                Merge(value1, value2, obj => obj.OtherProfileUrls),
                Merge(value1, value2, obj => obj.ContributeUrls),
                Merge(value1, value2, obj => obj.RecommendedUrls),
                Merge(value1, value2, obj => obj.PlacesLived),
                Merge(value1, value2, obj => obj.OtherNames));
        }
    }

    public enum ProfileUpdateApiFlag
    {
        Unloaded = 0,
        /// <summary>Id,Status,Name,IconUrlが参照可能</summary>
        Base = 1,
        /// <summary>Id,Status,Name,IconUrl,GreetingTextが参照可能</summary>
        LookupProfile = 3,
        /// <summary>Id,Status,Name,IconUrl,Circleが参照可能</summary>
        LookupCircle = 9,
        /// <summary>Circle以外の全てが参照可能</summary>
        ProfileGet = 7,//Id, Name, IconUrl
    }

    public class EducationInfo
    {
        internal EducationInfo(JToken json)
        {
            Name = (string)json[0];
            MajorOrFieldOfStudy = (string)json[1];
            var tmpJson = json[2];
            JToken yearJson;
            yearJson = tmpJson.ElementAtOrDefault(0);
            StartYear = yearJson != null && yearJson.Type != JTokenType.Null ? (int)yearJson[2] : -1;
            yearJson = tmpJson.ElementAtOrDefault(1);
            if (yearJson != null && yearJson.Type != JTokenType.Null)
            {
                EndYear = (int)yearJson[2];
                var tmp = tmpJson.ElementAtOrDefault(2);
                Current = tmp != null ? (int)tmp == 1 : false;
            }
            else
            {
                EndYear = -1;
                Current = false;
            }
        }

        public string Name { get; private set; }
        public string MajorOrFieldOfStudy { get; private set; }
        public int StartYear { get; private set; }
        public int EndYear { get; private set; }
        public bool Current { get; private set; }
    }
    public class EmploymentInfo
    {
        public EmploymentInfo(JToken json)
        {
            Name = (string)json[0];
            JobTitle = (string)json[1];
            var tmpJson = json[2];
            JToken yearJson;
            yearJson = tmpJson.ElementAtOrDefault(0);
            StartYear = yearJson != null && yearJson.Type != JTokenType.Null ? (int)yearJson[2] : -1;
            yearJson = tmpJson.ElementAtOrDefault(1);
            if (yearJson != null && yearJson.Type != JTokenType.Null)
            {
                EndYear = (int)yearJson[2];
                var tmp = tmpJson.ElementAtOrDefault(2);
                Current = tmp != null ? (int)tmp == 1 : false;
            }
            else
            {
                EndYear = -1;
                Current = false;
            }
        }

        public string Name { get; private set; }
        public string JobTitle { get; private set; }
        public int StartYear { get; private set; }
        public int EndYear { get; private set; }
        public bool Current { get; private set; }
    }
    public class ContactInfo
    {
        public ContactInfo(string info, ContactType type)
        {
            Info = info ?? null;
            ContactInfoType = type;
        }

        public ContactType ContactInfoType { get; private set; }
        public string Info { get; private set; }
    }
    public class UrlInfo
    {
        public UrlInfo(JToken json)
        {
            string tmpStr;
            Uri tmpUrl;

            tmpStr = (string)json[1];
            if (Uri.TryCreate(tmpStr, UriKind.Absolute, out tmpUrl))
                Url = tmpUrl;
            tmpStr = (string)json[2];
            if (Uri.TryCreate((tmpStr.Substring(0, 6) == "https:" ? string.Empty : "https:") + tmpStr, UriKind.Absolute, out tmpUrl))
                FaviconUrl = tmpUrl;
            Title = (string)json[3];
            //Description = json[7];
            //ImagePreviewUrl = new Uri(json[6][0][8]);
        }

        public string Title { get; private set; }
        //public string Description { get; private set; }
        public Uri FaviconUrl { get; private set; }
        public Uri Url { get; private set; }
        //public Uri ImagePreviewUrl { get; private set; }
    }
    public class LookingFor
    {
        public LookingFor(JToken json)
        {
            List<int> signals = new List<int>();
            foreach (var item in json[1])
                signals.Add((int)item[0]);
            Friends = signals.Contains(2);
            Dating = signals.Contains(3);
            Partner = signals.Contains(4);
            Networking = signals.Contains(5);
        }
        public bool Friends { get; private set; }
        public bool Dating { get; private set; }
        public bool Partner { get; private set; }
        public bool Networking { get; private set; }
    }

    public enum ContactType
    {
        AIM = 2, MSN = 3, Yahoo = 4, Skype = 5, QQ = 6,
        GoogleTalk = 7, ICQ = 8, Jabber = 9, NetMeeting = 10,

        Unknown = -1, Phone = -2, Mobile = -3, Email = -4,
        Adress = -5, Fax = -6, Pager = -7,
    }
    public enum RelationType
    {
        Disabled = -1, Private = 0, Single = 2, InRelationship = 3, Engaged = 4,
        Married = 5, Iffy = 6, InOpenRelationship = 7, Widowed = 8, InDomesticPartnership = 9,
        InCivilUnion = 10
    }
    public enum GenderType { Male = 1, Female = 2, Other = 3, Disabled = -1 }
    public enum AccountStatus { Active, MailOnly, Disable, }
}
