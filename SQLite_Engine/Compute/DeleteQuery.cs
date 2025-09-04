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

using BH.oM.Base.Attributes;
using BH.oM.SQLite.Objects;
using System.ComponentModel;
using System.Text;

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Builds a parameterised DELETE query from a FilterResult and table information.")]
        [Input("tableName", "The name of the table to delete from.")]
        [Input("filterResult", "The filter result containing WHERE clause and parameters. Required for safety.")]
        [Output("sql", "Complete SQL DELETE statement, or null if construction failed.")]
        public static string DeleteQuery(string tableName, FilterCommand filterResult)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                BH.Engine.Base.Compute.RecordError("Cannot build DELETE query: table name is null or empty.");
                return null;
            }

            if (filterResult == null || string.IsNullOrWhiteSpace(filterResult.WhereClause))
            {
                BH.Engine.Base.Compute.RecordError("Cannot build DELETE query: filter result with WHERE clause is required for safety.");
                return null;
            }

            StringBuilder sql = new StringBuilder();

            // DELETE FROM clause
            sql.Append($"DELETE FROM \"{tableName}\"");

            // WHERE clause (required)
            sql.Append($" WHERE {filterResult.WhereClause}");

            return sql.ToString();
        }

        /***************************************************/
    }
}
