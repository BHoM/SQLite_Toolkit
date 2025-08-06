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
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Optimizes column definitions based on actual data analysis from multiple objects. \n" +
            "Adjusts column constraints, types, and properties based on observed values.")]
        [Input("schema", "The base table schema to optimize.")]
        [Input("objects", "Collection of objects to analyze for optimization hints.")]
        [Output("success", "True if optimization completed successfully, false otherwise.")]
        public static bool OptimizeSchemaFromData(TableSchema schema, IEnumerable<IBHoMObject> objects)
        {
            if (schema == null || objects == null || !objects.Any())
            {
                BH.Engine.Base.Compute.RecordWarning("Cannot optimize schema: schema or objects are null/empty.");
                return false;
            }

            try
            {
                int objectCount = objects.Count();
                Dictionary<string, object> sampleValues = new Dictionary<string, object>();

                // Collect sample values for each column
                foreach (Column column in schema.Columns)
                {
                    if (column.Name == "BHoMGuid") continue; // Skip GUID column

                    // Find corresponding property values
                    List<object> columnValues = ExtractColumnValuesFromObjects(column.Name, objects);
                    
                    if (columnValues.Any())
                    {
                        AnalyzeColumnValues(column, columnValues, objectCount);
                    }
                }

                BH.Engine.Base.Compute.RecordNote($"Optimized schema for table '{schema.Name}' based on analysis of {objectCount} objects.");
                return true;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to optimize schema from data: {ex.Message}");
                return false;
            }
        }

        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private static List<object> ExtractColumnValuesFromObjects(string columnName, IEnumerable<IBHoMObject> objects)
        {
            List<object> values = new List<object>();

            try
            {
                foreach (IBHoMObject obj in objects)
                {
                    Type objectType = obj.GetType();
                    Dictionary<string, PropertyColumnInfo> columnSchema = objectType.ResolveColumnSchema(null);
                    List<PropertyColumnInfo> mappings = columnSchema.Values.ToList();

                    // Extract all column values and get the one for our target column
                    Dictionary<string, object> allColumnValues = obj.ExtractColumnValues(columnSchema);
                    if (allColumnValues.ContainsKey(columnName))
                    {
                        object value = allColumnValues[columnName];
                        if (value != null)
                        {
                            values.Add(value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to extract values for column '{columnName}': {ex.Message}");
            }

            return values;
        }

        /***************************************************/

        private static void AnalyzeColumnValues(Column column, List<object> values, int totalObjectCount)
        {
            try
            {
                if (!values.Any()) return;

                // Calculate null percentage
                int nullCount = values.Count(v => v == null);
                double nullPercentage = (double)nullCount / totalObjectCount * 100;

                // If less than 50% of values are null, consider making column NOT NULL
                if (nullPercentage < 50)
                {
                    column.AllowNull = false;
                }

                // Analyze string lengths for TEXT columns
                if (column.DataType == SqliteDataType.TEXT)
                {
                    List<string> stringValues = values.OfType<string>().ToList();
                    if (stringValues.Any())
                    {
                        int maxLength = stringValues.Max(s => s.Length);
                        if (maxLength > 0 && maxLength < 1000) // Reasonable limit
                        {
                            column.MaxLength = maxLength + 50; // Add buffer
                        }
                    }
                }

                // Check for unique values
                HashSet<object> uniqueValues = new HashSet<object>(values.Where(v => v != null));
                if (uniqueValues.Count == values.Count(v => v != null) && uniqueValues.Count > 1)
                {
                    // All non-null values are unique
                    column.IsUnique = true;
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to analyze values for column '{column.Name}': {ex.Message}");
            }
        }

        /***************************************************/
    }
}
