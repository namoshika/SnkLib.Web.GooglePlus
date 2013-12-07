using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class ImageData : DataBase
    {
        public ImageData(bool isUpdatedLightBox, string id, string name = null, int? width = null, int? height = null,
            string imageUrl = null, DateTime? createDate = null, ImageTagData[] attachedTags = null,
            ProfileData owner = null, ActivityData isolateActivity = null)
        {
            IsUpdatedLightBox = isUpdatedLightBox;
            Id = id;
            Name = name;
            Width = width;
            Height = height;
            ImageUrl = imageUrl;
            CreateDate = createDate;
            AttachedTags = attachedTags;
            Owner = owner;
            IsolateActivity = isolateActivity;
        }

        public bool IsUpdatedLightBox { get; private set; }
        public string Id { get; protected set; }
        public string Name { get; private set; }
        public string ImageUrl { get; private set; }
        public int? Width { get; private set; }
        public int? Height { get; private set; }
        public DateTime? CreateDate { get; private set; }
        public ImageTagData[] AttachedTags { get; private set; }
        public ActivityData IsolateActivity { get; private set; }
        public ProfileData Owner { get; private set; }
        public DateTime LastUpdateLightBoxDate { get; private set; }

        public static ImageData operator +(ImageData value1, ImageData value2)
        {
            var isUpdatedLightBox = value1 != null ? value1.IsUpdatedLightBox : false
                || value2 != null ? value2.IsUpdatedLightBox : false;
            return new ImageData(
                isUpdatedLightBox,
                Merge(value1, value2, obj => obj.Id),
                Merge(value1, value2, obj => obj.Name),
                Merge(value1, value2, obj => obj.Width),
                Merge(value1, value2, obj => obj.Height),
                Merge(value1, value2, obj => obj.ImageUrl),
                Merge(value1, value2, obj => obj.CreateDate),
                Merge(value1, value2, obj => obj.AttachedTags),
                Merge(value1, value2, obj => obj.Owner));
        }
    }
}
