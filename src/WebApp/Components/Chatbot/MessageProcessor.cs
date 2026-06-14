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
    private static readonly Regex AddToCartRegex = new(
        @"\[([^\]]+)\]\s*\((?:submit:)?add-to-cart:(\d+)\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public static List<ChatMessagePart> ParseMessage(string message)
    {
        var parts = new List<ChatMessagePart>();
        if (string.IsNullOrEmpty(message))
            return parts;

        var matches = AddToCartRegex.Matches(message);
        int lastIndex = 0;

        foreach (Match match in matches)
        {
            if (match.Index > lastIndex)
                parts.Add(
                    new TextMessagePart(message.Substring(lastIndex, match.Index - lastIndex))
                );

            string text = match.Groups[1].Value;
            string idStr = match.Groups[2].Value;

            if (int.TryParse(idStr, out int itemId))
                parts.Add(new AddToCartButtonMessagePart(itemId, text));
            else
                parts.Add(new TextMessagePart(match.Value));

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < message.Length)
            parts.Add(new TextMessagePart(message.Substring(lastIndex)));

        return parts;
    }

    public static string FormatMarkdownText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // ✅ QUAN TRỌNG: Xử lý markdown TRƯỚC, KHÔNG encode HTML trước
        // Bug cũ: HtmlEncode() biến [text](url) thành [text]&#40;url&#41; → regex không match được

        // 1. Convert markdown images: ![alt](url) → <img />
        var result = Regex.Replace(
            text,
            @"\!\[([^\]]+)\]\(([^)]+)\)",
            @"<img src=""$2"" alt=""$1"" title=""$1"" class=""chat-image"" />"
        );

        // 2. Convert markdown links: [text](url) → <a href>
        result = Regex.Replace(
            result,
            @"\[([^\]]+)\]\(([^)]+)\)",
            @"<a href=""$2"" class=""chat-link"">$1</a>"
        );

        // 3. Convert bold: **text** → <strong>
        result = Regex.Replace(result, @"\*\*(.*?)\*\*", "<strong>$1</strong>");

        // 4. Convert italic: *text* → <em>  (tránh nhầm với list bullet *)
        result = Regex.Replace(
            result,
            @"(?<!\*)\*(?!\*)(?!\s)(.*?)(?<!\s)(?<!\*)\*(?!\*)",
            "<em>$1</em>"
        );

        // 5. Xử lý list và newline
        var lines = result.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var sb = new StringBuilder();
        bool inList = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            var listMatch = Regex.Match(trimmed, @"^[\*\-]\s+(.*)$");
            if (listMatch.Success)
            {
                if (!inList)
                {
                    sb.Append("<ul class=\"chat-list\">");
                    inList = true;
                }
                sb.Append($"<li>{listMatch.Groups[1].Value}</li>");
            }
            else
            {
                if (inList)
                {
                    sb.Append("</ul>");
                    inList = false;
                }
                if (!string.IsNullOrWhiteSpace(line))
                {
                    sb.Append(line);
                    sb.Append("<br />");
                }
            }
        }

        if (inList)
            sb.Append("</ul>");

        return sb.ToString();
    }
}
