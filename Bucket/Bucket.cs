using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace discord.plugins
{
    public partial class Bucket
    {
        private readonly List<MethodInfo> handlers;
        private readonly Dictionary<string, MethodInfo> variableHandlers;
        private readonly string prefix;

        private readonly Random random = new Random();

        private ulong who;
        private FactRow that;

        public Action<string> Output;
        public Action<string> DebugOutput;
        public Func<ulong, string> NameFromId;

        public Bucket(string mentionPrefix, Action<string> debugOutput = null)
        {
            prefix = mentionPrefix;
            DebugOutput = debugOutput;

            var commands = GetType().GetMethods().ToList();

            // Find handlers
            var handlerRegex = new Regex(@"^Cmd(\d+)_([\S]*)");
            var handlerFuncs = commands.Where(c => handlerRegex.IsMatch(c.Name)).Select(c =>
            {
                var groups = handlerRegex.Match(c.Name).Groups;

                var priority = int.Parse(groups[1].Value);
                var name = groups[2].Value;

                return Tuple.Create(c, priority, name);
            }).OrderByDescending(c => c.Item2);
            handlers = handlerFuncs.Select(c => c.Item1).ToList();

            // Find variable handlers
            var variableRegex = new Regex(@"^Var_(\S+)");
            var variableFuncs = commands.Where(c => variableRegex.IsMatch(c.Name)).Select(c =>
            {
                var groups = variableRegex.Match(c.Name).Groups;
                var name = groups[1].Value.ToLower();
                return new KeyValuePair<string, MethodInfo>(name, c);
            });

            variableHandlers = new Dictionary<string, MethodInfo>();
            foreach (var variable in variableFuncs)
            {
                variableHandlers.Add(variable.Key, variable.Value);
            }

            Debug("Found Handlers: " + string.Join(", ", handlerFuncs.Select(f => f.Item3)));
            Debug("Found Variables: " + string.Join(", ", variableHandlers.Keys));
        }

        public void ProcessMessage(ulong sender, string message)
        {
            who = sender;
            message = message.Trim();

            var mention = false;
            if (message.StartsWith(prefix))
            {
                mention = true;
                message = message.Substring(prefix.Length).Trim();
            }

            var parameters = new object[]
            {
                sender, message, mention
            };

            foreach (var handler in handlers)
            {
                if ((bool)handler.Invoke(this, parameters))
                    return;
            }

            Debug("No matching handler");
        }

        private void SayFact(FactRow fact)
        {
            that = fact;

            switch (fact.Verb)
            {
                case "is":
                case "<is>":
                    Say(string.Format("{0} is {1}", fact.Fact, fact.Tidbit));
                    break;

                case "are":
                case "<are>":
                    Say(string.Format("{0} are {1}", fact.Fact, fact.Tidbit));
                    break;

                case "<reply>":
                    Say(fact.Tidbit);
                    break;

                case "<action>":
                    Say(string.Format("*{0}*", fact.Tidbit));
                    break;

                case "<'s>":
                    Say(string.Format("{0}'s {1}", fact.Fact, fact.Tidbit));
                    break;
            }
        }

        private void Say(string message, bool processVariables = true)
        {
            if (!processVariables)
                message = message.Replace("$", "$$");

            var str = message;
            var i = 0;

            var sb = new StringBuilder();

            while (i < str.Length)
            {
                if (str[i] != '$')
                {
                    sb.Append(str[i++]);
                    continue;
                }

                i++; // skip $

                // double $, print one
                if (str[i] == '$')
                {
                    sb.Append(str[i++]);
                    continue;
                }

                var variable = "";
                while (i < str.Length && char.IsLetter(str[i]))
                {
                    variable += str[i++];
                }

                var suffix = "";

                #region Variable Name Exceptions
                if (variable == "nouns")
                {
                    suffix = "s";
                    variable = "noun";
                }
                if (variable == "verbs")
                {
                    suffix = "s";
                    variable = "verb";
                }
                if (variable == "verbed")
                {
                    suffix = "ed";
                    variable = "verb";
                }
                if (variable == "verbing")
                {
                    suffix = "ing";
                    variable = "verb";
                }
                #endregion

                var value = VariableLookup(variable);

                if (value == null)
                {
                    value = "$" + variable;
                    suffix = "";
                }

                sb.Append(value);
                sb.Append(suffix);
            }

            message = sb.ToString().Trim();

            Output(message);
        }

        private void Debug(string message)
        {
            if (DebugOutput != null)
                DebugOutput(message);
        }

        private string VariableLookup(string name)
        {
            MethodInfo handler;
            if (variableHandlers.TryGetValue(name.ToLower(), out handler))
                return (string)handler.Invoke(this, null);
            return DbHelper.GetValue(name);
        }
    }
}
