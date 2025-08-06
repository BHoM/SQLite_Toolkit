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

using BH.oM.Adapter;
using BH.oM.Base.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.oM.SQLite.Configs
{
    /***************************************************/
    /****               Public Classes               ****/
    /***************************************************/

    [Description("Configuration for pushing objects to SQLite database with intelligent property mapping and filtering capabilities.")]
    public class PushConfig : ActionConfig
    {
        /***************************************************/
        /**** Properties                                ****/
        /***************************************************/

        [Description("Target table name for the push operation. If not specified, table name will be derived from object type.")]
        public virtual string Table { get; set; } = "";

        [Description("Property-to-column mappings using dot notation for nested properties. \n" +
            "Key is the database column name, value is the object property path (e.g., 'Position.X', 'Material.Properties.Density').")]
        public virtual Dictionary<string, string> PropertyMappings { get; set; } = new Dictionary<string, string>();

        [Description("Properties to exclude from automatic primitive property inclusion during table creation. \n" +
            "Use this to prevent certain primitive properties from being automatically mapped to database columns. \n" +
            "If empty, all primitive properties will be automatically included alongside any specified PropertyMappings.")]
        public virtual List<string> ExcludedProperties { get; set; } = new List<string>();

        [Description("Whether to validate property mappings before push operation. Default is true.")]
        public virtual bool ValidateMappings { get; set; } = true;

        /***************************************************/
    }

    /***************************************************/
}