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

    [Description("Represents the definition of an index in a SQLite database including columns and properties.")]
    public class Index : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("The name of the table this index belongs to.")]
        public virtual string TableName { get; set; } = "";

        [Description("List of column names included in this index in order.")]
        public virtual List<string> Columns { get; set; } = new List<string>();

        [Description("Whether this is a unique index that enforces uniqueness.")]
        public virtual bool IsUnique { get; set; } = false;

        [Description("Whether this is a partial index with a WHERE clause.")]
        public virtual bool IsPartial { get; set; } = false;

        [Description("The WHERE clause for partial indexes.")]
        public virtual string WhereClause { get; set; } = "";

        [Description("The SQL CREATE INDEX statement that created this index.")]
        public virtual string CreateStatement { get; set; } = "";

        [Description("Whether this index was created automatically by SQLite (e.g., for primary keys).")]
        public virtual bool IsAutoIndex { get; set; } = false;

        [Description("Sort order for each column in the index (ASC/DESC). Must match Columns list length.")]
        public virtual List<string> SortOrders { get; set; } = new List<string>();

        /***************************************************/
    }

    /***************************************************/
} 