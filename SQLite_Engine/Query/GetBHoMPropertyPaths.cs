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

        [Description("Gets all available primitive property paths from a BHoM object using BHoM's GetAllPropertyFullNames method.")]
        [Input("obj", "The BHoM object to analyze.")]
        [Input("maxDepth", "Maximum property nesting level to explore. Default is 3 for performance.")]
        [Output("propertyPaths", "List of primitive property paths available on the object.")]
        public static List<string> GetBHoMPropertyPaths(this IBHoMObject obj, int maxDepth = 3)
        {
            List<string> propertyPaths = new List<string>();

            if (obj == null)
                return propertyPaths;

            try
            {
                // Use BHoM's GetAllPropertyFullNames to get all property paths
                HashSet<string> allPropertyFullNames = obj.GetAllPropertyFullNames(maxDepth);
                
                // Filter to only include paths that end with primitive properties
                foreach (string fullPath in allPropertyFullNames)
                {
                    // Extract the property path part (remove the type prefix)
                    string[] parts = fullPath.Split('.');
                    if (parts.Length >= 2)
                    {
                        string propertyPath = string.Join(".", parts.Skip(1));
                        
                        // Check if the final property type is primitive
                        Type finalPropertyType = obj.GetType().GetPropertyType(propertyPath);
                        if (finalPropertyType != null && finalPropertyType.IsPrimitiveForDatabase())
                        {
                            propertyPaths.Add(propertyPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Engine.Base.Compute.RecordWarning($"Error getting property paths from object of type '{obj.GetType().Name}': {ex.Message}");
            }

            return propertyPaths;
        }

        /***************************************************/
    }
}