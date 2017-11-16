using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistributedExpressions.Utils;

namespace DistributedExpressions.Model
{
    public class MathStatement
    {
        public static bool IsValidArgChar(char c)
        {
            return c == ' ' || (!char.IsControl(c) && !char.IsWhiteSpace(c) && !IsOperatorChar(c) && c != '=' && c != '(' && c != ')');
        }

        public static bool IsOperatorChar(char c)
        {
            return c == '+' || c == '-' || c == '*' || c == '/' || c == '%' || c == '^';
        }

        public static int OperatorPrecedence(char op)
        {
            if (op == '+' || op == '-') return 1;
            if (op == '*' || op == '/' || op == '%') return 2;
            if (op == '^') return 3;
            if (op == 'N' || op == 'P') return 4;
            return -1;
        }

        public static bool IsValidArgName(string name)
        {
            if (name == null || name.Length == 0 || char.IsDigit(name[0])) return false;

            for (int i = 0; i < name.Length; ++i)
                if (!IsValidArgChar(name[i])) return false;
            return true;
        }

        private Codebook<string> _argBook;

        public int ArgCount { get { return _argBook.Count; } }
        public IReadOnlyList<string> ArgNames { get { return _argBook.Items; } }

        public string Postfix { get; private set; }
        public string Input { get; private set; }

        public string InputString { get; private set; }
        public string TokenizedPostfix { get; private set; }
        public string TokenizedInfix { get { return BuildInfix(true); } }
        public string Infix { get { return BuildInfix(false); } }

        public MathStatement(string infix = "0")
        {
            if (infix == null) throw new ArgumentNullException();
            Build(infix);
        }

        public bool ArgExists(string name)
        {
            return _argBook.Contains(name);
        }

        public int ArgIndex(string name)
        {
            return _argBook.GetCode(name);
        }

        public string ArgName(int index)
        {
            return _argBook.GetItem(index);
        }

        public bool RenameArg(string oldName, string newName)
        {
            int i = _argBook.GetCode(oldName);
            if (i == -1) return false;
            return RenameArgAt(i, newName);
        }

        public bool RenameArgAt(int index, string newName)
        {
            if (_argBook.Contains(newName)) return false;
            _argBook[index] = newName;
            return true;
        }

        public decimal Compute(params decimal[] args)
        {
            if (args.Length != ArgCount) throw new ArgumentException("Incorrect argument count.");
            
            int i = 0;
            var s = new Stack<decimal>();

            while (i < TokenizedPostfix.Length)
            {
                char c = TokenizedPostfix[i++];
                if (c == '{')
                {
                    if (TokenizedPostfix[i] == 'A')
                    {
                        int index = TokenizedPostfix[i += 3] - '0';
                        ++i;

                        while (TokenizedPostfix[i] != '}')
                        {
                            index *= 10;
                            index += TokenizedPostfix[i++] - '0';
                        }
                        s.Push(args[index]);
                        ++i;
                    }
                    else
                    {
                        int j = i + 1;
                        while (TokenizedPostfix[j] != '}') ++j;
                        s.Push(decimal.Parse(TokenizedPostfix.Substring(i, j - i)));
                        i = j + 1;
                    }
                }
                else if (c == 'N') s.Push(-s.Pop());
                else if (c == 'P') s.Push(Math.Abs(s.Pop()));
                else
                {
                    var x1 = s.Pop();
                    var x2 = s.Pop();

                    if (c == '+') s.Push(x2 + x1);
                    else if (c == '-') s.Push(x2 - x1);
                    else if (c == '*') s.Push(x2 * x1);
                    else if (c == '/')
                    {
                        if (x1 == 0)
                            throw new ArithmeticException("Division by zero.");
                        s.Push(x2 / x1);
                    }
                    else if (c == '^')
                    {
                        if (x2 < 0 && ((x1 > 0 && x1 < 1) || (x1 > -1 && x1 < 0)))
                            throw new ArithmeticException("Incorrect exponentation: " + x2.ToString() + "^" + x1.ToString() + ".");
                        s.Push((decimal)Math.Pow((double)x2, (double)x1));
                    }
                    else if (c == '%')
                    {
                        if (x1 == 0)
                            throw new ArithmeticException("Division by zero.");
                        s.Push(x2 % x1);
                    }
                }
            }
            return s.Pop();
        }

