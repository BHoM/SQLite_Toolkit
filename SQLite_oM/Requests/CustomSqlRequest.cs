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

    [Description("Request for executing custom SQL queries against SQLite databases with parameterised support.")]
    public class CustomSqlRequest : BHoMObject, IRequest
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("The SQL query to execute. Use parameter placeholders (@param) for parameterised queries.")]
        public virtual string SqlQuery { get; set; } = "";

        [Description("Parameters for the SQL query. Key is parameter name, value is parameter value.")]
        public virtual Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        [Description("Query timeout in seconds for long-running operations.")]
        public virtual int TimeoutSeconds { get; set; } = 30;

        [Description("Whether this is a read-only query (SELECT) or a write operation (INSERT, UPDATE, DELETE).")]
        public virtual bool IsReadOnly { get; set; } = true;

        [Description("Whether to return the number of affected rows for write operations.")]
        public virtual bool ReturnAffectedRowCount { get; set; } = false;

        /***************************************************/
    }

    /***************************************************/
} 