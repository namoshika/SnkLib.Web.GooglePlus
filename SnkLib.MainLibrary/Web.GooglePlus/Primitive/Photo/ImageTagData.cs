using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    [Stubable]
    public abstract class ImageTagData
    {
        public ImageTagData(int left, int top, int right, int bottom, ProfileData owner)
        {
            Left = left;
            Top = top;
            Width = right - left;
            Height = bottom - top;
            Owner = owner;
        }
        public readonly int Left;
        public readonly int Top;
        public readonly int Width;
        public readonly int Height;
        public readonly ProfileData Owner;
    }
    public class ImageTextTagInfo : ImageTagData
    {
        public ImageTextTagInfo(int left, int top, int right, int bottom, string text, ProfileData owner)
            : base(left, top, right, bottom, owner) { Text = text; }
        public string Text;
    }
    public class ImageMensionTagInfo : ImageTagData
    {
        public ImageMensionTagInfo(int left, int top, int right, int bottom, ProfileData target, ProfileData owner)
            : base(left, top, right, bottom, owner) { Target = target; }
        public ProfileData Target;
    }
}
