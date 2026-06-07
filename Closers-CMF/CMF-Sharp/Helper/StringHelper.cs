using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Leayal.Closers.CMF.Helper
{
    internal static class StringHelper
    {
        internal static string RemoveNullChar(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            StringBuilder sb = null;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '\0')
                {
                    if (sb == null)
                    {
                        sb = new StringBuilder(str.Length);
                        sb.Append(str, 0, i);
                    }
                }
                else if (sb != null)
                {
                    sb.Append(str[i]);
                }
            }
            return sb == null ? str : sb.ToString();
        }
    }
}
