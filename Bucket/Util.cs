using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace discord.plugins
{
    public static class Util
    {
        public static byte[] ToUtf8(this string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public static string BuildString(this IEnumerable<char> it)
        {
            var sb = new StringBuilder();
            foreach (var c in it)
            {
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
