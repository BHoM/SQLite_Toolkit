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

        [Description("Resolves the complete column schema for an object type using unified mapping strategy.")]
        [Input("objectType", "The object Type to analyse.")]
        [Input("config", "Optional PushConfig with custom property mappings, fragment mappings, and exclusions.")]
        [Output("columnSchema", "Dictionary of column names and their corresponding property information.")]
        public static Dictionary<string, PropertyColumnInfo> ColumnSchema(this Type objectType, PushConfig config = null)
        {
            Dictionary<string,PropertyColumnInfo> columnSchema = new Dictionary<string, PropertyColumnInfo>();

            if (objectType == null)
                return columnSchema;

            // Get excluded properties
            List<string> excludedProperties = config?.ExcludedProperties ?? new List<string>();

            // Add combined property and fragment mappings if provided
            if (config != null && ((config.PropertyMappings != null && config.PropertyMappings.Any()) || 
                (config.FragmentMappings != null && config.FragmentMappings.Any())))
            {
                Engine.Base.Compute.RecordNote($"Using PushConfig mappings for type '{objectType.Name}'.");
                
                Dictionary<string, MappingInfo> validMappings = config.CombinedMappings(objectType);
                foreach (KeyValuePair<string, MappingInfo> mapping in validMappings)
                {
                    columnSchema[mapping.Key] = new PropertyColumnInfo
                    {
                        ColumnName = mapping.Key,
                        PropertyPath = mapping.Value.PropertyPath,
                        PropertyType = mapping.Value.PropertyType,
                        IsFromMapping = true,
                        IsFragmentMapping = mapping.Value.IsFragmentMapping,
                        FragmentType = mapping.Value.FragmentType
                    };
                }
            }

            // Add primitive properties (excluding those in ExcludedProperties and already mapped)
            Dictionary<string, Type> primitiveProperties = objectType.GetPrimitiveProperties();

            foreach (KeyValuePair<string, Type> prop in primitiveProperties)
            {
                if (!excludedProperties.Contains(prop.Key) && !columnSchema.ContainsKey(prop.Key))
                {
                    columnSchema[prop.Key] = new PropertyColumnInfo
                    {
                        ColumnName = prop.Key,
                        PropertyPath = prop.Key,
                        PropertyType = prop.Value,
                        IsFromMapping = false
                    };
                }
            }

            return columnSchema;
        }

        /***************************************************/
    }

    /***************************************************/
}
