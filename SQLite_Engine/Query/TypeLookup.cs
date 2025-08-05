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
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.Engine.SQLite
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Looks up a type registration by full type name.")]
        [Input("connection", "Active SQLite database connection.")]
        [Input("fullTypeName", "The full type name including namespace.")]
        [Output("registration", "The TypeRegistration if found, null otherwise.")]
        public static TypeRegistration LookupTypeRegistration(this SqliteConnection connection, string fullTypeName)
        {
            if (connection == null || string.IsNullOrWhiteSpace(fullTypeName))
                return null;

            try
            {
                string selectSql = @"
                    SELECT Id, FullTypeName, TableName, DateCreated 
                    FROM __Types 
                    WHERE FullTypeName = @FullTypeName";

                using (var command = new SqliteCommand(selectSql, connection))
                {
                    command.Parameters.AddWithValue("@FullTypeName", fullTypeName);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new TypeRegistration
                            {
                                Id = reader.GetInt32(0),
                                FullTypeName = reader.GetString(1),
                                TableName = reader.GetString(2),
                                DateCreated = reader.GetDateTime(3)
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Engine.Base.Compute.RecordWarning($"Error looking up type registration for {fullTypeName}: {ex.Message}");
            }

            return null;
        }

        /***************************************************/

        [Description("Looks up a type registration by .NET Type.")]
        [Input("connection", "Active SQLite database connection.")]
        [Input("type", "The .NET Type to look up.")]
        [Output("registration", "The TypeRegistration if found, null otherwise.")]
        public static TypeRegistration LookupTypeRegistration(this SqliteConnection connection, Type type)
        {
            if (type == null)
                return null;

            string fullTypeName = type.FullName ?? type.Name;
            return connection.LookupTypeRegistration(fullTypeName);
        }

        /***************************************************/

        [Description("Gets the table name for a given .NET Type.")]
        [Input("connection", "Active SQLite database connection.")]
        [Input("type", "The .NET Type to get the table name for.")]
        [Output("tableName", "The table name if registered, empty string otherwise.")]
        public static string GetTableName(this SqliteConnection connection, Type type)
        {
            TypeRegistration registration = connection.LookupTypeRegistration(type);
            return registration?.TableName ?? "";
        }

        /***************************************************/

        [Description("Gets the table name for a given full type name.")]
        [Input("connection", "Active SQLite database connection.")]
        [Input("fullTypeName", "The full type name including namespace.")]
        [Output("tableName", "The table name if registered, empty string otherwise.")]
        public static string GetTableName(this SqliteConnection connection, string fullTypeName)
        {
            TypeRegistration registration = connection.LookupTypeRegistration(fullTypeName);
            return registration?.TableName ?? "";
        }

        /***************************************************/

        [Description("Gets all type registrations from the database.")]
        [Input("connection", "Active SQLite database connection.")]
        [Output("registrations", "List of all TypeRegistration objects.")]
        public static List<TypeRegistration> GetAllTypeRegistrations(this SqliteConnection connection)
        {
            List<TypeRegistration> registrations = new List<TypeRegistration>();

            if (connection == null)
                return registrations;

            try
            {
                string selectSql = @"
                    SELECT Id, FullTypeName, TableName, DateCreated 
                    FROM __Types 
                    ORDER BY DateCreated";

                using (var command = new SqliteCommand(selectSql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        registrations.Add(new TypeRegistration
                        {
                            Id = reader.GetInt32(0),
                            FullTypeName = reader.GetString(1),
                            TableName = reader.GetString(2),
                            DateCreated = reader.GetDateTime(3)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Engine.Base.Compute.RecordWarning($"Error retrieving type registrations: {ex.Message}");
            }

            return registrations;
        }

        /***************************************************/

        [Description("Checks if a type is already registered in the database.")]
        [Input("connection", "Active SQLite database connection.")]
        [Input("type", "The .NET Type to check.")]
        [Output("isRegistered", "True if the type is registered, false otherwise.")]
        public static bool IsTypeRegistered(this SqliteConnection connection, Type type)
        {
            TypeRegistration registration = connection.LookupTypeRegistration(type);
            return registration != null;
        }

        /***************************************************/

        [Description("Checks if a table exists in the database.")]
        [Input("connection", "Active SQLite database connection.")]
        [Input("tableName", "The table name to check.")]
        [Output("exists", "True if the table exists, false otherwise.")]
        public static bool TableExists(this SqliteConnection connection, string tableName)
        {
            if (connection == null || string.IsNullOrWhiteSpace(tableName))
                return false;

            try
            {
                string checkSql = @"
                    SELECT COUNT(*) 
                    FROM sqlite_master 
                    WHERE type='table' AND name=@TableName";

                using (var command = new SqliteCommand(checkSql, connection))
                {
                    command.Parameters.AddWithValue("@TableName", tableName);
                    long count = (long)command.ExecuteScalar();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Engine.Base.Compute.RecordWarning($"Error checking if table {tableName} exists: {ex.Message}");
                return false;
            }
        }

        /***************************************************/
    }
}