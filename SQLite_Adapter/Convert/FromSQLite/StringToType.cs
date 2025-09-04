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

using BH.oM.Base.Attributes;
using System;
using System.ComponentModel;

namespace BH.Adapter.SQLite
{
    public static partial class Convert
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Converts a string representation of a .NET type name to its corresponding Type object, supporting both simple and full type names.")]
        [Input("typeString", "The string representation of the .NET type name (e.g., 'System.Int32', 'int', 'Boolean').")]
        [Output("type", "The corresponding .NET Type object, or null if the type string cannot be resolved.")]
        public static Type StringToType(string typeString)
        {
            if (string.IsNullOrWhiteSpace(typeString))
                return null;

            try
            {
                // Handle common .NET types stored as strings
                switch (typeString.ToLowerInvariant())
                {
                    case "system.boolean":
                    case "boolean":
                    case "bool":
                        return typeof(bool);
                        
                    case "system.int32":
                    case "int32":
                    case "int":
                        return typeof(int);
                        
                    case "system.int64":
                    case "int64":
                    case "long":
                        return typeof(long);
                        
                    case "system.double":
                    case "double":
                        return typeof(double);
                        
                    case "system.single":
                    case "single":
                    case "float":
                        return typeof(float);
                        
                    case "system.string":
                    case "string":
                        return typeof(string);
                        
                    case "system.datetime":
                    case "datetime":
                        return typeof(DateTime);
                        
                    case "system.guid":
                    case "guid":
                        return typeof(Guid);
                        
                    default:
                        // Try to resolve the full type name
                        Type resolvedType = Type.GetType(typeString);
                        if (resolvedType != null)
                            return resolvedType;
                        
                        // If that fails, try to find it in loaded assemblies
                        foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            try
                            {
                                resolvedType = assembly.GetType(typeString);
                                if (resolvedType != null)
                                    return resolvedType;
                            }
                            catch (Exception)
                            {
                                // Continue to next assembly
                            }
                        }
                        
                        return null;
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to convert type string '{typeString}' to Type: {ex.Message}");
                return null;
            }
        }

        /***************************************************/
    }
}
