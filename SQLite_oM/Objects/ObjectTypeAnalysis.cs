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

namespace BH.oM.SQLite.Objects
{
    /***************************************************/
    /****               Public Classes              ****/
    /***************************************************/

    [Description("Analysis of an individual object type including its properties and relationships.")]
    public class ObjectTypeAnalysis : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("The object type being analysed.")]
        public virtual Type ObjectType { get; set; }

        [Description("Analysis of individual properties within the object type.")]
        public virtual List<PropertyAnalysis> Properties { get; set; } = new List<PropertyAnalysis>();

        [Description("Identified relationships for this object type.")]
        public virtual List<ObjectRelationship> Relationships { get; set; } = new List<ObjectRelationship>();

        [Description("Whether this object type has circular references to itself or other types.")]
        public virtual bool HasCircularReference { get; set; } = false;

        /***************************************************/
    }

    /***************************************************/
} 