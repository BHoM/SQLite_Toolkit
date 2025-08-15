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

        [Description("Looks up a type registration by full type name.")]
        [Input("connection", "Active SQLite database connection.")]
        [Input("fullTypeName", "The full type name including namespace.")]
        [Output("registration", "The TypeRegistration if found, null otherwise.")]
        public static TypeRegistration GetTypeRegistration(SqliteConnection connection, string fullTypeName)
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
        public static TypeRegistration GetTypeRegistration(SqliteConnection connection, Type type)
        {
            if (type == null)
                return null;

            string fullTypeName = type.FullName ?? type.Name;
            return SQLiteAdapter.GetTypeRegistration(connection, fullTypeName);
        }

        /***************************************************/
    }
}
