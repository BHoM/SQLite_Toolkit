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
        /**** Private Methods                           ****/
        /***************************************************/

        [Description("Checks the current journal mode of the database.")]
        [Input("connection", "The SQLite connection to check.")]
        [Output("journalMode", "The current journal mode string, or null if the check fails.")]
        private string CheckJournalMode(SqliteConnection connection)
        {
            if (connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot check journal mode: connection is null.");
                return null;
            }

            if (connection.State != System.Data.ConnectionState.Open)
            {
                BH.Engine.Base.Compute.RecordError("Cannot check journal mode: connection is not open.");
                return null;
            }

            try
            {
                // Execute journal mode check directly to avoid recursion through ExecuteCommand
                using (SqliteCommand sqlCommand = connection.CreateCommand())
                {
                    sqlCommand.CommandText = "PRAGMA journal_mode;";
                    
                    using (SqliteDataReader reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetValue(0)?.ToString();
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to check journal mode: {ex.Message}");
                return null;
            }
        }

        /***************************************************/
    }
}
