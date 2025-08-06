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

        [Description("Gets a list of all system table names.")]
        [Input("connection", "Active SQLite database connection.")]
        [Output("tableNames", "List of system table names, or empty list if query fails.")]
        public static List<string> GetSystemTableNames(this SqliteConnection connection)
        {
            List<string> tableNames = new List<string>();

            if (connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot get system table names: connection is null.");
                return tableNames;
            }

            try
            {
                string sql = @"
                    SELECT name FROM sqlite_master 
                    WHERE type='table' AND name LIKE '__%%'
                    ORDER BY name";

                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tableName = reader.GetString(0);
                            tableNames.Add(tableName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Error getting system table names: {ex.Message}");
            }

            return tableNames;
        }

        /***************************************************/
    }
}
