﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using Squiggle.UI.Resources;
using Squiggle.UI.MessageParsers;
using System.Windows.Media;

namespace Squiggle.UI.Controls.ChatItems
{
    public class MessageItem: ChatItem
    {
        public string User { get; private set; }
        public string Message { get; private set; }
        public string FontName { get; private set; }
        public int FontSize { get; private set; }
        public System.Drawing.FontStyle FontStyle { get; private set; }
        public System.Drawing.Color Color { get; private set; }
        public MultiParser Parsers { get; private set; }

        public MessageItem(string user, string message, string fontName, int fontSize, System.Drawing.FontStyle fontStyle, System.Drawing.Color color, MultiParser parsers)
        {
            this.User = user;
            this.Message = message;
            this.FontName = fontName;
            this.FontSize = fontSize;
            this.FontStyle = fontStyle;
            this.Color = color;
            this.Parsers = parsers;
        }

        public override void AddTo(InlineCollection inlines)
        {
            AddContactSays(inlines);

            inlines.Add(new LineBreak());

            AddMessage(inlines);
        }

        void AddMessage(InlineCollection inlines)
        {
            var items = Parsers.ParseText(Message);
            var fontsettings = new FontSetting(Color, FontName, FontSize, FontStyle);

            foreach (var item in items)
            {
                item.FontFamily = fontsettings.Family;
                item.FontSize = fontsettings.Size;
                item.Foreground = fontsettings.Foreground;
                item.FontStyle = fontsettings.Style;
                item.FontWeight = fontsettings.Weight;
            }

            inlines.AddRange(items);
        }

        void AddContactSays(InlineCollection inlines)
        {
            string text = String.Format("{0} " + Translation.Instance.Global_ContactSaid + " ({1}): ", this.User, Stamp.ToShortTimeString());
            var items = Parsers.ParseText(text);
            foreach (var item in items)
                item.Foreground = Brushes.Gray;
            inlines.AddRange(items);
        }
    }
}
