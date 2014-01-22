using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class NotificationInfo : AccessorBase
    {
        public NotificationInfo(NotificationData data, NotificationInfoContainer container, PlatformClient client)
            : base(client)
        {
            _data = data;
            _actor = client.People.InternalGetAndUpdateProfile(data.Actor);
            _followingNotifications = new ObservableCollection<ChainingNotificationInfo>(
                data.FollowingNotifications.Select(dt => new ChainingNotificationInfo(this, dt, client)));
            Container = container;
            FollowingNotifications = new ReadOnlyObservableCollection<ChainingNotificationInfo>(_followingNotifications);
        }
        NotificationData _data;
        ProfileInfo _actor;
        ObservableCollection<ChainingNotificationInfo> _followingNotifications;

        public NotificationInfoContainer Container { get; private set; }
        public ReadOnlyObservableCollection<ChainingNotificationInfo> FollowingNotifications { get; private set; }
        public string Id { get { return _data.Id; } }
        public bool IsReaded { get { return _data.NoticedDate < Container.LastReadedTime; } }
        public NotificationsFilter Type { get { return _data.Type; } }
        public ProfileInfo Actor { get { return _actor; } }
        public DateTime NoticedDate { get { return _data.NoticedDate; } }

        public virtual void Update(NotificationData data)
        {
            var newItems = data.FollowingNotifications.TakeWhile(dt => dt.Id != _data.Id)
                .Reverse().ToArray();
            foreach (var item in newItems)
                _followingNotifications.Insert(0, new ChainingNotificationInfo(this, item, Client));
            if(_actor.Id != _followingNotifications.First().Id)
                _actor = Client.People.InternalGetAndUpdateProfile(data.Actor);
            if (newItems.Length > 0)
                OnUpdated(new NotificationUpdatedEventArgs(newItems));
        }
        public event NotificationUpdatedEventHandler Updated;
        protected virtual void OnUpdated(NotificationUpdatedEventArgs e)
        {
            if (Updated != null)
                Updated(this, e);
        }
    }
    public class ChainingNotificationInfo : AccessorBase
    {
        public ChainingNotificationInfo(NotificationInfo latestInfo, ChainingNotificationData data, PlatformClient client)
            : base(client)
        {
            _latestInfo = latestInfo;
            _data = data;
        }
        NotificationInfo _latestInfo;
        ChainingNotificationData _data;

        public bool IsReaded { get { return _data.NoticedDate < _latestInfo.Container.LastReadedTime; } }
        public string Id { get { return _data.Id; } }
        public ProfileInfo Actor { get { return Client.People.InternalGetAndUpdateProfile(_data.Actor); } }
        public DateTime NoticedDate { get { return _data.NoticedDate; } }
    }
    public delegate void NotificationUpdatedEventHandler(object sender, NotificationUpdatedEventArgs e);
    public class NotificationUpdatedEventArgs : EventArgs
    {
        public NotificationUpdatedEventArgs(ChainingNotificationData[] items) { Notifications = items; }
        public ChainingNotificationData[] Notifications { get; private set; }
    }
}