        private void Build(string infix)
        {
            int argCount = 0, opCount = 0, parCount = 0;
            var tArgs = new Codebook<string>();

            bool expectsArg = true, signedArg = false;
            var postfix = new StringBuilder(200);
            var s = new Stack<char>();
            int i = 0;

            while (i < infix.Length)
            {
                char c = infix[i++];
                if (c == ' ') continue;

                if (c == '(')
                {
                    ++parCount;                 // increase the amount of currently opened parentheses
                    signedArg = false;          // an argument is expected now (or another open parentheses), and so the flags are reset
                    expectsArg = true;
                    s.Push(c);
                }
                else if (c == '*' || c == '/' || c == '^' || c == '%')
                {
                    if (expectsArg)
                        throw new ArgumentException("Expected an argument at [" + i.ToString() + "] - found an operator '" + c.ToString() + "' instead.");
                    // increase the amount of found operators
                    ++opCount;                  // and set the flag to expect an argument (or open parentheses)
                    expectsArg = true;          // at this point, signedArg flag is already set to false (done after argument parsing)
                    while (s.Count > 0 && OperatorPrecedence(c) <= OperatorPrecedence(s.Peek()))
                        postfix.Append(s.Pop());
                    s.Push(c);                  // operators with higher precedence get popped and added to the postfix string
                }
                else if (c == '+' || c == '-')
                {
                    if (!expectsArg)
                    {
                        ++opCount;              // same as with operators above
                        expectsArg = true;
                        while (s.Count > 0 && OperatorPrecedence(c) <= OperatorPrecedence(s.Peek()))
                            postfix.Append(s.Pop());
                        s.Push(c);
                    }
                    else if (!signedArg)
                    {
                        signedArg = true;               // additionally, + and - signs can be interpreted as unary operators
                        s.Push(c == '-' ? 'N' : 'P');   // so when an argument is expected, but one of those operators is found instead
                    }                                   // it is treated as a sign of a hopefully upcoming argument
                    else throw
                        new ArgumentException("Expected an argument at [" + i.ToString() + "] - found an operator '" + c.ToString() + "' instead.");
                }
                else if (c == ')')
                {
                    if (--parCount < 0)
                        throw new ArgumentException("Encountered too many closing parentheses at [" + i.ToString() + "].");

                    expectsArg = false;         // after a closing parentheses an operator is expected, also all operators within that parentheses get popped
                    while (s.Count > 0 && s.Peek() != '(')
                        postfix.Append(s.Pop());

                    if (s.Count > 0)
                    {
                        s.Pop();                // pop the open parentheses and check if the whole parentheses was signed
                        if (s.Count > 0 && (s.Peek() == 'N' || s.Peek() == 'P'))
                            postfix.Append(s.Pop());
                    }
                }
                else if (expectsArg)
                {
                    postfix.Append('{');        // marks the beginning of an argument token
                    int j = i;

                    if (char.IsDigit(c))
                    {                           // handles a constant numeric value parsing, isReal is here to accept only one decimal point symbol
                        bool isReal = false;    // also ignores spaces and can't deal with scientific notation (yet)
                        while (j < infix.Length && (char.IsDigit(infix[j]) || infix[j] == ' ' || ((infix[j] == '.' || infix[j] == ',') && (isReal = !isReal))))
                            ++j;

                        decimal n = decimal.Parse(infix.Substring(i - 1, j - i + 1).Replace(" ", string.Empty).Replace(',', '.'),
                                                                              System.Globalization.CultureInfo.InvariantCulture); // for decimal point
                        postfix.Append(n.ToString()).Append('}');       // adds the number and closes the token
                    }
                    else
                    {
                        --j;                    // finds the last symbol index for the argument's valid name
                        while (j < infix.Length && IsValidArgChar(infix[j]))
                            ++j;

                        string a = infix.Substring(i - 1, j - i + 1).Trim(' '); // TrimEnd should be enough
                        if (string.IsNullOrWhiteSpace(a))
                            throw new ArgumentException("Expected an argument at [" + i.ToString() + "] - it appears to be unnamed.");

                        tArgs.Add(a);            // adds the argument and closes the token
                        postfix.Append("Arg").Append(tArgs.GetCode(a).ToString()).Append('}');
                    }
                    // checks if the argument was signed, if so, pop the operator and add it to the postfix string
                    if (s.Count > 0 && (s.Peek() == 'N' || s.Peek() == 'P'))
                        postfix.Append(s.Pop());

                    expectsArg = signedArg = false;
                    ++argCount;
                    i = j;                      // skips characters scanned during argument parsing
                }
                else throw
                    new ArgumentException("Expected an operator at [" + i.ToString() + "] - found an argument instead.");
            }

            if (argCount == 0) throw new ArgumentException("Encountered an empty expression.");
            if (argCount != opCount + 1) throw new ArgumentException("Argument count must be equal to operator count + 1.");
            if (parCount > 0) throw new ArgumentException("Too many opening parentheses in the expression.");   // parantheses could be ignored here?
            if (parCount < 0) throw new ArgumentException("Too many closing parentheses in the expression.");

            while (s.Count > 0)
                postfix.Append(s.Pop());

            TokenizedPostfix = postfix.ToString();
            _argBook = tArgs;
            InputString = infix;
        }

