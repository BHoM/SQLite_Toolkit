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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Executes a parameterised INSERT statement against a SQLite connection.")]
        [Input("connection", "The SQLite connection to execute the statement against.")]
        [Input("tableName", "The name of the table to insert into.")]
        [Input("columnValues", "Dictionary of column names and their values to insert.")]
        [Input("conflictClause", "Optional conflict resolution clause (e.g., 'OR REPLACE', 'OR IGNORE').")]
        [Output("success", "True if the insert executed successfully, false otherwise.")]
        public static bool ExecuteInsert(this SqliteConnection connection, string tableName, Dictionary<string, object> columnValues, string conflictClause = "OR REPLACE")
        {
            if (!BH.Engine.SQLite.Query.ValidateInputParameters(connection, tableName, "execute insert"))
                return false;

            if (columnValues == null || !columnValues.Any())
            {
                BH.Engine.Base.Compute.RecordWarning("No column values provided for insert operation.");
                return false;
            }

            try
            {
                List<string> columnNames = columnValues.Keys.ToList();
                string columns = string.Join(", ", columnNames.Select(col => $"\"{col}\""));
                string placeholders = string.Join(", ", columnNames.Select(col => $"@{col}"));
                
                string insertSql = $"INSERT {conflictClause} INTO \"{tableName}\" ({columns}) VALUES ({placeholders})";

                Dictionary<string, object> parameters = new Dictionary<string, object>();
                foreach (KeyValuePair<string, object> columnValue in columnValues)
                {
                    parameters[$"@{columnValue.Key}"] = columnValue.Value;
                }

                return connection.ExecuteSql(insertSql, parameters, $"INSERT into table '{tableName}'");
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to build INSERT statement for table '{tableName}': {ex.Message}");
                return false;
            }
        }

        /***************************************************/
    }
}
