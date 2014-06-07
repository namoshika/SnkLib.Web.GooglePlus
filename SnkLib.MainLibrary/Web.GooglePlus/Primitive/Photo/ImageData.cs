using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    [Stubable]
    public class ImageData : CoreData
    {
        public ImageData(ImageUpdateApiFlag loadedApiType, string id, string name = null,
            int? width = null, int? height = null, string imageUrl = null, Uri linkUrl = null,
            DateTime? createDate = null, ImageTagData[] attachedTags = null, ProfileData owner = null,
            ActivityData isolateActivity = null)
        {
            LoadedApiTypes = loadedApiType;
            Id = id;
            Name = name;
            Width = width;
            Height = height;
            ImageUrl = imageUrl;
            LinkUrl = linkUrl;
            CreateDate = createDate;
            AttachedTags = attachedTags;
            Owner = owner;
            IsolateActivity = isolateActivity;
        }
        public readonly ImageUpdateApiFlag LoadedApiTypes;
        [Identification]
        public readonly string Id;
        public readonly string Name;
        public readonly string ImageUrl;
        public readonly Uri LinkUrl;
        public readonly int? Width;
        public readonly int? Height;
        public readonly DateTime? CreateDate;
        public readonly ImageTagData[] AttachedTags;
        public readonly ActivityData IsolateActivity;
        public readonly ProfileData Owner;
        public readonly DateTime LastUpdateLightBoxDate;

        public static ImageData operator +(ImageData value1, ImageData value2)
        {
            bool val1_isNotNull = value1 != null, val2_isNotNull = value2 != null;
            if (val1_isNotNull && val2_isNotNull)
            {
                var newUpdateType =
                    (value2 != null ? value2.LoadedApiTypes : ImageUpdateApiFlag.Unloaded)
                    | (value1 != null ? value1.LoadedApiTypes : ImageUpdateApiFlag.Unloaded);
                return new ImageData(
                    newUpdateType,
                    Merge(value1, value2, obj => obj.Id),
                    Merge(value1, value2, obj => obj.Name),
                    Merge(value1, value2, obj => obj.Width),
                    Merge(value1, value2, obj => obj.Height),
                    Merge(value1, value2, obj => obj.ImageUrl),
                    Merge(value1, value2, obj => obj.LinkUrl),
                    Merge(value1, value2, obj => obj.CreateDate),
                    Merge(value1, value2, obj => obj.AttachedTags),
                    Merge(value1, value2, obj => obj.Owner));
            }
            else if (val2_isNotNull)
                return value2;
            else
                return value1;
            }
    }
    [Flags]
    public enum ImageUpdateApiFlag
    { Unloaded = 0, Base = 1, LightBox = 3, }
}
