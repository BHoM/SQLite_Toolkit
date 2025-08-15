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

using BH.oM.SQLite.Objects;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private QueryResult ExecuteQuery(string tableName, FilterResult filterResult)
        {
            QueryResult result = new QueryResult
            {
                ExecutedAt = DateTime.Now
            };

            if (string.IsNullOrWhiteSpace(tableName))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Table name is null or empty.";
                BH.Engine.Base.Compute.RecordError("Cannot perform filtered retrieval: table name is null or empty.");
                return result;
            }

            try
            {
                // Check if table exists
                if (!TableExists(m_Connection, tableName))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"Table '{tableName}' does not exist in the database.";
                    BH.Engine.Base.Compute.RecordWarning($"Table '{tableName}' does not exist in the database.");
                    return result;
                }

                // Build the SELECT query
                string sql = BH.Engine.SQLite.Compute.SelectQuery(tableName, filterResult);
                if (string.IsNullOrEmpty(sql))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Failed to construct SELECT query for filtered retrieval.";
                    BH.Engine.Base.Compute.RecordError("Failed to construct SELECT query for filtered retrieval.");
                    return result;
                }

                result.ExecutedQuery = sql;

                // Execute the query following the same pattern as CustomSqlRequest
                using (SqliteCommand command = new SqliteCommand(sql, m_Connection))
                {
                    // Apply filter parameters if available
                    if (filterResult != null)
                    {
                        ApplyFilterParameters(command, filterResult);
                    }

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        // Get column names
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            result.ColumnNames.Add(reader.GetName(i));
                        }

                        // Try to get schema information for type conversion
                        Dictionary<string, Type> columnTypes = GetTableSchema(tableName);
                        if (columnTypes.Count > 0)
                        {
                            BH.Engine.Base.Compute.RecordNote($"Schema-based type conversion: Found {columnTypes.Count} column types for table '{tableName}'");
                        }
                        else
                        {
                            BH.Engine.Base.Compute.RecordNote($"Schema-based type conversion: No schema information found for table '{tableName}'");
                        }

                        // Read data rows
                        while (reader.Read())
                        {
                            Dictionary<string, object> row = new Dictionary<string, object>();
                            
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnName = reader.GetName(i);
                                object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                
                                // Apply type conversion if schema information is available
                                if (value != null && columnTypes.ContainsKey(columnName))
                                {
                                    Type targetType = columnTypes[columnName];
                                    value = Convert.Value(value, targetType);
                                }
                                
                                row[columnName] = value;
                            }
                            
                            result.Data.Add(row);
                        }

                        result.RowCount = result.Data.Count;
                    }
                }

                result.IsSuccess = true;
                BH.Engine.Base.Compute.RecordNote($"Retrieved {result.RowCount} rows from table '{tableName}' using filter '{filterResult?.FilterType ?? "Unknown"}'.");
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Error during filtered retrieval from table '{tableName}': {ex.Message}";
                BH.Engine.Base.Compute.RecordError($"Error during filtered retrieval from table '{tableName}': {ex.Message}");
            }

            return result;
        }

        /***************************************************/
        /**** Private Helper Methods                   ****/
        /***************************************************/

        private static bool ApplyFilterParameters(SqliteCommand command, FilterResult filterResult)
        {
            if (command == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot apply filter parameters: command is null.");
                return false;
            }

            if (filterResult == null || filterResult.Parameters == null)
            {
                // No parameters to apply - this is valid for queries without filters
                return true;
            }

            foreach (KeyValuePair<string, object> parameter in filterResult.Parameters)
            {
                string paramName = parameter.Key;
                object paramValue = parameter.Value;

                // Ensure parameter name starts with @
                if (!paramName.StartsWith("@"))
                {
                    paramName = "@" + paramName;
                }

                // Convert value to appropriate SQLite type
                object sqliteValue = Convert.Value(paramValue);
                
                command.Parameters.AddWithValue(paramName, sqliteValue);
            }

            BH.Engine.Base.Compute.RecordNote($"Applied {filterResult.Parameters.Count} parameters to SQL command.");
            return true;
        }

        /***************************************************/
    }
}
