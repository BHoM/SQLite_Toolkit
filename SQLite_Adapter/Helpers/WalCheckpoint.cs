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

using BH.oM.Base.Attributes;
using Microsoft.Data.Sqlite;
using System;
using System.ComponentModel;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Performs a WAL checkpoint operation to commit data from the write-ahead log to the main database file.")]
        [Input("connection", "The SQLite connection to perform the checkpoint on.")]
        [Input("checkpointMode", "The checkpoint mode to use. TRUNCATE is recommended for normal operations.")]
        [Output("success", "True if the checkpoint was successful, false otherwise.")]
        private bool WalCheckpoint(SqliteConnection connection, string checkpointMode = "TRUNCATE")
        {
            if (connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot perform WAL checkpoint: connection is null.");
                return false;
            }

            if (connection.State != System.Data.ConnectionState.Open)
            {
                BH.Engine.Base.Compute.RecordError("Cannot perform WAL checkpoint: connection is not open.");
                return false;
            }

            try
            {
                // First check if WAL mode is enabled
                using (SqliteCommand checkCommand = connection.CreateCommand())
                {
                    checkCommand.CommandText = "PRAGMA journal_mode;";
                    object journalModeResult = checkCommand.ExecuteScalar();
                    string journalMode = journalModeResult?.ToString();

                    if (!string.Equals(journalMode, "wal", StringComparison.OrdinalIgnoreCase))
                    {
                        BH.Engine.Base.Compute.RecordNote("WAL checkpoint skipped: database is not in WAL mode.");
                        return true; // Not an error, just not needed
                    }
                }

                // Perform the WAL checkpoint
                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"PRAGMA wal_checkpoint({checkpointMode});";
                    command.ExecuteNonQuery();
                }

                BH.Engine.Base.Compute.RecordNote($"WAL checkpoint completed successfully using mode: {checkpointMode}");
                return true;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"WAL checkpoint failed: {ex.Message}");
                return false;
            }
        }

        /***************************************************/
    }
}
