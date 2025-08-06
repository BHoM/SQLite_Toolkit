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
using BH.oM.SQLite;
using BH.oM.SQLite.Requests;
using BH.oM.SQLite.Objects;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Converts an EqualityFilterRequest to a parameterized SQL WHERE clause.")]
        [Input("filter", "The equality filter request to convert.")]
        [Input("parameterPrefix", "Prefix for parameter names to avoid conflicts. Default is 'eq'.")]
        [Output("result", "FilterResult containing the SQL WHERE clause and parameters, or null if conversion failed.")]
        public static FilterResult EqualityFilter(EqualityFilterRequest filter, string parameterPrefix = "eq")
        {
            if (filter == null || filter.ColumnValues == null || !filter.ColumnValues.Any())
            {
                BH.Engine.Base.Compute.RecordWarning("Cannot process equality filter: filter is null or has no column values.");
                return null;
            }

            List<string> whereConditions = new List<string>();
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            int paramIndex = 0;

            foreach (KeyValuePair<string, List<object>> columnFilter in filter.ColumnValues)
            {
                string columnName = columnFilter.Key;
                List<object> values = columnFilter.Value;

                if (string.IsNullOrWhiteSpace(columnName) || values == null || !values.Any())
                    continue;

                // Handle single value vs multiple values
                if (values.Count == 1)
                {
                    string paramName = $"@{parameterPrefix}_{paramIndex++}";
                    whereConditions.Add($"\"{columnName}\" = {paramName}");
                    parameters[paramName] = values[0];
                }
                else
                {
                    // Multiple values - use IN clause
                    List<string> paramNames = new List<string>();
                    foreach (object value in values)
                    {
                        string paramName = $"@{parameterPrefix}_{paramIndex++}";
                        paramNames.Add(paramName);
                        parameters[paramName] = value;
                    }
                    whereConditions.Add($"\"{columnName}\" IN ({string.Join(", ", paramNames)})");
                }
            }

            if (!whereConditions.Any())
            {
                BH.Engine.Base.Compute.RecordWarning("No valid equality conditions found in filter.");
                return null;
            }

            // Combine conditions with specified logic operator
            string logicOperator = filter.Logic == LogicalOperator.Or ? " OR " : " AND ";
            string whereClause = string.Join(logicOperator, whereConditions);

            return new FilterResult()
            {
                WhereClause = whereClause,
                Parameters = parameters,
                FilterType = "Equality"
            };
        }

        /***************************************************/
    }
}
