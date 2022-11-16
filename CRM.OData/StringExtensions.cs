using System;
using System.Collections.Generic;
using System.Text;

namespace Dynamics365.OData
{
    public static class StringExtensions
    {
        public static string GetPlural(this string text)
        {
            if (text.EndsWith("y")) text = text.Substring(0, text.Length - 1) + "ies";
            else if (text.EndsWith("x") || text.EndsWith("s")) text = text + "es";
            else text = text + "s";
            return text;
        }
    }
}
