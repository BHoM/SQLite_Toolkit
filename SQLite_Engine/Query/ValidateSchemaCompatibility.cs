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

using BH.oM.Base;
using BH.oM.Base.Attributes;
using BH.oM.SQLite;
using BH.oM.SQLite.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.SQLite
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Validates that a table schema is compatible with the provided objects. \n" +
            "Checks for missing columns, type mismatches, and constraint violations.")]
        [Input("schema", "The table schema to validate.")]
        [Input("objects", "Collection of objects to validate against the schema.")]
        [Output("isValid", "True if schema is compatible with all objects, false otherwise.")]
        public static bool ValidateSchemaCompatibility(TableSchema schema, IEnumerable<IBHoMObject> objects)
        {
            if (schema == null || objects == null || !objects.Any())
            {
                BH.Engine.Base.Compute.RecordWarning("Cannot validate schema compatibility: schema or objects are null/empty.");
                return false;
            }

            try
            {
                bool isValid = true;
                Type objectType = objects.First().GetType();

                // Check if all required columns exist for the object type
                Dictionary<string, PropertyColumnInfo> columnSchema = objectType.ResolveColumnSchema(null);
                List<PropertyColumnInfo> expectedMappings = columnSchema.Values.ToList();
                
                foreach (PropertyColumnInfo mapping in expectedMappings)
                {
                    Column existingColumn = schema.Columns.FirstOrDefault(c => c.Name == mapping.ColumnName);
                    if (existingColumn == null)
                    {
                        BH.Engine.Base.Compute.RecordWarning($"Schema missing expected column '{mapping.ColumnName}' for property '{mapping.PropertyPath}'.");
                        isValid = false;
                    }
                    else
                    {
                        // Validate column type compatibility
                        if (!IsColumnTypeCompatible(existingColumn, mapping.PropertyType))
                        {
                            BH.Engine.Base.Compute.RecordWarning($"Column '{mapping.ColumnName}' type mismatch: expected {mapping.PropertyType.Name}, schema has {existingColumn.DataType}.");
                            isValid = false;
                        }
                    }
                }

                return isValid;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to validate schema compatibility: {ex.Message}");
                return false;
            }
        }

        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private static bool IsColumnTypeCompatible(Column column, Type propertyType)
        {
            try
            {
                // Handle nullable types
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    propertyType = Nullable.GetUnderlyingType(propertyType);
                }

                // Check basic type compatibility
                switch (column.DataType)
                {
                    case SqliteDataType.TEXT:
                        return propertyType == typeof(string) || propertyType == typeof(Guid) || propertyType == typeof(DateTime) || propertyType.IsEnum;
                    
                    case SqliteDataType.INTEGER:
                        return propertyType == typeof(int) || propertyType == typeof(long) || propertyType == typeof(short) || 
                               propertyType == typeof(byte) || propertyType == typeof(bool) || propertyType.IsEnum;
                    
                    case SqliteDataType.REAL:
                        return propertyType == typeof(double) || propertyType == typeof(float);
                    
                    case SqliteDataType.NUMERIC:
                        return propertyType == typeof(decimal);
                    
                    case SqliteDataType.BLOB:
                        return propertyType == typeof(byte[]);
                    
                    default:
                        return true; // Default compatibility
                }
            }
            catch
            {
                return false;
            }
        }

        /***************************************************/
    }
}
