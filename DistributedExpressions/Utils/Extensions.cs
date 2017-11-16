using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedExpressions.Utils
{
    public static class StringExtension
    {
        public static void SkipSpaces(this string s, int index)
        {
            while (index < s.Length && s[index] == ' ')
                ++index;
        }

        public static void SkipSpaces(this string s, ref int index)
        {
            while (index < s.Length && s[index] == ' ')
                ++index;
        }
    }

    public static class ByteArrayExtension
    {
        public static void WriteAt(this byte[] b, byte[] data, int index)
        {
            for (int j = 0; j < data.Length; ++j)
                b[index++] = data[j];
        }

        public static void WriteAt(this byte[] b, byte[] data, ref int index)
        {
            for (int j = 0; j < data.Length; ++j)
                b[index++] = data[j];
        }

        public static void WriteAt(this byte[] b, char[] data, int index)
        {
            for (int j = 0; j < data.Length; ++j)
            {
                var c = BitConverter.GetBytes(data[j]);
                for (int k = 0; k < c.Length; ++k)
                    b[index++] = c[k];
            }
        }

        public static void WriteAt(this byte[] b, char[] data, ref int index)
        {
            for (int j = 0; j < data.Length; ++j)
            {
                var c = BitConverter.GetBytes(data[j]);
                for (int k = 0; k < c.Length; ++k)
                    b[index++] = c[k];
            }
        }
    }
}
