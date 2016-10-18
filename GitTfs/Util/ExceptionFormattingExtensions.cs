using System;
using System.Text;

namespace Sep.Git.Tfs.Util
{
    public static class ExceptionFormattingExtensions
    {
        public static string IndentExceptionMessage(this Exception e, string indent = "   ")
        {
            var sb = new StringBuilder();
            foreach (var s in e.ToString().Split('\n'))
            {
                sb.Append(indent).Append(s);
            }
            return sb.ToString();
        }
    }
}