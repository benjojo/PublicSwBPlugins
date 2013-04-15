using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

            if (message.Length == 0)
                return;

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

                /*case "<web>":
                    Say(DownloadPage(fact.Tidbit));
                    break;*/
            }
        }

        private void Say(string message, bool processVariables = true)
        {
            if (processVariables)
                message = ProcessVariables(message);

            message = PostProcessAn(message);

            Output(message);
        }

        private const int MaxRecursion = 2;
        private string ProcessVariables(string str, int recursion = 0)
        {
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
                string parameters = null;
                while (i < str.Length && char.IsLetter(str[i]))
                {
                    variable += str[i++];
                }
                if (i < str.Length && str[i] == '{')
                {
                    i++; // skip {
                    parameters = "";
                    while (i < str.Length && str[i] != '}')
                    {
                        parameters += str[i++];
                    }
                    i++; // skip }
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

                var value = recursion < MaxRecursion ? VariableLookup(variable, parameters) : null;

                if (value == null)
                {
                    value = "$" + variable + (parameters != null ? "{" + parameters + "}" : "");
                    suffix = "";
                }
                else
                {
                    value = ProcessVariables(value, recursion + 1);
                }

                sb.Append(value);
                sb.Append(suffix);
            }

            return sb.ToString().Trim();
        }

        private void Debug(string message)
        {
            if (DebugOutput != null)
                DebugOutput(message);
        }

        private string VariableLookup(string name, string parameters)
        {
            MethodInfo handler;
            if (variableHandlers.TryGetValue(name.ToLower(), out handler))
                return (string)handler.Invoke(this, new[] { parameters });
            return DbHelper.GetValue(name);
        }

        private static string DownloadPage(string uri)
        {
            try
            {
                return new WebClient().DownloadString(new Uri(uri));
            }
            catch
            {
                return ":(";
            }
        }

        private string PostProcessAn(string message)
        {
            var words = message.Split(' ');
            var res = new StringBuilder();

            for (var i = 0; i < words.Length; i++)
            {
                var w = words[i].ToLower();
                if ((w == "a" || w == "an") && i < words.Length - 1 && words[i + 1].Length > 0)
                {
                    var useAn = "aeiouAEIOU".Contains(words[i + 1][0]);
                    res.Append(PreserveCase(words[i], useAn ? "an" : "a") + " ");
                    Debug(string.Format("Replacing '{0}' with '{1}'", words[i], PreserveCase(words[i], useAn ? "an" : "a")));
                    continue;
                }

                res.Append(words[i] + " ");
            }

            Debug(res.ToString());
            return res.ToString();
        }

        private static string PreserveCase(string original, string output)
        {
            if (string.IsNullOrEmpty(original))
                return output;

            var firstUpper = char.IsUpper(original[0]);
            if (firstUpper && !string.IsNullOrEmpty(output))
                return char.ToUpper(output[0]) + output.Substring(1);

            return output;
        }
    }
}
