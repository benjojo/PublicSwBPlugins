﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Bucket
{
    public partial class Bucket
    {
        private readonly List<MethodInfo> handlers;
        private readonly string prefix;

        private readonly Random random = new Random();

        private ulong who;

        public Action<string> Output;
        public Action<string> DebugOutput;
        public Func<ulong, string> NameFromId;

        public Bucket(string mentionPrefix)
        {
            var commands = GetType().GetMethods().Where(m => m.Name.StartsWith("Cmd")).ToList();
            var regex = new Regex(@"^Cmd(\d+)_([\w\d]*)");

            var funcs = commands.Where(c => regex.IsMatch(c.Name)).Select(c =>
            {
                var groups = regex.Match(c.Name).Groups;

                var priority = int.Parse(groups[1].Value);
                var name = groups[2].Value;

                return Tuple.Create(c, priority, name);
            }).OrderByDescending(c => c.Item2);

            handlers = funcs.Select(c => c.Item1).ToList();
            prefix = mentionPrefix;

            Debug("Found Handlers: " + string.Join(", ", funcs.Select(f => f.Item3)));
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

        private void Say(string message)
        {
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
                    suffix = "s ";
                    variable = "noun";
                }
                if (variable == "verbs")
                {
                    suffix = "s ";
                    variable = "verb";
                }
                if (variable == "verbed")
                {
                    suffix = "ed ";
                    variable = "verb";
                }
                if (variable == "verbing")
                {
                    suffix = "ing ";
                    variable = "verb";
                }
                #endregion

                var value = " ";
                var values = Database.GetValues(variable);

                if (values.Count > 0)
                {
                    var idx = random.Next(values.Count);
                    value = values[idx];
                    suffix = "";
                }

                if (variable == "who")
                {
                    value = NameFromId(who);
                }

                if (variable == "someone")
                {
                    var id = RecentMessages.Skip(random.Next(RecentMessages.Count)).First().Item1;
                    value = NameFromId(id);
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
    }
}