using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace QCUForum.Helpers
{
    public static class DatabaseHelper
    {
        private static string ConnectionString => ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }
    }
}
