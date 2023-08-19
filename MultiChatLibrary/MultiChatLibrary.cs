using System;
using MultiChatLibrary.Models;
using static MultiChatLibrary.Models.MessageModel;

namespace MultiChatLibrary
{
    public static class MultiChatLibrary
    {
        private const string Delimiter = "|";

        public static MessageModel ExtractMessage(string text)
        {
            Console.WriteLine(text);
            var msg = GetBetween(text, "@payload:", Delimiter);
            var issuer = GetBetween(text, "@issuer:", Delimiter);
            Enum.TryParse(GetBetween(text, "@type:", Delimiter), out State state);
            return new MessageModel(issuer, msg, state);
        }

        private static string GetBetween(string strSource, string strStart, string strEnd)
        {
            if (!strSource.Contains(strStart) || !strSource.Contains(strEnd)) return "";
            var start = strSource.IndexOf(strStart, 0) + strStart.Length;
            var end = strSource.IndexOf(strEnd, start);
            return strSource.Substring(start, end - start);
        }
    }
}