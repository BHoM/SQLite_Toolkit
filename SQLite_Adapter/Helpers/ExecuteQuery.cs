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

using BH.oM.SQLite;
using BH.oM.SQLite.Objects;
using BH.Engine.SQLite;
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

        private QueryResult ExecuteQuery(SqlOperation operation, string tableName, FilterCommand filterCommand)
        {
            QueryResult result = new QueryResult
            {
                ExecutedAt = DateTime.Now
            };
            
            if (string.IsNullOrWhiteSpace(tableName))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Table name is null or empty.";
                BH.Engine.Base.Compute.RecordError($"Cannot perform {operation} operation: table name is null or empty.");
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

                // Build the appropriate SQL query based on operation
                string sql = BuildQueryForOperation(operation, tableName, filterCommand);
                if (string.IsNullOrEmpty(sql))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"Failed to construct {operation} query.";
                    BH.Engine.Base.Compute.RecordError($"Failed to construct {operation} query.");
                    return result;
                }

                result.ExecutedQuery = sql;

                // Determine NaN handling strategy
                NaNHandling nanHandling = m_sqliteSettings?.NaNHandling ?? NaNHandling.ConvertToNull;

                // Execute the query with appropriate handling based on operation type
                using (SqliteCommand command = new SqliteCommand(sql, m_Connection))
                {
                    // Apply filter parameters if available
                    if (filterCommand != null && filterCommand.Parameters != null)
                    {
                        ApplyFilterParameters(command, filterCommand, nanHandling);
                    }

                    if (operation == SqlOperation.Delete)
                    {
                        // For DELETE operations, execute non-query and return affected row count
                        int affectedRows = command.ExecuteNonQuery();
                        result.RowCount = affectedRows;
                        result.IsSuccess = true;
                        BH.Engine.Base.Compute.RecordNote($"Executed {operation} query on table '{tableName}'. Affected {result.RowCount} rows.");
                    }
                    else
                    {
                        // For SELECT and COUNT operations, read data
                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            // Get column names
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                result.ColumnNames.Add(reader.GetName(i));
                            }

                            // Get schema information for type conversion (only for SELECT operations)
                            Dictionary<string, Type> columnTypes = new Dictionary<string, Type>();
                            if (operation == SqlOperation.Select)
                            {
                                columnTypes = GetTableSchema(tableName);
                                if (columnTypes.Count > 0)
                                {
                                    BH.Engine.Base.Compute.RecordNote($"Schema-based type conversion: Found {columnTypes.Count} column types for table '{tableName}'");
                                }
                            }

                            // Read data rows
                            while (reader.Read())
                            {
                                Dictionary<string, object> row = new Dictionary<string, object>();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    
                                    // Apply type conversion if schema information is available and this is a SELECT operation
                                    if (operation == SqlOperation.Select && columnTypes.ContainsKey(columnName))
                                    {
                                        Type targetType = columnTypes[columnName];
                                        value = Convert.Value(value, targetType, nanHandling);
                                    }
                                    
                                    row[columnName] = value;
                                }
                                
                                result.Data.Add(row);
                            }

                            result.RowCount = result.Data.Count;
                        }

                        result.IsSuccess = true;
                        BH.Engine.Base.Compute.RecordNote($"Retrieved {result.RowCount} rows from table '{tableName}' using {operation} operation with filter '{filterCommand?.FilterType ?? "Unknown"}'.");
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Error during {operation} operation on table '{tableName}': {ex.Message}";
                BH.Engine.Base.Compute.RecordError($"Error during {operation} operation on table '{tableName}': {ex.Message}");
            }

            return result;
        }

        /***************************************************/
        /**** Private Helper Methods                   ****/
        /***************************************************/

        private string BuildQueryForOperation(SqlOperation operation, string tableName, FilterCommand filterCommand)
        {
            switch (operation)
            {
                case SqlOperation.Select:
                    return BH.Engine.SQLite.Compute.SelectQuery(tableName, filterCommand);
                case SqlOperation.Delete:
                    return BH.Engine.SQLite.Compute.DeleteQuery(tableName, filterCommand);
                case SqlOperation.Count:
                    return BH.Engine.SQLite.Compute.CountQuery(tableName, filterCommand);
                default:
                    BH.Engine.Base.Compute.RecordError($"Unsupported SQL operation: {operation}");
                    return null;
            }
        }

        /***************************************************/
    }
}

