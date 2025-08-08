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
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.oM.SQLite.Objects
{
    /***************************************************/
    /****               Public Classes              ****/
    /***************************************************/

    [Description("Contains the result of processing a filter request into SQL components. \n" +
        "Includes the generated WHERE clause and associated parameters for safe query execution.")]
    public class FilterResult : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("The SQL WHERE clause generated from the filter, without the 'WHERE' keyword.")]
        public virtual string WhereClause { get; set; } = "";

        [Description("Dictionary of parameter names and values for safe parameterised query execution. \n" +
            "Keys are parameter names (including @ prefix), values are the parameter values.")]
        public virtual Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        [Description("The type of filter that generated this result (e.g., 'Equality', 'Range', 'Combined').")]
        public virtual string FilterType { get; set; } = "";

        [Description("Optional limit on the number of results to return. Zero means no limit.")]
        public virtual int Limit { get; set; } = 0;

        [Description("Optional table name the filter is intended for. Can be used for validation.")]
        public virtual string TableName { get; set; } = "";

        /***************************************************/
    }

    /***************************************************/
}