        private string BuildInfix(bool tokenize)
        {
            int i = 0;
            var s = new Stack<BuildInfixNode>();
            var open = new BuildInfixNode("(");
            var close = new BuildInfixNode(")");

            while (i < TokenizedPostfix.Length)
            {
                char c = TokenizedPostfix[i++];
                if (c == '{')
                {
                    if (TokenizedPostfix[i] == 'A')
                    {
                        int j = i + 3;      // parsing an argument, j points at the 1st digit of the argument's index
                        int k = j + 1;
                        while (TokenizedPostfix[k] != '}') ++k;

                        if (tokenize)
                            s.Push(new BuildInfixNode(TokenizedPostfix.Substring(i - 1, k - i + 2)));
                        else
                            s.Push(new BuildInfixNode(_argBook[int.Parse(TokenizedPostfix.Substring(j, k - j))]));

                        i = k + 1;
                    }
                    else
                    {
                        int j = i + 1;      // parsing a const number, j points at the number's 1st digit 
                        while (TokenizedPostfix[j] != '}') ++j;

                        if (tokenize)
                            s.Push(new BuildInfixNode(TokenizedPostfix.Substring(i - 1, j - i + 2)));
                        else
                            s.Push(new BuildInfixNode(TokenizedPostfix.Substring(i, j - i)));

                        i = j + 1;
                    }
                }
                else if (c == 'N') s.Push(new BuildInfixNode("-", s.Pop()));
                else if (c == 'P') s.Push(new BuildInfixNode("+", s.Pop()));
                else
                {
                    var x1 = s.Pop();       // merges a binary operator with two of its operands
                    var x2 = s.Pop();
                    s.Push(new BuildInfixNode(open, x2, c.ToString(), x1, close));
                }
            }
            return s.Pop().ToString();
        }

        private class BuildInfixNode
        {
            public string Value { get; private set; }
            public BuildInfixNode[] Children { get; private set; }

            public BuildInfixNode(string value)
            {
                Value = value;
                Children = null;
            }

            public BuildInfixNode(string op, BuildInfixNode operand)
            {
                Value = null;
                Children = new BuildInfixNode[] { new BuildInfixNode(op), operand };
            }

            public BuildInfixNode(BuildInfixNode open, BuildInfixNode left, string op, BuildInfixNode right, BuildInfixNode close)
            {
                Value = null;
                Children = new BuildInfixNode[] { open, left, new BuildInfixNode(" " + op + " "), right, close };
            }

            public override string ToString()
            {
                if (Value != null) return Value;

                var sb = new StringBuilder(200);
                var s = new Stack<BuildInfixNode>();
                if (Children[0].Value[0] == '(')
                    for (int i = Children.Length - 2; i > 0; --i) s.Push(Children[i]);
                else for (int i = Children.Length - 1; i >= 0; --i) s.Push(Children[i]);

                while (s.Count > 0)                 // pre order traversal
                {
                    var n = s.Pop();
                    if (n.Value != null) sb.Append(n.Value);
                    else for (int i = n.Children.Length - 1; i >= 0; --i) s.Push(n.Children[i]);
                }
                return sb.ToString();
            }
        }
    }
}
