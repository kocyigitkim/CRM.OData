using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamics365.OData
{
    static class Localization
    {
        static CultureInfo cultureInfoEN = CultureInfo.GetCultureInfo("en-US");
        static CultureInfo cultureInfoTR = CultureInfo.GetCultureInfo("tr-TR");
        internal static string TitlecaseEN(this string value)
        {
            if (value == null) return null;
            return cultureInfoEN.TextInfo.ToTitleCase(value);
        }
        internal static string TitlecaseTR(this string value)
        {
            if (value == null) return null;
            return cultureInfoTR.TextInfo.ToTitleCase(value);
        }
        internal static string LowercaseEN(this string value)
        {
            if (value == null) return null;
            return cultureInfoEN.TextInfo.ToLower(value);
        }
        internal static string LowercaseTR(this string value)
        {
            if (value == null) return null;
            return cultureInfoTR.TextInfo.ToLower(value);
        }
        internal static string UppercaseEN(this string value)
        {
            if (value == null) return null;
            return cultureInfoEN.TextInfo.ToUpper(value);
        }
        internal static string UppercaseTR(this string value)
        {
            if (value == null) return null;
            return cultureInfoTR.TextInfo.ToUpper(value);
        }
    }
}
