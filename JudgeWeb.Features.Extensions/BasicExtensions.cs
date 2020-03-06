using System.Text.Json;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace System
{
    public static class BasicExtensions
    {
        /// <summary>
        /// 解开Base64编码。
        /// </summary>
        /// <param name="value">Base64字符串。</param>
        /// <returns>原字符串。</returns>
        public static string UnBase64(this string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            var bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// 进行Base64编码。
        /// </summary>
        /// <param name="value">元字符串。</param>
        /// <returns>Base64字符串。</returns>
        public static string ToBase64(this string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// 将JSON字符串转化为 <see cref="T" /> 的对象。
        /// </summary>
        /// <typeparam name="T">目标转换类型。</typeparam>
        /// <param name="jsonString">源JSON字符串。</param>
        /// <returns>反序列化后的值。</returns>
        /// <exception cref="JsonException" />
        public static T AsJson<T>(this string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString)) return default;
            return JsonSerializer.Deserialize<T>(jsonString);
        }

        /// <summary>
        /// 将对象序列化为JSON文本。
        /// </summary>
        /// <param name="value">要被序列化的对象。</param>
        /// <returns>序列化后的JSON文本。</returns>
        public static string ToJson<T>(this T value)
        {
            return JsonSerializer.Serialize(value);
        }

        /// <summary>
        /// 将 <see cref="byte[]" /> 转化为对应的十六进制码字符串。
        /// </summary>
        /// <param name="source">源编码数组。</param>
        /// <param name="lower">是否为小写字母。</param>
        /// <returns>十六进制字符串。</returns>
        public static string ToHexDigest(this byte[] source, bool lower = false)
        {
            var sb = new StringBuilder();
            var chars = (lower ? "0123456789abcdef" : "0123456789ABCDEF").ToCharArray();

            foreach (var ch in source)
            {
                var bit = (ch & 0x0f0) >> 4;
                sb.Append(chars[bit]);
                bit = ch & 0x0f;
                sb.Append(chars[bit]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 对 <see cref="byte[]" /> 进行MD5运算。
        /// </summary>
        /// <param name="source">源编码数组。</param>
        /// <returns>加密后编码数组。</returns>
        public static byte[] ToMD5(this byte[] source)
        {
            using var MD5p = new MD5CryptoServiceProvider();
            return MD5p.ComputeHash(source);
        }

        /// <summary>
        /// 对 <see cref="byte[]" /> 进行MD5运算。
        /// </summary>
        /// <param name="source">源编码数组。</param>
        /// <returns>加密后编码数组。</returns>
        public static byte[] ToMD5(this Stream source)
        {
            using var MD5p = new MD5CryptoServiceProvider();
            return MD5p.ComputeHash(source);
        }

        /// <summary>
        /// 对 <see cref="string" /> 进行MD5运算。
        /// </summary>
        /// <param name="source">源字符串。</param>
        /// <param name="encoding">指定编码，默认UTF-8。</param>
        /// <returns>编码后字符串。</returns>
        public static string ToMD5(this string source, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            return encoding.GetBytes(source).ToMD5().ToHexDigest(true);
        }
    }
}
