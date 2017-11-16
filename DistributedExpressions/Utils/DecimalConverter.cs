using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedExpressions.Utils
{
    public static class DecimalConverter
    {
        public static byte[] GetBytes(decimal d)
        {
            var ints = decimal.GetBits(d);
            var bytes = new byte[16];
            for (int i = 0, j = 0; i < 4; ++i)
            {
                var b = BitConverter.GetBytes(ints[i]);
                for (int k = 0; k < 4; ++k, ++j)
                    bytes[j] = b[k];
            }
            return bytes;
        }

        public static decimal FromBytes(byte[] value, int startIndex)
        {
            var ints = new int[4];
            for (int i = 0; i < 4; ++i)
                ints[i] = BitConverter.ToInt32(value, startIndex + (i << 2));
            return new decimal(ints);
        }
    }
}
