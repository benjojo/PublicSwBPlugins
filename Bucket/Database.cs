﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Dynamic;
using MySql.Data.MySqlClient;

namespace discord.plugins
{
    static class Database
    {
        private static readonly List<MySqlConnection> Connections;

        static Database()
        {
            Connections = new List<MySqlConnection>();
        }

        public static MySqlConnection CreateConnection()
        {
            var conn = Connections.FirstOrDefault(c => c.State == ConnectionState.Open);

            if (conn == null)
            {
                conn = new MySqlConnection(discord.core.Discord.GetDBConnectString());
                conn.Open();
                var c = new MySqlCommand("SET NAMES utf8;SET CHARACTER SET utf8", conn);
                c.ExecuteNonQuery();
                return conn;
            }

            Connections.Remove(conn);
            return conn;
        }

        public static void RecycleConnection(MySqlConnection conn)
        {
            Connections.Add(conn);
        }
    }

    class Command
    {
        private MySqlConnection connection;
        private readonly MySqlCommand command;

        public Command(string sql)
        {
            connection = Database.CreateConnection();
            command = new MySqlCommand(sql, connection);
            command.Prepare();
        }

        public object this[string name]
        {
            set
            {
                var idx = command.Parameters.IndexOf(name);
                if (idx != -1)
                    command.Parameters[idx].Value = value;
                command.Parameters.AddWithValue(name, value);
            }
        }

        public IEnumerable<dynamic> Execute()
        {
            MySqlDataReader reader = null;

            try
            {
                reader = command.ExecuteReader();

                var names = new string[reader.FieldCount];
                var values = new object[reader.FieldCount];

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    names[i] = reader.GetName(i);
                }

                while (reader.Read())
                {
                    var no = reader.GetValues(values);
                    yield return new Result(names, values);
                }
            }
            finally
            {
                if (reader != null)
                    reader.Dispose();

                Database.RecycleConnection(connection);
                connection = null;
            }
        }

        public void ExecuteNonQuery()
        {
            try
            {
                command.ExecuteNonQuery();
            }
            finally
            {
                Database.RecycleConnection(connection);
                connection = null;
            }
        }

        public object ExecuteScalar()
        {
            try
            {
                return command.ExecuteScalar();
            }
            finally
            {
                Database.RecycleConnection(connection);
                connection = null;
            }
        }
    }

    class Result : DynamicObject
    {
        private readonly Dictionary<string, object> columns;

        public Result(string[] names, object[] values)
        {
            columns = new Dictionary<string, object>();

            for (var i = 0; i < names.Length; i++)
            {
                columns.Add(names[i], values[i]);
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return columns.TryGetValue(binder.Name, out result);
        }
    }
}
