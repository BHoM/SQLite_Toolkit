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

        private QueryResult ExecuteFilteredQuery(string tableName, FilterResult filterResult)
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
                if (!m_Connection.TableExists(tableName))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"Table '{tableName}' does not exist in the database.";
                    BH.Engine.Base.Compute.RecordWarning($"Table '{tableName}' does not exist in the database.");
                    return result;
                }

                // Build the SELECT query
                string sql = BH.Engine.SQLite.Create.SelectQuery(tableName, filterResult);
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
                        BH.Engine.SQLite.Compute.ApplyFilterParameters(command, filterResult);
                    }

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        // Get column names
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            result.ColumnNames.Add(reader.GetName(i));
                        }

                        // Read data rows
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
    }
}
