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
using System;

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Converts an EqualityFilterRequest into a parameterised SQL WHERE clause with appropriate handling for floating-point tolerance comparisons and multi-value IN clauses. \n" +
            "This method intelligently distinguishes between floating-point values requiring tolerance-based comparisons and discrete values suitable for exact matching or IN clauses.")]
        [Input("filter", "The equality filter request containing column-value pairs to be converted into SQL conditions. Each column can specify multiple values for IN clause generation.")]
        [Input("parameterPrefix", "Optional prefix for SQL parameter names to prevent naming conflicts when combining multiple filters. Defaults to 'eq' if not specified.")]
        [Output("result", "FilterResult object containing the complete SQL WHERE clause and associated parameters, or null if the filter contains no valid conditions or conversion fails.")]
        public static FilterResult EqualityFilter(EqualityFilterRequest filter, string parameterPrefix = "eq")
        {
            if (filter == null || filter.ColumnFilters == null || !filter.ColumnFilters.Any())
            {
                BH.Engine.Base.Compute.RecordWarning("Cannot process equality filter: filter is null or has no column filters.");
                return null;
            }

            List<string> whereConditions = new List<string>();
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            int paramIndex = 0;

            foreach (ColumnFilter columnFilter in filter.ColumnFilters)
            {
                if (columnFilter == null || string.IsNullOrWhiteSpace(columnFilter.ColumnName) || 
                    columnFilter.Values == null || !columnFilter.Values.Any())
                    continue;

                string columnName = columnFilter.ColumnName;
                List<object> values = columnFilter.Values;

                // Check if all values are floating-point numbers (need tolerance comparison)
                bool allFloatingPoint = values.All(v => IsFloatingPointValue(v));

                // Handle single value vs multiple values
                if (values.Count == 1)
                {
                    string paramName = $"@{parameterPrefix}_{paramIndex++}";
                    
                    if (allFloatingPoint)
                    {
                        // Use tolerance-based comparison for floating-point values
                        whereConditions.Add(CreateFloatingPointCondition(columnName, paramName));
                    }
                    else
                    {
                        // Use exact comparison for integers, strings, and other values
                        whereConditions.Add($"\"{columnName}\" = {paramName}");
                    }
                    
                    parameters[paramName] = values[0];
                }
                else
                {
                    if (allFloatingPoint)
                    {
                        // Multiple floating-point values - use OR with tolerance comparisons
                        List<string> toleranceConditions = new List<string>();
                        foreach (object value in values)
                        {
                            string paramName = $"@{parameterPrefix}_{paramIndex++}";
                            toleranceConditions.Add(CreateFloatingPointCondition(columnName, paramName));
                            parameters[paramName] = value;
                        }
                        whereConditions.Add($"({string.Join(" OR ", toleranceConditions)})");
                    }
                    else
                    {
                        // Multiple integers, strings, or other values - use IN clause
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

        // Checks if a value is a floating-point type that needs tolerance-based comparison
        private static bool IsFloatingPointValue(object value)
        {
            if (value == null) return false;
            
            // Only floating-point types need tolerance comparison
            if (value is double || value is float || value is decimal)
                return true;
                
            if (value is string stringValue)
            {
                // Check if string represents a floating-point number (has decimal point)
                if (double.TryParse(stringValue, out double result))
                {
                    return stringValue.Contains(".") || stringValue.Contains("e") || stringValue.Contains("E");
                }
            }
            
            return false;
        }

        private static string CreateFloatingPointCondition(string columnName, string paramName, double tolerance = 1E-9)
        {
            return $"ABS(\"{columnName}\" - {paramName}) < {tolerance}";
        }

        /***************************************************/
    }
}
