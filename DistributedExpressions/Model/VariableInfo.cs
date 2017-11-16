using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using DistributedExpressions.Utils;

namespace DistributedExpressions.Model
{
    public class VariableInfo
    {
        public static VariableInfo CreateAssignee(string statement)
        {
            var v = new VariableInfo();
            int i = 0;

            statement.SkipSpaces(ref i);
            v.Ip = _ParseIp(statement, ref i);
            v.LocalName = _ParseLocalName(statement, ref i);
            statement.SkipSpaces(ref i);
            if (i < statement.Length) throw new ArgumentException("Wrong assignment syntax - there may only be a single variable on the left side of the equal sign.");
            v.Name = _BuildFullName(v.Ip, v.LocalName);
            v.IsAssignee = true;

            return v;
        }

        public static VariableInfo CreateArg(string argName)
        {
            var v = new VariableInfo();
            int i = 0;

            v.Ip = _ParseIp(argName, ref i);
            v.LocalName = _ParseLocalName(argName, ref i);
            v.Name = _BuildFullName(v.Ip, v.LocalName);
            v.IsAssignee = false;

            return v;
        }

        public string Name { get; private set; }
        public string LocalName { get; private set; }
        public IPAddress Ip { get; private set; }
        public bool IsRemote { get { return Ip != null; } }
        public bool IsAssignee { get; private set; }

        private static IPAddress _ParseIp(string statement, ref int i)
        {
            if (i < statement.Length && statement[i] != '{') return null;

            ++i;
            int index = 0;
            var adr = new byte[4];

            while (i < statement.Length)
            {
                if (char.IsDigit(statement[i]))
                {
                    int j = i + 1;
                    while (j < statement.Length && char.IsDigit(statement[j]))
                        ++j;

                    int len = j - i;
                    if (len > 3) throw new ArgumentException("Wrong IPv4 syntax - integer must be in [0, 255] range.");
                    int a = int.Parse(statement.Substring(i, len));
                    if (a > 255) throw new ArgumentException("Wrong IPv4 syntax - integer must be in [0, 255] range.");
                    adr[index++] = (byte)a;

                    if (j < statement.Length)
                    {
                        if (index < 4)
                        {
                            if (statement[j] != '.')
                                throw new ArgumentException("Wrong IPv4 syntax - integer must be succeeded by a dot.");
                            i = j + 1;
                        }
                        else if (statement[j] != '}')
                            throw new ArgumentException("Wrong IPv4 syntax - ip address must end with a closing bracket.");
                        else if (++j < statement.Length && statement[j] != '.')
                            throw new ArgumentException("Wrong IPv4 syntax - ip address must be succeeded by a dot.");
                        else
                        {
                            i = j + 1;
                            return new IPAddress(adr);
                        }
                    }
                }
                else throw new ArgumentException("Wrong IPv4 syntax - usage: {a.b.c.d}.VARIABLE_NAME, where a, b, c, d are integers in [0, 255] range.");
            }
            throw new ArgumentException("Wrong IPv4 syntax - usage: {a.b.c.d}.VARIABLE_NAME, where a, b, c, d are integers in [0, 255] range.");
        }

        private static string _ParseLocalName(string statement, ref int i)
        {
            if (i >= statement.Length) throw new ArgumentException("Wrong variable name - can't be empty.");
            if (!char.IsLetter(statement[i]) && statement[i] != '_')
                throw new ArgumentException("Wrong variable name - must start with a letter or an underscore.");

            int j = i + 1;
            while (j < statement.Length && (char.IsLetterOrDigit(statement[j]) || statement[j] == '_'))
                ++j;

            int len = j - i;
            if (len > 16) throw new ArgumentException("Wrong variable name - maximum length is 16.");
            int s = i;
            i = j;
            return statement.Substring(s, len);
        }

        private static string _BuildFullName(IPAddress ip, string localName)
        {
            if (ip == null) return localName;

            var adr = ip.GetAddressBytes();
            StringBuilder sb = new StringBuilder(40);
            sb.Append('{').Append(ip.ToString()).Append("}.").Append(localName);
            return sb.ToString();
        }

        private VariableInfo()
        { }
    }
}
