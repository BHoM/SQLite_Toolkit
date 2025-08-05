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
using BH.oM.SQLite;
using System;
using System.ComponentModel;

namespace BH.Engine.SQLite
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Determines if an object implements the IRecord interface, indicating it contains only primitive data types.")]
        [Input("obj", "The object to check.")]
        [Output("isRecord", "True if the object implements IRecord, false otherwise.")]
        public static bool IsIRecord(this object obj)
        {
            if (obj == null)
                return false;

            return obj is IRecord;
        }

        /***************************************************/

        [Description("Determines if a Type implements the IRecord interface.")]
        [Input("type", "The Type to check.")]
        [Output("isRecord", "True if the Type implements IRecord, false otherwise.")]
        public static bool IsIRecord(this Type type)
        {
            if (type == null)
                return false;

            return typeof(IRecord).IsAssignableFrom(type);
        }

        /***************************************************/
    }
}