using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace discord.plugins
{
    public partial class Bucket
    {
        /*
         * Handlers must all have names in a specific format.
         * 
         * Cmd<Priority>_<Name>
         * 
         * Higher priortity handlers will be handled first.
         * 
         * Handlers should return true if work was done or false to
         * pass to another handler.
         */

        public bool Cmd0_FactCheck(ulong sender, string message, bool mention)
        {
            if (!mention && message.Length < 5)
                return false;

            var facts = Database.GetFacts(message);
            if (facts.Count == 0)
            {
                if (mention)
                {
                    facts = Database.GetFacts("don't know");
                    if (facts.Count > 0)
                    {
                        SayFact(facts[random.Next(facts.Count)]);
                    }
                }
                return true;
            }

            var idx = random.Next(facts.Count);
            SayFact(facts[idx]);
            return true;
        }

        private static readonly Regex IsAreRule = new Regex(@"(.*?) (is|are) (.*)");
        public bool Cmd45_TeachIsAre(ulong sender, string message, bool mention)
        {
            if (!mention || !IsAreRule.IsMatch(message))
                return false;
            Debug("IsAre Match");

            var groups = IsAreRule.Match(message).Groups;
            var fact = groups[1].Value;
            var verb = groups[2].Value;
            var tidbit = groups[3].Value;

            try
            {
                Database.AddFact(fact, tidbit, verb, false);
            }
            catch (Exception e)
            {
                Say("I already had it that way!");
                return true;
            }

            Say("Okay, $who!");
            return true;
        }

        private static readonly Regex VerbRule = new Regex(@"(.*?)\s*(<\S+>) (.*)"); // (.*?) (<[^>]+>) (.*)
        public bool Cmd50_TeachVerb(ulong sender, string message, bool mention)
        {
            if (!mention || !VerbRule.IsMatch(message))
                return false;
            Debug("Verb Match");

            var groups = VerbRule.Match(message).Groups;
            var fact = groups[1].Value;
            var verb = groups[2].Value;
            var tidbit = groups[3].Value;

            try
            {
                Database.AddFact(fact, tidbit, verb, false);
            }
            catch (Exception e)
            {
                Say("I already had it that way!");
                return true;
            }

            Say("Okay, $who!");
            return true;
        }

        private static readonly LinkedList<Tuple<ulong, string>> RecentMessages = new LinkedList<Tuple<ulong, string>>();
        public bool Cmd1000_RecentMessages(ulong sender, string message, bool mention)
        {
            if (mention)
                return false;
            if (RecentMessages.Count > 50)
                RecentMessages.RemoveFirst();
            RecentMessages.AddLast(Tuple.Create(sender, message));
            return false;
        }
    }
}
