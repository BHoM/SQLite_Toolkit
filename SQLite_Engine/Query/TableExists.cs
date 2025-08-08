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

        [Description("Checks if a table exists in the database.")]
        [Input("connection", "Active SQLite database connection.")]
        [Input("tableName", "The table name to check.")]
        [Output("exists", "True if the table exists, false otherwise.")]
        public static bool TableExists(this SqliteConnection connection, string tableName)
        {
            if (connection == null || string.IsNullOrWhiteSpace(tableName))
                return false;

            try
            {
                string checkSql = @"
                    SELECT COUNT(*) 
                    FROM sqlite_master 
                    WHERE type='table' AND name=@TableName";

                using (var command = new SqliteCommand(checkSql, connection))
                {
                    command.Parameters.AddWithValue("@TableName", tableName);
                    long count = (long)command.ExecuteScalar();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Engine.Base.Compute.RecordWarning($"Error checking if table {tableName} exists: {ex.Message}");
                return false;
            }
        }

        /***************************************************/
    }
}
