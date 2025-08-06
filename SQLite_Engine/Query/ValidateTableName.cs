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
using System.ComponentModel;

namespace BH.Engine.SQLite
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Validates that a table name is safe for use in SQL queries. \n" +
            "Checks for SQL injection attempts and ensures proper formatting.")]
        [Input("tableName", "The table name to validate.")]
        [Output("isValid", "True if the table name is safe to use, false otherwise.")]
        public static bool ValidateTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return false;

            // Check for basic SQL injection patterns
            string lowerName = tableName.ToLowerInvariant();
            string[] forbiddenKeywords = { "drop", "delete", "insert", "update", "create", "alter", "truncate", "--", "/*", "*/" };
            
            foreach (string keyword in forbiddenKeywords)
            {
                if (lowerName.Contains(keyword))
                {
                    BH.Engine.Base.Compute.RecordWarning($"Table name '{tableName}' contains forbidden keyword: {keyword}");
                    return false;
                }
            }

            // Check for dangerous characters
            char[] forbiddenChars = { ';', '\'', '"', '\\', '\n', '\r', '\t' };
            if (tableName.IndexOfAny(forbiddenChars) >= 0)
            {
                BH.Engine.Base.Compute.RecordWarning($"Table name '{tableName}' contains forbidden characters.");
                return false;
            }

            // Check length (SQLite has a default limit of 1000 characters for identifiers)
            if (tableName.Length > 999)
            {
                BH.Engine.Base.Compute.RecordWarning($"Table name '{tableName}' is too long (max 999 characters).");
                return false;
            }

            return true;
        }

        /***************************************************/
    }
}
