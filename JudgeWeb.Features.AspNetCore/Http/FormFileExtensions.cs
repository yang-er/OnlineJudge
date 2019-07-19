using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    public static class FormFileExtensions
    {
        public static async Task<(byte[], string)> ReadAsync(this IFormFile file)
        {
            using (var stream = file.OpenReadStream())
            {
                var input = new byte[file.Length];
                int cursor = 0;
                while (cursor < file.Length)
                    cursor += await stream.ReadAsync(input, cursor, input.Length - cursor);
                var inputHash = input.ToMD5().ToHexDigest(true);
                return (input, inputHash);
            }
        }
    }
}
