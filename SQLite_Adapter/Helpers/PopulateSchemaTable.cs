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
using BH.oM.SQLite.Objects;
using Microsoft.Data.Sqlite;
using System;
using System.ComponentModel;
using System.Linq;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Populates the __Schema system table with information about a table's columns.")]
        [Input("connection", "Active SQLite database connection.")]
        [Input("tableSchema", "The table schema to document in the __Schema table.")]
        [Output("success", "True if schema information was recorded successfully, false otherwise.")]
        private bool PopulateSchemaTable(SqliteConnection connection, TableSchema tableSchema)
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
                bool schemaTableExists = SchemaTable(connection);
                if (!schemaTableExists)
                {
                    BH.Engine.Base.Compute.RecordError("Failed to ensure __Schema table exists.");
                    return false;
                }

                // Delete existing entries for this table (in case of schema updates)
                bool deletedEntries = DeleteSchemaEntries(connection, tableSchema.Name);
                if (!deletedEntries)
                {
                    BH.Engine.Base.Compute.RecordWarning($"Failed to delete existing schema entries for table '{tableSchema.Name}', but will continue with insertion.");
                }

                // Insert schema information for each column
                foreach (Column column in tableSchema.Columns.OrderBy(c => c.Position))
                {
                    bool inserted = InsertColumnSchema(connection, tableSchema.Name, column);
                    if (!inserted)
                    {
                        BH.Engine.Base.Compute.RecordError($"Failed to insert schema for column '{column.Name}' in table '{tableSchema.Name}'.");
                        return false;
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

