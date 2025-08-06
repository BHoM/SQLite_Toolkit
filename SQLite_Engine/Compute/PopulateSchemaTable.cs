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
using BH.oM.SQLite.Objects;
using Microsoft.Data.Sqlite;
using System;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Populates the __Schema system table with information about a table's columns.")]
        [Input("connection", "Active SQLite database connection.")]
        [Input("tableSchema", "The table schema to document in the __Schema table.")]
        [Output("success", "True if schema information was recorded successfully, false otherwise.")]
        public static bool PopulateSchemaTable(this SqliteConnection connection, TableSchema tableSchema)
        {
            if (connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot populate schema table: connection is null.");
                return false;
            }

            if (tableSchema == null || string.IsNullOrWhiteSpace(tableSchema.Name))
            {
                BH.Engine.Base.Compute.RecordError("Cannot populate schema table: table schema is invalid.");
                return false;
            }

            try
            {
                // Ensure __Schema table exists
                bool schemaTableExists = BH.Engine.SQLite.Create.SchemaTable(connection);
                if (!schemaTableExists)
                {
                    BH.Engine.Base.Compute.RecordError("Failed to ensure __Schema table exists.");
                    return false;
                }

                // Delete existing entries for this table (in case of schema updates)
                string deleteSql = "DELETE FROM __Schema WHERE TableName = @TableName";
                using (SqliteCommand deleteCommand = new SqliteCommand(deleteSql, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@TableName", tableSchema.Name);
                    deleteCommand.ExecuteNonQuery();
                }

                // Insert schema information for each column
                string insertSql = @"
                    INSERT INTO __Schema (TableName, ColumnName, DataType, IsNullable, IsPrimaryKey, DefaultValue) 
                    VALUES (@TableName, @ColumnName, @DataType, @IsNullable, @IsPrimaryKey, @DefaultValue)";

                using (SqliteCommand insertCommand = new SqliteCommand(insertSql, connection))
                {
                    foreach (Column column in tableSchema.Columns.OrderBy(c => c.Position))
                    {
                        insertCommand.Parameters.Clear();
                        insertCommand.Parameters.AddWithValue("@TableName", tableSchema.Name);
                        insertCommand.Parameters.AddWithValue("@ColumnName", column.Name);
                        insertCommand.Parameters.AddWithValue("@DataType", column.DataType.ToString());
                        insertCommand.Parameters.AddWithValue("@IsNullable", column.AllowNull);
                        insertCommand.Parameters.AddWithValue("@IsPrimaryKey", column.IsPrimaryKey);
                        insertCommand.Parameters.AddWithValue("@DefaultValue", column.DefaultValue ?? (object)DBNull.Value);

                        insertCommand.ExecuteNonQuery();
                    }
                }

                BH.Engine.Base.Compute.RecordNote($"Successfully populated __Schema table with {tableSchema.Columns.Count} column definitions for table '{tableSchema.Name}'.");
                return true;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to populate schema table for '{tableSchema?.Name}': {ex.Message}");
                return false;
            }
        }

        /***************************************************/
    }
}
