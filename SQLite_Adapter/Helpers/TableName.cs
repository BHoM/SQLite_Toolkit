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

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Generates a unique, database-safe table name derived from a .NET type, with intelligent conflict resolution to prevent naming collisions. \n" +
            "When a connection is provided, the method queries the __Types table to ensure uniqueness by appending numeric suffixes when necessary.")]
        [Input("type", "The .NET Type object from which to derive the table name. The simple type name (without namespace) forms the basis for the generated table name.")]
        [Input("connection", "Optional active SQLite database connection used for uniqueness verification against the __Types system table. If not provided, returns the basic type name without conflict checking.")]
        [Output("tableName", "A unique, SQL-compliant table name for the specified type. If conflicts exist, a numeric suffix is appended (e.g., 'MyClass_1', 'MyClass_2') to ensure uniqueness.")]
        private string TableName(Type type, SqliteConnection connection = null)
        {
            if (type == null)
                return "";

            string baseName = type.Name;

            // If no connection provided, just return the type name
            if (connection == null)
                return baseName;

            try
            {
                // Check if table name already exists in __Types
                string checkSql = "SELECT COUNT(*) FROM __Types WHERE TableName = @TableName";
                using (var command = new SqliteCommand(checkSql, connection))
                {
                    command.Parameters.AddWithValue("@TableName", baseName);
                    long count = (long)command.ExecuteScalar();

                    if (count == 0)
                        return baseName;

                    // Generate unique name with suffix
                    int suffix = 1;
                    string uniqueName;
                    do
                    {
                        uniqueName = $"{baseName}_{suffix}";
                        command.Parameters["@TableName"].Value = uniqueName;
                        count = (long)command.ExecuteScalar();
                        suffix++;
                    } while (count > 0);

                    return uniqueName;
                }
            }
            catch (Exception ex)
            {
                Engine.Base.Compute.RecordWarning($"Error generating unique table name for {type.Name}: {ex.Message}. Using base name.");
                return baseName;
            }
        }

        /***************************************************/
    }
}
