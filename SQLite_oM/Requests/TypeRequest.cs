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
using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace BH.oM.SQLite.Requests
{
    /***************************************************/
    /****               Public Classes              ****/
    /***************************************************/

    [Description("Request for retrieving objects of specific types from SQLite databases with BHoM type filtering.")]
    public class TypeRequest : BHoMObject, IRequest
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("The .NET type to filter objects by. Objects will be converted to this type.")]
        public virtual Type Type { get; set; } = null;

        [Description("Additional filter conditions based on object properties.")]
        public virtual Dictionary<string, object> PropertyFilters { get; set; } = new Dictionary<string, object>();

        [Description("Whether to include derived types in the results.")]
        public virtual bool IncludeDerivedTypes { get; set; } = true;

        [Description("Maximum number of objects to return. Zero means no limit.")]
        public virtual int Limit { get; set; } = 0;

        [Description("Properties to include in the results. Empty list includes all properties.")]
        public virtual List<string> IncludeProperties { get; set; } = new List<string>();

        [Description("Properties to exclude from the results.")]
        public virtual List<string> ExcludeProperties { get; set; } = new List<string>();

        [Description("Order by property name for sorting results. Format: 'PropertyName ASC/DESC'.")]
        public virtual List<string> OrderBy { get; set; } = new List<string>();

        /***************************************************/
    }

    /***************************************************/
} 