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

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Creates a property mapping suggestion for a BHoM object based on its available primitive properties.")]
        [Input("obj", "The BHoM object to analyse.")]
        [Input("maxDepth", "Maximum property nesting level to explore. Default is 3 for performance.")]
        [Output("suggestions", "Dictionary of suggested column names and their property paths.")]
        public static Dictionary<string, string> SuggestPropertyMappings(this IBHoMObject obj, int maxDepth = 3)
        {
            Dictionary<string, string> suggestions = new Dictionary<string, string>();

            if (obj == null)
                return suggestions;

            try
            {
                List<string> propertyPaths = obj.GetBHoMPropertyPaths(maxDepth);

                foreach (string path in propertyPaths)
                {
                    // Create column name suggestion
                    string columnName = path.Replace('.', '_'); // Convert dot notation to underscore for column names
                    
                    // Check if the property type is suitable for database storage
                    Type propertyType = obj.GetType().GetPropertyType(path);
                    if (propertyType?.IsPrimitiveForDatabase() == true)
                    {
                        suggestions[columnName] = path;
                    }
                }

                Engine.Base.Compute.RecordNote($"Generated {suggestions.Count} property mapping suggestions for object of type '{obj.GetType().Name}'.");
            }
            catch (Exception ex)
            {
                Engine.Base.Compute.RecordWarning($"Error generating property mapping suggestions for object of type '{obj.GetType().Name}': {ex.Message}");
            }

            return suggestions;
        }

        /***************************************************/
    }
}