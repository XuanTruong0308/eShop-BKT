using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace eShop.WebApp.Chatbot;

public abstract record ChatMessagePart;
public record TextMessagePart(string Text) : ChatMessagePart;
public record ImageMessagePart(string Alt, string Url) : ChatMessagePart;
public record LinkMessagePart(string Text, string Url) : ChatMessagePart;
public record AddToCartButtonMessagePart(int ItemId, string ItemName) : ChatMessagePart;

public static partial class MessageProcessor
{
    private static readonly Regex AddToCartRegex = new(@"\[([^\]]+)\]\s*\((?:submit:)?add-to-cart:(\d+)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static List<ChatMessagePart> ParseMessage(string message)
    {
        var parts = new List<ChatMessagePart>();
        if (string.IsNullOrEmpty(message))
        {
            return parts;
        }

        var matches = AddToCartRegex.Matches(message);
        int lastIndex = 0;

        foreach (Match match in matches)
        {
            // Add leading text if any
            if (match.Index > lastIndex)
            {
                parts.Add(new TextMessagePart(message.Substring(lastIndex, match.Index - lastIndex)));
            }

            string text = match.Groups[1].Value;
            string idStr = match.Groups[2].Value;

            if (int.TryParse(idStr, out int itemId))
            {
                parts.Add(new AddToCartButtonMessagePart(itemId, text));
            }
            else
            {
                parts.Add(new TextMessagePart(match.Value));
            }

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < message.Length)
        {
            parts.Add(new TextMessagePart(message.Substring(lastIndex)));
        }

        return parts;
    }

    public static string FormatMarkdownText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // 1. HTML encode to prevent XSS
        var encoded = System.Net.WebUtility.HtmlEncode(text);

        // 2. Convert markdown images: ![alt](url) -> <img class="chat-image" />
        encoded = Regex.Replace(encoded, @"\!\[([^\]]+)\]\(([^)]+)\)", @"<img src=""$2"" alt=""$1"" title=""$1"" class=""chat-image"" />");

        // 3. Convert standard markdown links: [text](url) -> <a href="url" class="chat-link">text</a>
        encoded = Regex.Replace(encoded, @"\[([^\]]+)\]\(([^)]+)\)", @"<a href=""$2"" class=""chat-link"" target=""_blank"">$1</a>");

        // 4. Convert bold text: **text** -> <strong>text</strong>
        encoded = Regex.Replace(encoded, @"\*\*(.*?)\*\*", "<strong>$1</strong>");

        // 5. Convert italic text: *text* -> <em>text</em>
        encoded = Regex.Replace(encoded, @"\*(?!\s)(.*?)(?<!\s)\*", "<em>$1</em>");

        // 6. Handle newlines and group lists into ul/li blocks
        var lines = encoded.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var result = new StringBuilder();
        bool inList = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            var match = Regex.Match(trimmed, @"^[\*\-]\s+(.*)$");
            if (match.Success)
            {
                if (!inList)
                {
                    result.Append("<ul class=\"chat-list\">");
                    inList = true;
                }
                var content = match.Groups[1].Value;
                result.Append($"<li>{content}</li>");
            }
            else
            {
                if (inList)
                {
                    result.Append("</ul>");
                    inList = false;
                }
                result.Append(line);
                result.Append("<br />");
            }
        }

        if (inList)
        {
            result.Append("</ul>");
        }

        return result.ToString();
    }
}
