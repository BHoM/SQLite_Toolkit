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

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Creates a table in the database from an existing TableSchema object with type registration integration.")]
        [Input("connection", "Active SQLite database connection.")]
        [Input("schema", "The table schema to create.")]
        [Input("objectType", "Optional object type to register with this table. If provided, creates type mapping.")]
        [Input("dropIfExists", "Whether to drop the table first if it already exists. Default is false.")]
        [Output("success", "True if table was created successfully, false otherwise.")]
        public static bool CreateTableFromSchema(SqliteConnection connection, TableSchema schema, Type objectType = null, bool dropIfExists = false)
        {
            if (connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create table: connection is null.");
                return false;
            }

            if (schema == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create table: schema is null.");
                return false;
            }

            try
            {
                // Register object type if provided
                if (objectType != null)
                {
                    // Ensure type management table exists
                    if (!connection.EnsureTypesTableExists())
                    {
                        BH.Engine.Base.Compute.RecordError("Failed to ensure __Types table exists.");
                        return false;
                    }

                    // Register the type with explicit table name
                    TypeRegistration registration = connection.RegisterType(objectType, schema.Name);
                    if (registration == null)
                    {
                        BH.Engine.Base.Compute.RecordWarning($"Failed to register type mapping for {objectType.FullName} -> {schema.Name}. Proceeding with table creation.");
                    }
                }

                // Generate and execute the CREATE TABLE SQL
                string createSql = BH.Engine.SQLite.Create.Table(schema, !dropIfExists, dropIfExists);
                if (string.IsNullOrWhiteSpace(createSql))
                {
                    BH.Engine.Base.Compute.RecordError($"Failed to generate CREATE TABLE SQL for schema '{schema.Name}'.");
                    return false;
                }

                using (SqliteCommand command = new SqliteCommand(createSql, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Populate the __Schema system table with column information
                bool schemaPopulated = connection.PopulateSchemaTable(schema);
                if (!schemaPopulated)
                {
                    BH.Engine.Base.Compute.RecordWarning($"Failed to populate __Schema table for '{schema.Name}', but table was created successfully.");
                }

                BH.Engine.Base.Compute.RecordNote($"Successfully created table '{schema.Name}' with {schema.Columns.Count} columns.");
                return true;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to create table from schema '{schema?.Name}': {ex.Message}");
                return false;
            }
        }

        /***************************************************/
    }
}
