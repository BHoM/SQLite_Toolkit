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
using BH.oM.SQLite.Configs;
using BH.oM.SQLite.Objects;
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

        [Description("Combines PropertyMappings and FragmentMappings from PushConfig into a unified mapping configuration.")]
        [Input("config", "PushConfig containing both property and fragment mappings.")]
        [Input("objectType", "The object Type to validate property mappings against.")]
        [Output("mappings", "Combined dictionary with column names as keys and mapping information as values.")]
        public static Dictionary<string, MappingInfo> CombinedMappings(this PushConfig config, Type objectType)
        {
            Dictionary<string, MappingInfo> combinedMappings = new Dictionary<string, MappingInfo>();

            if (config == null)
                return combinedMappings;

            // Add regular property mappings
            Dictionary<string, string> validPropertyMappings = config.ValidatePropertyMappings(objectType);
            foreach (KeyValuePair<string, string> mapping in validPropertyMappings)
            {
                combinedMappings[mapping.Key] = new MappingInfo
                {
                    ColumnName = mapping.Key,
                    PropertyPath = mapping.Value,
                    IsFragmentMapping = false,
                    PropertyType = objectType.GetPropertyType(mapping.Value)
                };
            }

            // Add fragment mappings
            Dictionary<string, string> validFragmentMappings = config.ValidateFragmentMappings();
            foreach (KeyValuePair<string, string> mapping in validFragmentMappings)
            {
                if (!combinedMappings.ContainsKey(mapping.Key)) // Avoid overwriting property mappings
                {
                    if (config.FragmentTypes.ContainsKey(mapping.Key))
                    {
                        Type fragmentType = config.FragmentTypes[mapping.Key];
                        Type propertyType = fragmentType.GetPropertyType(mapping.Value);
                        
                        // Handle special case for object ID properties - treat as string for database storage
                        if (propertyType == typeof(object) && IsObjectIdProperty(mapping.Value))
                        {
                            propertyType = typeof(string);
                        }
                        
                        combinedMappings[mapping.Key] = new MappingInfo
                        {
                            ColumnName = mapping.Key,
                            PropertyPath = mapping.Value,
                            IsFragmentMapping = true,
                            FragmentType = fragmentType,
                            PropertyType = propertyType
                        };
                    }
                }
                else
                {
                    Engine.Base.Compute.RecordWarning($"Column '{mapping.Key}' already mapped to property. Fragment mapping ignored.");
                }
            }

            return combinedMappings;
        }

        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private static bool IsObjectIdProperty(string propertyPath)
        {
            // Check if this appears to be an ID property based on name
            string[] idPropertyNames = { "id", "key", "identifier" };
            string propertyName = propertyPath.Split('.').Last().ToLower().Trim();
            
            return idPropertyNames.Any(idName => string.Equals(propertyName, idName, StringComparison.OrdinalIgnoreCase));
        }

        /***************************************************/
    }

    /***************************************************/
}
