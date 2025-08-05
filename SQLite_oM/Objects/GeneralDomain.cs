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

namespace BH.oM.SQLite.Objects
{
    /***************************************************/
    /****               Public Classes               ****/
    /***************************************************/

    [Description("A general-purpose domain defined by minimum and maximum values that supports both numeric and DateTime types for range filtering operations.")]
    public class GeneralDomain : BHoMObject
    {
        /***************************************************/
        /**** Properties                                ****/
        /***************************************************/

        [Description("The minimum bound of the domain. Can be numeric (int, double, decimal) or DateTime.")]
        public virtual object Min { get; set; }

        [Description("The maximum bound of the domain. Can be numeric (int, double, decimal) or DateTime.")]
        public virtual object Max { get; set; }

        /***************************************************/
        /**** Constructors                              ****/
        /***************************************************/

        [Description("Create a GeneralDomain with minimum and maximum values.")]
        public GeneralDomain()
        {
        }

        /***************************************************/

        [Description("Create a GeneralDomain with specified minimum and maximum values.")]
        public GeneralDomain(object min, object max)
        {
            Min = min;
            Max = max;
        }



        /***************************************************/
    }

    /***************************************************/
}