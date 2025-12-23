/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2026, the respective contributors. All rights reserved.
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
using BH.oM.SQLite.Objects;
using BH.Engine.Base;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace BH.Engine.SQLite
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Extracts a value from an object using property path or fragment mapping information.")]
        [Input("obj", "The BHoM object to extract value from.")]
        [Input("mappingInfo", "Mapping information containing property path and fragment type details.")]
        [Output("value", "Extracted value, or null if extraction fails.")]
        public static object ExtractMappedValue(this IObject obj, MappingInfo mappingInfo)
        {
            if (obj == null || mappingInfo == null || string.IsNullOrWhiteSpace(mappingInfo.PropertyPath))
                return null;

            try
            {
                if (mappingInfo.IsFragmentMapping)
                {
                    if (obj is IBHoMObject)
                    {
                        return ExtractFragmentValue(obj as IBHoMObject, mappingInfo.FragmentType, mappingInfo.PropertyPath);
                    }
                    else
                    {
                        Engine.Base.Compute.RecordError($"Fragment Mapping provided for object of type {obj.GetType()}, for Fragments to be included use an object \n" +
                            $"that inherits IBHoMObject.");
                        return null;
                    }
                }
                else
                {
                    // Regular property path using BHoM GetProperty method
                    return Base.Query.PropertyValue(obj, mappingInfo.PropertyPath);
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to extract value using mapping path '{mappingInfo.PropertyPath}': {ex.Message}");
                return null;
            }
        }

        /***************************************************/

        [Description("Extracts a value from an object using property path (legacy method for backward compatibility).")]
        [Input("obj", "The BHoM object to extract value from.")]
        [Input("propertyPath", "Property path (e.g., 'Position.X').")]
        [Output("value", "Extracted value, or null if path is invalid.")]
        public static object ExtractMappedValue(this IObject obj, string propertyPath)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propertyPath))
                return null;

            try
            {
                return Base.Query.PropertyValue(obj, propertyPath);
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to extract value using property path '{propertyPath}': {ex.Message}");
                return null;
            }
        }

        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private static object ExtractFragmentValue(IBHoMObject obj, Type fragmentType, string propertyPath)
        {
            if (obj == null || fragmentType == null || string.IsNullOrWhiteSpace(propertyPath))
                return null;

            // Use BHoM FindFragment method to get the fragment
            IFragment fragment = obj.FindFragment<IFragment>(fragmentType);

            if (fragment == null)
            {
                BH.Engine.Base.Compute.RecordNote($"Object does not contain fragment of type '{fragmentType.Name}'.");
                return null;
            }

            // Use BHoM GetProperty method to extract the value from the fragment
            object value = Base.Query.PropertyValue(fragment, propertyPath);

            // Debug logging for fragment value extraction
            BH.Engine.Base.Compute.RecordNote($"Extracted value from fragment {fragmentType.Name}.{propertyPath}: " +
                $"Value={value ?? "null"}, Type={value?.GetType()?.Name ?? "null"}");

            // Handle object ID properties - convert to appropriate type for database storage
            if (value != null && IsIdProperty(propertyPath))
            {
                object convertedValue = ConvertIdValue(value);
                BH.Engine.Base.Compute.RecordNote($"Converted ID value: Original={value} ({value.GetType().Name}) -> " +
                    $"Converted={convertedValue} ({convertedValue?.GetType()?.Name ?? "null"})");
                return convertedValue;
            }

            return value;
        }

        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private static bool IsIdProperty(string propertyPath)
        {
            // Check if this appears to be an ID property based on name
            string[] idPropertyNames = { "id", "key", "identifier" };
            string propertyName = propertyPath.Split('.').Last().ToLower().Trim();

            return idPropertyNames.Any(idName => string.Equals(propertyName, idName, StringComparison.OrdinalIgnoreCase));
        }

        /***************************************************/

        private static object ConvertIdValue(object value)
        {
            if (value == null)
            {
                BH.Engine.Base.Compute.RecordNote("ConvertIdValue: Input value is null");
                return null;
            }

            try
            {
                // Handle different possible ID value types
                Type valueType = value.GetType();

                // If it's already a primitive type, return as-is
                if (valueType.IsPrimitive() || valueType == typeof(string))
                    return value;

                // Special handling for boxed primitives or object type containing primitives
                if (value is int intValue)
                    return intValue;

                if (value is long longValue)
                    return longValue;

                if (value is string stringValue)
                    return stringValue;

                // For object type, try to extract the actual value
                string stringRepresentation = value?.ToString();

                // Try parsing as int first (most common ID type)
                if (int.TryParse(stringRepresentation, out int intId))
                    return intId;

                // Try parsing as long
                if (long.TryParse(stringRepresentation, out long longId))
                    return longId;

                // Fall back to string representation
                return stringRepresentation;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to convert ID value of type {value.GetType().Name}: {ex.Message}. Using string representation.");
                return value?.ToString() ?? "";
            }
        }

        /***************************************************/
    }
}

