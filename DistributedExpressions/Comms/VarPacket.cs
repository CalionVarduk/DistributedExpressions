using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using DistributedExpressions.Utils;

namespace DistributedExpressions.Comms
{
    public class VarPacket
    {
        public static VarPacket CreateReadRequest(object owner, IPEndPoint endPoint, List<string> varNames)
        {
            if (endPoint == null || varNames == null) throw new ArgumentNullException();

            var p = new VarPacket() { EndPoint = endPoint, Owner = owner };

            int len = 9 + (varNames.Count << 2);
            for (int i = 0; i < varNames.Count; ++i)
                len += (sizeof(char) * varNames[i].Length);

            int index = 0;
            p.Data = new byte[len];
            p.Data.WriteAt(BitConverter.GetBytes(len), ref index);
            p.Data[4] = 0;
            ++index;
            p.Data.WriteAt(BitConverter.GetBytes(varNames.Count), ref index);

            for (int i = 0; i < varNames.Count; ++i)
            {
                p.Data.WriteAt(BitConverter.GetBytes(varNames[i].Length * sizeof(char)), ref index);
                p.Data.WriteAt(varNames[i].ToCharArray(), ref index);
            }
            return p;
        }

        public static VarPacket CreateReadResponse(object owner, IPEndPoint endPoint, List<decimal> varValues)
        {
            if (endPoint == null) throw new ArgumentNullException();

            var p = new VarPacket() { EndPoint = endPoint, Owner = owner };

            int index = 0;
            if (varValues == null)
            {
                p.Data = new byte[9];
                p.Data.WriteAt(BitConverter.GetBytes(9), ref index);
                p.Data[4] = 1;
                ++index;
                p.Data.WriteAt(BitConverter.GetBytes(0), ref index);
            }
            else
            {
                int len = 9 + (varValues.Count << 4);
                p.Data = new byte[len];
                p.Data.WriteAt(BitConverter.GetBytes(len), ref index);
                p.Data[4] = 1;
                ++index;

                p.Data.WriteAt(BitConverter.GetBytes(varValues.Count), ref index);
                for (int i = 0; i < varValues.Count; ++i)
                    p.Data.WriteAt(DecimalConverter.GetBytes(varValues[i]), ref index);
            }
            return p;
        }

        public static VarPacket CreateWriteRequest(object owner, IPEndPoint endPoint, List<string> varNames, decimal value)
        {
            if (endPoint == null || varNames == null) throw new ArgumentNullException();

            var p = new VarPacket() { EndPoint = endPoint, Owner = owner };

            int len = 25 + (varNames.Count << 2);
            for (int i = 0; i < varNames.Count; ++i)
                len += (sizeof(char) * varNames[i].Length);

            int index = 0;
            p.Data = new byte[len];
            p.Data.WriteAt(BitConverter.GetBytes(len), ref index);
            p.Data[4] = 2;
            ++index;
            p.Data.WriteAt(BitConverter.GetBytes(varNames.Count), ref index);

            for (int i = 0; i < varNames.Count; ++i)
            {
                p.Data.WriteAt(BitConverter.GetBytes(varNames[i].Length * sizeof(char)), ref index);
                p.Data.WriteAt(varNames[i].ToCharArray(), ref index);
            }
            p.Data.WriteAt(DecimalConverter.GetBytes(value), ref index);

            return p;
        }

        public static VarPacket CreateReceived(object owner, IPEndPoint endPoint, byte[] data)
        {
            if (endPoint == null || data == null) throw new ArgumentNullException();
            if (data.Length < 9) throw new ArgumentException("Wrong packet size.");

            var p = new VarPacket() { EndPoint = endPoint, Owner = owner };
            int len = BitConverter.ToInt32(data, 0);
            if (data.Length != len) throw new ArgumentException("Wrong packet size.");
            if (data[4] > 2) throw new ArgumentException("Wrong packet type.");
            int varCount = BitConverter.ToInt32(data, 5);

            if (data[4] == 0)
            {
                int index = 9;
                for (int i = 0; i < varCount; ++i)
                {
                    int nlen = BitConverter.ToInt32(data, index);
                    index += 4 + nlen;
                    if (index > len) throw new ArgumentException("Wrong packet size.");
                }
                if (index != len) throw new ArgumentException("Wrong packet size.");
            }
            else if (data[4] == 1)
            {
                int index = 9 + (varCount << 4);
                if (index != len) throw new ArgumentException("Wrong packet size.");
            }
            else
            {
                int index = 9;
                for (int i = 0; i < varCount; ++i)
                {
                    int nlen = BitConverter.ToInt32(data, index);
                    index += 4 + nlen;
                    if (index > len) throw new ArgumentException("Wrong packet size.");
                }
                if (index + 16 != len) throw new ArgumentException("Wrong packet size.");
            }

            p.Data = data;
            return p;
        }

        public IPEndPoint EndPoint { get; private set; }

        public object Owner { get; private set; }
        public byte[] Data { get; private set; }

        public int Length { get { return BitConverter.ToInt32(Data, 0); } }

        public bool IsReadRequest { get { return Data[4] == 0; } }
        public bool IsReadResponse { get { return Data[4] == 1; } }
        public bool IsWriteRequest { get { return Data[4] == 2; } }
        public bool IsReadResponseOk { get { return IsReadResponse && VarCount > 0; } }

        public int VarCount { get { return BitConverter.ToInt32(Data, 5); } }

        public string[] RequestVarNames
        {
            get
            {
                if (IsReadResponse) return null;

                var names = new string[VarCount];
                for (int i = 0, index = 9; i < names.Length; ++i)
                {
                    int len = BitConverter.ToInt32(Data, index);
                    index += 4;
                    names[i] = Encoding.Unicode.GetString(Data, index, len);
                    index += len;
                }
                return names;
            }
        }

        public decimal[] ReadResponseVarValues
        {
            get
            {
                if (!IsReadResponseOk) return null;

                var values = new decimal[VarCount];
                for (int i = 0, index = 9; i < values.Length; ++i)
                {
                    values[i] = DecimalConverter.FromBytes(Data, index);
                    index += 16;
                }
                return values;
            }
        }

        public decimal? WriteRequestValue
        {
            get
            {
                if (!IsWriteRequest) return null;
                return DecimalConverter.FromBytes(Data, Length - 16);
            }
        }

        private VarPacket()
        { }
    }
}
