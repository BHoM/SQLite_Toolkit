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
using BH.oM.SQLite;
using BH.oM.SQLite.Configs;
using BH.oM.SQLite.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

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
        public static Dictionary<string, string> ValidatePropertyMappings(this PushConfig config, Type objectType)
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

                // Validate that the property path exists on the object type
                if (objectType.IsValidPropertyPath(propertyPath))
                {
                    // Validate that the property type is suitable for database storage
                    Type propertyType = objectType.GetPropertyType(propertyPath);
                    if (propertyType != null && propertyType.IsPrimitiveForDatabase())
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

        [Description("Resolves the complete column schema for an object type using the three-tier mapping strategy.")]
        [Input("objectType", "The object Type to analyze.")]
        [Input("config", "Optional PushConfig with custom property mappings and exclusions.")]
        [Output("columnSchema", "Dictionary of column names and their corresponding property information.")]
        public static Dictionary<string, PropertyColumnInfo> ResolveColumnSchema(this Type objectType, PushConfig config = null)
        {
            var columnSchema = new Dictionary<string, PropertyColumnInfo>();

            if (objectType == null)
                return columnSchema;

            // Tier 1: Check if object implements IRecord
            if (objectType.IsIRecord())
            {
                Engine.Base.Compute.RecordNote($"Type '{objectType.Name}' implements IRecord. Using all properties for schema.");
                
                // Validate IRecord properties first
                if (!objectType.ValidateIRecordProperties())
                {
                    Engine.Base.Compute.RecordError($"IRecord type '{objectType.Name}' contains non-primitive properties. Schema resolution failed.");
                    return columnSchema;
                }

                Dictionary<string, Type> allProperties = objectType.GetPrimitiveProperties();
                foreach (KeyValuePair<string, Type> prop in allProperties)
                {
                    columnSchema[prop.Key] = new PropertyColumnInfo
                    {
                        ColumnName = prop.Key,
                        PropertyPath = prop.Key,
                        PropertyType = prop.Value,
                        IsFromMapping = false
                    };
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
                List<string> excludedProperties = config.ExcludedProperties ?? new List<string>();

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
                columnSchema[prop.Key] = new PropertyColumnInfo
                {
                    ColumnName = prop.Key,
                    PropertyPath = prop.Key,
                    PropertyType = prop.Value,
                    IsFromMapping = false
                };
            }

            return columnSchema;
        }

        /***************************************************/

        [Description("Extracts column values from a BHoM object using the resolved column schema.")]
        [Input("obj", "The BHoM object to extract values from.")]
        [Input("columnSchema", "The column schema defining how to extract values.")]
        [Output("columnValues", "Dictionary of column names and their extracted values.")]
        public static Dictionary<string, object> ExtractColumnValues(this IBHoMObject obj, Dictionary<string, PropertyColumnInfo> columnSchema)
        {
            Dictionary<string, object> columnValues = new Dictionary<string, object>();

            if (obj == null || columnSchema == null)
                return columnValues;

            foreach (KeyValuePair<string, PropertyColumnInfo> schema in columnSchema)
            {
                string columnName = schema.Key;
                PropertyColumnInfo columnInfo = schema.Value;

                try
                {
                    // Use direct property access for BHoM objects
                    object value = GetPropertyValueFromPath(obj, columnInfo.PropertyPath);
                    columnValues[columnName] = value;
                }
                catch (Exception ex)
                {
                    Engine.Base.Compute.RecordWarning($"Error extracting value for column '{columnName}' from property path '{columnInfo.PropertyPath}': {ex.Message}");
                    columnValues[columnName] = null;
                }
            }

            return columnValues;
        }

        /***************************************************/
        /**** Private Methods                          ****/
        /***************************************************/

        private static object GetPropertyValueFromPath(object obj, string propertyPath)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propertyPath))
                return null;

            // Handle simple property access
            if (!propertyPath.Contains('.'))
            {
                PropertyInfo property = obj.GetType().GetProperty(propertyPath);
                return property?.GetValue(obj);
            }

            // Handle nested property access using dot notation
            string[] propertyParts = propertyPath.Split('.');
            object currentObj = obj;

            foreach (string propertyName in propertyParts)
            {
                if (currentObj == null)
                    return null;

                PropertyInfo property = currentObj.GetType().GetProperty(propertyName);
                if (property == null)
                    return null;

                currentObj = property.GetValue(currentObj);
            }

            return currentObj;
        }

        /***************************************************/
    }

    /***************************************************/
}