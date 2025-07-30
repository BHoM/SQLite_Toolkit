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
using BH.oM.Base;
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

        [Description("Creates a HashSet<T> from an IEnumerable<T> using the default equality comparer.")]
        [Input("source", "The source collection to convert to a HashSet.")]
        [Output("hashSet", "A new HashSet containing the elements from the source collection.")]
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                BH.Engine.Base.Compute.RecordError("Source collection cannot be null.");
                return new HashSet<T>();
            }

            return new HashSet<T>(source);
        }

        [Description("Creates a HashSet<T> from an IEnumerable<T> using the specified equality comparer.")]
        [Input("source", "The source collection to convert to a HashSet.")]
        [Input("comparer", "The equality comparer to use for the HashSet.")]
        [Output("hashSet", "A new HashSet containing the elements from the source collection.")]
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            if (source == null)
            {
                BH.Engine.Base.Compute.RecordError("Source collection cannot be null.");
                return new HashSet<T>();
            }

            return new HashSet<T>(source, comparer);
        }

        /***************************************************/
    }

    /***************************************************/
} 