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

        [Description("Registers a .NET type with its corresponding SQLite table name in the __Types system table.")]
        [Input("connection", "Active SQLite database connection.")]
        [Input("type", "The .NET type to register.")]
        [Input("tableName", "The database table name. If not specified, uses the type name.")]
        [Output("registration", "The TypeRegistration object that was created, or null if registration failed.")]
        public static TypeRegistration RegisterType(this SqliteConnection connection, Type type, string tableName = "")
        {
            if (connection == null || type == null)
            {
                Engine.Base.Compute.RecordError("Cannot register type: connection or type is null.");
                return null;
            }

            string fullTypeName = type.FullName ?? type.Name;
            string finalTableName = string.IsNullOrWhiteSpace(tableName) ? type.Name : tableName;

            try
            {
                // Check if type is already registered
                var existing = connection.LookupTypeRegistration(fullTypeName);
                if (existing != null)
                {
                    Engine.Base.Compute.RecordNote($"Type {fullTypeName} is already registered with table {existing.TableName}.");
                    return existing;
                }

                // Create new registration
                var registration = new TypeRegistration(type, finalTableName);

                // Insert into __Types table
                string insertSql = @"
                    INSERT INTO __Types (FullTypeName, TableName, DateCreated) 
                    VALUES (@FullTypeName, @TableName, @DateCreated)";

                using (var command = new SqliteCommand(insertSql, connection))
                {
                    command.Parameters.AddWithValue("@FullTypeName", registration.FullTypeName);
                    command.Parameters.AddWithValue("@TableName", registration.TableName);
                    command.Parameters.AddWithValue("@DateCreated", registration.DateCreated);

                    int result = command.ExecuteNonQuery();
                    if (result > 0)
                    {
                        Engine.Base.Compute.RecordNote($"Successfully registered type {fullTypeName} with table {finalTableName}.");
                        return registration;
                    }
                }

                Engine.Base.Compute.RecordError($"Failed to register type {fullTypeName}.");
                return null;
            }
            catch (Exception ex)
            {
                Engine.Base.Compute.RecordError($"Error registering type {fullTypeName}: {ex.Message}");
                return null;
            }
        }

        /***************************************************/

        [Description("Ensures the __Types system table exists in the database, creating it if necessary.")]
        [Input("connection", "Active SQLite database connection.")]
        [Output("success", "True if the table exists or was created successfully, false otherwise.")]
        public static bool EnsureTypesTableExists(this SqliteConnection connection)
        {
            if (connection == null)
            {
                Engine.Base.Compute.RecordError("Cannot ensure __Types table: connection is null.");
                return false;
            }

            // Use the centralized system table creation method
            return BH.Engine.SQLite.Create.TypesTable(connection);
        }

        /***************************************************/

        [Description("Generates a unique table name from a .NET type, handling potential conflicts.")]
        [Input("type", "The .NET type to generate a table name for.")]
        [Input("connection", "Optional SQLite connection to check for existing table names.")]
        [Output("tableName", "A unique table name for the type.")]
        public static string GenerateTableName(this Type type, SqliteConnection connection = null)
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