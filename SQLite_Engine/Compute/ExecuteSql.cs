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

        [Description("Executes a parameterised SQL statement against a SQLite connection with comprehensive error handling and automatic value conversion. \n" +
            "This method provides secure SQL execution by converting .NET types to SQLite-compatible values and preventing SQL injection through parameterisation.")]
        [Input("connection", "Active SQLite database connection in an open state with appropriate permissions for the intended SQL operation (SELECT, INSERT, UPDATE, DELETE, etc.).")]
        [Input("sql", "The SQL statement to execute, which may contain named parameter placeholders (e.g., '@paramName'). The statement will be executed as a non-query operation.")]
        [Input("parameters", "Optional dictionary mapping parameter names to their values. Parameter names should include the '@' prefix. Values are automatically converted to SQLite-compatible types.")]
        [Input("operationName", "Optional descriptive name for the operation used in error reporting and logging. Helps identify the source of database errors during debugging.")]
        [Output("success", "True if the SQL statement executed successfully without throwing exceptions, false if execution failed due to database errors, connection issues, or parameter problems.")]
        public static bool ExecuteSql(this SqliteConnection connection, string sql, Dictionary<string, object> parameters = null, string operationName = "SQL operation")
        {
            if (!BH.Engine.SQLite.Query.ValidateInputParameters(connection, $"execute {operationName}"))
                return false;

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
                    if (parameters != null && parameters.Any())
                    {
                        foreach (KeyValuePair<string, object> parameter in parameters)
                        {
                            object sqliteValue = ConvertToSqliteValue(parameter.Value);
                            command.Parameters.AddWithValue(parameter.Key, sqliteValue);
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