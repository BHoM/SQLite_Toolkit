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
using System.ComponentModel;
using System.Collections.Generic;

namespace BH.oM.SQLite.Objects
{
    /***************************************************/
    /****               Public Classes              ****/
    /***************************************************/

    [Description("Represents the complete schema definition of a SQLite table including columns, indexes, and constraints.")]
    public class TableSchema : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("List of column definitions for this table.")]
        public virtual List<Column> Columns { get; set; } = new List<Column>();

        [Description("List of index definitions for this table.")]
        public virtual List<Index> Indexes { get; set; } = new List<Index>();

        [Description("List of foreign key constraint definitions.")]
        public virtual List<string> ForeignKeys { get; set; } = new List<string>();

        [Description("The SQL CREATE TABLE statement that created this table.")]
        public virtual string CreateStatement { get; set; } = "";

        [Description("Whether this is a temporary table.")]
        public virtual bool IsTemporary { get; set; } = false;

        [Description("Whether this table has a WITHOUT ROWID optimisation.")]
        public virtual bool WithoutRowId { get; set; } = false;

        [Description("Number of rows in the table if available.")]
        public virtual long RowCount { get; set; } = -1;

        [Description("Size of the table in bytes if available.")]
        public virtual long SizeInBytes { get; set; } = -1;

        /***************************************************/
    }

    /***************************************************/
} 