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
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.Engine.SQLite
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Gets database statistics from system tables.")]
        [Input("connection", "Active SQLite database connection.")]
        [Output("stats", "Dictionary containing database statistics.")]
        public static Dictionary<string, object> GetDatabaseStatistics(this SqliteConnection connection)
        {
            Dictionary<string, object> stats = new Dictionary<string, object>();

            if (connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot get database statistics: connection is null.");
                return stats;
            }

            try
            {
                // Get type registration count
                string typeCountSql = "SELECT COUNT(*) FROM __Types";
                using (SqliteCommand command = new SqliteCommand(typeCountSql, connection))
                {
                    object result = command.ExecuteScalar();
                    stats["RegisteredTypes"] = result ?? 0;
                }

                // Get total user tables count (excluding system tables)
                string tableCountSql = @"
                    SELECT COUNT(*) FROM sqlite_master 
                    WHERE type='table' AND name NOT LIKE '__%%' AND name != 'sqlite_sequence'";
                using (SqliteCommand command = new SqliteCommand(tableCountSql, connection))
                {
                    object result = command.ExecuteScalar();
                    stats["UserTables"] = result ?? 0;
                }

                // Get database size (page count * page size)
                using (SqliteCommand command = new SqliteCommand("PRAGMA page_count", connection))
                {
                    object pageCount = command.ExecuteScalar();
                    command.CommandText = "PRAGMA page_size";
                    object pageSize = command.ExecuteScalar();
                    
                    if (pageCount != null && pageSize != null)
                    {
                        long size = Convert.ToInt64(pageCount) * Convert.ToInt64(pageSize);
                        stats["DatabaseSizeBytes"] = size;
                    }
                }

                // Get SQLite version
                stats["SqliteVersion"] = connection.GetSqliteVersion();
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Error getting database statistics: {ex.Message}");
            }

            return stats;
        }

        /***************************************************/
    }
}
