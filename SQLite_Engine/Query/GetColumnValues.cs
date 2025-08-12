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
using BH.oM.SQLite.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace BH.Engine.SQLite
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Gets column values from a BHoM object using the resolved column schema.")]
        [Input("obj", "The BHoM object to extract values from.")]
        [Input("columnSchema", "The column schema defining how to extract values.")]
        [Output("columnValues", "Dictionary of column names and their extracted values.")]
        public static Dictionary<string, object> GetColumnValues(this IBHoMObject obj, Dictionary<string, PropertyColumnInfo> columnSchema)
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
            if (!propertyPath.Contains("."))
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
