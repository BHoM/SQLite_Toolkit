/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2026, the respective contributors. All rights reserved.
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

        [Description("Gets the SQLite version from an active database connection.")]
        [Input("connection", "Active SQLite database connection.")]
        [Output("version", "The SQLite version string, or 'Unknown' if query fails.")]
        private string GetSqliteVersion(SqliteConnection connection)
        {
            if (connection == null || connection.State != System.Data.ConnectionState.Open)
            {
                BH.Engine.Base.Compute.RecordWarning("Cannot get SQLite version: connection is null or not open.");
                return "Unknown";
            }

            try
            {
                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT sqlite_version();";
                    object result = command.ExecuteScalar();
                    return result?.ToString() ?? "Unknown";
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to get SQLite version: {ex.Message}");
                return "Unknown";
            }
        }

        /***************************************************/
    }
}

