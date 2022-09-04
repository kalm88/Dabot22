using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace SnowLib.Common
{
    public static class Utils
    {
        public static void PerformSafely<T>(this Control target, Action<T> action) where T : Control
        {
            if (target.InvokeRequired)
                target.Invoke(action, target);
            else
                action(target as T);
        }

        public static string GetHashString(string value)
        {
            var md5 = MD5.Create();
            var buffer = Encoding.ASCII.GetBytes(value);
            var hash = md5.ComputeHash(buffer);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
        }
    }
}