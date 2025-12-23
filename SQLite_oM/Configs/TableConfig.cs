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

using BH.oM.Base;
using BH.oM.Base.Attributes;
using System.ComponentModel;
using System.Collections.Generic;

namespace BH.oM.SQLite.Configs
{
    /***************************************************/
    /****               Public Classes              ****/
    /***************************************************/

    [Description("Configuration settings for SQLite table operations and schema management.")]
    public class TableConfig : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("The name of the table to operate on.")]
        public virtual string TableName { get; set; } = "";

        [Description("Conflict resolution strategy for handling data conflicts during Insert or Update operations.")]
        public virtual ConflictResolution ConflictResolution { get; set; } = ConflictResolution.Ignore;

        [Description("Whether to include the BHoM_Guid column for object identification.")]
        public virtual bool IncludeBHoMGuid { get; set; } = true;

        [Description("Whether to include timestamp columns for creation and modification tracking.")]
        public virtual bool IncludeTimestamps { get; set; } = true;

        [Description("Custom column mappings from property names to column names.")]
        public virtual Dictionary<string, string> ColumnMappings { get; set; } = new Dictionary<string, string>();

        [Description("Properties to exclude from table schema when auto-creating tables.")]
        public virtual List<string> ExcludedProperties { get; set; } = new List<string>();

        [Description("Whether to create indexes automatically on commonly queried columns.")]
        public virtual bool AutoCreateIndexes { get; set; } = true;

        [Description("Maximum number of rows to process in a single batch operation.")]
        public virtual int BatchSize { get; set; } = 1000;

        /***************************************************/
    }

    /***************************************************/
} 
