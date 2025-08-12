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

        [Description("Executes a SQL statement with ordered parameters against a SQLite connection.")]
        [Input("connection", "The SQLite connection to execute the statement against.")]
        [Input("sql", "The SQL statement to execute with positional parameter placeholders.")]
        [Input("parameterValues", "List of parameter values in the order they appear in the SQL.")]
        [Input("operationName", "Optional name for the operation for error reporting.")]
        [Output("success", "True if the SQL executed successfully, false otherwise.")]
        public static bool ExecuteSqlWithOrderedParameters(this SqliteConnection connection, string sql, List<object> parameterValues, string operationName = "SQL operation")
        {
            if (connection == null)
            {
                BH.Engine.Base.Compute.RecordError($"Cannot execute {operationName}: no database connection.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                BH.Engine.Base.Compute.RecordError($"Cannot execute {operationName}: SQL statement is null or empty.");
                return false;
            }

            try
            {
                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    // Add parameters if provided
                    if (parameterValues != null && parameterValues.Any())
                    {
                        for (int i = 0; i < parameterValues.Count; i++)
                        {
                            object sqliteValue = ConvertToSqliteValue(parameterValues[i]);
                            command.Parameters.Add($"@param{i}", Microsoft.Data.Sqlite.SqliteType.Text);
                            command.Parameters[$"@param{i}"].Value = sqliteValue;
                        }
                    }

                    int rowsAffected = command.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to execute {operationName}: {ex.Message}");
                return false;
            }
        }

        /***************************************************/
    }
}
