using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class NotificationInfoWithActor : NotificationInfo
    {
        public NotificationInfoWithActor(SocialNotificationData data, NotificationInfoContainer container, PlatformClient client)
            : base(data, container, client)
        {
            _actionLogs = new ObservableCollection<NotificationItemInfo>(
                data.LogItems.Select(dt => new NotificationItemInfo(dt, client)));
            Actor = client.People.InternalGetAndUpdateProfile(data.LogItems.First().Actor);
            ActionLogs = new ReadOnlyObservableCollection<NotificationItemInfo>(_actionLogs);
        }
        ObservableCollection<NotificationItemInfo> _actionLogs;
        public ReadOnlyObservableCollection<NotificationItemInfo> ActionLogs { get; private set; }
        public ProfileInfo Actor { get; private set; }

        internal override void Update(NotificationData data)
        {
            base.Update(data);
            var newItems = ((SocialNotificationData)data).LogItems
                .TakeWhile(dt => dt.NoticeDate > ActionLogs.First().NoticedDate)
                .Reverse().ToArray();
            foreach (var item in newItems)
                _actionLogs.Insert(0, new NotificationItemInfo(item, Client));
            if (Actor.Id != _actionLogs.First().Actor.Id)
                Actor = Client.People.InternalGetAndUpdateProfile(((SocialNotificationData)data).Actor);
        }
    }
    public class NotificationItemInfo : AccessorBase
    {
        public NotificationItemInfo(NotificationItemData data, PlatformClient client)
            : base(client)
        {
            _data = data;
            Actor = Client.People.InternalGetAndUpdateProfile(_data.Actor);
        }
        NotificationItemData _data;

        public NotificationFlag Type { get { return _data.Type; } }
        public DateTime NoticedDate { get { return _data.NoticeDate; } }
        public ProfileInfo Actor { get; private set; }
    }
}
