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
using System;
using System.ComponentModel;
using System.Reflection;

namespace BH.Engine.SQLite
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Validates that a property path exists and is accessible on the specified object type, with comprehensive support for dot notation traversal. \n" +
            "This method recursively validates nested property access chains, ensuring each property in the path is publicly accessible and properly typed.")]
        [Input("type", "The root .NET Type object on which to validate the property path. This serves as the starting point for property traversal and validation.")]
        [Input("propertyPath", "The property access path to validate, using dot notation for nested properties (e.g., 'Position.X', 'Material.Properties.Density'). Simple property names are also supported.")]
        [Output("isValid", "True if the complete property path exists and all intermediate properties are accessible, false if any part of the path is invalid, non-existent, or inaccessible.")]
        public static bool IsValidPropertyPath(this Type type, string propertyPath)
        {
            if (type == null || string.IsNullOrWhiteSpace(propertyPath))
                return false;

            try
            {
                // Handle simple property access
                if (!propertyPath.Contains("."))
                {
                    PropertyInfo property = type.GetProperty(propertyPath);
                    return property != null;
                }

                // Handle nested property access
                string[] propertyParts = propertyPath.Split('.');
                Type currentType = type;

                foreach (string propertyName in propertyParts)
                {
                    PropertyInfo property = currentType.GetProperty(propertyName);
                    if (property == null)
                        return false;

                    currentType = property.PropertyType;
                }

                return true;
            }
            catch (Exception ex)
            {
                Engine.Base.Compute.RecordWarning($"Error validating property path '{propertyPath}' for type '{type.Name}': {ex.Message}");
                return false;
            }
        }

        /***************************************************/
    }
}