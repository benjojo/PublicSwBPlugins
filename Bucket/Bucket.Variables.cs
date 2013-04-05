using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace discord.plugins
{
    public partial class Bucket
    {
        public string Var_Who()
        {
            return NameFromId(who);
        }

        public string Var_Someone()
        {
            var id = RecentMessages.Skip(random.Next(RecentMessages.Count)).First().Item1;
            return NameFromId(id);
        }

        public string Var_Digit()
        {
            return random.Next(10).ToString();
        }

        public string Var_Nonzero()
        {
            return random.Next(1, 10).ToString();
        }

        // Low latency version of asciidicks.com
        public string Var_Dick()
        {
            return string.Format("8{0}D", new string('=', random.Next(1, 8)));
        }
    }
}
