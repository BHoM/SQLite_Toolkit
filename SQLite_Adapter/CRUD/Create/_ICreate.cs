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
using BH.oM.SQLite.Objects;
using BH.oM.SQLite;
using BH.oM.SQLite.Configs;
using BH.Engine.SQLite;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter : BHoMAdapter
    {
        // NOTE: CRUD folder methods
        // All methods in the CRUD folder are used as "back-end" methods by the Adapter itself.
        // They are automatically invoked by the Adapter Actions (Push, Pull, etc.).
        // Specifically, the Create is primarily called by the Push (in the context of the CRUD method, and also by other methods that require it: Update, UpdateProperty).

        // The Create should only contain the logic that generates the objects in the external software.
        // Note: With simplified scope, users manage their own table creation and data insertion.
        // This adapter focuses on connection management and query execution.
        protected override bool ICreate<T>(IEnumerable<T> objects, ActionConfig actionConfig = null)
        {
            if (m_Connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create objects: no database connection. Please open a connection first.");
                return false;
            }

            // Update last used timestamp
            m_LastUsed = DateTime.Now;

            bool success = true;

            foreach (T obj in objects)
            {
                success &= Create(obj as dynamic);
            }

            // Perform WAL checkpoint after push operation if WAL mode is enabled
            if (m_WalModeEnabled && success)
            {
                WalCheckpoint(m_Connection, "TRUNCATE");
            }

            return success;
        }

        /***************************************************/

        // Fallback case. If no specific Create is found, use the intelligent object push logic.
        protected bool Create(IBHoMObject obj)
        {
            if (obj == null)
            {
                BH.Engine.Base.Compute.RecordWarning("Cannot create object: object is null.");
                return false;
            }

            if (m_Connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create object: no database connection.");
                return false;
            }

            Type objectType = obj.GetType();
            PushConfig config = new PushConfig(); // Use default config for CRUD operations

            try
            {
                // Step 1: Ensure __Types table exists
                if (!TableExists(m_Connection, "__Types"))
                {
                    TypesTable(m_Connection);
                }

                // Step 2: Get or register table name for this type
                string tableName = GetTableName(m_Connection, objectType.FullName);
                if (string.IsNullOrEmpty(tableName))
                {
                    TypeRegistration registration = RegisterType(m_Connection, objectType);
                    if (registration == null)
                    {
                        BH.Engine.Base.Compute.RecordError($"Failed to register type {objectType.FullName} in __Types table.");
                        return false;
                    }
                    tableName = registration.TableName;
                    if (string.IsNullOrEmpty(tableName))
                    {
                        BH.Engine.Base.Compute.RecordError($"Failed to register type {objectType.FullName} in __Types table.");
                        return false;
                    }
                }

                // Step 3: Ensure table exists (create if it doesn't)
                if (!TableExists(m_Connection, tableName))
                {
                    bool tableCreated = Table(m_Connection, objectType, config);
                    if (!tableCreated)
                    {
                        BH.Engine.Base.Compute.RecordError($"Failed to create table '{tableName}' for type {objectType.Name}.");
                        return false;
                    }
                    BH.Engine.Base.Compute.RecordNote($"Created table '{tableName}' for type {objectType.Name}.");
                }

                // Step 4: Insert the object data
                return InsertSingleObject(obj, tableName, objectType, config);
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to create object of type {objectType.Name}: {ex.Message}");
                return false;
            }
        }

        /***************************************************/

        private bool InsertSingleObject(IBHoMObject obj, string tableName, Type objectType, PushConfig config)
        {
            if (obj == null || string.IsNullOrEmpty(tableName))
                return false;

            try
            {
                // Resolve the column schema for this object type
                Dictionary<string, PropertyColumnInfo> columnSchema = objectType.ResolveColumnSchema(config);
                if (columnSchema == null || !columnSchema.Any())
                {
                    BH.Engine.Base.Compute.RecordWarning($"No column mappings found for type {objectType.Name}. Cannot insert data.");
                    return false;
                }

                // Extract column values from the object
                // BHoM_Guid is automatically included via primitive property resolution unless excluded
                Dictionary<string, object> columnValues = obj.GetColumnValues(columnSchema);

                // Build and execute INSERT statement
                return ExecuteSingleInsert(tableName, columnValues);
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to insert object into table '{tableName}': {ex.Message}");
                return false;
            }
        }

        /***************************************************/

        private bool ExecuteSingleInsert(string tableName, Dictionary<string, object> columnValues)
        {
            if (columnValues == null || !columnValues.Any())
                return false;

            // Use the shared Insert method
            return Insert(m_Connection, tableName, columnValues, "OR REPLACE");
        }

        /***************************************************/
        /**** Private Helper Methods                   ****/
        /***************************************************/

        private string GetConflictClause(ConflictResolution conflictResolution)
        {
            switch(conflictResolution)
            {
                case ConflictResolution.Replace:
                    return "OR REPLACE";
                case ConflictResolution.Ignore:
                    return "OR IGNORE";
                case ConflictResolution.Fail:
                    return "OR FAIL";
                case ConflictResolution.Abort:
                    return "OR FAIL"; // OR ABORT is not valid for INSERT statements, use OR FAIL instead
                case ConflictResolution.Rollback:
                    return "OR ROLLBACK";
                default:
                    return "";
            };
        }
    }
}


