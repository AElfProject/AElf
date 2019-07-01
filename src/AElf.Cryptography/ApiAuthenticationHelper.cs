using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Cryptography
{
    public class ApiAuthenticationHelper
    {
        // Done, removed: check
        public static string GetSign(string chainApikey, string chainId, string method, string timestamp)
        {
            var signValue = $"{chainApikey}{chainId}{method}{timestamp}".ToLower();
            var encrypt = Encoding.ASCII.GetBytes(signValue);
            var md5Csp = new MD5CryptoServiceProvider();
            var resEncrypt = md5Csp.ComputeHash(encrypt);
            var sBuilder = new StringBuilder();
            foreach (var t in resEncrypt)
            {
                sBuilder.Append(t.ToString("x2"));
            }

            return sBuilder.ToString();
        }

        public static bool Check(string chainApikey, string chainId, string method, string timestamp, string sign,int timeout)
        {
            var time = GetTime(timestamp);
            if (time.AddMinutes(timeout) < DateTime.Now)
            {
                return false;
            }

            if (sign != GetSign(chainApikey, chainId, method, timestamp))
            {
                return false;
            }

            return true;
        }

        public static string GetTimestamp(DateTime time)
        {
            var startTime = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1, 0, 0, 0, 0), TimeZoneInfo.Local);
            var t = (time.Ticks - startTime.Ticks) / 10000000;
            return t.ToString();
        }

        public static DateTime GetTime(string timeStamp)
        {
            var dtStart = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1), TimeZoneInfo.Local);
            var lTime = long.Parse(timeStamp + "0000000");
            var toNow = new TimeSpan(lTime);
            return dtStart.Add(toNow);
        }
    }
}