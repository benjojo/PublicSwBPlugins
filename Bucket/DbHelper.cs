using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace discord.plugins
{
    [Flags]
    public enum Permissions : ushort
    {
        Trusted = 1,
        ModifyPermissions = 2,
        Banned = 4,

        None = 0,
        All = ushort.MaxValue
    }

    public static class DbHelper
    {
        public static void AddFact(ulong steamId, string fact, string tidbit, string verb, bool protect, bool re = false, byte? mood = null, byte? chance = null)
        {
            var cmd = new Command("INSERT INTO bucket_facts (SteamID,fact,tidbit,verb,RE,protected,mood,chance) VALUES (@steamid,@fact,@tidbit,@verb,@RE,@protected,@mood,@chance)");
            cmd["@steamid"] = steamId;
            cmd["@fact"] = fact.ToUtf8();
            cmd["@tidbit"] = tidbit.ToUtf8();
            cmd["@verb"] = verb.ToUtf8();
            cmd["@RE"] = re;
            cmd["@protected"] = protect;
            cmd["@mood"] = mood;
            cmd["@chance"] = chance;
            cmd.ExecuteNonQuery();
        }

        public static FactRow GetFact(string fact)
        {
            var cmd = new Command("SELECT * FROM bucket_facts WHERE fact=@fact ORDER BY RAND()");
            cmd["@fact"] = fact.ToUtf8();
            return cmd.Execute().Select(r => new FactRow(r)).FirstOrDefault();
        }

        public static List<FactRow> GetFacts(string fact)
        {
            var cmd = new Command("SELECT * FROM bucket_facts WHERE fact=@fact");
            cmd["@fact"] = fact.ToUtf8();
            return cmd.Execute().Select(r => new FactRow(r)).ToList();
        }

        public static string GetValue(string variable)
        {
            try
            {
                var cmd = new Command("SELECT vars.id id, name, perms, type, value FROM bucket_vars vars LEFT JOIN bucket_values vals ON vars.id = vals.var_id WHERE name=@variable ORDER BY RAND() LIMIT 1");
                cmd["@variable"] = variable.ToUtf8();
                return cmd.Execute().Select(r => (string)r.value).FirstOrDefault();
            }
            catch
            {
                // No values for variable
                return null;
            }
        }

        public static Permissions GetPermissions(ulong steamId)
        {
            var cmd = new Command("SELECT AuthLevel FROM bucket_users WHERE SteamID=@SteamID");
            cmd["@SteamID"] = steamId;
            var perms = cmd.Execute().ToList();
            if (perms.Count == 0)
                return Permissions.None;
            return (Permissions)perms[0].AuthLevel;
        }

        public static void ProtectFact(string fact, bool protect)
        {
            var cmd = new Command("UPDATE bucket_facts SET protected=@protect WHERE fact=@fact");
            cmd["@fact"] = fact.ToUtf8();
            cmd["@protect"] = protect;
            cmd.ExecuteNonQuery();
        }
    }
}
