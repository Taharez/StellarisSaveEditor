using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StellarisSaveEditor.Extensions
{
    public static class FileLineExtensions
    {
        public static string FindAndGetValue(this List<string> fileLines, string key)
        {
            var matchingLine = fileLines.FirstOrDefault(l => l.TrimStart().StartsWith(key));
            if (matchingLine == null)
                return string.Empty;

            return matchingLine.GetValue(key);
        }

        public static string GetValue(this string fileLine, string key)
        {
            return fileLine.Substring(fileLine.IndexOf(key) + key.Length + 1).Trim('"');
        }
    }
}
