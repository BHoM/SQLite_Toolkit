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

using BH.oM.Base;
using BH.oM.Base.Attributes;
using BH.oM.SQLite.Configs;
using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace BH.oM.SQLite.Objects
{
    /***************************************************/
    /****               Public Classes              ****/
    /***************************************************/

    [Description("Context information for object relationship analysis operations.")]
    public class AnalysisContext : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("Push configuration settings that control the analysis behaviour.")]
        public virtual PushConfig PushConfig { get; set; }

        [Description("Names of tables that already exist in the database.")]
        public virtual HashSet<string> ExistingTables { get; set; }

        [Description("Types that have already been processed to prevent infinite recursion.")]
        public virtual HashSet<Type> ProcessedTypes { get; set; }

        /***************************************************/
        /**** Constructors                            ****/
        /***************************************************/

        [Description("Creates a new analysis context with the specified configuration and existing data.")]
        public AnalysisContext(PushConfig pushConfig, HashSet<string> existingTables, HashSet<Type> processedTypes)
        {
            PushConfig = pushConfig;
            ExistingTables = existingTables;
            ProcessedTypes = processedTypes;
        }

        /***************************************************/
        /**** Fallback Constructor                    ****/
        /***************************************************/

        public AnalysisContext()
        {
            PushConfig = new PushConfig();
            ExistingTables = new HashSet<string>();
            ProcessedTypes = new HashSet<Type>();
        }

        /***************************************************/
    }

    /***************************************************/
} 