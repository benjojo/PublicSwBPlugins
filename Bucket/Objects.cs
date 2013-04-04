using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace discord.plugins
{
    class FactRow
    {
        public readonly string Fact;
        public readonly string Tidbit;
        public readonly string Verb;

        public FactRow(dynamic row)
        {
            Fact = row.fact;
            Tidbit = row.tidbit;
            Verb = row.verb;
        }
    }
}
