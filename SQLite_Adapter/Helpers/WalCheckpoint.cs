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

using BH.oM.Base;
using BH.oM.Base.Attributes;
using BH.oM.SQLite;
using BH.oM.SQLite.Commands;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
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
        [Input("checkpointMode", "The checkpoint mode to use. Truncate is recommended for normal operations.")]
        [Output("success", "True if the checkpoint was successful, false otherwise.")]
        private bool WalCheckpoint(SqliteConnection connection, WalCheckpointMode checkpointMode = WalCheckpointMode.Truncate)
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
                string journalMode = CheckJournalMode(connection);
                if (journalMode == null)
                {
                    BH.Engine.Base.Compute.RecordError("Failed to check journal mode.");
                    return false;
                }

                if (!string.Equals(journalMode, "wal", StringComparison.OrdinalIgnoreCase))
                {
                    BH.Engine.Base.Compute.RecordNote("WAL checkpoint skipped: database is not in WAL mode.");
                    return true; // Not an error, just not needed
                }

                // Use Engine method to generate the command
                SQLCommand command = BH.Engine.SQLite.Compute.WalCheckpointCommand(checkpointMode);
                if (command == null)
                    return false;

                // Execute the command using the existing ExecuteCommand method
                Output<List<object>, bool> result = ExecuteCommand(command);
                if (result.Item2)
                {
                    BH.Engine.Base.Compute.RecordNote($"WAL checkpoint completed successfully using mode: {checkpointMode}");
                }
                return result.Item2;
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
