using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;
using DistributedExpressions.Utils;
using DistributedExpressions.Comms;

namespace DistributedExpressions.Model
{
    public class StatementParser
    {
        private List<VariableInfo> _variables;

        public int VarCount { get { return _variables.Count; } }
        public string Statement { get; private set; }
        public MathStatement MathStatement { get; private set; }

        public StatementExecutor Executor { get; private set; }
        public string Error { get; private set; }
        public bool Successful { get { return Executor != null; } }

        public StatementParser()
        { }

        public StatementParser(string statement)
        {
            Parse(statement);
        }

        public VariableInfo InfoAt(int index)
        {
            return _variables[index];
        }

        public void Clear()
        {
            _variables = null;
            Statement = null;
            MathStatement = null;
            Executor = null;
            Error = null;
        }

        public StatementExecutor Parse(string statement)
        {
            Clear();
            if (statement == null || statement.Length == 0)
                Error = "Statement can't be empty.";
            else
            {
                var vars = new List<VariableInfo>();
                var statements = statement.Split('=');

                if (_ParseAssignees(statements, vars))
                {
                    var ms = _ParseMathStatement(statements[statements.Length - 1]);
                    if (ms != null && _ParseReadOnlyVars(ms, vars))
                    {
                        _variables = vars;
                        Statement = statement;
                        MathStatement = ms;
                        Executor = new StatementExecutor(vars, ms);
                    }
                }
            }
            return Executor;
        }

        private bool _ParseAssignees(string[] statements, List<VariableInfo> vars)
        {
            int i = statements.Length - 2;

            try
            {
                while (i >= 0)
                {
                    vars.Add(VariableInfo.CreateAssignee(statements[i]));
                    --i;
                }
            }
            catch (ArgumentException ex)
            {
                Error = "[At: '" + statements[i] + "']" + Environment.NewLine + ex.Message;
                return false;
            }
            return true;
        }

        private MathStatement _ParseMathStatement(string statement)
        {
            MathStatement ms = null;
            try
            {
                ms = new MathStatement(statement);
            }
            catch (ArgumentException ex)
            {
                Error = "[At math statement]" + Environment.NewLine + ex.Message;
                return null;
            }
            return ms;
        }

        private bool _ParseReadOnlyVars(MathStatement ms, List<VariableInfo> vars)
        {
            var args = ms.ArgNames;
            int i = 0;

            try
            {
                while (i < args.Count)
                {
                    vars.Add(VariableInfo.CreateArg(args[i]));
                    ++i;
                }
            }
            catch (ArgumentException ex)
            {
                Error = "[At: '" + args[i] + "']" + Environment.NewLine + ex.Message;
                return false;
            }
            return true;
        }
    }
}
