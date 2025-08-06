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
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Generates unique parameter names with a given prefix to avoid conflicts.")]
        [Input("prefix", "Prefix for the parameter names.")]
        [Input("count", "Number of parameter names to generate.")]
        [Output("parameterNames", "List of unique parameter names with @ prefix.")]
        public static List<string> GenerateParameterNames(string prefix, int count)
        {
            List<string> parameterNames = new List<string>();

            if (string.IsNullOrWhiteSpace(prefix))
                prefix = "param";

            for (int i = 0; i < count; i++)
            {
                string paramName = $"@{prefix}_{i}";
                parameterNames.Add(paramName);
            }

            return parameterNames;
        }

        /***************************************************/
    }
}
