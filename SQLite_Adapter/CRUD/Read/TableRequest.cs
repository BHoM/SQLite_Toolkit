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
using BH.oM.Base;
using BH.oM.SQLite.Objects;
using BH.oM.SQLite.Requests;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private IEnumerable<object> ReadTableRequest(TableRequest tableRequest)
        {
            List<object> result = new List<object>();

            QueryResult queryResult = ExecuteTableQuery(tableRequest);
            
            if (queryResult.IsSuccess)
            {
                result.Add(queryResult);
            }
            else
            {
                BH.Engine.Base.Compute.RecordError($"Table query failed: {queryResult.ErrorMessage}");
            }

            return result;
        }

        private QueryResult ExecuteTableQuery(TableRequest tableRequest)
        {
            if (string.IsNullOrEmpty(tableRequest.TableName))
            {
                QueryResult errorResult = new QueryResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Table name is required for table requests.",
                    ExecutedAt = DateTime.Now
                };
                BH.Engine.Base.Compute.RecordError("Table name is required for table requests.");
                return errorResult;
            }

            // Build SELECT clause
            string selectClause = "*";
            if (tableRequest.Columns != null && tableRequest.Columns.Any())
            {
                List<string> quotedColumns = tableRequest.Columns.Select(col => $"\"{col}\"").ToList();
                selectClause = string.Join(", ", quotedColumns);
            }

            // Build DISTINCT clause
            string distinctClause = tableRequest.Distinct ? "DISTINCT " : "";

            string sqlQuery = $"SELECT {distinctClause}{selectClause} FROM \"{tableRequest.TableName}\"";
            
            // Add WHERE clause if specified
            if (tableRequest.WhereConditions != null && tableRequest.WhereConditions.Any())
            {
                sqlQuery += " WHERE " + string.Join(" AND ", tableRequest.WhereConditions);
            }
            
            // Add ORDER BY clause if specified
            if (tableRequest.OrderBy != null && tableRequest.OrderBy.Any())
            {
                sqlQuery += " ORDER BY " + string.Join(", ", tableRequest.OrderBy);
            }
            
            // Add LIMIT clause if specified
            if (tableRequest.Limit > 0)
            {
                sqlQuery += $" LIMIT {tableRequest.Limit}";
            }

            // Add OFFSET clause if specified
            if (tableRequest.Offset > 0)
            {
                sqlQuery += $" OFFSET {tableRequest.Offset}";
            }

            sqlQuery += ";";

            return ExecuteQuery(sqlQuery);
        }

        /***************************************************/
    }
} 