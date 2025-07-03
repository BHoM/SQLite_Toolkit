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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using BH.oM.SQLite.Objects;
using BH.oM.SQLite.Requests;
using Microsoft.Data.Sqlite;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private IEnumerable<object> ReadCustomSqlRequest(CustomSqlRequest customRequest)
        {
            List<object> result = new List<object>();

            QueryResult queryResult = ExecuteQuery(customRequest.SqlQuery, customRequest.Parameters, customRequest.TimeoutSeconds);
            
            if (queryResult.IsSuccess)
            {
                // For custom SQL requests, return the QueryResult object
                result.Add(queryResult);
            }
            else
            {
                BH.Engine.Base.Compute.RecordError($"Custom SQL query failed: {queryResult.ErrorMessage}");
            }

            return result;
        }

        private QueryResult ExecuteQuery(string sqlQuery, Dictionary<string, object> parameters = null, int timeoutSeconds = 0)
        {
            QueryResult result = new QueryResult
            {
                ExecutedQuery = sqlQuery,
                ExecutedAt = DateTime.Now
            };

            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "SQL query is null or empty.";
                BH.Engine.Base.Compute.RecordError("Cannot execute query: SQL query is null or empty.");
                return result;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                using (SqliteCommand command = m_Connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    
                    // Set timeout if specified
                    if (timeoutSeconds > 0)
                        command.CommandTimeout = timeoutSeconds;

                    // Add parameters if provided
                    if (parameters != null)
                    {
                        foreach (KeyValuePair<string, object> param in parameters)
                        {
                            SqliteParameter sqlParam = command.CreateParameter();
                            sqlParam.ParameterName = param.Key.StartsWith("@") ? param.Key : "@" + param.Key;
                            sqlParam.Value = param.Value ?? DBNull.Value;
                            command.Parameters.Add(sqlParam);
                        }
                    }

                    // Determine if this is a SELECT query or a modification query
                    string trimmedQuery = sqlQuery.Trim().ToUpper();
                    bool isSelectQuery = trimmedQuery.StartsWith("SELECT") || 
                                       trimmedQuery.StartsWith("WITH") || 
                                       trimmedQuery.StartsWith("PRAGMA");

                    if (isSelectQuery)
                    {
                        ExecuteSelectQuery(command, result);
                    }
                    else
                    {
                        ExecuteNonQuery(command, result);
                    }

                    result.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                BH.Engine.Base.Compute.RecordError($"Query execution failed: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.ElapsedMilliseconds / 1000.0; // Convert to seconds for BHoM conventions
            }

            return result;
        }

        private void ExecuteSelectQuery(SqliteCommand command, QueryResult result)
        {
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

        private void ExecuteNonQuery(SqliteCommand command, QueryResult result)
        {
            int affectedRows = command.ExecuteNonQuery();
            result.AffectedRows = affectedRows;
            result.RowCount = 0; // Non-query doesn't return rows
            
            // Add affected rows to metadata
            result.Metadata["AffectedRows"] = affectedRows;
        }

        /***************************************************/
    }
} 