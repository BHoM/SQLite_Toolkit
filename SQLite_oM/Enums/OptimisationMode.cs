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

using System.ComponentModel;

namespace BH.oM.SQLite
{
    /***************************************************/
    /****               Public Enums               ****/
    /***************************************************/

    [Description("Defines performance optimisation strategies for SQLite database operations.")]
    public enum OptimisationMode
    {
        [Description("Undefined optimisation mode.")]
        Undefined,

        [Description("Default SQLite settings with standard performance.")]
        Default,

        [Description("Optimised for frequent read operations with minimal write performance impact.")]
        ReadOptimised,

        [Description("Optimised for bulk write operations and large dataset insertion.")]
        WriteOptimised,

        [Description("Balanced optimisation for mixed read/write operations.")]
        Balanced,

        [Description("Maximum performance settings for large analysis datasets (may sacrifice safety).")]
        MaxPerformance,

        [Description("Memory-optimised settings for in-memory databases and large result sets.")]
        MemoryOptimised
    }

    /***************************************************/
} 
