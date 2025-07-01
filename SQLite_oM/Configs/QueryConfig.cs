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

using BH.oM.Base;
using BH.oM.Base.Attributes;
using System.ComponentModel;
using System.Collections.Generic;

namespace BH.oM.SQLite.Configs
{
    /***************************************************/
    /****               Public Classes              ****/
    /***************************************************/

    [Description("Configuration settings for SQLite query operations and data retrieval.")]
    public class QueryConfig : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("Maximum number of rows to return from a query. Zero means no limit.")]
        public virtual int MaxResults { get; set; } = 0;

        [Description("Number of rows to skip before returning results for pagination.")]
        public virtual int SkipResults { get; set; } = 0;

        [Description("Query timeout in seconds for long-running operations.")]
        public virtual int QueryTimeoutSeconds { get; set; } = 30;

        [Description("Whether to return results in a case-sensitive manner for string comparisons.")]
        public virtual bool CaseSensitive { get; set; } = false;

        [Description("Columns to include in the query results. Empty list includes all columns.")]
        public virtual List<string> IncludeColumns { get; set; } = new List<string>();

        [Description("Columns to exclude from the query results.")]
        public virtual List<string> ExcludeColumns { get; set; } = new List<string>();

        [Description("ORDER BY clause for sorting results. Format: 'column ASC/DESC'.")]
        public virtual List<string> OrderBy { get; set; } = new List<string>();

        [Description("WHERE clause conditions for filtering results.")]
        public virtual List<string> WhereConditions { get; set; } = new List<string>();

        [Description("Whether to use parameterised queries for better security and performance.")]
        public virtual bool UseParameterisedQueries { get; set; } = true;

        /***************************************************/
    }

    /***************************************************/
} 