using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace discord.plugins
{
    public class FactRow
    {
        public readonly uint Id;
        public readonly string Fact;
        public readonly string Tidbit;
        public readonly string Verb;

        public FactRow(dynamic row)
        {
            Id = row.id;
            Fact = row.fact;
            Tidbit = row.tidbit;
            Verb = row.verb;
        }
    }
}
