/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2025, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using System.Collections.Generic;
using System.IO;
using System;
using BH.oM.Adapter;
using BH.oM.Base;
using BH.oM.Adapter.Commands;
using BH.oM.SQLite;
using BH.oM.SQLite.Configs;
using Microsoft.Data.Sqlite;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public override Output<List<object>, bool> Execute(IExecuteCommand command, ActionConfig actionConfig = null)
        {
            return ExecuteCommand(command as dynamic, actionConfig);
        }

        /***************************************************/
        /**** Execute Methods                           ****/
        /***************************************************/

        public Output<List<object>, bool> ExecuteCommand(Open command, ActionConfig actionConfig = null)
        {
            Output<List<object>, bool> output = new Output<List<object>, bool>() { Item1 = null, Item2 = false };

            try
            {
                // Close existing connection if open
                if (m_Connection != null)
                {
                    BH.Engine.Base.Compute.RecordNote("Closing existing connection before opening new one.");
                    CloseConnection(false);
                }

                // Use adapter settings for connection
                SQLiteSettings settings = m_AdapterSettings as SQLiteSettings;
                if (settings == null)
                    settings = new SQLiteSettings();

                // Use command file name or fall back to adapter file path
                string filePath = !string.IsNullOrEmpty(command.FileName) ? command.FileName : m_FilePath;

                m_Connection = ConnectToDatabase(filePath, settings);
                
                if (m_Connection != null)
                {
                    m_ConnectionState = ConnectionState.Open;
                    m_ConnectedAt = DateTime.Now;
                    m_LastUsed = DateTime.Now;
                    
                    BH.Engine.Base.Compute.RecordNote($"Successfully opened SQLite database connection: {(string.IsNullOrEmpty(filePath) ? "in-memory" : filePath)}");
                    output.Item2 = true;
                }
                else
                {
                    m_ConnectionState = ConnectionState.Faulted;
                    BH.Engine.Base.Compute.RecordError("Failed to open SQLite database connection.");
                    output.Item2 = false;
                }
            }
            catch (Exception ex)
            {
                m_ConnectionState = ConnectionState.Faulted;
                BH.Engine.Base.Compute.RecordError($"Failed to open SQLite database connection: {ex.Message}");
                output.Item2 = false;
            }

            return output;
        }

        /***************************************************/

        public Output<List<object>, bool> ExecuteCommand(Close command, ActionConfig actionConfig = null)
        {
            Output<List<object>, bool> output = new Output<List<object>, bool>() { Item1 = null, Item2 = false };

            try
            {
                if (m_Connection == null)
                {
                    BH.Engine.Base.Compute.RecordNote("No connection to close.");
                    output.Item2 = true;
                    return output;
                }

                bool optimiseOnClose = false; // Default behaviour, could be enhanced with settings
                bool success = CloseConnection(optimiseOnClose);
                
                if (success)
                {
                    m_ConnectionState = ConnectionState.Closed;
                    BH.Engine.Base.Compute.RecordNote("SQLite database connection closed successfully.");
                    output.Item2 = true;
                }
                else
                {
                    m_ConnectionState = ConnectionState.Faulted;
                    BH.Engine.Base.Compute.RecordError("Failed to close SQLite database connection properly.");
                    output.Item2 = false;
                }
            }
            catch (Exception ex)
            {
                m_ConnectionState = ConnectionState.Faulted;
                BH.Engine.Base.Compute.RecordError($"Failed to close SQLite database connection: {ex.Message}");
                output.Item2 = false;
            }

            return output;
        }

        /***************************************************/

        public Output<List<object>, bool> ExecuteCommand(IExecuteCommand command, ActionConfig actionConfig = null)
        {
            Output<List<object>, bool> output = new Output<List<object>, bool>() { Item1 = null, Item2 = false };
            
            BH.Engine.Base.Compute.RecordWarning($"The command {command.GetType().Name} is not supported by this Adapter.");
            
            return output;
        }

        /***************************************************/
        /**** Private helper methods                    ****/
        /***************************************************/

        private SqliteConnection ConnectToDatabase(string filePath, SQLiteSettings settings)
        {
            try
            {
                SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder();

                // Set data source based on database mode
                switch (settings.DatabaseMode)
                {
                    case DatabaseMode.InMemoryDatabase:
                        builder.DataSource = ":memory:";
                        break;
                    case DatabaseMode.TemporaryDatabase:
                        builder.DataSource = "";
                        break;
                    case DatabaseMode.FileDatabase:
                    default:
                        if (string.IsNullOrEmpty(filePath))
                        {
                            BH.Engine.Base.Compute.RecordWarning("No file path provided for file database. Using in-memory database instead.");
                            builder.DataSource = ":memory:";
                        }
                        else
                        {
                            builder.DataSource = filePath;
                            
                            // Create directory if it doesn't exist
                            string directory = Path.GetDirectoryName(filePath);
                            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                            {
                                try
                                {
                                    Directory.CreateDirectory(directory);
                                }
                                catch (Exception ex)
                                {
                                    BH.Engine.Base.Compute.RecordError($"Failed to create directory for database: {ex.Message}");
                                    return null;
                                }
                            }
                        }
                        break;
                }

                // Set connection timeout
                builder.DefaultTimeout = settings.ConnectionTimeoutSeconds;

                // Set cache mode based on optimisation strategy
                switch (settings.OptimisationMode)
                {
                    case OptimisationMode.ReadOptimised:
                        builder.Cache = SqliteCacheMode.Shared;
                        break;
                    case OptimisationMode.WriteOptimised:
                    case OptimisationMode.MaxPerformance:
                        builder.Cache = SqliteCacheMode.Private;
                        break;
                    default:
                        builder.Cache = SqliteCacheMode.Default;
                        break;
                }

                SqliteConnection connection = new SqliteConnection(builder.ConnectionString);
                
                try
                {
                    connection.Open();
                }
                catch (Exception ex)
                {
                    BH.Engine.Base.Compute.RecordError($"Failed to open SQLite connection: {ex.Message}");
                    connection.Dispose();
                    return null;
                }

                // Configure the connection based on settings
                ConfigureConnection(connection, settings);

                return connection;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to create SQLite connection: {ex.Message}");
                return null;
            }
        }

        private void ConfigureConnection(SqliteConnection connection, SQLiteSettings settings)
        {
            try
            {
                // Enable WAL mode if requested
                if (settings.EnableWalMode)
                {
                    try
                    {
                        using (SqliteCommand command = connection.CreateCommand())
                        {
                            command.CommandText = "PRAGMA journal_mode = WAL;";
                            command.ExecuteNonQuery();
                            m_WalModeEnabled = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        BH.Engine.Base.Compute.RecordWarning($"Failed to enable WAL mode: {ex.Message}");
                    }
                }

                // Enable foreign keys if requested
                if (settings.EnableForeignKeys)
                {
                    try
                    {
                        using (SqliteCommand command = connection.CreateCommand())
                        {
                            command.CommandText = "PRAGMA foreign_keys = ON;";
                            command.ExecuteNonQuery();
                            m_ForeignKeysEnabled = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        BH.Engine.Base.Compute.RecordWarning($"Failed to enable foreign keys: {ex.Message}");
                    }
                }

                // Set cache size
                try
                {
                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = $"PRAGMA cache_size = {settings.CacheSize};";
                        command.ExecuteNonQuery();
                        m_CacheSize = settings.CacheSize;
                    }
                }
                catch (Exception ex)
                {
                    BH.Engine.Base.Compute.RecordWarning($"Failed to set cache size: {ex.Message}");
                }

                // Set optimisation-specific pragmas
                try
                {
                    ApplyOptimisationSettings(connection, settings.OptimisationMode);
                }
                catch (Exception ex)
                {
                    BH.Engine.Base.Compute.RecordWarning($"Failed to apply optimisation settings: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to apply some connection settings: {ex.Message}");
            }
        }

        private void ApplyOptimisationSettings(SqliteConnection connection, OptimisationMode mode)
        {
            try
            {
                using (SqliteCommand command = connection.CreateCommand())
                {
                    switch (mode)
                    {
                        case OptimisationMode.ReadOptimised:
                            command.CommandText = @"
                                PRAGMA query_only = ON;
                                PRAGMA temp_store = MEMORY;
                                PRAGMA mmap_size = 268435456;";
                            break;

                        case OptimisationMode.WriteOptimised:
                            command.CommandText = @"
                                PRAGMA synchronous = NORMAL;
                                PRAGMA temp_store = MEMORY;";
                            break;

                        case OptimisationMode.MaxPerformance:
                            command.CommandText = @"
                                PRAGMA synchronous = OFF;
                                PRAGMA temp_store = MEMORY;
                                PRAGMA mmap_size = 268435456;";
                            break;

                        case OptimisationMode.MemoryOptimised:
                            command.CommandText = @"
                                PRAGMA temp_store = FILE;
                                PRAGMA mmap_size = 0;";
                            break;

                        case OptimisationMode.Balanced:
                        case OptimisationMode.Default:
                        default:
                            command.CommandText = @"
                                PRAGMA synchronous = FULL;
                                PRAGMA temp_store = DEFAULT;";
                            break;
                    }

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to apply optimisation settings for mode {mode}: {ex.Message}");
            }
        }

        private bool CloseConnection(bool optimiseOnClose)
        {
            if (m_Connection == null)
                return true;

            try
            {
                if (m_Connection.State == System.Data.ConnectionState.Open)
                {
                    // Run optimisation commands if requested
                    if (optimiseOnClose)
                    {
                        OptimiseDatabase();
                    }

                    // Close the connection
                    m_Connection.Close();
                }

                // Dispose of the connection object
                m_Connection.Dispose();
                m_Connection = null;
                return true;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to close SQLite connection: {ex.Message}");
                return false;
            }
        }

        private void OptimiseDatabase()
        {
            try
            {
                BH.Engine.Base.Compute.RecordNote("Running database optimisation before closing...");

                using (SqliteCommand command = m_Connection.CreateCommand())
                {
                    // Update statistics for query optimiser
                    command.CommandText = "ANALYZE;";
                    command.ExecuteNonQuery();

                    // Only vacuum if not in WAL mode (can cause issues)
                    command.CommandText = "PRAGMA journal_mode;";
                    object journalModeResult = command.ExecuteScalar();
                    string journalMode = journalModeResult?.ToString();

                    if (!string.Equals(journalMode, "wal", StringComparison.OrdinalIgnoreCase))
                    {
                        command.CommandText = "VACUUM;";
                        command.ExecuteNonQuery();
                        BH.Engine.Base.Compute.RecordNote("Database optimisation completed (ANALYZE + VACUUM).");
                    }
                    else
                    {
                        BH.Engine.Base.Compute.RecordNote("Database optimisation completed (ANALYZE only - WAL mode detected).");
                    }
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Database optimisation failed but connection will still close: {ex.Message}");
            }
        }

        /***************************************************/
    }
} 