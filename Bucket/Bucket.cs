using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace discord.plugins
{
    public partial class Bucket
    {
        private delegate bool MessageHandler(ulong sender, string message, bool mention);
        private delegate string VariableHandler(dynamic context, string parameters);

        private readonly List<MessageHandler> handlers;
        private readonly Dictionary<string, VariableHandler> variableHandlers;
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
            handlers = handlerFuncs.Select(c => (MessageHandler)Delegate.CreateDelegate(typeof(MessageHandler), this, c.Item1)).ToList();

            // Find variable handlers
            var variableRegex = new Regex(@"^Var_(\S+)");
            var variableFuncs = commands.Where(c => variableRegex.IsMatch(c.Name)).Select(c =>
            {
                var groups = variableRegex.Match(c.Name).Groups;
                var name = groups[1].Value.ToLower();
                return new KeyValuePair<string, MethodInfo>(name, c);
            });

            variableHandlers = new Dictionary<string, VariableHandler>();
            foreach (var variable in variableFuncs)
            {
                variableHandlers.Add(variable.Key, (VariableHandler)Delegate.CreateDelegate(typeof(VariableHandler), this, variable.Value));
            }

            Debug("Found Handlers: " + string.Join(", ", handlerFuncs.Select(f => f.Item3)));
            Debug("Found Variables: " + string.Join(", ", variableHandlers.Keys));
        }

        public void ProcessMessage(ulong sender, string message)
        {
            who = sender;
            message = message.Trim();

            var mention = false;
            if (message.StartsWith(prefix + " "))
            {
                mention = true;
                message = message.Substring(prefix.Length + 1).Trim();
            }

            if (message.Length == 0)
                return;

            foreach (var handler in handlers)
            {
                if (handler(sender, message, mention))
                    return;
            }

            Debug("No matching handler");
        }

        private void SayFact(string fact, bool useUnknownResponse, bool allowAlias = true)
        {
            var factRow = DbHelper.GetFact(fact);

            if (factRow == null)
            {
                if (!useUnknownResponse)
                    return;

                factRow = DbHelper.GetFact("don't know");
                if (factRow == null)
                {
                    Debug("No \"don't know\" responses");
                    return;
                }
            }

            if (!allowAlias && factRow.Verb == "<alias>")
            {
                Debug("Ignored recursive alias");
                return;
            }

            SayFact(factRow);
        }

        private void SayFact(FactRow fact)
        {
            if (fact == null)
                return;

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

                case "<alias>":
                    SayFact(fact.Tidbit, true, false);
                    break;

                /*case "<web>":
                    Say(DownloadPage(fact.Tidbit));
                    break;*/

                default:
                    Debug("Unknown verb: " + fact.Verb);
                    break;
            }
        }

        private void Say(string message, bool processVariables = true)
        {
            if (processVariables)
                message = ProcessVariables(message);

            message = PostProcessAn(message);
            message = message.Trim();

            if (Output != null)
                Output(message);
        }

        private const int MaxRecursion = 2;
        private string ProcessVariables(string str, dynamic context = null, int recursion = 0)
        {
            if (context == null)
                context = new ExpandoObject();

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

                // no text or double $, print a $
                if (i == str.Length || str[i] == '$')
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

                var value = recursion < MaxRecursion ? VariableLookup(context, variable, parameters) : null;

                if (value == null)
                {
                    value = "$" + variable + (parameters != null ? "{" + parameters + "}" : "");
                    suffix = "";
                }
                else
                {
                    value = ProcessVariables(value, context, recursion + 1);
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

        private string VariableLookup(dynamic context, string name, string parameters)
        {
            VariableHandler handler;
            if (variableHandlers.TryGetValue(name.ToLower(), out handler))
                return handler(context, parameters);
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
                    res.Append(PreserveCase(words[i], words[i + 1], useAn ? "an" : "a") + " ");
                    continue;
                }

                res.Append(words[i] + " ");
            }

            return res.ToString();
        }

        private static string PreserveCase(string original, string other, string output)
        {
            if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(output))
                return output;

            var firstUpper = char.IsUpper(original[0]);
            var otherCaps = other.Where(char.IsLetter).All(char.IsUpper);
            var otherLower = other.Where(char.IsLetter).All(char.IsLower);

            if (firstUpper && otherLower)
                return char.ToUpper(output[0]) + output.Substring(1);

            if (otherCaps)
                return output.ToUpper();
            if (otherLower)
                return output.ToLower();

            return output;
        }
    }
}
