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

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Registers a .NET type with its corresponding SQLite table name in the __Types system table, enabling automatic type-to-table mapping for subsequent operations. \n" +
            "If the type is already registered, returns the existing registration. Automatically generates unique table names when conflicts occur.")]
        [Input("connection", "Active SQLite database connection with an open transaction state. The connection must have the __Types system table already created.")]
        [Input("type", "The .NET Type object to register for database storage. The full type name including namespace will be stored for precise type resolution.")]
        [Input("tableName", "Optional custom database table name for this type. If not specified or empty, generates a unique table name based on the type name with conflict resolution.")]
        [Output("registration", "The TypeRegistration object containing the assigned ID, full type name, table name and creation timestamp, or null if registration fails due to database errors.")]
        private TypeRegistration RegisterType(SqliteConnection connection, Type type, string tableName = "")
        {
            string fullTypeName = type.FullName ?? type.Name;
            string finalTableName;
            
            if (string.IsNullOrWhiteSpace(tableName))
                finalTableName = TableName(type, connection);
            else
                finalTableName = tableName;

            try
            {
                // Check if type is already registered
                var existing = GetTypeRegistration(connection, fullTypeName);
                if (existing != null)
                {
                    Engine.Base.Compute.RecordNote($"Type {fullTypeName} is already registered with table {existing.TableName}.");
                    return existing;
                }

                // Create new registration
                var registration = new TypeRegistration()
                {
                    FullTypeName = fullTypeName,
                    TableName = finalTableName,
                    DateCreated = DateTime.UtcNow
                };

                // Insert into __Types table
                string insertSql = @"
                    INSERT INTO __Types (FullTypeName, TableName, DateCreated) 
                    VALUES (@FullTypeName, @TableName, @DateCreated)";

                using (SqliteCommand command = new SqliteCommand(insertSql, connection))
                {
                    command.Parameters.AddWithValue("@FullTypeName", registration.FullTypeName);
                    command.Parameters.AddWithValue("@TableName", registration.TableName);
                    command.Parameters.AddWithValue("@DateCreated", registration.DateCreated);

                    int result = command.ExecuteNonQuery();
                    if (result > 0)
                    {
                        // Get the generated ID
                        command.CommandText = "SELECT last_insert_rowid()";
                        command.Parameters.Clear();
                        object idResult = command.ExecuteScalar();
                        if (idResult != null && long.TryParse(idResult.ToString(), out long id))
                        {
                            registration.Id = (int)id;
                        }

                        Engine.Base.Compute.RecordNote($"Successfully registered type {fullTypeName} with table {finalTableName} (ID: {registration.Id}).");
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
    }
}
