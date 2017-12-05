﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using static ComputerV2_class.Exceptions.InvalidExpressionException;

namespace ComputerV2_class
{
    public static class Parser
    {
        public static string Assign(string expr, string val, ref List<List<string>> vars, ref List<List<string>> funcs)
        {
            var rgxStr = @"([a-zA-Z]+\(([a-zA-Z]+)|(\d+([\.,]\d+)?)\))";
            var rgx = new Regex(rgxStr);
            var fVar = "";
            (bool Found, string Value) mx;
            if (!rgx.IsMatch(expr))
            {
                rgxStr = @"^(?<!(\d+([\.,]\d+)?))([a-zA-Z]+)(?!(\d+([\.,]\d+)?))$";
                rgx = new Regex(rgxStr);
                if (!rgx.IsMatch(expr)) throw new InvalidExpressionException("No variable or Function found");
                if (expr == "i") throw new InvalidExpressionException("Variable: i is reserved.");
                val = Substitute(val, funcs, vars, fVar);
                Undefined(val, "i");
                mx = Matrix.MatrixManipulation(val);
                if (mx.Found) val = mx.Value;
                return (AssignVariable(expr, val, ref vars));
            }
            fVar = Regex.Split(expr, @"\(|\)")[1];
            val = Substitute(val, funcs, vars, fVar);
            mx = Matrix.MatrixManipulation(val);
            if (mx.Found) val = mx.Value;
            val = Functions.NormaliseFunc(val);
            return(AssignFunction(expr, val, ref funcs));
        }

        public static string Substitute(string expr, List<List<string>> funcs, List<List<string>> vars, string fVar)
        {
            var func = MatchFunction(expr, funcs, vars);
            expr = func.Value;
            fVar = func.Variable == "" ? fVar : func.Variable;
            expr = MatchVariable(expr, vars);
            Undefined(expr, fVar);
            return (expr);
        }

        private static (string Variable, string Value) MatchFunction(string expr, List<List<string>> funcs, List<List<string>> vars)
        {
            var rgx = new Regex(@"([a-zA-Z]+\((([a-zA-Z]+)|((\-)?\d+([\.]\d+)?))\))");
            var match = rgx.Matches(expr);
            var fVar = "";
            for (var i = 0; i < match.Count; i++)
            {
                if (!match[i].Success) continue;
                var tmp = match[i].Value;
                var func = Regex.Split(tmp, @"\(|\)");
                foreach (var f in funcs)
                {
                    if (f[0] != func[0]) continue;
                    fVar = func[1];
                    var rplc = f[2];
                    if (Regex.IsMatch(func[1], @"^[a-zA-Z]+$"))
                    {
                        foreach (var v in vars)
                        {
                            if (v[0] != func[1]) continue;
                            rplc = rplc.Replace(f[1], $"1*{v[1]}");
                            rplc = $"1*{Maths.Calculate(rplc)}";
                            expr = expr.Replace(tmp, rplc);
                            break;
                        }
                        expr = expr.Replace(tmp, f[2]);
                    }
                    else
                    {
                        rplc = rplc.Replace(f[1], $"1*{func[1]}");
                        rplc = $"1*{Maths.Calculate(rplc)}";
                        expr = expr.Replace(tmp, rplc);
                    }
                }
            }
            return (fVar, expr); 
        }

        //find the value of the variable
        private static string MatchVariable(string expr, List<List<string>> vars)
        {
            var rgx = new Regex(@"[A-Za-z]+");
            var match = rgx.Matches(expr);
            for (var i = 0; i < match.Count; i++)
            {
                foreach (var v in vars)
                {
                    if (v[0] != match[i].Value) continue;
                    expr = expr.Replace(match[i].Value, $"1*{v[1]}");
                    break;
                }
            }
            if (!Regex.IsMatch(expr, @"[A-zA-Z]+"))
                expr = Maths.Calculate(expr);
            return expr;
        }
        
        private static string AssignFunction(string expr, string value, ref List<List<string>> funcs)
        {
            if (!Regex.IsMatch(expr, @"[a-zA-Z]+(\()(.*)(\))")) 
                throw new InvalidExpressionException("Function is not formatted correctly.");
            var func = Regex.Split(expr, @"[\(\)]");
            var matches = Regex.Matches(value, @"[a-zA-Z]+");
            var var = new string[matches.Count];
            if (func[1] == "i") 
                throw new InvalidExpressionException("variable i cannot be used with a function");
            for (var i = 0; i < matches.Count; i++)
                var[i] = matches[i].Value;
            if (var.Any(v => v != func[1] && v !="i"))
                throw new InvalidExpressionException("Function should only contain one variable");
            foreach (var t in funcs)
            {
                if (t[0] != func[0]) continue;
                t[1] = func[1];
                t[2] = value;
                return (t[2]);
            }
            var newFunc = new List<string>
            {
                func[0],
                func[1],
                value
            };
            funcs.Add(newFunc);
            return (funcs[funcs.Count - 1][2]);
        }

        private static string AssignVariable(string expr, string value, ref List<List<string>> vars)
        {
            foreach (var t in vars)
            {
                if (t[0] != expr) continue;
                t[1] = Maths.Calculate(value);
                return (t[1]);
            }
            var newVar = new List<string>
            {
                expr,
                value
            };
            vars.Add(newVar);
            return (vars[vars.Count - 1][1]);
        }
        
        private static void Undefined(string val, string fVar)
        {
            var rgx = new Regex(@"(((\-|\+)(\s+)?)?[a-zA-Z]+\((([a-zA-Z]+)|(\d+([\.,]\d+)?))\))", RegexOptions.None);
            var match = rgx.Match(val);
            if (match.Success) throw new InvalidExpressionException($"Function : {match.Value} is not defined.");
            rgx = new Regex(@"[A-Za-z]+", RegexOptions.None);
            match = rgx.Match(val);
            if (match.Success && match.Value != fVar && match.Value != "i") throw new InvalidExpressionException($"Variable : {match.Value} is not defined.");
        }
    }
}
