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
using BH.oM.SQLite;
using BH.oM.SQLite.Configs;
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

        [Description("Creates a table in the database using intelligent schema detection from an object type. \n" +
            "Combines type registration, schema generation, and table creation in one operation.")]
        [Input("connection", "Active SQLite database connection.")]
        [Input("objectType", "The .NET type of objects that will be stored in this table.")]
        [Input("config", "Optional PushConfig for property mappings and exclusions.")]
        [Input("dropIfExists", "Whether to drop the table first if it already exists. Default is false.")]
        [Output("success", "True if table was created successfully, false otherwise.")]
        public static bool CreateTableForObjectType(SqliteConnection connection, Type objectType, PushConfig config = null, bool dropIfExists = false)
        {
            if (connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create table: connection is null.");
                return false;
            }

            if (objectType == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create table: object type is null.");
                return false;
            }

            try
            {
                // Ensure type management table exists
                if (!connection.EnsureTypesTableExists())
                {
                    BH.Engine.Base.Compute.RecordError("Failed to ensure __Types table exists.");
                    return false;
                }

                // Determine table name - use custom name from config if provided, otherwise use type-based name
                string tableName = null;
                
                // Check if a custom table name is provided in config
                if (config != null && !string.IsNullOrWhiteSpace(config.Table))
                {
                    // Validate the provided table name
                    if (!BH.Engine.SQLite.Query.ValidateTableName(config.Table))
                    {
                        BH.Engine.Base.Compute.RecordError($"Invalid table name provided in config: '{config.Table}'.");
                        return false;
                    }
                    
                    tableName = config.Table;
                    
                    // Ensure the type is registered with the custom table name
                    string existingTableName = connection.GetTableName(objectType.FullName);
                    if (string.IsNullOrEmpty(existingTableName) || existingTableName != tableName)
                    {
                        // Register the type with the custom table name
                        TypeRegistration registration = connection.RegisterType(objectType, tableName);
                        if (registration == null)
                        {
                            BH.Engine.Base.Compute.RecordError($"Failed to register type {objectType.FullName} with custom table name '{tableName}'.");
                            return false;
                        }
                    }
                }
                else
                {
                    // Use type-based table name resolution
                    tableName = connection.GetTableName(objectType.FullName);
                    if (string.IsNullOrWhiteSpace(tableName))
                    {
                        // Register new type
                        TypeRegistration registration = connection.RegisterType(objectType);
                        if (registration == null)
                        {
                            BH.Engine.Base.Compute.RecordError($"Failed to register object type {objectType.FullName}.");
                            return false;
                        }
                        tableName = connection.GetTableName(objectType.FullName);
                    }
                }

                if (string.IsNullOrWhiteSpace(tableName))
                {
                    BH.Engine.Base.Compute.RecordError($"Could not determine table name for type {objectType.FullName}.");
                    return false;
                }

                // Check if table already exists
                bool tableExists = connection.TableExists(tableName);
                
                if (tableExists && !dropIfExists)
                {
                    BH.Engine.Base.Compute.RecordNote($"Table '{tableName}' already exists and dropIfExists is false. Skipping creation.");
                    return true;
                }

                // Generate table schema using smart creation
                TableSchema schema = BH.Engine.SQLite.Create.TableSchema(objectType, tableName, config);
                if (schema == null)
                {
                    BH.Engine.Base.Compute.RecordError($"Failed to generate table schema for type {objectType.FullName}.");
                    return false;
                }

                // Create the table
                string createSql = BH.Engine.SQLite.Create.Table(schema, !dropIfExists, dropIfExists);
                if (string.IsNullOrWhiteSpace(createSql))
                {
                    BH.Engine.Base.Compute.RecordError($"Failed to generate CREATE TABLE SQL for schema '{schema.Name}'.");
                    return false;
                }

                // Execute the creation
                using (SqliteCommand command = new SqliteCommand(createSql, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Populate the __Schema system table with column information
                bool schemaPopulated = connection.PopulateSchemaTable(schema);
                if (!schemaPopulated)
                {
                    BH.Engine.Base.Compute.RecordWarning($"Failed to populate __Schema table for '{tableName}', but table was created successfully.");
                }

                BH.Engine.Base.Compute.RecordNote($"Successfully created table '{tableName}' for type {objectType.FullName} with {schema.Columns.Count} columns.");
                return true;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to create table for object type {objectType.FullName}: {ex.Message}");
                return false;
            }
        }



        /***************************************************/
    }
}
