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

namespace BH.Adapter.SQLite
{
    public static partial class Convert
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Converts a SQLite value back to its original .NET type based on schema information.")]
        [Input("sqliteValue", "The value returned from SQLite.")]
        [Input("targetType", "The original .NET type to convert to.")]
        [Output("convertedValue", "The value converted to its original .NET type.")]
        public static object Value(object sqliteValue, Type targetType)
        {
            if (sqliteValue == null || sqliteValue == DBNull.Value)
                return null;

            if (targetType == null)
                return sqliteValue;

            try
            {
                // If the value is already the correct type, return as-is
                if (targetType.IsAssignableFrom(sqliteValue.GetType()))
                    return sqliteValue;

                // Handle nullable types
                Type underlyingType = Nullable.GetUnderlyingType(targetType);
                if (underlyingType != null)
                    targetType = underlyingType;

                // Handle specific type conversions from SQLite storage
                if (targetType == typeof(bool))
                {
                    // SQLite stores booleans as integers (0/1)
                    if (sqliteValue is long longValue)
                        return longValue != 0;
                    if (sqliteValue is int intValue)
                        return intValue != 0;
                    if (sqliteValue is double doubleValue)
                        return doubleValue != 0.0;
                    
                    // Try to parse as boolean
                    return System.Convert.ToBoolean(sqliteValue);
                }
                else if (targetType == typeof(int))
                {
                    // SQLite may return integers as long
                    if (sqliteValue is long longValue)
                        return (int)longValue;
                    
                    return System.Convert.ToInt32(sqliteValue);
                }
                else if (targetType == typeof(DateTime))
                {
                    // SQLite may store DateTime as string
                    if (sqliteValue is string dateString)
                        return DateTime.Parse(dateString);
                    
                    return System.Convert.ToDateTime(sqliteValue);
                }
                else if (targetType == typeof(Guid))
                {
                    // SQLite stores GUIDs as strings
                    if (sqliteValue is string guidString)
                        return Guid.Parse(guidString);
                    
                    return (Guid)sqliteValue;
                }
                else if (targetType.IsEnum)
                {
                    // Handle enum types
                    if (sqliteValue is long longValue)
                        return Enum.ToObject(targetType, (int)longValue);
                    if (sqliteValue is int intValue)
                        return Enum.ToObject(targetType, intValue);
                    if (sqliteValue is string stringValue)
                        return Enum.Parse(targetType, stringValue);
                    
                    return Enum.ToObject(targetType, sqliteValue);
                }
                else
                {
                    // Generic conversion for other types
                    return System.Convert.ChangeType(sqliteValue, targetType);
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to convert SQLite value '{sqliteValue}' (type: {sqliteValue.GetType().Name}) to target type '{targetType.Name}': {ex.Message}");
                return sqliteValue; // Return original value if conversion fails
            }
        }

        /***************************************************/
    }
}
