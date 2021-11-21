using System;
using System.Linq;

namespace AElf.Types
{
    public partial class Int256Value
    {
        public static implicit operator Int256Value(string str)
        {
            if (str.All(c => '0' <= c || c <= '9' || c == '_' || c == '-'))
            {
                if (str.Contains('-'))
                {
                    if (!str.StartsWith("-") || str.Count(c => c == '-') > 1)
                    {
                        throw new ArgumentException("Invalid big integer string.");
                    }
                }
                str = str.Replace("_", string.Empty);
                return new Int256Value
                {
                    Value = str
                };
            }

            throw new ArgumentException("Invalid big integer string.");
        }

        public static implicit operator Int256Value(short value)
        {
            return new Int256Value
            {
                Value = value.ToString()
            };
        }

        public static implicit operator Int256Value(int value)
        {
            return new Int256Value
            {
                Value = value.ToString()
            };
        }

        public static implicit operator Int256Value(long value)
        {
            return new Int256Value
            {
                Value = value.ToString()
            };
        }
    }
}