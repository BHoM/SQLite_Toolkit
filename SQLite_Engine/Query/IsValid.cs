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

using BH.Engine.Base;
using BH.oM.Base.Attributes;
using BH.oM.SQLite;
using BH.oM.SQLite.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace BH.Engine.SQLite
{
    public static partial class Query
    {

        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Validates that a GeneralDomain has compatible Min and Max values with Min <= Max.")]
        [Input("domain", "The GeneralDomain to validate.")]
        [Output("isValid", "True if the domain is valid, false otherwise.")]
        public static bool IsValid(this GeneralDomain domain)
        {
            if (domain == null || domain.Min == null || domain.Max == null)
                return false;

            // Check if both are numeric types
            if (domain.Min.IsNumeric() && domain.Max.IsNumeric())
            {
                double minVal = System.Convert.ToDouble(domain.Min);
                double maxVal = System.Convert.ToDouble(domain.Max);
                return minVal <= maxVal;
            }

            // Check if both are DateTime
            if (domain.Min is DateTime minDate && domain.Max is DateTime maxDate)
            {
                return minDate <= maxDate;
            }

            // Types must match for other comparable types
            if (domain.Min.GetType() == domain.Max.GetType() && domain.Min is IComparable minComp && domain.Max is IComparable maxComp)
            {
                return minComp.CompareTo(maxComp) <= 0;
            }

            return false;
        }

        /***************************************************/

        [Description("Validates that a string is safe for use in SQL queries as column name, table name, or SQL command.")]
        [Input("value", "The string value to validate.")]
        [Input("validationType", "Type of validation: 'column', 'table', or 'command'.")]
        [Output("isValid", "True if the value is safe to use, false otherwise.")]
        public static bool IsValid(string value, string validationType = "column")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                BH.Engine.Base.Compute.RecordError($"{validationType} cannot be null or empty.");
                return false;
            }

            string lowerValue = value.ToLowerInvariant().Trim();

            switch (validationType.ToLowerInvariant())
            {
                case "column":
                    return ValidateColumnName(value, lowerValue);
                case "table":
                    return ValidateTableName(value, lowerValue);
                case "command":
                    return ValidateSqlCommand(value, lowerValue);
                default:
                    BH.Engine.Base.Compute.RecordWarning($"Unknown validation type '{validationType}'. Defaulting to column validation.");
                    return ValidateColumnName(value, lowerValue);
            }
        }

        /***************************************************/

        [Description("Validates that a single column name is safe for use in SQL queries.")]
        [Input("columnName", "The column name to validate.")]
        [Output("isValid", "True if the column name is safe to use, false otherwise.")]
        public static bool IsValid(string columnName)
        {
            return IsValid(columnName, "column");
        }

        /***************************************************/

        [Description("Validates that column names are safe for use in SQL queries. \n" +
            "Checks for SQL injection attempts and ensures proper formatting.")]
        [Input("columnNames", "The column names to validate.")]
        [Output("isValid", "True if all column names are safe to use, false otherwise.")]
        public static bool IsValid(IEnumerable<string> columnNames)
        {
            if (columnNames == null)
                return false;

            foreach (string columnName in columnNames)
            {
                if (!IsValid(columnName))
                    return false;
            }

            return true;
        }

        /***************************************************/

        [Description("Validates that a table name is safe for use in SQL queries. \n" +
            "Checks for SQL injection attempts and ensures proper formatting.")]
        [Input("tableName", "The table name to validate.")]
        [Output("isValid", "True if the table name is safe to use, false otherwise.")]
        public static bool IsValid(string tableName, bool isTableName)
        {
            return isTableName ? IsValid(tableName, "table") : IsValid(tableName, "column");
        }

        /***************************************************/

        [Description("Validates that all data rows in a TableData object are compatible with the defined schema.")]
        [Input("tableData", "The TableData object to validate.")]
        [Output("isValid", "True if all rows are valid against the schema.")]
        public static bool IsValid(this Table tableData)
        {
            if (tableData == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot validate data: tableData is null.");
                return false;
            }

            if (tableData.Schema?.Columns == null || !tableData.Schema.Columns.Any())
            {
                BH.Engine.Base.Compute.RecordWarning("Cannot validate data: no schema columns defined.");
                return false;
            }

            if (tableData.Rows == null || !tableData.Rows.Any())
                return true; // Empty data is valid

            int rowsToCheck = tableData.MaxValidationRows < 0 ? tableData.Rows.Count : Math.Min(tableData.MaxValidationRows, tableData.Rows.Count);

            for (int i = 0; i < rowsToCheck; i++)
            {
                Dictionary<string, object> row = tableData.Rows[i];
                if (!ValidateRow(row, tableData.Schema.Columns))
                {
                    BH.Engine.Base.Compute.RecordError($"Data validation failed at row {i + 1}.");
                    return false;
                }
            }

            return true;
        }

        /***************************************************/

        [Description("Validates that a property path exists and is accessible on the specified object type, with comprehensive support for dot notation traversal. \n" +
            "This method recursively validates nested property access chains, ensuring each property in the path is publicly accessible and properly typed.")]
        [Input("type", "The root .NET Type object on which to validate the property path. This serves as the starting point for property traversal and validation.")]
        [Input("propertyPath", "The property access path to validate, using dot notation for nested properties (e.g., 'Position.X', 'Material.Properties.Density'). Simple property names are also supported.")]
        [Output("isValid", "True if the complete property path exists and all intermediate properties are accessible, false if any part of the path is invalid, non-existent, or inaccessible.")]
        public static bool IsValid(this Type type, string propertyPath)
        {
            if (type == null || string.IsNullOrWhiteSpace(propertyPath))
                return false;

            try
            {
                // Handle simple property access
                if (!propertyPath.Contains("."))
                {
                    PropertyInfo property = type.GetProperty(propertyPath);
                    return property != null;
                }

                // Handle nested property access
                string[] propertyParts = propertyPath.Split('.');
                Type currentType = type;

                foreach (string propertyName in propertyParts)
                {
                    PropertyInfo property = currentType.GetProperty(propertyName);
                    if (property == null)
                        return false;

                    currentType = property.PropertyType;
                }

                return true;
            }
            catch (Exception ex)
            {
                Engine.Base.Compute.RecordWarning($"Error validating property path '{propertyPath}' for type '{type.Name}': {ex.Message}");
                return false;
            }
        }

        /***************************************************/

        [Description("Validates that a SQL command is safe for execution by checking for dangerous patterns and injection attempts.")]
        [Input("sqlCommand", "The SQL command text to validate.")]
        [Output("isValid", "True if the SQL command appears safe to execute, false if potentially dangerous patterns are detected.")]
        public static bool IsValidSqlCommand(string sqlCommand)
        {
            return IsValid(sqlCommand, "command");
        }

        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private static bool ValidateColumnName(string columnName, string lowerName)
        {
            // Check for dangerous patterns
            foreach (string pattern in DangerousSqlPatterns)
            {
                if (lowerName.Contains(pattern))
                {
                    BH.Engine.Base.Compute.RecordWarning($"Column name '{columnName}' contains dangerous SQL pattern: {pattern}");
                    return false;
                }
            }

            // Check for forbidden characters
            if (columnName.IndexOfAny(ForbiddenChars) >= 0)
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

            // Must start with letter or underscore
            if (!char.IsLetter(columnName[0]) && columnName[0] != '_')
            {
                BH.Engine.Base.Compute.RecordWarning($"Column name '{columnName}' must start with a letter or underscore.");
                return false;
            }

            return true;
        }

        /***************************************************/

        private static bool ValidateTableName(string tableName, string lowerName)
        {
            // Check for forbidden keywords
            foreach (string keyword in ForbiddenKeywords)
            {
                if (lowerName.Contains(keyword))
                {
                    BH.Engine.Base.Compute.RecordWarning($"Table name '{tableName}' contains forbidden keyword: {keyword}");
                    return false;
                }
            }

            // Check for forbidden characters
            if (tableName.IndexOfAny(ForbiddenChars) >= 0)
            {
                BH.Engine.Base.Compute.RecordWarning($"Table name '{tableName}' contains forbidden characters.");
                return false;
            }

            // Check length
            if (tableName.Length > 999)
            {
                BH.Engine.Base.Compute.RecordWarning($"Table name '{tableName}' is too long (max 999 characters).");
                return false;
            }

            // Must start with letter or underscore
            if (!char.IsLetter(tableName[0]) && tableName[0] != '_')
            {
                BH.Engine.Base.Compute.RecordWarning($"Table name '{tableName}' must start with a letter or underscore.");
                return false;
            }

            return true;
        }

        /***************************************************/

        private static bool ValidateSqlCommand(string sqlCommand, string lowerCommand)
        {
            // Check for multiple statements (potential injection)
            if (lowerCommand.Count(c => c == ';') > 1)
            {
                BH.Engine.Base.Compute.RecordWarning("SQL command contains multiple statements (multiple semicolons). This may indicate SQL injection.");
                return false;
            }

            // Check for dangerous patterns
            foreach (string pattern in DangerousSqlPatterns)
            {
                if (lowerCommand.Contains(pattern))
                {
                    BH.Engine.Base.Compute.RecordError($"SQL command contains potentially dangerous pattern: '{pattern}'. Command rejected for security.");
                    return false;
                }
            }

            // Check allowed operations
            bool isAllowedOperation = AllowedSqlStarts.Any(allowed => lowerCommand.StartsWith(allowed));
            if (!isAllowedOperation)
            {
                BH.Engine.Base.Compute.RecordWarning($"SQL command does not start with a recognized safe operation. Command: {sqlCommand.Substring(0, Math.Min(50, sqlCommand.Length))}...");
                return false;
            }

            // Check command length
            if (sqlCommand.Length > 10000)
            {
                BH.Engine.Base.Compute.RecordWarning("SQL command is excessively long (>10000 characters). This may indicate an injection attempt.");
                return false;
            }

            return true;
        }

        private static bool ValidateColumnNameLength(string columnName)
        {
            if (columnName.Length > 999)
            {
                BH.Engine.Base.Compute.RecordWarning($"Column name '{columnName}' is too long (max 999 characters).");
                return false;
            }

            return true;
        }

        /***************************************************/

        private static bool ValidateRow(Dictionary<string, object> row, List<Column> columns)
        {
            if (row == null)
                return false;

            foreach (Column column in columns)
            {
                if (row.ContainsKey(column.Name))
                {
                    object value = row[column.Name];
                    if (!ValidateColumnValue(value, column))
                        return false;
                }
                else if (!column.AllowNull && string.IsNullOrEmpty(column.DefaultValue))
                {
                    return false; // Required column missing
                }
            }

            return true;
        }

        private static bool ValidateColumnValue(object value, Column column)
        {
            if (value == null)
                return column.AllowNull;

            // Basic type validation based on SQLite data types
            switch (column.DataType)
            {
                case SqliteDataType.INTEGER:
                    return IsIntegerType(value);
                case SqliteDataType.REAL:
                    return IsNumericType(value);
                case SqliteDataType.TEXT:
                    return ValidateTextLength(value, column);
                case SqliteDataType.BLOB:
                    return value is byte[] || value is System.IO.Stream;
                case SqliteDataType.NUMERIC:
                    return IsNumericType(value);
                default:
                    return true; // Default to accepting any value
            }
        }

        private static bool IsIntegerType(object value)
        {
            return value is sbyte || value is byte || value is short || value is ushort ||
                   value is int || value is uint || value is long || value is ulong;
        }

        private static bool IsNumericType(object value)
        {
            return IsIntegerType(value) || value is float || value is double || value is decimal;
        }

        private static bool ValidateTextLength(object value, Column column)
        {
            if (column.MaxLength <= 0)
                return true;

            string textValue = value?.ToString() ?? "";
            return textValue.Length <= column.MaxLength;
        }

        /***************************************************/
        /**** Constants                                 ****/
        /***************************************************/

        // SQL Security constants
        private static readonly string[] DangerousSqlPatterns = {
            "; drop", "; delete", "; insert", "; update", "; create", "; alter", "; truncate",
            "; exec", "; execute", "union select", "union all select", "' or '", "\" or \"",
            "' or 1=1", "\" or 1=1", "' union", "\" union", "/*", "*/", "--",
            "xp_", "sp_", "exec(", "execute(", "eval(", "script", "javascript"
        };

        private static readonly string[] AllowedSqlStarts = {
            "select", "create table", "create index", "insert into", "update", "delete from",
            "pragma", "with", "drop table", "drop index", "alter table"
        };

        private static readonly string[] ForbiddenKeywords = {
            "drop", "delete", "insert", "update", "create", "alter", "truncate", "--", "/*", "*/"
        };

        private static readonly char[] ForbiddenChars = {
            ';', '\'', '"', '\\', '\n', '\r', '\t', ' ', '-', '.'
        };

        /***************************************************/

    }
}
