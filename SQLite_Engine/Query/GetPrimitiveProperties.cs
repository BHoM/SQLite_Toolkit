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
using System.Collections.Generic;
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

        [Description("Extracts all primitive properties from an object type that can be mapped to database columns.")]
        [Input("type", "The object Type to analyze.")]
        [Output("properties", "Dictionary of property names and their types that are suitable for database storage.")]
        public static Dictionary<string, Type> GetPrimitiveProperties(this Type type)
        {
            Dictionary<string, Type> primitiveProperties = new Dictionary<string, Type>();

            if (type == null)
                return primitiveProperties;

            try
            {
                // Get all readable properties with no parameters (indexers excluded)
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.GetMethod.GetParameters().Length == 0).ToArray();

                foreach (PropertyInfo property in properties)
                {
                    if (property.PropertyType.IsPrimitiveForDatabase())
                    {
                        primitiveProperties[property.Name] = property.PropertyType;
                    }
                }
            }
            catch (Exception ex)
            {
                Engine.Base.Compute.RecordWarning($"Error extracting primitive properties from {type.Name}: {ex.Message}");
            }

            return primitiveProperties;
        }

        /***************************************************/
    }
}