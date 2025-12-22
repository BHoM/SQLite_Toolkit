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

using BH.Engine.Base;
using BH.oM.Base.Attributes;
using BH.oM.SQLite.Objects;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Gets all type registrations from the database.")]
        [Input("connection", "Active SQLite database connection.")]
        [Output("registrations", "List of all TypeRegistration objects.")]
        private List<TypeRegistration> TypeRegistrations(SqliteConnection connection)
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
    }
}

