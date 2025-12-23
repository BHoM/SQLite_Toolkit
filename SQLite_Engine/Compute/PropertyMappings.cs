/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2026, the respective contributors. All rights reserved.
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
using BH.oM.SQLite.Configs;
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

        [Description("Validates all property mappings in a PushConfig against a given object type.")]
        [Input("config", "The PushConfig containing property mappings to validate.")]
        [Input("objectType", "The object Type to validate mappings against.")]
        [Output("validMappings", "Dictionary of valid column-to-property mappings.")]
        public static Dictionary<string, string> PropertyMappings(this PushConfig config, Type objectType)
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

                // Validate that the column name is valid for SQL
                if (!BH.Engine.SQLite.Query.IsValid(columnName))
                {
                    Engine.Base.Compute.RecordWarning($"Column name '{columnName}' is not valid for SQL database storage.");
                    continue;
                }

                // Validate that the property path exists on the object type
                if (objectType.IsValid(propertyPath))
                {
                    // Validate that the property type is suitable for database storage
                    Type propertyType = objectType.GetPropertyType(propertyPath);
                    if (propertyType != null && propertyType.IsPrimitive())
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

    }

    /***************************************************/
}

