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

        [Description("Resolves the complete column schema for an object type using the three-tier mapping strategy.")]
        [Input("objectType", "The object Type to analyse.")]
        [Input("config", "Optional PushConfig with custom property mappings and exclusions.")]
        [Output("columnSchema", "Dictionary of column names and their corresponding property information.")]
        public static Dictionary<string, PropertyColumnInfo> ResolveColumnSchema(this Type objectType, PushConfig config = null)
        {
            Dictionary<string,PropertyColumnInfo> columnSchema = new Dictionary<string, PropertyColumnInfo>();

            if (objectType == null)
                return columnSchema;

            // Get excluded properties once for all tiers
            List<string> excludedProperties = config?.ExcludedProperties ?? new List<string>();

            // Tier 1: Check if object implements IRecord
            if (objectType.IsIRecord())
            {
                Engine.Base.Compute.RecordNote($"Type '{objectType.Name}' implements IRecord. Using all properties for schema.");
                
                // Validate IRecord properties first, this should never be hit as an IRecord should only contain primitives 
                if (!objectType.ValidateIRecordProperties())
                {
                    Engine.Base.Compute.RecordError($"IRecord type '{objectType.Name}' contains non-primitive properties. Schema resolution failed.");
                    return columnSchema;
                }

                Dictionary<string, Type> allProperties = objectType.GetPrimitiveProperties();
                
                foreach (KeyValuePair<string, Type> prop in allProperties)
                {
                    if (!excludedProperties.Contains(prop.Key))
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

            // Tier 2: Check for PushConfig mappings
            if (config?.PropertyMappings != null && config.PropertyMappings.Any())
            {
                Engine.Base.Compute.RecordNote($"Using PushConfig mappings for type '{objectType.Name}'.");
                
                // Add mapped properties
                Dictionary<string, string> validMappings = config.ValidatePropertyMappings(objectType);
                foreach (KeyValuePair<string, string> mapping in validMappings)
                {
                    Type propertyType = objectType.GetPropertyType(mapping.Value);
                    columnSchema[mapping.Key] = new PropertyColumnInfo
                    {
                        ColumnName = mapping.Key,
                        PropertyPath = mapping.Value,
                        PropertyType = propertyType,
                        IsFromMapping = true
                    };
                }

                // Add primitive properties (excluding those in ExcludedProperties)
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

            // Tier 3: Fallback to primitive properties only
            Engine.Base.Compute.RecordNote($"Using primitive properties fallback for type '{objectType.Name}'.");
            Dictionary<string, Type> fallbackProperties = objectType.GetPrimitiveProperties();
            
            foreach (KeyValuePair<string, Type> prop in fallbackProperties)
            {
                if (!excludedProperties.Contains(prop.Key))
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
