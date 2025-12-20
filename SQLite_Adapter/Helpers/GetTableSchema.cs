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
        /**** Private Methods                           ****/
        /***************************************************/

        [Description("Retrieves the schema information for a table from the __Schema system table, including column names and their .NET types.")]
        [Input("tableName", "The name of the table to retrieve schema information for.")]
        [Output("columnTypes", "Dictionary mapping column names to their corresponding .NET Types. Empty dictionary if no schema found or error occurs.")]
        private Dictionary<string, Type> GetTableSchema(string tableName)
        {
            Dictionary<string, Type> columnTypes = new Dictionary<string, Type>();

            try
            {
                // Execute the query without type conversion to avoid recursion
                string sqlQuery = "SELECT ColumnName, NetTypeName FROM __Schema WHERE TableName = @TableName";
                Dictionary<string, object> parameters = new Dictionary<string, object> { { "@TableName", tableName } };
                QueryResult schemaResult = ExecuteQueryWithoutTypeConversion(sqlQuery, parameters);
                
                if (schemaResult.IsSuccess && schemaResult.Data.Count > 0)
                {
                    foreach (Dictionary<string, object> row in schemaResult.Data)
                    {
                        if (row.ContainsKey("ColumnName") && row.ContainsKey("NetTypeName"))
                        {
                            string columnName = row["ColumnName"]?.ToString();
                            string netTypeName = row["NetTypeName"]?.ToString();
                            
                            if (!string.IsNullOrWhiteSpace(columnName) && !string.IsNullOrWhiteSpace(netTypeName))
                            {
                                Type columnType = Convert.StringToType(netTypeName);
                                if (columnType != null)
                                {
                                    columnTypes[columnName] = columnType;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to retrieve schema for table '{tableName}' using Pull method: {ex.Message}");
            }

            return columnTypes;
        }

        /***************************************************/

        [Description("Executes a SQL query without applying type conversion to avoid recursion when retrieving schema information.")]
        [Input("sqlQuery", "The SQL query to execute.")]
        [Input("parameters", "Optional parameters for the SQL query.")]
        [Output("result", "The QueryResult containing the raw data without type conversion.")]
        private QueryResult ExecuteQueryWithoutTypeConversion(string sqlQuery, Dictionary<string, object> parameters = null)
        {
            QueryResult result = new QueryResult
            {
                ExecutedQuery = sqlQuery,
                ExecutedAt = DateTime.Now,
                IsSuccess = false
            };

            try
            {
                using (SqliteCommand command = new SqliteCommand(sqlQuery, m_Connection))
                {
                    // Add parameters
                    if (parameters != null)
                    {
                        foreach (KeyValuePair<string, object> param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        // Get column names
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            result.ColumnNames.Add(reader.GetName(i));
                        }

                        // Read data rows without type conversion
                        while (reader.Read())
                        {
                            Dictionary<string, object> row = new Dictionary<string, object>();
                            
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnName = reader.GetName(i);
                                object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                row[columnName] = value;
                            }
                            
                            result.Data.Add(row);
                        }

                        result.RowCount = result.Data.Count;
                        result.IsSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                result.IsSuccess = false;
            }

            return result;
        }

        /***************************************************/
    }
}

