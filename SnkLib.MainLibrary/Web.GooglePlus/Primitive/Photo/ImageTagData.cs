using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
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
        public int Left { get; private set; }
        public int Top { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public ProfileData Owner { get; private set; }
    }
    public class ImageTextTagInfo : ImageTagData
    {
        public ImageTextTagInfo(int left, int top, int right, int bottom, string text, ProfileData owner)
            : base(left, top, right, bottom, owner) { Text = text; }
        public string Text { get; private set; }
    }
    public class ImageMensionTagInfo : ImageTagData
    {
        public ImageMensionTagInfo(int left, int top, int right, int bottom, ProfileData target, ProfileData owner)
            : base(left, top, right, bottom, owner) { Target = target; }
        public ProfileData Target { get; private set; }
    }
}
