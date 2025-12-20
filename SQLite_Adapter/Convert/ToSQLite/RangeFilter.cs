/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2026, the respective contributors. All rights reserved.
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
using BH.oM.SQLite;
using BH.oM.SQLite.Requests;
using BH.oM.SQLite.Objects;
using BH.Engine.SQLite;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Adapter.SQLite
{
    public static partial class Convert
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Converts a RangeFilterRequest to a parameterised SQL WHERE clause with support for multiple range filter groups combined using logical operators.")]
        [Input("filter", "The range filter request to convert, containing a list of column range dictionaries.")]
        [Input("parameterPrefix", "Prefix for parameter names to avoid conflicts. Default is 'rng'.")]
        [Output("result", "FilterResult containing the SQL WHERE clause and parameters, or null if conversion failed.")]
        public static FilterCommand RangeFilter(RangeFilterRequest filter, string parameterPrefix = "rng")
        {
            if (filter == null || filter.ColumnRanges == null || !filter.ColumnRanges.Any())
            {
                BH.Engine.Base.Compute.RecordWarning("Cannot process range filter: filter is null or has no column ranges.");
                return null;
            }

            List<string> groupConditions = new List<string>();
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            int paramIndex = 0;

            // Process each group of column ranges
            foreach (Dictionary<string, GeneralDomain> columnRangeGroup in filter.ColumnRanges)
            {
                if (columnRangeGroup == null || !columnRangeGroup.Any())
                    continue;

                List<string> groupWhereConditions = new List<string>();

                foreach (KeyValuePair<string, GeneralDomain> columnRange in columnRangeGroup)
                {
                    string columnName = columnRange.Key;
                    GeneralDomain domain = columnRange.Value;

                    if (string.IsNullOrWhiteSpace(columnName) || domain == null)
                        continue;

                    // Validate the domain
                    if (!BH.Engine.SQLite.Query.IsValid(domain))
                    {
                        BH.Engine.Base.Compute.RecordWarning($"Invalid domain for column '{columnName}': domain validation failed.");
                        continue;
                    }

                    List<string> rangeConditions = new List<string>();

                    // Add minimum condition
                    if (domain.Min != null)
                    {
                        string minParamName = $"@{parameterPrefix}_{paramIndex++}_min";
                        string minOperator = filter.InclusiveBounds ? ">=" : ">";
                        rangeConditions.Add($"\"{columnName}\" {minOperator} {minParamName}");
                        parameters[minParamName] = domain.Min;
                    }

                    // Add maximum condition
                    if (domain.Max != null)
                    {
                        string maxParamName = $"@{parameterPrefix}_{paramIndex++}_max";
                        string maxOperator = filter.InclusiveBounds ? "<=" : "<";
                        rangeConditions.Add($"\"{columnName}\" {maxOperator} {maxParamName}");
                        parameters[maxParamName] = domain.Max;
                    }

                    if (rangeConditions.Any())
                    {
                        groupWhereConditions.Add($"({string.Join(" AND ", rangeConditions)})");
                    }
                }

                // Add this group's conditions as a single grouped condition
                if (groupWhereConditions.Any())
                {
                    // Within each group, conditions are combined with AND
                    string groupCondition = groupWhereConditions.Count == 1 
                        ? groupWhereConditions[0] 
                        : $"({string.Join(" AND ", groupWhereConditions)})";
                    groupConditions.Add(groupCondition);
                }
            }

            if (!groupConditions.Any())
            {
                BH.Engine.Base.Compute.RecordWarning("No valid range conditions found in filter.");
                return null;
            }

            // Combine group conditions with specified logic operator
            string logicOperator = filter.Logic == LogicalOperator.Or ? " OR " : " AND ";
            string whereClause = groupConditions.Count == 1 
                ? groupConditions[0] 
                : string.Join(logicOperator, groupConditions);

            // Generate ORDER BY clause from sort columns
            string orderByClause = OrderByClause(filter.SortColumns);

            return new FilterCommand()
            {
                WhereClause = whereClause,
                Parameters = parameters,
                FilterType = "Range",
                OrderByClause = orderByClause,
                Limit = filter.MaxResults
            };
        }

        /***************************************************/
    }
}

