using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace DivaHook.Injection
{
    internal static class InjectionHelper
    {
        internal static TDelegate GetDelegateForFunctionPointer<TDelegate>(long address)
        {
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(new IntPtr(address));
        }

        internal static string Sprintf(string format, params object[] arguments)
        {
            int index = 0;
            format = Regex.Replace(format, "%.", m => ("{" + ++index + "}"));

            return string.Format(format, arguments);
        }
    }
}
