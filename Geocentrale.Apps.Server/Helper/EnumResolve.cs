using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Geocentrale.Apps.Server.Helper
{
    public class EnumResolve
    {
        public static T ParseEnum<T>(string value) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            T result;

            if (!Enum.TryParse<T>(value, out result))
            {
                throw new Exception("value1 is not valid member of enumeration MyEnum");
            }

            return result;
        }
    }
}