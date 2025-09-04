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

        [Description("Combines multiple filter results into a single WHERE clause with proper parameter handling.")]
        [Input("filterResults", "Collection of filter results to combine.")]
        [Input("combineLogic", "Logic operator to combine the filters (AND/OR). Default is AND.")]
        [Output("result", "Combined FilterResult, or null if combination failed.")]
        public static FilterCommand CombineFilterResults(IEnumerable<FilterCommand> filterResults, LogicalOperator combineLogic = LogicalOperator.And)
        {
            if (filterResults == null || !filterResults.Any())
            {
                BH.Engine.Base.Compute.RecordWarning("Cannot combine filter results: no filter results provided.");
                return null;
            }

            List<FilterCommand> validResults = filterResults.Where(r => r != null && !string.IsNullOrWhiteSpace(r.WhereClause)).ToList();
            
            if (!validResults.Any())
            {
                BH.Engine.Base.Compute.RecordWarning("No valid filter results to combine.");
                return null;
            }

            if (validResults.Count == 1)
            {
                // Return single result as-is
                return validResults[0];
            }

            // Combine WHERE clauses
            List<string> whereClauses = validResults.Select(r => $"({r.WhereClause})").ToList();
            string logicOperator = combineLogic == LogicalOperator.Or ? " OR " : " AND ";
            string combinedWhereClause = string.Join(logicOperator, whereClauses);

            // Combine parameters
            Dictionary<string, object> combinedParameters = new Dictionary<string, object>();
            foreach (FilterCommand result in validResults)
            {
                if (result.Parameters != null)
                {
                    foreach (KeyValuePair<string, object> param in result.Parameters)
                    {
                        if (combinedParameters.ContainsKey(param.Key))
                        {
                            BH.Engine.Base.Compute.RecordWarning($"Parameter name conflict detected: {param.Key}. Using unique prefix recommended.");
                        }
                        combinedParameters[param.Key] = param.Value;
                    }
                }
            }

            List<string> filterTypes = validResults.Select(r => r.FilterType).Distinct().ToList();
            string combinedFilterType = filterTypes.Count == 1 ? filterTypes[0] : "Combined";

            return new FilterCommand()
            {
                WhereClause = combinedWhereClause,
                Parameters = combinedParameters,
                FilterType = combinedFilterType
            };
        }

        /***************************************************/
    }
}
