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
using BH.oM.Data.Requests;
using System.ComponentModel;
using System.Collections.Generic;

namespace BH.oM.SQLite.Requests
{
    /***************************************************/
    /****               Public Classes              ****/
    /***************************************************/

    [Description("Request for retrieving schema information from SQLite databases including tables, columns, and indexes.")]
    public class SchemaRequest : BHoMObject, IRequest
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("Specific table names to get schema for. Empty list returns schema for all tables.")]
        public virtual List<string> TableNames { get; set; } = new List<string>();

        [Description("Whether to include column information in the schema results.")]
        public virtual bool IncludeColumns { get; set; } = true;

        [Description("Whether to include index information in the schema results.")]
        public virtual bool IncludeIndexes { get; set; } = true;

        [Description("Whether to include foreign key constraints in the schema results.")]
        public virtual bool IncludeForeignKeys { get; set; } = true;

        [Description("Whether to include view definitions in the schema results.")]
        public virtual bool IncludeViews { get; set; } = false;

        [Description("Whether to include trigger definitions in the schema results.")]
        public virtual bool IncludeTriggers { get; set; } = false;

        [Description("Pattern to filter table names. Supports SQL LIKE patterns with % and _ wildcards.")]
        public virtual string TableNamePattern { get; set; } = "";

        /***************************************************/
    }

    /***************************************************/
} 