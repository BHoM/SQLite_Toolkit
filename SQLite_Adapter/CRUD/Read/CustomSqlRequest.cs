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
using System.Linq; // Added for .Select() and .Where()
using System.Text.RegularExpressions; // Added for Regex

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
            
            // Always return the QueryResult object so callers can inspect success/failure
            result.Add(queryResult);
            
            if (!queryResult.IsSuccess)
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

            // Check if connection is available
            if (m_Connection == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "No database connection available. Please open a connection first.";
                BH.Engine.Base.Compute.RecordError("Cannot execute query: No database connection available.");
                return result;
            }

            // Check connection state
            if (m_Connection.State != System.Data.ConnectionState.Open)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Database connection is not open. Please open a connection first.";
                BH.Engine.Base.Compute.RecordError("Cannot execute query: Database connection is not open.");
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
                            try
                            {
                                SqliteParameter sqlParam = command.CreateParameter();
                                sqlParam.ParameterName = param.Key.StartsWith("@") ? param.Key : "@" + param.Key;
                                sqlParam.Value = param.Value ?? DBNull.Value;
                                command.Parameters.Add(sqlParam);
                            }
                            catch (Exception ex)
                            {
                                result.IsSuccess = false;
                                result.ErrorMessage = $"Failed to add parameter '{param.Key}': {ex.Message}";
                                BH.Engine.Base.Compute.RecordError($"Parameter binding failed for '{param.Key}': {ex.Message}");
                                return result;
                            }
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
            catch (Microsoft.Data.Sqlite.SqliteException sqlEx)
            {
                result.IsSuccess = false;
                result.ErrorMessage = GetUserFriendlySqliteErrorMessage(sqlEx, sqlQuery);
                BH.Engine.Base.Compute.RecordError($"SQLite query execution failed: {result.ErrorMessage}");
            }
            catch (System.Data.Common.DbException dbEx)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Database error: {dbEx.Message}";
                BH.Engine.Base.Compute.RecordError($"Database query execution failed: {dbEx.Message}");
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Unexpected error during query execution: {ex.Message}";
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
            try
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
            catch (Exception ex)
            {
                // This should not happen as the main try-catch in ExecuteQuery should handle it
                // But we'll add it as a safety net
                BH.Engine.Base.Compute.RecordError($"Unexpected error during SELECT query execution: {ex.Message}");
                throw; // Re-throw to be caught by the main exception handler
            }
        }

        private void ExecuteNonQuery(SqliteCommand command, QueryResult result)
        {
            try
            {
                int affectedRows = command.ExecuteNonQuery();
                result.AffectedRows = affectedRows;
                result.RowCount = 0; // Non-query doesn't return rows
                
                // Add affected rows to metadata
                result.Metadata["AffectedRows"] = affectedRows;
            }
            catch (Exception ex)
            {
                // This should not happen as the main try-catch in ExecuteQuery should handle it
                // But we'll add it as a safety net
                BH.Engine.Base.Compute.RecordError($"Unexpected error during non-query execution: {ex.Message}");
                throw; // Re-throw to be caught by the main exception handler
            }
        }

        private string GetUserFriendlySqliteErrorMessage(Microsoft.Data.Sqlite.SqliteException sqlEx, string originalSqlQuery)
        {
            string message = sqlEx.Message.ToLower();
            
            if (message.Contains("no such table"))
            {
                string tableName = ExtractTableNameFromError(originalSqlQuery);
                if (!string.IsNullOrEmpty(tableName))
                {
                    return $"Table Error: '{tableName}'. Table does not exist. Please check the table name and ensure it has been created.";
                }
                return "Table Error: Table does not exist. Please check the table name and ensure it has been created.";
            }
            
            if (message.Contains("no such column"))
            {
                string columnName = ExtractColumnNameFromError(sqlEx.Message, originalSqlQuery);
                if (!string.IsNullOrEmpty(columnName))
                {
                    return $"Column Error: '{columnName}'. Column does not exist. Please check the column name in your query.";
                }
                return "Column Error: Column does not exist. Please check the column name in your query.";
            }
            
            if (message.Contains("syntax error"))
            {
                string snippet = GetQuerySnippet(originalSqlQuery);
                return $"SQL Syntax Error: There is a syntax error in your SQL query. Query: {snippet}";
            }
            
            if (message.Contains("constraint"))
            {
                string constraintType = ExtractConstraintType(sqlEx.Message);
                return $"Constraint Error: {constraintType}. Database constraint violation. Check that your data meets the table's requirements (e.g., unique constraints, foreign keys).";
            }
            
            if (message.Contains("parameter"))
            {
                string paramName = ExtractParameterName(sqlEx.Message, originalSqlQuery);
                return $"Parameter Error: '{paramName}'. There was an issue with a query parameter. Please check the parameter name and type.";
            }
            
            if (message.Contains("busy"))
                return "Database Error: Database is busy. Another operation may be in progress. Please try again.";
            
            if (message.Contains("locked"))
                return "Database Error: Database is locked. Another process may be using the database.";
            
            if (message.Contains("readonly"))
                return "Database Error: Database is read-only. Cannot perform write operations.";
            
            if (message.Contains("corrupt"))
                return "Database Error: Database file is corrupted. The database may need to be repaired or restored from backup.";
            
            if (message.Contains("not found"))
                return "Database Error: Database file not found. Please check the file path.";
            
            if (message.Contains("full"))
                return "Database Error: Database is full. Check available disk space.";
            
            if (message.Contains("cant open"))
                return "Database Error: Cannot open database. Check file permissions and ensure the file is not locked by another process.";
            
            if (message.Contains("mismatch"))
                return "Type Error: Data type mismatch. Check that your parameter values match the expected column types.";
            
            if (message.Contains("too big"))
                return "Query Error: Query result is too large. Consider using LIMIT or filtering the query.";
            
            // Fallback to original message if no specific case matches
            return $"SQLite Error: {sqlEx.Message}";
        }

        private string ExtractTableNameFromError(string sqlQuery)
        {
            try
            {
                // Use the original query for case preservation, but uppercase version for parsing
                string originalQuery = sqlQuery.Trim();
                string query = originalQuery.ToUpper();
                
                // Handle SELECT statements
                if (query.StartsWith("SELECT"))
                {
                    // Look for FROM clause
                    int fromIndex = query.IndexOf(" FROM ");
                    if (fromIndex > 0)
                    {
                        string afterFrom = originalQuery.Substring(fromIndex + 6).Trim();
                        // Find the end of the table name (space, semicolon, or other clause)
                        int endIndex = afterFrom.IndexOfAny(new char[] { ' ', ';', '(', ')' });
                        if (endIndex > 0)
                        {
                            string tableName = afterFrom.Substring(0, endIndex).Trim();
                            // Remove quotes if present
                            tableName = tableName.Trim('"', '[', ']', '`');
                            return tableName;
                        }
                        else
                        {
                            string tableName = afterFrom.Trim();
                            // Remove quotes if present
                            tableName = tableName.Trim('"', '[', ']', '`');
                            return tableName;
                        }
                    }
                }
                
                // Handle INSERT statements
                if (query.StartsWith("INSERT"))
                {
                    // Look for INTO clause
                    int intoIndex = query.IndexOf(" INTO ");
                    if (intoIndex > 0)
                    {
                        string afterInto = originalQuery.Substring(intoIndex + 6).Trim();
                        // Find the end of the table name
                        int endIndex = afterInto.IndexOfAny(new char[] { ' ', '(', ')' });
                        if (endIndex > 0)
                        {
                            string tableName = afterInto.Substring(0, endIndex).Trim();
                            tableName = tableName.Trim('"', '[', ']', '`');
                            return tableName;
                        }
                        else
                        {
                            string tableName = afterInto.Trim();
                            tableName = tableName.Trim('"', '[', ']', '`');
                            return tableName;
                        }
                    }
                }
                
                // Handle UPDATE statements
                if (query.StartsWith("UPDATE"))
                {
                    string afterUpdate = originalQuery.Substring(6).Trim();
                    // Find the end of the table name
                    int endIndex = afterUpdate.IndexOfAny(new char[] { ' ', ';' });
                    if (endIndex > 0)
                    {
                        string tableName = afterUpdate.Substring(0, endIndex).Trim();
                        tableName = tableName.Trim('"', '[', ']', '`');
                        return tableName;
                    }
                    else
                    {
                        string tableName = afterUpdate.Trim();
                        tableName = tableName.Trim('"', '[', ']', '`');
                        return tableName;
                    }
                }
                
                // Handle DELETE statements
                if (query.StartsWith("DELETE"))
                {
                    // Look for FROM clause
                    int fromIndex = query.IndexOf(" FROM ");
                    if (fromIndex > 0)
                    {
                        string afterFrom = originalQuery.Substring(fromIndex + 6).Trim();
                        // Find the end of the table name
                        int endIndex = afterFrom.IndexOfAny(new char[] { ' ', ';', 'W' }); // W for WHERE
                        if (endIndex > 0)
                        {
                            string tableName = afterFrom.Substring(0, endIndex).Trim();
                            tableName = tableName.Trim('"', '[', ']', '`');
                            return tableName;
                        }
                        else
                        {
                            string tableName = afterFrom.Trim();
                            tableName = tableName.Trim('"', '[', ']', '`');
                            return tableName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to extract table name from SQL query: {ex.Message}");
            }
            
            return string.Empty;
        }

        /// <summary>
        /// Extracts column name from SQLite error message or SQL query
        /// </summary>
        private string ExtractColumnNameFromError(string errorMessage, string sqlQuery)
        {
            // Try error message first
            if (errorMessage.Contains("no such column:"))
            {
                string[] parts = errorMessage.Split(new[] { "no such column:" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    string columnPart = parts[parts.Length - 1].Trim();
                    // Remove table prefix if present (e.g., TableName.ColumnName -> ColumnName)
                    if (columnPart.Contains("."))
                    {
                        columnPart = columnPart.Split('.')[1];
                    }
                    columnPart = columnPart.Trim('"', '[', ']', '`');
                    return columnPart;
                }
            }
            // Try to extract from SQL query (for SELECT ... FROM ...)
            try
            {
                string originalQuery = sqlQuery.Trim();
                string query = originalQuery.ToUpper();
                if (query.StartsWith("SELECT"))
                {
                    int selectIndex = query.IndexOf("SELECT");
                    int fromIndex = query.IndexOf(" FROM ");
                    if (selectIndex >= 0 && fromIndex > selectIndex)
                    {
                        string columnsPart = originalQuery.Substring(selectIndex + 6, fromIndex - (selectIndex + 6)).Trim();
                        // If multiple columns, return the first one that is not *
                        var columns = columnsPart.Split(',').Select(c => c.Trim()).Where(c => c != "*").ToList();
                        if (columns.Count > 0)
                        {
                            string col = columns[0];
                            col = col.Trim('"', '[', ']', '`');
                            return col;
                        }
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        /// <summary>
        /// Extracts a snippet of the query for error messages
        /// </summary>
        private string GetQuerySnippet(string sqlQuery)
        {
            if (string.IsNullOrWhiteSpace(sqlQuery))
                return "<empty query>";
            return sqlQuery.Length > 80 ? sqlQuery.Substring(0, 80) + "..." : sqlQuery;
        }

        /// <summary>
        /// Extracts constraint type from SQLite error message
        /// </summary>
        private string ExtractConstraintType(string errorMessage)
        {
            // Try to extract constraint type (e.g., UNIQUE, FOREIGN KEY, CHECK)
            string msg = errorMessage.ToUpper();
            if (msg.Contains("UNIQUE")) return "UNIQUE constraint";
            if (msg.Contains("FOREIGN KEY")) return "FOREIGN KEY constraint";
            if (msg.Contains("CHECK")) return "CHECK constraint";
            if (msg.Contains("NOT NULL")) return "NOT NULL constraint";
            return "Constraint violation";
        }

        /// <summary>
        /// Extracts parameter name from SQLite error message or SQL query
        /// </summary>
        private string ExtractParameterName(string errorMessage, string sqlQuery)
        {
            // Try error message first
            if (errorMessage.Contains(":"))
            {
                int idx = errorMessage.IndexOf(":");
                if (idx >= 0)
                {
                    string param = errorMessage.Substring(idx + 1).Trim();
                    param = param.Split(' ', '.', ',', ';')[0];
                    return param.Trim('@', '"', '[', ']', '`');
                }
            }
            // Try to extract from SQL query (look for @param)
            var matches = System.Text.RegularExpressions.Regex.Matches(sqlQuery, "@[a-zA-Z0-9_]+", System.Text.RegularExpressions.RegexOptions.None);
            if (matches.Count > 0)
            {
                return matches[0].Value.Trim('@');
            }
            return string.Empty;
        }

        /***************************************************/
    }
} 