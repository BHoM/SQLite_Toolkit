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

using BH.oM.Base;
using BH.oM.Base.Attributes;
using BH.oM.SQLite.Commands;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        [Description("Executes a parameterised INSERT statement against a SQLite connection.")]
        [Input("connection", "The SQLite connection to execute the statement against.")]
        [Input("tableName", "The name of the table to insert into.")]
        [Input("columnValues", "Dictionary of column names and their values to insert.")]
        [Input("conflictClause", "Optional conflict resolution clause (e.g., 'OR REPLACE', 'OR IGNORE').")]
        [Output("success", "True if the insert executed successfully, false otherwise.")]
        private bool Insert(SqliteConnection connection, string tableName, Dictionary<string, object> columnValues, string conflictClause = "OR REPLACE")
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                BH.Engine.Base.Compute.RecordError("Cannot execute insert: table name is null or empty.");
                return false;
            }

            if (columnValues == null || !columnValues.Any())
            {
                BH.Engine.Base.Compute.RecordWarning("No column values provided for insert operation.");
                return false;
            }

            try
            {
                // Use Engine method to generate the command
                SQLCommand command = BH.Engine.SQLite.Compute.InsertCommand(tableName, columnValues, conflictClause);
                if (command == null)
                    return false;

                // Execute the command using the existing ExecuteCommand method
                Output<List<object>, bool> result = ExecuteCommand(command);
                return result.Item2;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to execute INSERT statement for table '{tableName}': {ex.Message}");
                return false;
            }
        }

        /***************************************************/
    }
}

