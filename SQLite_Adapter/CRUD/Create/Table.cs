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

        protected bool Create(Table table)
        {
            // Create method for Table objects - creates table and inserts data
            if (table == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create table data: Table is null.");
                return false;
            }

            try
            {
                // Create the table if requested
                if (table.CreateTableIfNotExists)
                {
                    bool tableCreated = Create(table.Schema);
                    if (!tableCreated)
                    {
                        BH.Engine.Base.Compute.RecordError($"Failed to create table for Table: {table.Schema.Name}");
                        return false;
                    }
                }

                // Insert data if provided
                if (table.Rows != null && table.Rows.Any())
                {
                    return InsertTableDataRows(table);
                }

                return true;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to create Table for {table.Schema.Name}: {ex.Message}");
                return false;
            }
        }

        /***************************************************/
        /**** Private Helper Methods                   ****/
        /***************************************************/

        private bool InsertTableDataRows(Table table)
        {
            if (table?.Rows == null || !table.Rows.Any())
                return true;

            try
            {
                string tableName = table.Schema.Name;
                Dictionary<string, object> firstRow = table.Rows.First();
                List<string> columnNames = firstRow.Keys.ToList();

                string conflictClause = GetConflictClause(table.TableConfig.ConflictResolution);

                // Process rows in batches
                int batchSize = table.TableConfig.BatchSize;
                List<List<Dictionary<string, object>>> batches = table.Rows
                    .Select((row, index) => new { row, index })
                    .GroupBy(x => x.index / batchSize)
                    .Select(g => g.Select(x => x.row).ToList())
                    .ToList();

                foreach (List<Dictionary<string, object>> batch in batches)
                {
                    foreach (Dictionary<string, object> row in batch)
                    {
                        // Use the shared ExecuteInsert method for each row
                        bool success = BH.Engine.SQLite.Compute.ExecuteInsert(m_Connection, tableName, row, conflictClause);
                        if (!success)
                        {
                            BH.Engine.Base.Compute.RecordWarning($"Failed to insert a row into table '{tableName}'.");
                        }
                    }
                }

                BH.Engine.Base.Compute.RecordNote($"Successfully processed {table.Rows.Count} rows for table: {tableName}");
                return true;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to insert data into {table.Schema.Name}: {ex.Message}");
                return false;
            }
        }

        /***************************************************/

    }
}