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

using BH.oM.Base.Attributes;
using BH.oM.SQLite.Objects;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace BH.Engine.SQLite
{
    public static partial class Create
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Builds a complete SELECT query from a FilterResult and table information.")]
        [Input("tableName", "The name of the table to query.")]
        [Input("filterResult", "The filter result containing WHERE clause and parameters. Can be null for no filtering.")]
        [Input("selectColumns", "List of column names to select. If empty, selects all columns (*).")]
        [Input("orderBy", "Optional ORDER BY clause (without the 'ORDER BY' keyword).")]
        [Output("sql", "Complete SQL SELECT statement, or null if construction failed.")]
        public static string SelectQuery(string tableName, FilterResult filterResult = null, List<string> selectColumns = null, string orderBy = "")
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                BH.Engine.Base.Compute.RecordError("Cannot build SELECT query: table name is null or empty.");
                return null;
            }

            StringBuilder sql = new StringBuilder();

            // SELECT clause
            sql.Append("SELECT ");
            if (selectColumns != null && selectColumns.Any())
            {
                List<string> quotedColumns = selectColumns.Select(col => $"\"{col}\"").ToList();
                sql.Append(string.Join(", ", quotedColumns));
            }
            else
            {
                sql.Append("*");
            }

            // FROM clause
            sql.Append($" FROM \"{tableName}\"");

            // WHERE clause
            if (filterResult != null && !string.IsNullOrWhiteSpace(filterResult.WhereClause))
            {
                sql.Append($" WHERE {filterResult.WhereClause}");
            }

            // ORDER BY clause
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                sql.Append($" ORDER BY {orderBy}");
            }

            // LIMIT clause
            if (filterResult != null && filterResult.Limit > 0)
            {
                sql.Append($" LIMIT {filterResult.Limit}");
            }

            return sql.ToString();
        }

        /***************************************************/
    }
}
