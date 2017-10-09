using System.Net.Mime;
using System.Text.RegularExpressions;

namespace SenseNet.Search.Azure.Querying
{
    public static class StringHelper
    {
        public static int WordCount(this string text)
        {
            return Regex.Matches(text, "(?:\\S+)").Count;
        }
    }
}