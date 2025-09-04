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
using BH.oM.SQLite;

namespace BH.Adapter.SQLite
{
    public static partial class Convert
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Converts a list of SortColumn objects into a SQL ORDER BY clause.")]
        [Input("sortColumns", "List of sort column specifications to convert.")]
        [Output("orderByClause", "SQL ORDER BY clause without the 'ORDER BY' keyword, or empty string if no valid sort columns.")]
        public static string OrderByClause(List<SortColumn> sortColumns)
        {
            if (sortColumns == null || !sortColumns.Any())
                return "";

            List<string> orderByItems = new List<string>();

            // Sort by priority first, then by original order
            IEnumerable<SortColumn> sortedColumns = sortColumns
                .Where(sc => sc != null && !string.IsNullOrWhiteSpace(sc.ColumnName))
                .OrderBy(sc => sc.Priority)
                .ThenBy(sc => sortColumns.IndexOf(sc));

            foreach (SortColumn sortColumn in sortedColumns)
            {
                // Validate column name
                if (!BH.Engine.SQLite.Query.IsValid(sortColumn.ColumnName))
                {
                    BH.Engine.Base.Compute.RecordWarning($"Invalid column name for sorting: '{sortColumn.ColumnName}'. Skipping.");
                    continue;
                }

                string order = "DESC";
                if (sortColumn.SortDirection == SortOrder.ASC)
                    order = "ASC";

                orderByItems.Add($"\"{sortColumn.ColumnName}\" {order}");
            }

            return orderByItems.Any() ? string.Join(", ", orderByItems) : "";
        }

        /***************************************************/
    }
}
