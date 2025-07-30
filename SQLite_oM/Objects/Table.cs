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
using System.ComponentModel;
using System.Collections.Generic;

namespace BH.oM.SQLite.Objects
{
    /***************************************************/
    /****               Public Classes              ****/
    /***************************************************/

    [Description("Represents a combination of table schema definition and data rows for atomic table creation and population operations.")]
    public class Table : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("The table schema definition including columns, indexes, and constraints.")]
        public virtual TableSchema Schema { get; set; } = new TableSchema();

        [Description("The data rows to insert into the table. Each dictionary represents a row with column names as keys.")]
        public virtual List<Dictionary<string, object>> Rows { get; set; } = new List<Dictionary<string, object>>();

        [Description("Configuration settings for table creation and data insertion operations.")]
        public virtual TableConfig TableConfig { get; set; } = new TableConfig();

        [Description("Whether to create the table if it doesn't exist before inserting data.")]
        public virtual bool CreateTableIfNotExists { get; set; } = true;

        [Description("Whether to drop and recreate the table if it already exists.")]
        public virtual bool DropTableIfExists { get; set; } = false;

        [Description("Whether to validate that all row data matches the schema before insertion.")]
        public virtual bool ValidateDataAgainstSchema { get; set; } = true;

        [Description("Whether to automatically add missing columns to the schema based on data row keys.")]
        public virtual bool AutoExpandSchema { get; set; } = false;

        [Description("Default data type to use for auto-expanded columns when data type cannot be inferred.")]
        public virtual SqliteDataType DefaultDataType { get; set; } = SqliteDataType.TEXT;

        [Description("Maximum number of rows to validate against schema. Set to -1 to validate all rows.")]
        public virtual int MaxValidationRows { get; set; } = 100;

        /***************************************************/
    }

    /***************************************************/
} 