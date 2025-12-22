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

using BH.oM.Base;
using BH.oM.Base.Attributes;
using BH.oM.SQLite.Commands;
using BH.oM.SQLite.Configs;
using BH.oM.SQLite.Objects;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Create Methods                            ****/
        /***************************************************/

        internal bool Create(Table table)
        {
            // Create method for Table objects - creates table and inserts data
            if (table == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create table data: Table is null.");
                return false;
            }

            try
            {
                // Create the table if requested
                if (table.CreateTableIfNotExists)
                {
                    bool tableCreated = Create(table.Schema);
                    if (!tableCreated)
                    {
                        BH.Engine.Base.Compute.RecordError($"Failed to create table for Table: {table.Schema.Name}");
                        return false;
                    }
                }

                // Insert data if provided
                if (table.Rows != null && table.Rows.Any())
                {
                    return InsertTableDataRows(table);
                }

                return true;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to create Table for {table.Schema.Name}: {ex.Message}");
                return false;
            }
        }

        [Description("Creates a database table using intelligent schema detection strategy based on object type analysis. \n" +
            "Automatically determines the optimal table structure through PushConfig mappings and primitive property detection. \n" +
            "Handles type registration, schema optimisation, and complete table creation workflow including system table population.")]
        [Input("connection", "Active SQLite database connection with write permissions and sufficient privileges for table creation and system table modifications.")]
        [Input("objectType", "The .NET Type representing objects that will be stored in the created table. This type guides the unified schema detection process.")]
        [Input("config", "Optional PushConfig containing custom property mappings, exclusion lists, and table naming preferences. If null, uses automatic schema detection.")]
        [Input("dropIfExists", "Whether to drop and recreate the table if it already exists. When false, skips creation for existing tables. Defaults to false for safety.")]
        [Output("success", "True if the table was created successfully with proper schema population, false if creation failed due to validation errors, database constraints, or system table issues.")]
        internal bool Table(SqliteConnection connection, Type objectType, PushConfig config = null, bool dropIfExists = false)
        {
            if (connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create table: no database connection.");
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
                if (!TableExists(connection, "__Types"))
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
                    if (!BH.Engine.SQLite.Query.IsValid(config.Table, true))
                    {
                        BH.Engine.Base.Compute.RecordError($"Invalid table name provided in config: '{config.Table}'.");
                        return false;
                    }

                    tableName = config.Table;

                    // Ensure the type is registered with the custom table name
                    string existingTableName = GetTableName(connection, objectType.FullName);
                    if (string.IsNullOrEmpty(existingTableName) || existingTableName != tableName)
                    {
                        // Register the type with the custom table name
                        TypeRegistration registration = RegisterType(connection, objectType, tableName);
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
                    tableName = GetTableName(connection, objectType.FullName);
                    if (string.IsNullOrWhiteSpace(tableName))
                    {
                        // Register new type
                        TypeRegistration registration = RegisterType(connection, objectType);
                        if (registration == null)
                        {
                            BH.Engine.Base.Compute.RecordError($"Failed to register object type {objectType.FullName}.");
                            return false;
                        }
                        tableName = GetTableName(connection, objectType.FullName);
                    }
                }

                if (string.IsNullOrWhiteSpace(tableName))
                {
                    BH.Engine.Base.Compute.RecordError($"Could not determine table name for type {objectType.FullName}.");
                    return false;
                }

                // Check if table already exists
                bool tableExists = TableExists(connection, tableName);

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
                string createSql = BH.Engine.SQLite.Compute.TableCommand(schema, !dropIfExists, dropIfExists);
                if (string.IsNullOrWhiteSpace(createSql))
                {
                    BH.Engine.Base.Compute.RecordError($"Failed to generate CREATE TABLE SQL for schema '{schema.Name}'.");
                    return false;
                }

                // Execute the creation using the new Engine method and ExecuteCommand
                SQLCommand command = BH.Engine.SQLite.Compute.CreateTableCommand(createSql);
                if (command == null)
                {
                    BH.Engine.Base.Compute.RecordError($"Failed to create SQL command for table '{tableName}'.");
                    return false;
                }

                Output<List<object>, bool> result = ExecuteCommand(command);
                if (!result.Item2)
                {
                    BH.Engine.Base.Compute.RecordError($"Failed to execute CREATE TABLE command for '{tableName}'.");
                    return false;
                }

                // Populate the __Schema system table with column information
                bool schemaPopulated = PopulateSchemaTable(connection, schema);
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

        /***************************************************/
        /**** Private Helper Methods                   ****/
        /***************************************************/

        private bool InsertTableDataRows(Table table)
        {
            if (table?.Rows == null || !table.Rows.Any())
                return true;

            try
            {
                string tableName = table.Schema.Name;
                Dictionary<string, object> firstRow = table.Rows.First();
                List<string> columnNames = firstRow.Keys.ToList();

                string conflictClause = GetConflictClause(table.TableConfig.ConflictResolution);

                // Process rows in batches
                int batchSize = table.TableConfig.BatchSize;
                List<List<Dictionary<string, object>>> batches = table.Rows
                    .Select((row, index) => new { row, index })
                    .GroupBy(x => x.index / batchSize)
                    .Select(g => g.Select(x => x.row).ToList())
                    .ToList();

                foreach (List<Dictionary<string, object>> batch in batches)
                {
                    foreach (Dictionary<string, object> row in batch)
                    {
                        // Use the shared Insert method for each row
                        bool success = Insert(m_Connection, tableName, row, conflictClause);
                        if (!success)
                        {
                            BH.Engine.Base.Compute.RecordWarning($"Failed to insert a row into table '{tableName}'.");
                        }
                    }
                }

                BH.Engine.Base.Compute.RecordNote($"Successfully processed {table.Rows.Count} rows for table: {tableName}");
                return true;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to insert data into {table.Schema.Name}: {ex.Message}");
                return false;
            }
        }

        /***************************************************/

    }
}
