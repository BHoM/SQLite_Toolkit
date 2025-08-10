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
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.Engine.SQLite
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Validates that a single column name is safe for use in SQL queries.")]
        [Input("columnName", "The column name to validate.")]
        [Output("isValid", "True if the column name is safe to use, false otherwise.")]
        public static bool ValidateColumnName(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return false;

            // Check for exact SQL keywords (not substrings)
            string lowerName = columnName.ToLowerInvariant();
            string[] forbiddenKeywords = { "drop", "delete", "insert", "update", "create", "alter", "truncate", "select", "from", "where", "join", "union", "exec", "execute" };
            
            foreach (string keyword in forbiddenKeywords)
            {
                if (lowerName.Equals(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    BH.Engine.Base.Compute.RecordWarning($"Column name '{columnName}' cannot be a reserved SQL keyword: {keyword}");
                    return false;
                }
            }

            // Check for SQL injection comment patterns
            if (lowerName.Contains("--") || lowerName.Contains("/*") || lowerName.Contains("*/"))
            {
                BH.Engine.Base.Compute.RecordWarning($"Column name '{columnName}' contains SQL comment patterns.");
                return false;
            }

            // Check for dangerous characters
            char[] forbiddenChars = { ';', '\'', '"', '\\', '\n', '\r', '\t', ' ', '-', '.' };
            if (columnName.IndexOfAny(forbiddenChars) >= 0)
            {
                BH.Engine.Base.Compute.RecordWarning($"Column name '{columnName}' contains forbidden characters.");
                return false;
            }

            // Check length
            if (columnName.Length > 999)
            {
                BH.Engine.Base.Compute.RecordWarning($"Column name '{columnName}' is too long (max 999 characters).");
                return false;
            }

            // Must start with letter or underscore (not digit)
            if (!char.IsLetter(columnName[0]) && columnName[0] != '_')
            {
                BH.Engine.Base.Compute.RecordWarning($"Column name '{columnName}' must start with a letter or underscore, not a digit or special character.");
                return false;
            }

            return true;
        }

        /***************************************************/

        [Description("Validates that column names are safe for use in SQL queries. \n" +
            "Checks for SQL injection attempts and ensures proper formatting.")]
        [Input("columnNames", "The column names to validate.")]
        [Output("isValid", "True if all column names are safe to use, false otherwise.")]
        public static bool ValidateColumnName(IEnumerable<string> columnNames)
        {
            if (columnNames == null)
                return false;

            foreach (string columnName in columnNames)
            {
                if (!ValidateColumnName(columnName))
                    return false;
            }

            return true;
        }

        /***************************************************/
    }
}
