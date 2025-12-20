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
using BH.oM.Quantities.Attributes;
using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace BH.oM.SQLite.Objects
{
    /***************************************************/
    /****               Public Classes              ****/
    /***************************************************/

    [Description("Represents the result of a SQLite query operation including data, metadata, and execution information.")]
    public class QueryResult : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("The data rows returned by the query. Each row is represented as a dictionary of column name to value.")]
        public virtual List<Dictionary<string, object>> Data { get; set; } = new List<Dictionary<string, object>>();

        [Description("The column names in the result set in order.")]
        public virtual List<string> ColumnNames { get; set; } = new List<string>();

        [Description("The number of rows returned in the result set.")]
        public virtual int RowCount { get; set; } = 0;

        [Description("The number of rows affected by the query (for INSERT, UPDATE, DELETE operations).")]
        public virtual int AffectedRows { get; set; } = 0;

        [Description("The time taken to execute the query. Returned in milliseconds but converted as per BHoM conventions.")]
        [Time]
        public virtual double ExecutionTime { get; set; } = 0;

        [Description("The SQL query that was executed.")]
        public virtual string ExecutedQuery { get; set; } = "";

        [Description("Whether the query executed successfully.")]
        public virtual bool IsSuccess { get; set; } = true;

        [Description("Error message if the query failed.")]
        public virtual string ErrorMessage { get; set; } = "";

        [Description("When the query was executed.")]
        public virtual DateTime ExecutedAt { get; set; } = DateTime.Now;

        [Description("Whether this result was cached or came directly from the database.")]
        public virtual bool IsCached { get; set; } = false;

        [Description("Additional metadata about the query execution.")]
        public virtual Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /***************************************************/
    }

    /***************************************************/
} 
