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
using System;
using System.ComponentModel;
using System.Globalization;

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Converts a .NET object to an appropriate value for SQLite parameter binding.")]
        [Input("value", "The .NET object to convert.")]
        [Output("sqliteValue", "The converted value suitable for SQLite, or DBNull if conversion failed.")]
        public static object ConvertToSqliteValue(object value)
        {
            if (value == null)
                return DBNull.Value;

            Type valueType = value.GetType();

            // Handle nullable types
            if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                object underlyingValue = Convert.ChangeType(value, Nullable.GetUnderlyingType(valueType));
                return ConvertToSqliteValue(underlyingValue);
            }

            // Direct SQLite compatible types
            if (value is string || value is long || value is double || value is byte[])
            {
                return value;
            }

            // Convert integers to long (SQLite INTEGER)
            if (value is int || value is short || value is byte)
            {
                return Convert.ToInt64(value);
            }

            // Convert floating point to double (SQLite REAL)
            if (value is float || value is decimal)
            {
                return Convert.ToDouble(value);
            }

            // Convert boolean to integer
            if (value is bool)
            {
                return (bool)value ? 1L : 0L;
            }

            // Convert DateTime to ISO string
            if (value is DateTime)
            {
                DateTime dateTime = (DateTime)value;
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            }

            // Convert Guid to string
            if (value is Guid)
            {
                return value.ToString();
            }

            // Convert enums to their underlying value
            if (valueType.IsEnum)
            {
                return Convert.ToInt64(value);
            }

            // Fallback: convert to string
            BH.Engine.Base.Compute.RecordWarning($"Converting unknown type '{valueType.Name}' to string for SQLite parameter.");
            return value.ToString();
        }

        /***************************************************/
    }
}
