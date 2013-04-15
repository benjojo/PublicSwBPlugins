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

        private const string BadPermissions = "You want me to do what now?";
        private const string Success = "Consider it done.";

        // List of facts which users should not be able to trigger.
        private static readonly string[] ReservedFacts = new[]
        {
            "band name reply", "tumblr name reply", "don't know", "drops item", "duplicate item",
            "list items", "pickup full", "takes item"
        };

        public bool Cmd0_FactCheck(ulong sender, string message, bool mention)
        {
            if (message.Length == 0)
                return true;

            var words = message.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            if (!mention && words.Length < 2)
                return true;

            if (ReservedFacts.Contains(message.ToLower()))
                return true;

            var facts = DbHelper.GetFacts(message);
            if (facts.Count == 0)
            {
                if (mention)
                {
                    facts = DbHelper.GetFacts("don't know");
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

        /*private static readonly Regex IsAreRule = new Regex(@"(.*?) (is|are) (.*)");
        public bool Cmd45_TeachIsAre(ulong sender, string message, bool mention)
        {
            if (!mention || !IsAreRule.IsMatch(message))
                return false;

            var groups = IsAreRule.Match(message).Groups;
            var fact = groups[1].Value;
            var verb = groups[2].Value;
            var tidbit = groups[3].Value;
            
            if (fact.Length == 0 || tidbit.Length == 0)
            {
                Say("Why would you want me to remember nothing?");
                return true;
            }

            try
            {
                DbHelper.AddFact(fact, tidbit, verb, false);
            }
            catch
            {
                Say("I already had it that way!");
                return true;
            }

            Say("Okay, $who!");
            return true;
        }*/

        private static readonly List<string> AllowedVerbs = new List<string>()
        {
            "is",
            "<is>",
            "are",
            "<are>",
            "<reply>",
            "<action>",
            "<'s>",
            "<web>"
        };
        private static readonly Regex VerbRule = new Regex(@"(.*?)\s*(<\S+>)\s*(.*)");
        public bool Cmd50_TeachVerb(ulong sender, string message, bool mention)
        {
            if (!mention || !VerbRule.IsMatch(message))
                return false;

            var groups = VerbRule.Match(message).Groups;
            var fact = groups[1].Value;
            var verb = groups[2].Value;
            var tidbit = groups[3].Value;

            if (fact.Length == 0 || tidbit.Length == 0)
            {
                Say("Why would you want me to remember nothing?");
                return true;
            }

            if (!AllowedVerbs.Contains(verb))
            {
                Say("I wouldn't know what do do with that.");
                return true;
            }

            var permissions = DbHelper.GetPermissions(sender);
            if (permissions.HasFlag(Permissions.Banned) || (verb == "<web>" && !permissions.HasFlag(Permissions.Trusted)))
            {
                Say(BadPermissions);
                return true;
            }

            var existingFacts = DbHelper.GetFacts(fact);
            var protect = false;
            if (existingFacts.Any(f => f.Protected))
            {
                protect = true;
                if (!DbHelper.GetPermissions(sender).HasFlag(Permissions.Trusted))
                {
                    Say("You aren't qualified to change that.");
                    return true;
                }
            }

            try
            {
                DbHelper.AddFact(who, fact, tidbit, verb, protect);
            }
            catch
            {
                Say("I already had it that way!");
                return true;
            }

            Say("Okay, $who!");
            return true;
        }

        public bool Cmd55_SomethingRandom(ulong sender, string message, bool mention)
        {
            if (!mention || !message.ToLower().StartsWith("something random"))
                return false;

            var cmd = new Command("SELECT * FROM bucket_facts ORDER BY RAND() LIMIT 1");
            var res = cmd.Execute().Select(r => new FactRow(r)).ToList();

            if (res.Count > 0)
                SayFact(res[0]);
            else
                Say("Nothing random!");

            return true;
        }

        public bool Cmd55_WhatWasThat(ulong sender, string message, bool mention)
        {
            if (!mention || !message.ToLower().StartsWith("what was that"))
                return false;

            if (that == null)
            {
                Say("What was what?");
                return true;
            }

            Say(string.Format("That was: {0}(#{1}, by {4}) {2} {3}", that.Fact, that.Id, that.Verb, that.Tidbit, that.SteamId), false);
            return true;
        }

        private static readonly Regex ForgetXRule = new Regex(@"^forget (.*)");
        public bool Cmd55_Forget(ulong sender, string message, bool mention)
        {
            if (!mention || !ForgetXRule.IsMatch(message))
                return false;

            if (!DbHelper.GetPermissions(sender).HasFlag(Permissions.Trusted))
            {
                Say(BadPermissions);
                return true;
            }

            var x = ForgetXRule.Match(message).Groups[1].Value;

            if (x == "that")
            {
                if (that == null)
                {
                    Say("Forget what?");
                    return true;
                }

                var cmd = new Command("DELETE FROM bucket_facts WHERE id=@id");
                cmd["@id"] = that.Id;
                cmd.ExecuteNonQuery();

                Say(Success);
                return true;
            }
            else
            {
                var cmd = new Command("DELETE FROM bucket_facts WHERE fact=@fact");
                cmd["@fact"] = x.ToUtf8();
                cmd.ExecuteNonQuery();

                Say(Success);
                return true;
            }
        }

        private static readonly Regex DeleteNRule = new Regex(@"^delete #(.*)");
        public bool Cmd55_DeleteN(ulong sender, string message, bool mention)
        {
            if (!mention || !DeleteNRule.IsMatch(message))
                return false;

            if (!DbHelper.GetPermissions(sender).HasFlag(Permissions.Trusted))
            {
                Say(BadPermissions);
                return true;
            }

            uint n;
            if (!uint.TryParse(DeleteNRule.Match(message).Groups[1].Value, out n))
            {
                Say("I may be able to if you gave me a valid number.");
                return true;
            }

            var cmd = new Command("SELECT * FROM bucket_facts WHERE id=@id");
            cmd["@id"] = n;
            var rows = cmd.Execute().Count();
            if (rows == 0)
            {
                Say("That is not a fact.");
                return true;
            }

            cmd = new Command("DELETE FROM bucket_facts WHERE id=@id");
            cmd["@id"] = n;
            cmd.ExecuteNonQuery();

            Say(Success);
            return true;
        }

        private static readonly Regex DeleteXRule = new Regex(@"^delete (.*)");
        public bool Cmd55_DeleteX(ulong sender, string message, bool mention)
        {
            if (!mention || !DeleteXRule.IsMatch(message))
                return false;

            if (!DbHelper.GetPermissions(sender).HasFlag(Permissions.Trusted))
            {
                Say(BadPermissions);
                return true;
            }

            if (DeleteXRule.IsMatch(message))
            {
                var x = DeleteXRule.Match(message).Groups[1].Value;

                if (DbHelper.GetFacts(x).Count == 0)
                {
                    Say("That is not a fact.");
                    return true;
                }

                var cmd = new Command("DELETE FROM bucket_facts WHERE fact=@fact");
                cmd["@fact"] = x.ToUtf8();
                cmd.ExecuteNonQuery();

                Say(Success);
                return true;
            }

            return false;
        }

        public static readonly Regex CreateVarRule = new Regex(@"^create var (.*)");
        public bool Cmd60_CreateVar(ulong sender, string message, bool mention)
        {
            if (!mention || !CreateVarRule.IsMatch(message))
                return false;

            if (!DbHelper.GetPermissions(sender).HasFlag(Permissions.Trusted))
            {
                Say(BadPermissions);
                return true;
            }

            var name = CreateVarRule.Match(message).Groups[1].Value.ToLower();

            if (name.Length <= 1 || !name.All(char.IsLetter))
            {
                Say("I probably would if you gave me a valid name.");
                return true;
            }

            var cmd = new Command("SELECT * FROM bucket_vars WHERE name=@name");
            cmd["@name"] = name.ToUtf8();
            var count = cmd.Execute().Count();

            if (count > 0)
            {
                Say("That variable already exists!");
                return true;
            }

            cmd = new Command("INSERT INTO bucket_vars (name, perms, type) VALUES (@name, 'read-only', 'var')");
            cmd["@name"] = name.ToUtf8();
            cmd.ExecuteNonQuery();

            Say(Success);
            return true;
        }

        public static readonly Regex DeleteVarRule = new Regex(@"^delete var (.*)");
        public bool Cmd60_DeleteVar(ulong sender, string message, bool mention)
        {
            if (!mention || !DeleteVarRule.IsMatch(message))
                return false;

            if (!DbHelper.GetPermissions(sender).HasFlag(Permissions.Trusted))
            {
                Say(BadPermissions);
                return true;
            }

            var name = DeleteVarRule.Match(message).Groups[1].Value.ToLower();

            if (name.Length <= 1 || !name.All(char.IsLetter))
            {
                Say("I probably would if you gave me a valid name.");
                return true;
            }

            var cmd = new Command("SELECT * FROM bucket_vars WHERE name=@name");
            cmd["@name"] = name.ToUtf8();
            var variable = cmd.Execute().FirstOrDefault();

            if (variable == null)
            {
                Say("Did you already delete that variable?");
                return true;
            }

            if ((string)variable.perms == "read-only")
            {
                Say("That variable cannot be deleted.");
                return true;
            }

            cmd = new Command("DELETE FROM bucket_values WHERE var_id=@id; DELETE FROM bucket_vars WHERE id=@id");
            cmd["@id"] = (uint)variable.id;
            cmd.ExecuteNonQuery();

            Say(Success);
            return true;
        }

        public static readonly Regex AddValueRule = new Regex(@"^add value (\S+) (.*)");
        public bool Cmd60_AddValue(ulong sender, string message, bool mention)
        {
            if (!mention || !AddValueRule.IsMatch(message))
                return false;

            if (!DbHelper.GetPermissions(sender).HasFlag(Permissions.Trusted))
            {
                Say(BadPermissions);
                return true;
            }

            var groups = AddValueRule.Match(message).Groups;
            var name = groups[1].Value.ToLower();
            var value = groups[2].Value;

            if (name.Length <= 1 || !name.All(char.IsLetter))
            {
                Say("I probably would if you gave me a valid name.");
                return true;
            }

            var cmd = new Command("SELECT * FROM bucket_vars WHERE name=@name");
            cmd["@name"] = name.ToUtf8();
            var variable = cmd.Execute().FirstOrDefault();

            if (variable == null)
            {
                Say("That variable doesn't even exist!");
                return true;
            }

            cmd = new Command("SELECT * FROM bucket_values WHERE var_id=@id AND value=@value");
            cmd["@id"] = (uint)variable.id;
            cmd["@value"] = value.ToUtf8();
            var valueRow = cmd.Execute().FirstOrDefault();

            if (valueRow != null)
            {
                Say("That variable has that value already, silly!");
                return true;
            }

            cmd = new Command("INSERT INTO bucket_values (var_id, value) VALUES (@id, @value)");
            cmd["@id"] = (uint)variable.id;
            cmd["@value"] = value.ToUtf8();
            cmd.ExecuteNonQuery();

            Say(Success);
            return true;
        }

        public static readonly Regex RemoveValueRule = new Regex(@"^remove value (\S+) (.*)");
        public bool Cmd60_RemoveValue(ulong sender, string message, bool mention)
        {
            if (!mention || !RemoveValueRule.IsMatch(message))
                return false;

            if (!DbHelper.GetPermissions(sender).HasFlag(Permissions.Trusted))
            {
                Say(BadPermissions);
                return true;
            }

            var groups = RemoveValueRule.Match(message).Groups;
            var name = groups[1].Value.ToLower();
            var value = groups[2].Value;

            if (name.Length <= 1 || !name.All(char.IsLetter))
            {
                Say("I probably would if you gave me a valid name.");
                return true;
            }

            var cmd = new Command("SELECT * FROM bucket_vars WHERE name=@name");
            cmd["@name"] = name.ToUtf8();
            var variable = cmd.Execute().FirstOrDefault();

            if (variable == null)
            {
                Say("That variable doesn't even exist!");
                return true;
            }

            cmd = new Command("SELECT * FROM bucket_values WHERE var_id=@id AND value=@value");
            cmd["@id"] = (uint)variable.id;
            cmd["@value"] = value.ToUtf8();
            var valueRow = cmd.Execute().FirstOrDefault();

            if (valueRow == null)
            {
                Say("That variable doesn't even have that value!");
                return true;
            }

            cmd = new Command("DELETE FROM bucket_values WHERE var_id=@id AND value=@value");
            cmd["@id"] = (uint)variable.id;
            cmd["@value"] = value.ToUtf8();
            cmd.ExecuteNonQuery();

            Say(Success);
            return true;
        }

        public static readonly Regex PermissionsRule = new Regex(@"^permissions (\d*) (\d*)");
        public bool Cmd60_Permissions(ulong sender, string message, bool mention)
        {
            if (!mention || !PermissionsRule.IsMatch(message))
                return false;

            if (!DbHelper.GetPermissions(sender).HasFlag(Permissions.ModifyPermissions))
            {
                Say(BadPermissions);
                return true;
            }

            var groups = PermissionsRule.Match(message).Groups;

            ulong steamId;
            ushort value;
            if (!ulong.TryParse(groups[1].Value, out steamId) || !ushort.TryParse(groups[2].Value, out value))
            {
                Say("I probably would if you gave me valid parameters.");
                return true;
            }

            var cmd = new Command("INSERT INTO bucket_users (SteamID,AuthLevel) VALUES(@SteamID,@AuthLevel) ON DUPLICATE KEY UPDATE AuthLevel=@AuthLevel");
            cmd["@SteamID"] = steamId;
            cmd["@AuthLevel"] = value;
            cmd.ExecuteNonQuery();

            Say(Success);
            return true;
        }

        public static readonly Regex ProtectRule = new Regex(@"^protect (.*)");
        public bool Cmd60_Protect(ulong sender, string message, bool mention)
        {
            if (!mention || !ProtectRule.IsMatch(message))
                return false;

            if (!DbHelper.GetPermissions(sender).HasFlag(Permissions.Trusted))
            {
                Say(BadPermissions);
                return true;
            }

            var groups = ProtectRule.Match(message).Groups;
            var fact = groups[1].Value;

            DbHelper.ProtectFact(fact, true);

            Say(Success);
            return true;
        }

        public static readonly Regex UnprotectRule = new Regex(@"^unprotect (.*)");
        public bool Cmd60_Unprotect(ulong sender, string message, bool mention)
        {
            if (!mention || !UnprotectRule.IsMatch(message))
                return false;

            if (!DbHelper.GetPermissions(sender).HasFlag(Permissions.Trusted))
            {
                Say(BadPermissions);
                return true;
            }

            var groups = UnprotectRule.Match(message).Groups;
            var fact = groups[1].Value;

            DbHelper.ProtectFact(fact, false);

            Say(Success);
            return true;
        }

        private readonly LinkedList<ulong> recentPosters = new LinkedList<ulong>();
        public bool Cmd1000_RecentPosters(ulong sender, string message, bool mention)
        {
            if (recentPosters.Contains(sender))
                return false;
            if (recentPosters.Count > 10)
                recentPosters.RemoveFirst();
            recentPosters.AddLast(sender);
            return false;
        }
    }
}
