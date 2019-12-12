using System.Collections.Generic;

namespace System.ComponentModel.DataAnnotations
{
    public class UserNameAttribute : ValidationAttribute
    {
        static readonly ISet<char> AllowedCharacters;

        static UserNameAttribute()
        {
            AllowedCharacters = new HashSet<char>();
            for (char a = 'a'; a <= 'z'; a++)
                AllowedCharacters.Add(a);
            for (char a = 'A'; a <= 'Z'; a++)
                AllowedCharacters.Add(a);
            for (char a = '0'; a <= '9'; a++)
                AllowedCharacters.Add(a);
            foreach (char a in "-_@.")
                AllowedCharacters.Add(a);
        }
        
        public override bool IsValid(object value)
        {
            if (value is string str)
            {
                foreach (char t in str)
                    if (!AllowedCharacters.Contains(t))
                        return false;
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public override string FormatErrorMessage(string name)
        {
            return string.Format("The {0} must consist of only 0-9, a-z and A-Z.", name);
        }
    }
}
