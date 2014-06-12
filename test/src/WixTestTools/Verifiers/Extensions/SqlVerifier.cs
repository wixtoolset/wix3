//-------------------------------------------------------------------------------------------------
// <copyright file="SqlVerifier.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//      Contains methods for verification for Sql Extension
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixTest.Verifiers.Extensions
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Sql;
    using System.Data.SqlClient;
    
    /// <summary>
    /// Contains methods for Sql Extension test verification
    /// </summary>
    public static class SqlVerifier
    {
        /// <summary>
        /// Executes a sqlscript and returns a SQLDATAREADER 
        /// </summary>
        /// <param name="hostName">Server name</param>
        /// <param name="instanceName">Database Instance name</param>
        /// <param name="databaseName">Database name</param>
        /// <param name="sqlString">SQL statment</param>
        public static void ExecuteSQlCommand(string hostName, string instalnceName, string databaseName, string sqlString)
        {
            using (SqlObjectWrapper sqlObjectWrapper = new SqlObjectWrapper(hostName, instalnceName, databaseName))
            {
                SqlDataReader dr = sqlObjectWrapper.ExecuteSqlCommands(sqlString);
          
                // close the reader
                dr.Close();
                dr.Dispose();
            }
        }
        
        /// <summary>
        /// Checks if a result of a SQL statment exists
        /// </summary>
        /// <param name="hostName">Server name</param>
        /// <param name="instanceName">Database Instance name</param>
        /// <param name="databaseName">Database name</param>
        /// <param name="sqlString">SQL statment</param>
        /// <returns>True if the result exists, false otherwise</returns>
        public static bool SqlObjectExists(string hostName,string instalnceName, string databaseName, string sqlString)
        {
            bool exists = false;
            using (SqlObjectWrapper sqlObjectWrapper = new SqlObjectWrapper(hostName, instalnceName, databaseName))
            {
                using (SqlDataReader dr = sqlObjectWrapper.ExecuteSqlCommands(sqlString))
                {
                    exists = dr.HasRows;
                }
            }
            return exists;
        }
        
        /// <summary>
        /// Checks if a table exists
        /// </summary>
        /// <param name="hostName">Server name</param>
        /// <param name="instanceName">Database Instance name</param>
        /// <param name="databaseName">Database name</param>
        /// <param name="tableName">Table name</param>
        /// <param name="tableName"></param>
        /// <returns>True if the table exists, false otherwise</returns>
        public static bool TableExists(string hostName, string instanceName, string databaseName, string tableName)
        {
            string sqlString = "select * from sys.tables where name ='" + tableName + "'";
            return SqlObjectExists(hostName, instanceName, databaseName, sqlString);
        }

        /// <summary>
        /// Checks if a DB exists
        /// </summary>
        /// <param name="hostName">Server name</param>
        /// <param name="instanceName">Database Instance name</param>
        /// <param name="databaseName">Database name</param>
        /// <returns>True if the database exists, false otherwise</returns>
        public static bool DatabaseExists(string hostName, string instanceName, string databaseName)
        {
            string sqlString = "select * from sysdatabases where name ='" + databaseName + "'";
            return SqlObjectExists(hostName, instanceName, "master", sqlString);
        }
    }

    /// <summary>
    /// Wrapper to a SQL connection
    /// </summary>
    public class SqlObjectWrapper : IDisposable
    {
        private SqlConnection connection;

        /// <summary>
        /// Create a new connection and open it
        /// </summary>
        /// <param name="hostName">Server name</param>
        /// <param name="instanceName">Database Instance name</param>
        /// <param name="databaseName">Database name</param>
        public SqlObjectWrapper(string hostName, string instanceName, string databaseName)
        {
            if (string.IsNullOrEmpty(hostName) || string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentException("sqlConnectionString is not defined");
            }

            string serverName;
            if (!string.IsNullOrEmpty(instanceName))
            {
                serverName = string.Format(@"{0}\{1}", hostName, instanceName);
            }
            else
            {
                serverName = string.Format(@"{0}", hostName);
            }

            this.connection = new SqlConnection();
            this.connection.ConnectionString = string.Format(@"Persist Security Info=False;Integrated Security = SSPI;database={0};server={1};Connect Timeout=20;Min Pool Size=0;Pooling=false ", databaseName, serverName);
            this.connection.Open();
        }

        /// <summary>
        /// Close the connection
        /// </summary>
        public void Dispose()
        {
            this.CloseConnection();
        }

        /// <summary>
        /// This method will execute a sql command and return a SqlDataReader
        /// </summary>
        /// <param name="sqlString">SQL String to execute</param>
        /// <returns>SqlDataReader</returns>
        public  SqlDataReader ExecuteSqlCommands(string sqlString)
        {
            SqlCommand sqlcmd = new SqlCommand(sqlString, this.connection);

            return sqlcmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        /// <summary>
        /// Close and dispose the sql connection
        /// </summary>
        public void CloseConnection()
        {
            connection.Close();
            connection.Dispose();
        }
    }
}
