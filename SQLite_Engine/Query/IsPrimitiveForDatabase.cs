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

using BH.Engine.Base;
using BH.oM.Base.Attributes;
using System;
using System.ComponentModel;

namespace BH.Engine.SQLite
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Determines if a Type is suitable for database storage as a primitive column.")]
        [Input("type", "The Type to check.")]
        [Output("isPrimitive", "True if the type can be stored as a database column, false otherwise.")]
        public static bool IsPrimitiveForDatabase(this Type type)
        {
            if (type == null)
                return false;

            // Handle nullable types
            Type actualType = Nullable.GetUnderlyingType(type) ?? type;

            // Check explicit numeric types first (fallback if BHoM IsNumeric fails)
            if (actualType == typeof(double) || actualType == typeof(float) || 
                actualType == typeof(int) || actualType == typeof(long) || 
                actualType == typeof(short) || actualType == typeof(byte) ||
                actualType == typeof(decimal))
                return true;

            // Use BHoM's IsNumeric method for additional numeric types (if available)
            try
            {
                if (actualType.IsNumeric())
                    return true;
            }
            catch
            {
                // If BHoM IsNumeric fails, continue with other checks
            }

            // Additional types that map well to database columns
            return actualType == typeof(string) ||
                   actualType == typeof(DateTime) ||
                   actualType == typeof(Guid) ||
                   actualType.IsEnum;
        }

        /***************************************************/
    }
}