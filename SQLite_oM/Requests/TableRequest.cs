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
using BH.oM.Data.Requests;
using System.ComponentModel;
using System.Collections.Generic;

namespace BH.oM.SQLite.Requests
{
    /***************************************************/
    /****               Public Classes              ****/
    /***************************************************/

    [Description("Request for retrieving data from a specific SQLite table with optional filtering and sorting.")]
    public class TableRequest : BHoMObject, IRequest
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("List of column names to include in the result. Empty list returns all columns.")]
        public virtual List<string> Columns { get; set; } = new List<string>();

        [Description("WHERE clause conditions for filtering rows. Each string represents a condition.")]
        public virtual List<string> WhereConditions { get; set; } = new List<string>();

        [Description("ORDER BY clauses for sorting results. Format: 'column_name ASC/DESC'.")]
        public virtual List<string> OrderBy { get; set; } = new List<string>();

        [Description("Maximum number of rows to return. Zero means no limit.")]
        public virtual int Limit { get; set; } = 0;

        [Description("Number of rows to skip before returning results for pagination.")]
        public virtual int Offset { get; set; } = 0;

        [Description("Whether to return distinct rows only.")]
        public virtual bool Distinct { get; set; } = false;

        /***************************************************/
    }

    /***************************************************/
} 