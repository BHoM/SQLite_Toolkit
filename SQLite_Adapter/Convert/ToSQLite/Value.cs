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
using BH.oM.SQLite;
using System;
using System.ComponentModel;
using System.Globalization;

namespace BH.Adapter.SQLite
{
    public static partial class Convert
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Converts a .NET object to an appropriate value for SQLite parameter binding.")]
        [Input("value", "The .NET object to convert.")]
        [Input("nanHandling", "Strategy for handling NaN and Infinity values. Defaults to ConvertToNull.")]
        [Output("sqliteValue", "The converted value suitable for SQLite, or DBNull if conversion failed.")]
        public static object Value(object value, NaNHandling nanHandling = NaNHandling.ConvertToNull)
        {
            if (value == null)
                return DBNull.Value;

            Type valueType = value.GetType();

            // Handle nullable types
            if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                object underlyingValue = System.Convert.ChangeType(value, Nullable.GetUnderlyingType(valueType));
                return Value(underlyingValue, nanHandling);
            }

            // Handle double values with NaN/Infinity checking
            if (value is double doubleValue)
            {
                if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
                {
                    return HandleNaNValue(nanHandling);
                }
                return doubleValue;
            }

            // Handle float values with NaN/Infinity checking
            if (value is float floatValue)
            {
                if (float.IsNaN(floatValue) || float.IsInfinity(floatValue))
                {
                    return HandleNaNValue(nanHandling);
                }
                return System.Convert.ToDouble(floatValue);
            }

            // Direct SQLite compatible types (excluding double/float which are handled above)
            if (value is string || value is long || value is byte[])
            {
                return value;
            }

            // Convert integers to long (SQLite INTEGER)
            if (value is int || value is short || value is byte)
            {
                return System.Convert.ToInt64(value);
            }

            // Convert decimal to double (SQLite REAL)
            if (value is decimal)
            {
                return System.Convert.ToDouble(value);
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
                return System.Convert.ToInt64(value);
            }

            // Fallback: convert to string
            Engine.Base.Compute.RecordWarning($"Converting unknown type '{valueType.Name}' to string for SQLite parameter.");
            return value.ToString();
        }

        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        [Description("Handles NaN and Infinity values according to the specified handling strategy.")]
        [Input("nanHandling", "The strategy for handling the NaN/Infinity value.")]
        [Output("convertedValue", "The converted value suitable for SQLite storage.")]
        private static object HandleNaNValue(NaNHandling nanHandling)
        {
            switch (nanHandling)
            {
                case NaNHandling.ConvertToNull:
                    return DBNull.Value;

                case NaNHandling.ConvertToZero:
                    return 0.0;

                default:
                    Engine.Base.Compute.RecordWarning($"Unknown NaN handling strategy '{nanHandling}', defaulting to NULL.");
                    return DBNull.Value;
            }
        }

        /***************************************************/
    }
}
