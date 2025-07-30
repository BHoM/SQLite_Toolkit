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
using System;
using BH.oM.SQLite;
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

        [Description("Validates that all data rows in a TableData object are compatible with the defined schema.")]
        [Input("tableData", "The TableData object to validate.")]
        [Output("isValid", "True if all rows are valid against the schema.")]
        public static bool ValidateData(Table tableData)
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
        /**** Private Methods                           ****/
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
    }
} 