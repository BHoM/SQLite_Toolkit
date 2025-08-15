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

        [Description("Creates the __Schema system table for storing database schema metadata.")]
        [Input("connection", "Active SQLite database connection.")]
        [Output("success", "True if the table was created successfully, false otherwise.")]
        public static bool SchemaTable(SqliteConnection connection)
        {
            if (connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create __Schema table: connection is null.");
                return false;
            }

            try
            {
                string createTableSql = @"
                    CREATE TABLE IF NOT EXISTS __Schema (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TableName TEXT NOT NULL,
                        ColumnName TEXT NOT NULL,
                        DataType TEXT NOT NULL,
                        NetTypeName TEXT,
                        IsNullable BOOLEAN DEFAULT 1,
                        IsPrimaryKey BOOLEAN DEFAULT 0,
                        DefaultValue TEXT,
                        DateCreated DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UNIQUE(TableName, ColumnName)
                    )";

                using (SqliteCommand command = new SqliteCommand(createTableSql, connection))
                {
                    command.ExecuteNonQuery();
                    BH.Engine.Base.Compute.RecordNote("Successfully created __Schema system table.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Error creating __Schema table: {ex.Message}");
                return false;
            }
        }

        /***************************************************/
    }
}
