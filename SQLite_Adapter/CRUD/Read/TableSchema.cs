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
using System.Linq;
using System.Text;
using BH.oM.SQLite;
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

        private bool TableExists(string tableName)
        {
            try
            {
                using (SqliteCommand command = m_Connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT COUNT(*) 
                        FROM sqlite_master 
                        WHERE type = 'table' AND name = @tableName;";

                    command.Parameters.AddWithValue("@tableName", tableName);

                    object result = command.ExecuteScalar();
                    int count = System.Convert.ToInt32(result);
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to check if table '{tableName}' exists: {ex.Message}");
                return false;
            }
        }

        private List<ColumnDefinition> GetColumnDefinitions(string tableName)
        {
            List<ColumnDefinition> columns = new List<ColumnDefinition>();

            try
            {
                using (SqliteCommand command = m_Connection.CreateCommand())
                {
                    command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // PRAGMA table_info returns: cid, name, type, notnull, dflt_value, pk
                            ColumnDefinition column = new ColumnDefinition
                            {
                                Position = reader.GetInt32(0),        // cid
                                Name = reader.GetString(1),           // name
                                AllowNull = reader.GetInt32(3) == 0,  // notnull (0 = nullable, 1 = not null)
                                IsPrimaryKey = reader.GetInt32(5) > 0, // pk
                                DefaultValue = reader.IsDBNull(4) ? null : reader.GetString(4) // dflt_value
                            };

                            // Parse data type
                            string typeString = reader.GetString(2).ToUpper(); // type
                            column.DataType = ParseSqliteDataType(typeString);

                            // Extract max length from type string if present
                            if (typeString.Contains("(") && typeString.Contains(")"))
                            {
                                string lengthPart = typeString.Substring(typeString.IndexOf("(") + 1);
                                lengthPart = lengthPart.Substring(0, lengthPart.IndexOf(")"));
                                if (int.TryParse(lengthPart, out int maxLength))
                                {
                                    column.MaxLength = maxLength;
                                }
                            }

                            columns.Add(column);
                        }
                    }
                }

                // Check for auto-increment on primary key columns
                CheckAutoIncrement(tableName, columns);
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to get column definitions for table '{tableName}': {ex.Message}");
            }

            return columns;
        }

        private SqliteDataType ParseSqliteDataType(string typeString)
        {
            if (string.IsNullOrWhiteSpace(typeString))
                return SqliteDataType.TEXT;

            typeString = typeString.ToUpper();

            if (typeString.Contains("INT"))
                return SqliteDataType.INTEGER;
            else if (typeString.Contains("REAL") || typeString.Contains("FLOAT") || typeString.Contains("DOUBLE"))
                return SqliteDataType.REAL;
            else if (typeString.Contains("BLOB"))
                return SqliteDataType.BLOB;
            else if (typeString.Contains("NUMERIC") || typeString.Contains("DECIMAL"))
                return SqliteDataType.NUMERIC;
            else
                return SqliteDataType.TEXT;
        }

        private void CheckAutoIncrement(string tableName, List<ColumnDefinition> columns)
        {
            try
            {
                ColumnDefinition pkColumn = columns.Find(c => c.IsPrimaryKey && c.DataType == SqliteDataType.INTEGER);
                if (pkColumn != null)
                {
                    using (SqliteCommand command = m_Connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT sql 
                            FROM sqlite_master 
                            WHERE type = 'table' AND name = @tableName;";

                        command.Parameters.AddWithValue("@tableName", tableName);

                        object result = command.ExecuteScalar();
                        if (result != null)
                        {
                            string createSql = result.ToString().ToUpper();
                            if (createSql.Contains("AUTOINCREMENT"))
                            {
                                pkColumn.IsAutoIncrement = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to check auto-increment for table '{tableName}': {ex.Message}");
            }
        }

        private string GetCreateStatement(string tableName)
        {
            try
            {
                using (SqliteCommand command = m_Connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT sql 
                        FROM sqlite_master 
                        WHERE type = 'table' AND name = @tableName;";

                    command.Parameters.AddWithValue("@tableName", tableName);

                    object result = command.ExecuteScalar();
                    return result?.ToString();
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to get CREATE statement for table '{tableName}': {ex.Message}");
                return null;
            }
        }

        private List<IndexDefinition> GetIndexDefinitions(string tableName)
        {
            List<IndexDefinition> indexes = new List<IndexDefinition>();

            try
            {
                using (SqliteCommand command = m_Connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT name, [unique], sql
                        FROM sqlite_master 
                        WHERE type = 'index' 
                        AND tbl_name = @tableName
                        AND name NOT LIKE 'sqlite_autoindex_%'
                        ORDER BY name;";

                    command.Parameters.AddWithValue("@tableName", tableName);

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // sqlite_master columns: name, unique, sql
                            IndexDefinition index = new IndexDefinition
                            {
                                IndexName = reader.GetString(0),        // name
                                TableName = tableName,
                                IsUnique = reader.GetInt32(1) == 1,     // unique
                                CreateStatement = reader.IsDBNull(2) ? null : reader.GetString(2) // sql
                            };

                            indexes.Add(index);
                        }
                    }
                }

                // Get column information for each index
                foreach (IndexDefinition index in indexes)
                {
                    GetIndexColumns(index);
                    if (string.IsNullOrEmpty(index.CreateStatement))
                    {
                        GenerateIndexCreateStatement(index);
                    }
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to get index definitions for table '{tableName}': {ex.Message}");
            }

            return indexes;
        }

        private void GetIndexColumns(IndexDefinition index)
        {
            try
            {
                using (SqliteCommand command = m_Connection.CreateCommand())
                {
                    command.CommandText = $"PRAGMA index_info(\"{index.IndexName}\");";

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // PRAGMA index_info returns: seqno, cid, name
                            string columnName = reader.GetString(2); // name (column 2)
                            index.Columns.Add(columnName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to get columns for index '{index.IndexName}': {ex.Message}");
            }
        }

        private void GenerateIndexCreateStatement(IndexDefinition index)
        {
            if (index.Columns.Any())
            {
                StringBuilder sql = new StringBuilder();
                sql.Append("CREATE ");

                if (index.IsUnique)
                    sql.Append("UNIQUE ");

                sql.Append($"INDEX \"{index.IndexName}\" ON \"{index.TableName}\" (");

                List<string> quotedColumns = index.Columns.Select(col => $"\"{col}\"").ToList();
                sql.Append(string.Join(", ", quotedColumns));

                sql.Append(");");

                index.CreateStatement = sql.ToString();
            }
        }

        private List<string> GetForeignKeyDefinitions(string tableName)
        {
            List<string> foreignKeys = new List<string>();

            try
            {
                using (SqliteCommand command = m_Connection.CreateCommand())
                {
                    command.CommandText = $"PRAGMA foreign_key_list(\"{tableName}\");";

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // PRAGMA foreign_key_list returns: id, seq, table, from, to, on_update, on_delete, match
                            string fromColumn = reader.GetString(3);   // from
                            string toTable = reader.GetString(2);      // table
                            string toColumn = reader.GetString(4);     // to

                            string fkDefinition = $"FOREIGN KEY (\"{fromColumn}\") REFERENCES \"{toTable}\" (\"{toColumn}\")";

                            if (!reader.IsDBNull(5)) // on_update
                            {
                                string onUpdate = reader.GetString(5);
                                if (!string.IsNullOrEmpty(onUpdate))
                                    fkDefinition += $" ON UPDATE {onUpdate}";
                            }

                            if (!reader.IsDBNull(6)) // on_delete
                            {
                                string onDelete = reader.GetString(6);
                                if (!string.IsNullOrEmpty(onDelete))
                                    fkDefinition += $" ON DELETE {onDelete}";
                            }

                            foreignKeys.Add(fkDefinition);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to get foreign key definitions for table '{tableName}': {ex.Message}");
            }

            return foreignKeys;
        }

        private void GetTableStatistics(string tableName, TableSchema schema)
        {
            try
            {
                using (SqliteCommand command = m_Connection.CreateCommand())
                {
                    command.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\";";
                    object result = command.ExecuteScalar();
                    if (result != null)
                    {
                        schema.RowCount = System.Convert.ToInt64(result);
                    }
                }
            }
            catch (Exception ex)
            {
                // If we can't get row count, just continue
                BH.Engine.Base.Compute.RecordWarning($"Could not get row count for table {tableName}: {ex.Message}");
            }
        }

        private void CheckTableProperties(TableSchema schema)
        {
            if (schema.CreateStatement != null)
            {
                string createSql = schema.CreateStatement.ToUpper();
                schema.WithoutRowId = createSql.Contains("WITHOUT ROWID");
            }
        }

    }
}