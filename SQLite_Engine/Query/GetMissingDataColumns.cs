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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.SQLite
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Gets a list of required columns (non-nullable) that are missing from data rows.")]
        [Input("tableData", "The TableData object to analyse.")]
        [Output("columns", "List of required column names missing from data.")]
        public static List<string> GetMissingDataColumns(Table tableData)
        {
            if (tableData == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot analyse missing data columns: tableData is null.");
                return new List<string>();
            }

            if (tableData.Schema?.Columns == null || !tableData.Schema.Columns.Any())
                return new List<string>();

            if (tableData.Rows == null || !tableData.Rows.Any())
                return tableData.Schema.Columns.Where(c => !c.AllowNull).Select(c => c.Name).ToList();

            HashSet<string> requiredColumns = tableData.Schema.Columns
                .Where(c => !c.AllowNull && string.IsNullOrEmpty(c.DefaultValue))
                .Select(c => c.Name)
                .ToHashSet();

            HashSet<string> dataColumns = new HashSet<string>();
            foreach (var row in tableData.Rows)
            {
                foreach (string key in row.Keys)
                {
                    dataColumns.Add(key);
                }
            }

            return requiredColumns.Where(col => !dataColumns.Contains(col)).ToList();
        }

        /***************************************************/
    }
} 