using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace discord.plugins
{
    public class FactRow
    {
        public readonly uint Id;
        public readonly ulong SteamId;
        public readonly string Fact;
        public readonly string Tidbit;
        public readonly string Verb;
        public readonly bool Protected;

        public FactRow(dynamic row)
        {
            Id = row.id;
            SteamId = (ulong)row.SteamID;
            Fact = row.fact;
            Tidbit = row.tidbit;
            Verb = row.verb;
            Protected = row.@protected;
        }
    }
}
