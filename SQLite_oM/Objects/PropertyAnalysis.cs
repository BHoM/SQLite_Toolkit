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
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;

namespace BH.oM.SQLite.Objects
{
    /***************************************************/
    /****               Public Classes              ****/
    /***************************************************/

    [Description("Analysis of an individual property within a BHoM object type including its data characteristics and relationships.")]
    public class PropertyAnalysis : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("The PropertyInfo object representing the property being analysed.")]
        public virtual PropertyInfo PropertyInfo { get; set; }

        [Description("The name of the property.")]
        public virtual string PropertyName { get; set; }

        [Description("The .NET type of the property.")]
        public virtual Type PropertyType { get; set; }

        [Description("The corresponding SQLite data type for this property.")]
        public virtual SqliteDataType SqliteDataType { get; set; }

        [Description("The type of relationship this property represents.")]
        public virtual RelationshipType RelationshipType { get; set; }

        [Description("Whether this property contains a BHoM object.")]
        public virtual bool IsBHoMObject { get; set; } = false;

        [Description("Whether this property contains a collection of BHoM objects.")]
        public virtual bool IsBHoMCollection { get; set; } = false;

        [Description("The element type if this property is a collection.")]
        public virtual Type CollectionElementType { get; set; }

        [Description("Sample values from actual objects to understand data patterns.")]
        public virtual List<object> SampleValues { get; set; } = new List<object>();

        [Description("Whether null values were found in the sample data.")]
        public virtual bool HasNullValues { get; set; } = false;

        [Description("Maximum text length found in the sample data for text properties.")]
        public virtual int MaxTextLength { get; set; } = 0;

        /***************************************************/
    }

    /***************************************************/
} 