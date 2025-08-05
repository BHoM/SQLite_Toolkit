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
using BH.oM.SQLite.Objects;
using System;
using System.ComponentModel;

namespace BH.Engine.SQLite
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Validates that a GeneralDomain has compatible Min and Max values with Min <= Max.")]
        [Input("domain", "The GeneralDomain to validate.")]
        [Output("isValid", "True if the domain is valid, false otherwise.")]
        public static bool IsValid(this GeneralDomain domain)
        {
            if (domain == null || domain.Min == null || domain.Max == null)
                return false;

            // Check if both are numeric types
            if (IsNumeric(domain.Min) && IsNumeric(domain.Max))
            {
                double minVal = Convert.ToDouble(domain.Min);
                double maxVal = Convert.ToDouble(domain.Max);
                return minVal <= maxVal;
            }

            // Check if both are DateTime
            if (domain.Min is DateTime minDate && domain.Max is DateTime maxDate)
            {
                return minDate <= maxDate;
            }

            // Types must match for other comparable types
            if (domain.Min.GetType() == domain.Max.GetType() && domain.Min is IComparable minComp && domain.Max is IComparable maxComp)
            {
                return minComp.CompareTo(maxComp) <= 0;
            }

            return false;
        }

        /***************************************************/

        [Description("Determines if an object is a numeric type.")]
        [Input("value", "The object to check.")]
        [Output("isNumeric", "True if the object is a numeric type, false otherwise.")]
        public static bool IsNumeric(this object value)
        {
            if (value == null)
                return false;

            return value is byte || value is sbyte || value is short || value is ushort ||
                   value is int || value is uint || value is long || value is ulong ||
                   value is float || value is double || value is decimal;
        }

        /***************************************************/
    }
}