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
using System.Linq;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Create Methods                            ****/
        /***************************************************/

        // Create method for multiple TableData objects
        private bool Create(IEnumerable<TableData> tableDatas)
        {
            if (tableDatas == null)
                return false;

            bool success = true;
            foreach (TableData tableData in tableDatas)
            {
                success &= Create(tableData);
            }
            return success;
        }

        private bool Create(TableData tableData)
        {
            // Create method for TableData objects - creates table and inserts data
            if (tableData == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create table data: TableData is null.");
                return false;
            }

            try
            {
                // Create the table if requested
                if (tableData.CreateTableIfNotExists)
                {
                    bool tableCreated = Create(tableData.Schema);
                    if (!tableCreated)
                    {
                        BH.Engine.Base.Compute.RecordError($"Failed to create table for TableData: {tableData.Schema.Name}");
                        return false;
                    }
                }

                // Insert data if provided
                if (tableData.Rows != null && tableData.Rows.Any())
                {
                    return InsertTableDataRows(tableData);
                }

                return true;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to create TableData for {tableData.Schema.Name}: {ex.Message}");
                return false;
            }
        }

        /***************************************************/
        /**** Private Helper Methods                   ****/
        /***************************************************/

        private bool InsertTableDataRows(TableData tableData)
        {
            if (tableData?.Rows == null || !tableData.Rows.Any())
                return true;

            try
            {
                string tableName = tableData.Schema.Name;
                var firstRow = tableData.Rows.First();
                var columnNames = firstRow.Keys.ToList();

                string columns = string.Join(", ", columnNames.Select(col => $"\"{col}\""));
                string placeholders = string.Join(", ", columnNames.Select(col => $"@{col}"));

                string conflictClause = GetConflictClause(tableData.TableConfig.ConflictResolution);
                string insertSql = $"INSERT {conflictClause} INTO \"{tableName}\" ({columns}) VALUES ({placeholders})";

                using (Microsoft.Data.Sqlite.SqliteCommand command = new Microsoft.Data.Sqlite.SqliteCommand(insertSql, m_Connection))
                {
                    // Add parameters for all columns
                    foreach (string column in columnNames)
                    {
                        command.Parameters.Add($"@{column}", Microsoft.Data.Sqlite.SqliteType.Text);
                    }

                    // Process rows in batches
                    int batchSize = tableData.TableConfig.BatchSize;
                    var batches = tableData.Rows
                        .Select((row, index) => new { row, index })
                        .GroupBy(x => x.index / batchSize)
                        .Select(g => g.Select(x => x.row));

                    foreach (var batch in batches)
                    {
                        foreach (var row in batch)
                        {
                            // Set parameter values
                            foreach (string column in columnNames)
                            {
                                object value = row.ContainsKey(column) ? row[column] : DBNull.Value;
                                command.Parameters[$"@{column}"].Value = value ?? DBNull.Value;
                            }

                            command.ExecuteNonQuery();
                        }
                    }
                }

                BH.Engine.Base.Compute.RecordNote($"Successfully inserted {tableData.Rows.Count} rows into table: {tableName}");
                return true;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to insert data into {tableData.Schema.Name}: {ex.Message}");
                return false;
            }
        }

        /***************************************************/

    }
}