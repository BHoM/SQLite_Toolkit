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

using BH.oM.Adapter;
using BH.oM.Base;
using BH.oM.Base.Attributes;
using BH.oM.SQLite.Configs;
using BH.oM.SQLite.Objects;
using BH.Engine.SQLite;
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
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Pushes objects to SQLite database using intelligent type mapping and automatic table creation.")]
        [Input("objects", "Collection of BHoM objects to push to the database.")]
        [Input("tag", "Optional tag for the push operation.")]
        [Input("pushType", "Type of push operation to perform.")]
        [Input("actionConfig", "Configuration for the push operation. Should be a PushConfig object.")]
        [Output("result", "List of objects that were successfully pushed to the database.")]
        public override List<object> Push(IEnumerable<object> objects, string tag = "", PushType pushType = PushType.AdapterDefault, ActionConfig actionConfig = null)
        {
            List<object> result = new List<object>();

            if (objects == null)
                return result;

            objects = objects.Where(x => x != null).ToList();
            if (!objects.Any())
                return result;

            // Check that we have a valid database connection
            if (m_Connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot push objects: no database connection. Please open a connection first.");
                return result;
            }

            // Update last used timestamp
            m_LastUsed = DateTime.Now;

            // Get the type of the pushed objects and ensure they are all the same type
            List<Type> objectTypes = objects.Select(x => x.GetType()).Distinct().ToList();
            if (objectTypes.Count != 1)
            {
                string message = "The SQLite adapter only allows pushing objects of a single type to a table." +
                    "\nRight now you are providing objects of the following types: " +
                    objectTypes.Select(x => x.ToString()).Aggregate((a, b) => a + ", " + b);
                BH.Engine.Base.Compute.RecordError(message);
                return result;
            }

            Type objectType = objectTypes[0];

            // Special routing for Table and TableSchema objects - use specific Create methods
            if (typeof(TableSchema).IsAssignableFrom(objectType))
            {
                return PushTableSchemas(objects.Cast<TableSchema>());
            }

            if (typeof(Table).IsAssignableFrom(objectType))
            {
                return PushTables(objects.Cast<Table>());
            }

            // Ensure objects are BHoM objects
            if (!typeof(IBHoMObject).IsAssignableFrom(objectType))
            {
                BH.Engine.Base.Compute.RecordError($"SQLite adapter only supports BHoM objects. Type {objectType.Name} does not implement IBHoMObject.");
                return result;
            }

            // Cast to IBHoMObject collection
            IEnumerable<IBHoMObject> bhoMObjects = objects.Cast<IBHoMObject>();

            // Get PushConfig
            PushConfig config = actionConfig as PushConfig ?? new PushConfig();

            try
            {
                // Step 1: Ensure __Types table exists
                m_Connection.EnsureTypesTableExists();

                // Step 2: Get or register table name for this type
                string tableName = GetTableNameForType(objectType, config);
                if (string.IsNullOrEmpty(tableName))
                {
                    BH.Engine.Base.Compute.RecordError($"Failed to get or register table name for type {objectType.Name}.");
                    return result;
                }

                // Step 3: Ensure table exists (create if it doesn't)
                if (!m_Connection.TableExists(tableName))
                {
                    bool tableCreated = BH.Engine.SQLite.Compute.CreateTableForObjectType(m_Connection, objectType, config);
                    if (!tableCreated)
                    {
                        BH.Engine.Base.Compute.RecordError($"Failed to create table '{tableName}' for type {objectType.Name}.");
                        return result;
                    }
                    BH.Engine.Base.Compute.RecordNote($"Created table '{tableName}' for type {objectType.Name}.");
                }

                // Step 4: Insert the objects
                bool insertSuccess = InsertObjects(bhoMObjects, tableName, objectType, config);
                if (insertSuccess)
                {
                    result = objects.ToList();
                    BH.Engine.Base.Compute.RecordNote($"Successfully pushed {objects.Count()} objects of type {objectType.Name} to table '{tableName}'.");
                }

                // Perform WAL checkpoint after push operation if WAL mode is enabled
                if (m_WalModeEnabled && insertSuccess)
                {
                    BH.Engine.SQLite.Compute.WalCheckpoint(m_Connection, "TRUNCATE");
                }

                return result;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to push objects of type {objectType.Name}: {ex.Message}");
                return result;
            }
        }

        /***************************************************/
        /**** Private Helper Methods                   ****/
        /***************************************************/

        private string GetTableNameForType(Type objectType, PushConfig config)
        {
            // Check if a specific table name is provided in config
            if (!string.IsNullOrWhiteSpace(config.Table))
            {
                // Validate the provided table name
                if (!BH.Engine.SQLite.Query.ValidateTableName(config.Table))
                {
                    BH.Engine.Base.Compute.RecordError($"Invalid table name provided in config: '{config.Table}'.");
                    return null;
                }

                // Check if this table is already registered to a different type
                string existingTypeName = m_Connection.GetTypeName(config.Table);
                if (!string.IsNullOrEmpty(existingTypeName) && existingTypeName != objectType.FullName)
                {
                    BH.Engine.Base.Compute.RecordError($"Table '{config.Table}' is already registered to type '{existingTypeName}', cannot use for type '{objectType.FullName}'.");
                    return null;
                }

                return config.Table;
            }

            // Try to get existing table name for this type
            string tableName = m_Connection.GetTableName(objectType.FullName);
            if (!string.IsNullOrEmpty(tableName))
            {
                return tableName;
            }

            // Register new type and get table name
            TypeRegistration registration = m_Connection.RegisterType(objectType);
            if (registration == null)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to register type {objectType.FullName} in __Types table.");
                return null;
            }
            tableName = registration.TableName;
            if (string.IsNullOrEmpty(tableName))
            {
                BH.Engine.Base.Compute.RecordError($"Failed to register type {objectType.FullName} in __Types table.");
                return null;
            }

            return tableName;
        }

        /***************************************************/

        private bool InsertObjects(IEnumerable<IBHoMObject> objects, string tableName, Type objectType, PushConfig config)
        {
            if (objects == null || !objects.Any() || string.IsNullOrEmpty(tableName))
                return false;

            List<IBHoMObject> objectList = objects.ToList();
            if (!objectList.Any())
                return false;

            try
            {
                // Resolve the column schema once for all objects of this type
                Dictionary<string, PropertyColumnInfo> columnSchema = objectType.ResolveColumnSchema(config);
                if (columnSchema == null || !columnSchema.Any())
                {
                    BH.Engine.Base.Compute.RecordWarning($"No column mappings found for type {objectType.Name}. Cannot insert data.");
                    return false;
                }

                // Process objects in batches for performance
                int batchSize = 1000; // Default batch size
                bool overallSuccess = true;

                List<List<IBHoMObject>> batches = objectList
                    .Select((obj, index) => new { obj, index })
                    .GroupBy(x => x.index / batchSize)
                    .Select(g => g.Select(x => x.obj).ToList())
                    .ToList();

                foreach (List<IBHoMObject> batch in batches)
                {
                    bool batchSuccess = InsertObjectBatch(batch, tableName, columnSchema);
                    if (!batchSuccess)
                    {
                        overallSuccess = false;
                        BH.Engine.Base.Compute.RecordWarning($"Failed to insert batch of {batch.Count} objects into table '{tableName}'.");
                    }
                }

                return overallSuccess;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to insert objects into table '{tableName}': {ex.Message}");
                return false;
            }
        }

        /***************************************************/

        private bool InsertObjectBatch(List<IBHoMObject> objects, string tableName, Dictionary<string, PropertyColumnInfo> columnSchema)
        {
            if (objects == null || !objects.Any())
                return true;

            try
            {
                // Extract all column names from the schema
                // BHoM_Guid is automatically included via primitive property resolution unless excluded
                List<string> columnNames = columnSchema.Keys.ToList();

                // Build INSERT statement
                string columns = string.Join(", ", columnNames.Select(col => $"\"{col}\""));
                string placeholders = string.Join(", ", columnNames.Select((col, index) => $"@param{index}"));

                string insertSql = $"INSERT OR REPLACE INTO \"{tableName}\" ({columns}) VALUES ({placeholders})";

                using (SqliteCommand command = new SqliteCommand(insertSql, m_Connection))
                {
                    // Add parameters for all columns
                    for (int i = 0; i < columnNames.Count; i++)
                    {
                        command.Parameters.Add($"@param{i}", Microsoft.Data.Sqlite.SqliteType.Text);
                    }

                    // Insert each object in the batch
                    foreach (IBHoMObject obj in objects)
                    {
                        // Extract column values for this object
                        Dictionary<string, object> columnValues = obj.ExtractColumnValues(columnSchema);

                        // BHoM_Guid is automatically extracted via ExtractColumnValues if it's in the schema
                        // No special handling needed as it's a primitive property

                        // Set parameter values
                        for (int i = 0; i < columnNames.Count; i++)
                        {
                            string columnName = columnNames[i];
                            object value = columnValues.ContainsKey(columnName) ? columnValues[columnName] : null;

                            // Convert to SQLite-compatible value
                            object sqliteValue = BH.Engine.SQLite.Compute.ConvertToSqliteValue(value);
                            command.Parameters[$"@param{i}"].Value = sqliteValue;
                        }

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to insert batch into table '{tableName}': {ex.Message}");
                return false;
            }
        }

        /***************************************************/

        private List<object> PushTableSchemas(IEnumerable<TableSchema> tableSchemas)
        {
            List<object> result = new List<object>();

            if (tableSchemas == null)
                return result;

            List<TableSchema> schemaList = tableSchemas.ToList();
            if (!schemaList.Any())
                return result;

            // Update last used timestamp
            m_LastUsed = DateTime.Now;

            try
            {
                bool success = ICreate(schemaList);
                if (success)
                {
                    result = schemaList.Cast<object>().ToList();
                    BH.Engine.Base.Compute.RecordNote($"Successfully pushed {schemaList.Count} TableSchema objects.");
                }

                // Perform WAL checkpoint after push operation if WAL mode is enabled
                if (m_WalModeEnabled && success)
                {
                    BH.Engine.SQLite.Compute.WalCheckpoint(m_Connection, "TRUNCATE");
                }

                return result;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to push TableSchema objects: {ex.Message}");
                return result;
            }
        }

        /***************************************************/

        private List<object> PushTables(IEnumerable<Table> tables)
        {
            List<object> result = new List<object>();

            if (tables == null)
                return result;

            List<Table> tableList = tables.ToList();
            if (!tableList.Any())
                return result;

            // Update last used timestamp
            m_LastUsed = DateTime.Now;

            try
            {
                bool success = ICreate(tableList);
                if (success)
                {
                    result = tableList.Cast<object>().ToList();
                    BH.Engine.Base.Compute.RecordNote($"Successfully pushed {tableList.Count} Table objects.");
                }

                // Perform WAL checkpoint after push operation if WAL mode is enabled
                if (m_WalModeEnabled && success)
                {
                    BH.Engine.SQLite.Compute.WalCheckpoint(m_Connection, "TRUNCATE");
                }

                return result;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to push Table objects: {ex.Message}");
                return result;
            }
        }

        /***************************************************/
    }
}
