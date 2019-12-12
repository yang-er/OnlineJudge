using System;

namespace Microsoft.AspNetCore.Identity
{
    public class MySqlOldPasswordHasher<TUser> : PasswordHasher<TUser>
        where TUser : class
    {
        internal static string OldPassword(string origin)
        {
            long nr = 1345345333L, add = 7, nr2 = 0x12345671L;

            long tmp = 0;

            for (int i = 0; i < origin.Length; i++)
            {
                tmp = origin[i];
                if (tmp == ' ' || tmp == '\t') continue;
                nr ^= (((nr & 63) + add) * tmp) + (nr << 8);
                nr2 += (nr2 << 8) ^ nr; add += tmp;
            }

            long result_1 = nr & ((1L << 31) - 1L);
            long result_2 = nr2 & ((1L << 31) - 1L);
            return ToHexDigest(result_1) + ToHexDigest(result_2);
        }

        private static string ToHexDigest(long qwq)
        {
            var chars = "0123456789abcdef".ToCharArray();

            char[] vs = new char[8];
            for (int i = 7; i >= 0; i--)
            {
                vs[i] = chars[qwq & 0xf];
                qwq >>= 4;
            }

            return new string(vs);
        }

        public override PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
        {
            if (hashedPassword == null)
                throw new ArgumentNullException(nameof(hashedPassword));
            if (providedPassword == null)
                throw new ArgumentNullException(nameof(providedPassword));

            if (hashedPassword == OldPassword(providedPassword))
                return PasswordVerificationResult.SuccessRehashNeeded;
            return base.VerifyHashedPassword(user, hashedPassword, providedPassword);
        }
    }
}
