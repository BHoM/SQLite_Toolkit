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
using BH.oM.SQLite;
using BH.oM.SQLite.Configs;
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

        [Description("Ensures that a table exists for the given object type, creating it if necessary using smart schema detection.")]
        [Input("connection", "Active SQLite database connection.")]
        [Input("objectType", "The .NET type of objects that will be stored in this table.")]
        [Input("config", "Optional PushConfig for property mappings and exclusions.")]
        [Output("tableName", "The name of the table that exists or was created, or empty string if operation failed.")]
        public static string EnsureTableExistsForType(SqliteConnection connection, Type objectType, PushConfig config = null)
        {
            if (connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot ensure table exists: connection is null.");
                return "";
            }

            if (objectType == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot ensure table exists: object type is null.");
                return "";
            }

            try
            {
                // Ensure type management table exists
                if (!connection.EnsureTypesTableExists())
                {
                    BH.Engine.Base.Compute.RecordError("Failed to ensure __Types table exists.");
                    return "";
                }

                // Check if type is already registered
                string existingTableName = connection.GetTableName(objectType.FullName);
                
                if (!string.IsNullOrWhiteSpace(existingTableName))
                {
                    // Check if the table actually exists
                    if (connection.TableExists(existingTableName))
                    {
                        BH.Engine.Base.Compute.RecordNote($"Table '{existingTableName}' already exists for type {objectType.FullName}.");
                        return existingTableName;
                    }
                    else
                    {
                        BH.Engine.Base.Compute.RecordWarning($"Type {objectType.FullName} is registered to table '{existingTableName}' but table does not exist. Recreating table.");
                    }
                }

                // Create the table
                bool created = BH.Engine.SQLite.Compute.CreateTableForObjectType(connection, objectType, config, false);
                if (created)
                {
                    string tableName = connection.GetTableName(objectType.FullName);
                    return tableName ?? "";
                }

                return "";
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to ensure table exists for type {objectType.FullName}: {ex.Message}");
                return "";
            }
        }

        /***************************************************/
    }
}
