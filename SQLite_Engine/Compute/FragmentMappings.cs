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
using BH.oM.Base;
using BH.oM.Base.Attributes;
using BH.oM.SQLite.Configs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Validates all property mappings in a PushConfig against a given object type.")]
        [Input("config", "The PushConfig containing property mappings to validate.")]
        [Input("objectType", "The object Type to validate mappings against.")]
        [Output("validMappings", "Dictionary of valid column-to-property mappings.")]
        public static Dictionary<string, string> PropertyMappings(this PushConfig config, Type objectType)
        {
            Dictionary<string, string> validMappings = new Dictionary<string, string>();

            if (config?.PropertyMappings == null || objectType == null)
                return validMappings;

            foreach (KeyValuePair<string, string> mapping in config.PropertyMappings)
            {
                string columnName = mapping.Key;
                string propertyPath = mapping.Value;

                if (string.IsNullOrWhiteSpace(columnName) || string.IsNullOrWhiteSpace(propertyPath))
                {
                    Engine.Base.Compute.RecordWarning($"Invalid mapping: column '{columnName}' or property path '{propertyPath}' is empty.");
                    continue;
                }

                // Validate that the column name is valid for SQL
                if (!BH.Engine.SQLite.Query.IsValid(columnName))
                {
                    Engine.Base.Compute.RecordWarning($"Column name '{columnName}' is not valid for SQL database storage.");
                    continue;
                }

                // Validate that the property path exists on the object type
                if (objectType.IsValid(propertyPath))
                {
                    // Validate that the property type is suitable for database storage
                    Type propertyType = objectType.GetPropertyType(propertyPath);
                    if (propertyType != null && propertyType.IsPrimitive())
                    {
                        validMappings[columnName] = propertyPath;
                    }
                    else
                    {
                        Engine.Base.Compute.RecordWarning($"Property '{propertyPath}' on type '{objectType.Name}' is not suitable for database storage. " +
                            $"Property type: {propertyType?.Name ?? "null"}");
                    }
                }
                else
                {
                    Engine.Base.Compute.RecordWarning($"Property path '{propertyPath}' does not exist on type '{objectType.Name}'.");
                }
            }

            Engine.Base.Compute.RecordNote($"Validated {validMappings.Count} of {config.PropertyMappings.Count} property mappings for type '{objectType.Name}'.");
            return validMappings;
        }

        /***************************************************/

        [Description("Validates fragment mappings in a PushConfig, ensuring fragment types exist and property paths are valid.")]
        [Input("config", "The PushConfig containing fragment mappings to validate.")]
        [Output("validMappings", "Dictionary of valid column-to-fragment property mappings.")]
        public static Dictionary<string, string> FragmentMappings(this PushConfig config)
        {
            Dictionary<string, string> validMappings = new Dictionary<string, string>();

            if (config?.FragmentMappings == null || config.FragmentTypes == null)
                return validMappings;

            foreach (KeyValuePair<string, string> fragmentMapping in config.FragmentMappings)
            {
                string columnName = fragmentMapping.Key;
                string fragmentPropertyPath = fragmentMapping.Value;

                if (string.IsNullOrWhiteSpace(columnName) || string.IsNullOrWhiteSpace(fragmentPropertyPath))
                {
                    Engine.Base.Compute.RecordWarning($"Invalid fragment mapping: column '{columnName}' or property path '{fragmentPropertyPath}' is empty.");
                    continue;
                }

                // Validate that the column name is valid for SQL
                if (!BH.Engine.SQLite.Query.IsValid(columnName))
                {
                    Engine.Base.Compute.RecordWarning($"Column name '{columnName}' is not valid for SQL database storage.");
                    continue;
                }

                // Check if we have a type specification for this fragment mapping
                if (!config.FragmentTypes.ContainsKey(columnName))
                {
                    Engine.Base.Compute.RecordWarning($"Fragment mapping for column '{columnName}' missing corresponding FragmentType specification.");
                    continue;
                }

                Type fragmentType = config.FragmentTypes[columnName];

                // Validate that the fragment type implements IFragment
                if (!typeof(IFragment).IsAssignableFrom(fragmentType))
                {
                    Engine.Base.Compute.RecordWarning($"Fragment type '{fragmentType.Name}' does not implement IFragment interface.");
                    continue;
                }

                // Validate that the property path exists on the fragment type
                if (fragmentType.IsValid(fragmentPropertyPath))
                {
                    // Validate that the property type is suitable for database storage
                    Type propertyType = fragmentType.GetPropertyType(fragmentPropertyPath);
                    if (propertyType != null && (propertyType.IsPrimitive() || IsObjectIdProperty(propertyType, fragmentPropertyPath)))
                    {
                        validMappings[columnName] = fragmentPropertyPath;
                    }
                    else
                    {
                        Engine.Base.Compute.RecordWarning($"Property '{fragmentPropertyPath}' on fragment type '{fragmentType.Name}' is not suitable for database storage. " +
                            $"Property type: {propertyType?.Name ?? "null"}");
                    }
                }
                else
                {
                    Engine.Base.Compute.RecordWarning($"Property path '{fragmentPropertyPath}' does not exist on fragment type '{fragmentType.Name}'.");
                }
            }

            Engine.Base.Compute.RecordNote($"Validated {validMappings.Count} of {config.FragmentMappings.Count} fragment mappings.");
            return validMappings;
        }



        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private static bool IsObjectIdProperty(Type propertyType, string propertyPath)
        {
            // Handle special case for adapter ID properties that are typically object type but contain strings/ints
            if (propertyType == typeof(object))
            {
                // Common ID property names that are typically stored as strings/ints in object properties
                string[] idPropertyNames = { "id", "key", "identifier" };
                string propertyName = propertyPath.Split('.').Last().ToLower().Trim();
                
                if (idPropertyNames.Any(idName => string.Equals(propertyName, idName, StringComparison.OrdinalIgnoreCase)))
                {
                    Engine.Base.Compute.RecordNote($"Property '{propertyPath}' is object type but appears to be an ID property. Will be converted to string for database storage.");
                    return true;
                }
            }
            
            return false;
        }

        /***************************************************/
    }

    /***************************************************/
}
