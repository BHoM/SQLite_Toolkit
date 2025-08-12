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

using BH.Engine.Base;
using BH.oM.Base.Attributes;
using Microsoft.Data.Sqlite;
using System;
using System.ComponentModel;

namespace BH.Engine.SQLite
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Checks if a specific column exists in the given table.")]
        [Input("connection", "The SQLite database connection to use.")]
        [Input("tableName", "Name of the table to check.")]
        [Input("columnName", "Name of the column to check for.")]
        [Output("exists", "True if the column exists in the table, false otherwise.")]
        public static bool ColumnExists(this SqliteConnection connection, string tableName, string columnName)
        {
            if (connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot check column existence: connection is null.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                BH.Engine.Base.Compute.RecordError("Cannot check column existence: table name is null or empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(columnName))
            {
                BH.Engine.Base.Compute.RecordError("Cannot check column existence: column name is null or empty.");
                return false;
            }

            try
            {
                using (SqliteCommand command = new SqliteCommand($"PRAGMA table_info(\"{tableName}\")", connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string existingColumnName = reader.GetString(1); // Column name is at index 1
                            if (string.Equals(existingColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to check if column '{columnName}' exists in table '{tableName}': {ex.Message}");
                return false;
            }
        }

        /***************************************************/
    }
}
