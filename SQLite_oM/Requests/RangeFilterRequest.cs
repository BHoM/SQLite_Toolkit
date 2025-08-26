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
using BH.oM.SQLite;
using BH.oM.SQLite.Objects;
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.oM.SQLite.Requests
{
    /***************************************************/
    /****               Public Classes              ****/
    /***************************************************/

    [Description("Request for filtering database records based on value ranges for numeric and DateTime columns.")]
    public class RangeFilterRequest : BHoMObject, ISqlRequest
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("Column range filters where each column has minimum and maximum values defined by GeneralDomain objects. \n" +
            "Key is the column name, value is a GeneralDomain with Min and Max values. \n" +
            "Supports numeric types (int, double, decimal) and DateTime. \n" +
            "Example: {'Price': new GeneralDomain(100.0, 500.0), 'DateCreated': new GeneralDomain(DateTime(2023,1,1), DateTime(2023,12,31))}")]
        public virtual List<Dictionary<string, GeneralDomain>> ColumnRanges { get; set; } = new List<Dictionary<string, GeneralDomain>>();

        [Description("Target table name for the filter operation. If not specified, will be derived from the request context.")]
        public virtual string TableName { get; set; } = "";

        [Description("Whether range bounds are inclusive (default) or exclusive. \n" +
            "If true: Min <= value <= Max. If false: Min < value < Max.")]
        public virtual bool InclusiveBounds { get; set; } = true;

        [Description("Logical operator to combine multiple column filters. Default is AND.")]
        public virtual LogicalOperator Logic { get; set; } = LogicalOperator.And;

        [Description("Maximum number of results to return. If 0, returns all matching records.")]
        public virtual int MaxResults { get; set; } = 0;

        [Description("List of columns to sort the results by. Applied in order of list sequence.")]
        public virtual List<SortColumn> SortColumns { get; set; } = new List<SortColumn>();

        /***************************************************/
    }

    /***************************************************/
}