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
using BH.oM.SQLite.Objects;
using BH.oM.SQLite.Requests;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private IEnumerable<object> ReadSchemaRequest(SchemaRequest schemaRequest)
        {
            List<object> result = new List<object>();

            QueryResult queryResult = ExecuteSchemaQuery(schemaRequest);
            
            if (queryResult.IsSuccess)
            {
                result.Add(queryResult);
            }
            else
            {
                BH.Engine.Base.Compute.RecordError($"Schema query failed: {queryResult.ErrorMessage}");
            }

            return result;
        }

        private QueryResult ExecuteSchemaQuery(SchemaRequest schemaRequest)
        {
            string sqlQuery;
            
            if (schemaRequest.TableNames == null || !schemaRequest.TableNames.Any())
            {
                // Get all tables when no specific tables requested
                sqlQuery = @"
                    SELECT name as TableName, type as ObjectType, sql as CreateStatement 
                    FROM sqlite_master 
                    WHERE type IN ('table', 'view')";

                // Apply table name pattern filter if specified
                if (!string.IsNullOrEmpty(schemaRequest.TableNamePattern))
                {
                    sqlQuery += $" AND name LIKE '{schemaRequest.TableNamePattern}'";
                }

                sqlQuery += " ORDER BY name;";
            }
            else
            {
                // Get schema for specific tables
                List<string> tableQueries = new List<string>();
                
                foreach (string tableName in schemaRequest.TableNames)
                {
                    if (!string.IsNullOrEmpty(tableName))
                    {
                        string tableQuery = $@"
                            SELECT 
                                '{tableName}' as TableName,
                                cid as ColumnId,
                                name as ColumnName,
                                type as DataType,
                                [notnull] as NotNull,
                                dflt_value as DefaultValue,
                                pk as IsPrimaryKey
                            FROM pragma_table_info('{tableName}')";
                        
                        tableQueries.Add(tableQuery);
                    }
                }

                if (tableQueries.Any())
                {
                    sqlQuery = string.Join(" UNION ALL ", tableQueries) + " ORDER BY TableName, ColumnId;";
                }
                else
                {
                    sqlQuery = "SELECT 'No valid table names provided' as ErrorMessage;";
                }
            }

            return ExecuteQuery(sqlQuery);
        }

        /***************************************************/
    }
} 