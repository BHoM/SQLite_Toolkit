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
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Validates that parameter values are safe and compatible with SQLite.")]
        [Input("parameters", "Dictionary of parameter names and values to validate.")]
        [Output("isValid", "True if all parameters are valid, false otherwise.")]
        public static bool ValidateParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null)
                return true; // No parameters is valid

            foreach (KeyValuePair<string, object> parameter in parameters)
            {
                string paramName = parameter.Key;
                object paramValue = parameter.Value;

                // Validate parameter name
                if (!ValidateParameterName(paramName))
                {
                    BH.Engine.Base.Compute.RecordWarning($"Invalid parameter name: {paramName}");
                    return false;
                }

                // Validate parameter value
                if (!ValidateParameterValue(paramValue))
                {
                    BH.Engine.Base.Compute.RecordWarning($"Invalid parameter value for {paramName}: {paramValue}");
                    return false;
                }
            }

            return true;
        }

        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private static bool ValidateParameterName(string paramName)
        {
            if (string.IsNullOrWhiteSpace(paramName))
                return false;

            // Remove @ prefix for validation
            string cleanName = paramName.StartsWith("@") ? paramName.Substring(1) : paramName;

            // Check for dangerous characters
            char[] forbiddenChars = { ';', '\'', '"', '\\', '\n', '\r', '\t', ' ' };
            if (cleanName.IndexOfAny(forbiddenChars) >= 0)
                return false;

            // Check length
            if (cleanName.Length == 0 || cleanName.Length > 100)
                return false;

            // Must start with letter or underscore
            if (!char.IsLetter(cleanName[0]) && cleanName[0] != '_')
                return false;

            return true;
        }

        /***************************************************/

        private static bool ValidateParameterValue(object value)
        {
            if (value == null)
                return true; // NULL is valid

            Type valueType = value.GetType();

            // Check for types that could be problematic
            if (valueType == typeof(IntPtr) || valueType == typeof(UIntPtr))
                return false;

            // Check string length (SQLite has a default limit of ~1 billion characters, but be reasonable)
            if (value is string stringValue && stringValue.Length > 1000000)
                return false;

            // Check for byte array size
            if (value is byte[] byteArray && byteArray.Length > 10000000) // 10MB limit
                return false;

            return true;
        }

        /***************************************************/
    }
}
