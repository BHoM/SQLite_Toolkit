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
using System.Linq;
using System.Reflection;

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Validates that all properties of an IRecord object are indeed primitive types suitable for database storage.")]
        [Input("type", "The Type that implements IRecord to validate.")]
        [Output("isValid", "True if all properties are primitive, false otherwise.")]
        public static bool ValidateIRecordProperties(this Type type)
        {
            if (type == null || !type.IsIRecord())
                return false;

            try
            {
                PropertyInfo[] allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.GetMethod.GetParameters().Length == 0).ToArray();

                foreach (PropertyInfo property in allProperties)
                {
                    if (!property.PropertyType.IsPrimitiveForDatabase())
                    {
                        Engine.Base.Compute.RecordWarning($"IRecord type {type.Name} contains non-primitive property {property.Name} of type {property.PropertyType.Name}. " +
                            "IRecord objects should only contain primitive properties.");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Engine.Base.Compute.RecordError($"Error validating IRecord properties for {type.Name}: {ex.Message}");
                return false;
            }
        }

        /***************************************************/
    }
}