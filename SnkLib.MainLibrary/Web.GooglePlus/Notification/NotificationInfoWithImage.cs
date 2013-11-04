using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class NotificationInfoWithImage : NotificationInfo
    {
        public NotificationInfoWithImage(NotificationDataWithImage data, NotificationInfoContainer container, PlatformClient client)
            : base(data, container, client) { Image = data.Images.Select(dt => new ImageInfo(client, dt)).ToArray(); }
        public ImageInfo[] Image { get; private set; }
    }
}
