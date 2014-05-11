using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public abstract class ContentElement
    {
        public ContentElement(ElementType type) { Type = type; }
        public ElementType Type { get; private set; }

        public static StyleElement ParseHtml(string contentHtml, IPlatformClient client)
        {
            if (string.IsNullOrEmpty(contentHtml))
                return null;

            //"<"はhtml上では&lt;と書くため、これの出現は確実にタグの開始となる。よって、
            //内容を読み取ることができる。以下のwhile文はhtmlをリンク、メンション、テキスト
            //に分割する処理を記述している
            int nextReadIdx = 0, idx, startIdx, endIdx, tmpInt;
            string tmpStr;
            var currentEleStack = new Stack<char>();
            var blocks = new Stack<List<ContentElement>>();
            blocks.Push(new List<ContentElement>());
            while ((idx = contentHtml.IndexOf('<', nextReadIdx)) >= 0)
            {
                //タグが開始される前に出現したテキストをblocksに入れる
                tmpStr = contentHtml.Substring(nextReadIdx, idx - nextReadIdx);
                if (tmpStr.Length > 0)
                    blocks.Peek().Add(new TextElement(Primitive.ApiAccessorUtility.DecodeHtmlText(tmpStr)));

                //メンション、リンク用
                if (contentHtml[idx + 1] == 'a' || contentHtml.IndexOf("span", idx + 1) == idx + 1)
                {
                    //開始されたタグがspanタグである場合は12/03/08時点ではメンション
                    //用の要素である
                    var isMension = contentHtml[idx + 1] == 's';

                    //IndexOf("<a href=\"", idx)のidx引数の部分は効率を考えた場合はidx + 1
                    //するとメンションの読み取りはより良い。しかしリンクの場合には現状の検索
                    //文字ではidxの添字に存在する"<"が存在しないと"<a"を検出出来ない。現状で
                    //は"<a href"の"<"を省かず、検索開始位置をidx + 1にしない事で動かしてる
                    var innerCloseEleIdx = contentHtml.IndexOf("</span>", idx + 5);
                    var outerCloseEleIdx = isMension ? contentHtml.IndexOf("</span>", innerCloseEleIdx + 7) : contentHtml.IndexOf("</a>", idx);
                    startIdx = Math.Min(
                        (tmpInt = contentHtml.IndexOf("href=\"", idx, outerCloseEleIdx - idx)) < 0 ? int.MaxValue : tmpInt,
                        (tmpInt = contentHtml.IndexOf("href='", idx, outerCloseEleIdx - idx)) < 0 ? int.MaxValue : tmpInt) + 6;
                    string mensionTargetProfileUrl = null, mensionTargetName = null;
                    if (startIdx >= 0)
                    {
                        endIdx = Math.Min(
                            (tmpInt = contentHtml.IndexOf("\"", startIdx, outerCloseEleIdx - startIdx)) < 0 ? int.MaxValue : tmpInt,
                            (tmpInt = contentHtml.IndexOf("'", startIdx, outerCloseEleIdx - startIdx)) < 0 ? int.MaxValue : tmpInt);
                        mensionTargetProfileUrl = contentHtml.Substring(startIdx, endIdx - startIdx);
                        idx = endIdx + 1;
                    }
                    else
                        idx = innerCloseEleIdx;

                    startIdx = contentHtml.IndexOf(">", idx) + 1;
                    endIdx = contentHtml.IndexOf("</", startIdx);
                    mensionTargetName = Primitive.ApiAccessorUtility.DecodeHtmlText(
                        contentHtml.Substring(startIdx, endIdx - startIdx));

                    //最端の閉じタグの最後の">"部分の直後の番地を次の文字処理位置とする
                    //たまに閉じタグが探し出せない(IndexOfのバグ?)事があり、無限ループする。これをMath.Maxで対策
                    nextReadIdx = Math.Max(nextReadIdx + 1, contentHtml.IndexOf('>', outerCloseEleIdx) + 1);

                    if (isMension)
                    {
                        if (mensionTargetProfileUrl != null)
                            blocks.Peek().Add(new MensionElement(new ProfileData(
                                mensionTargetProfileUrl.Substring(mensionTargetProfileUrl.LastIndexOf('/') + 1),
                                mensionTargetName, loadedApiTypes: ProfileUpdateApiFlag.Base)));
                        else
                            blocks.Peek().Add(new TextElement("+" + mensionTargetName));
                    }
                    else
                    {
                        Uri tmpUrl;
                        if (!Uri.TryCreate(client.PlusBaseUrl, mensionTargetProfileUrl, out tmpUrl)
                            && !Uri.TryCreate("about:blank", UriKind.Absolute, out tmpUrl)) { }
                        blocks.Peek().Add(new HyperlinkElement(tmpUrl, mensionTargetName));
                    }
                }
                else if (contentHtml.IndexOf("br", idx + 1) == idx + 1)
                {
                    blocks.Peek().Add(new BreakElement());
                    //たまに閉じタグが探し出せない(IndexOfのバグ?)事があり、無限ループする。これをMath.Maxで対策
                    nextReadIdx = Math.Max(nextReadIdx + 1, contentHtml.IndexOf(">", idx + 3) + 1);
                }
                //文字装飾タグ用
                else
                    switch (contentHtml[idx + 1])
                    {
                        case '/':
                            var currentEleChildren = blocks.Pop().ToArray();
                            switch (currentEleStack.Pop())
                            {
                                case 'b':
                                    blocks.Peek().Add(new StyleElement(StyleType.Bold, currentEleChildren));
                                    break;
                                case 'i':
                                    blocks.Peek().Add(new StyleElement(StyleType.Italic, currentEleChildren));
                                    break;
                                case 's':
                                case 'd':
                                    blocks.Peek().Add(new StyleElement(StyleType.Middle, currentEleChildren));
                                    break;
                                default:
                                    blocks.Peek().Add(new StyleElement(StyleType.Unknown, currentEleChildren));
                                    break;
                            }
                            nextReadIdx = Math.Max(nextReadIdx + 1, contentHtml.IndexOf('>', idx + 2) + 1);
                            break;
                        default:
                            startIdx = contentHtml.IndexOf('>', idx + 1) + 1;
                            var closeEleIdx = contentHtml.IndexOf("</", startIdx);
                            var otherStartEleIdx = contentHtml.IndexOf("<", startIdx);
                            if (closeEleIdx == otherStartEleIdx)
                            {
                                var elements = new[] { new TextElement(
                                    Primitive.ApiAccessorUtility.DecodeHtmlText(
                                    contentHtml.Substring(startIdx, closeEleIdx - startIdx))) };
                                switch (contentHtml[idx + 1])
                                {
                                    case 'b':
                                        blocks.Peek().Add(new StyleElement(StyleType.Bold, elements));
                                        break;
                                    case 'i':
                                        blocks.Peek().Add(new StyleElement(StyleType.Italic, elements));
                                        break;
                                    case 's':
                                    case 'd':
                                        blocks.Peek().Add(new StyleElement(StyleType.Middle, elements));
                                        break;
                                    default:
                                        blocks.Peek().Add(new StyleElement(StyleType.Unknown, elements));
                                        break;
                                }
                                nextReadIdx = contentHtml.IndexOf('>', closeEleIdx + 1) + 1;
                            }
                            else
                            {
                                var elements = new List<ContentElement>();
                                tmpStr = Primitive.ApiAccessorUtility.DecodeHtmlText(
                                    contentHtml.Substring(startIdx, otherStartEleIdx - startIdx));
                                if (tmpStr.Length > 0)
                                    elements.Add(new TextElement(tmpStr));
                                blocks.Push(elements);

                                currentEleStack.Push(contentHtml[idx + 1]);
                                nextReadIdx = otherStartEleIdx;
                            }
                            break;
                    }
            }
            //タグが無くなったら残りの文字列をblocksに入れる
            tmpStr = contentHtml.Substring(nextReadIdx, contentHtml.Length - nextReadIdx);
            if (tmpStr.Length > 0)
                blocks.Peek().Add(new TextElement(Primitive.ApiAccessorUtility.DecodeHtmlText(tmpStr)));

            //ルート要素は書式設定無しのスタイル要素にする。しかし、配列に要素が
            //一つしか入っておらず、要素がスタイル要素だった場合には新しく生成せ
            //ずにそのスタイル要素を戻り値とする
            if (blocks.Peek().Count == 1 && blocks.Peek().First() is StyleElement)
                return (StyleElement)blocks.Peek().First();
            else
                return new StyleElement(StyleType.None, blocks.Pop().ToArray());
        }
        public static StyleElement ParseJson(JToken contentJson)
        {
            contentJson = contentJson.ElementAtOrDefault(0);
            if (contentJson == null)
                return new StyleElement(StyleType.None, new List<ContentElement>());

            var styleStatus = new[] { false, false, false };
            var styleStack = new Stack<StyleType>();
            var blocksStack = new Stack<List<ContentElement>>();
            blocksStack.Push(new List<ContentElement>());
            foreach (var item in contentJson)
                switch ((int)item[0])
                {
                    case 0:
                    case 2:
                    case 3:
                    case 4:
                        //itemの書式を取得
                        var newStyleStatus = new[] { false, false, false };
                        if (item.Count() > 2 && item[2].Type == JTokenType.Array)
                            foreach (var styleItem in item[2].Select((token, idx) => new { Token = token, Index = idx }))
                                newStyleStatus[styleItem.Index] = styleItem.Token.Type != JTokenType.Null && (int)styleItem.Token == 1;
                        //書式解除があった場合は新旧比較し、解除書式全てを消すまで書式スタックを削る
                        //ここで開始地点がスライドしてる書式などの巻き添えになる書式も発生する。その
                        //書式は後続の書式追加で補填する
                        for (var currentStyleIdx = 0; currentStyleIdx < styleStatus.Length; currentStyleIdx++)
                        {
                            if (newStyleStatus[currentStyleIdx] == false && styleStatus[currentStyleIdx])
                                while (blocksStack.Count > 0)
                                {
                                    styleStatus[(int)styleStack.Peek()] = false;
                                    var newElement = new StyleElement(styleStack.Peek(), blocksStack.Pop());
                                    blocksStack.Peek().Add(newElement);
                                    if (styleStack.Pop() == (StyleType)currentStyleIdx)
                                        break;
                                }
                        }
                        //書式追加がある場合は書式スタックの追加とブロックスタックの最上にある
                        //要素リストを新規追加で新品リストにする
                        for (var currentStyleIdx = 0; currentStyleIdx < styleStatus.Length; currentStyleIdx++)
                        {
                            if (newStyleStatus[currentStyleIdx] && styleStatus[currentStyleIdx] == false)
                            {
                                styleStack.Push((StyleType)currentStyleIdx);
                                styleStatus[currentStyleIdx] = true;
                                blocksStack.Push(new List<ContentElement>());
                            }
                        }
                        switch ((int)item[0])
                        {
                            case 0:
                                blocksStack.Peek().Add(new TextElement((string)item[1]));
                                break;
                            case 2:
                            case 4:
                                blocksStack.Peek().Add(new HyperlinkElement(new Uri((string)item[3][0]), (string)item[1]));
                                break;
                            case 3:
                                blocksStack.Peek().Add(new MensionElement(new ProfileData((string)item[4][1], (string)item[1])));
                                break;
                        }
                        break;
                    case 1:
                        blocksStack.Peek().Add(new BreakElement());
                        break;
                }
            //ルート要素は書式設定無しのスタイル要素にする。しかし、配列に要素が
            //一つしか入っておらず、要素がスタイル要素だった場合には新しく生成せ
            //ずにそのスタイル要素を戻り値とする
            if (blocksStack.Peek().Count == 1 && blocksStack.Peek().First() is StyleElement)
                return (StyleElement)blocksStack.Peek().First();
            else
                return new StyleElement(StyleType.None, blocksStack.Pop());
        }
    }
    [System.Diagnostics.DebuggerDisplay("<br />")]
    public class BreakElement : ContentElement
    { public BreakElement() : base(ElementType.Break) { } }
    [System.Diagnostics.DebuggerDisplay("Text = {Text}, Type = {Type}")]
    public class TextElement : ContentElement
    {
        public TextElement(string text) : base(ElementType.Text) { Text = text; }
        public TextElement(string text, ElementType type) : base(type) { Text = text; }
        public string Text { get; private set; }
    }
    [System.Diagnostics.DebuggerDisplay("Style = {Style}")]
    public class StyleElement : ContentElement
    {
        public StyleElement(StyleType style, IEnumerable<ContentElement> children)
            : base(ElementType.Style)
        {
            Style = style;
            Children = children.ToArray();
        }
        public StyleType Style { get; private set; }
        public ContentElement[] Children { get; private set; }
    }
    public class MensionElement : TextElement
    {
        public MensionElement(ProfileData target)
            : base(string.Format("+{0}", target.Name), ElementType.Mension) { Target = target; }
        public ProfileData Target { get; private set; }
    }
    public class HyperlinkElement : TextElement
    {
        public HyperlinkElement(Uri link, string linkText)
            : base(linkText, ElementType.Hyperlink) { Target = link; }
        public Uri Target { get; private set; }
    }

    public enum ElementType { Text, Mension, Hyperlink, Style, Break }
    public enum StyleType { Bold = 0, Italic = 1, Middle = 2, None = 3, Unknown = 4 }
}
