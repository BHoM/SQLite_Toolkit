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
using Microsoft.Data.Sqlite;
using System;
using System.ComponentModel;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Validates that a SQLite database connection is not null and records an appropriate error message if validation fails. \n" +
            "This centralised validation method eliminates duplicate connection checking logic throughout the toolkit.")]
        [Input("connection", "The SQLite database connection to validate for null reference.")]
        [Input("operationName", "Descriptive name of the operation being attempted, used in error messages to provide clear context for debugging.")]
        [Output("isValid", "True if the connection is not null and can be used for database operations, false if the connection is null and an error has been recorded.")]
        private bool ValidateInputParameters(SqliteConnection connection, string operationName)
        {
            if (connection == null)
            {
                BH.Engine.Base.Compute.RecordError($"Cannot {operationName}: database connection is null.");
                return false;
            }
            return true;
        }

        /***************************************************/

        [Description("Validates that a .NET Type object is not null and records an appropriate error message if validation fails. \n" +
            "This centralised validation method ensures consistent error handling for type-related operations throughout the engine.")]
        [Input("objectType", "The .NET Type object to validate for null reference.")]
        [Input("operationName", "Descriptive name of the operation being attempted, used in error messages to provide clear context for debugging.")]
        [Output("isValid", "True if the type is not null and can be used for schema generation or type analysis, false if the type is null and an error has been recorded.")]
        private bool ValidateInputParameters(Type objectType, string operationName)
        {
            if (objectType == null)
            {
                BH.Engine.Base.Compute.RecordError($"Cannot {operationName}: object type is null.");
                return false;
            }
            return true;
        }

        /***************************************************/

        [Description("Validates that a table name is not null, empty, or whitespace and records an appropriate error message if validation fails. \n" +
            "This centralised validation method ensures consistent table name validation and error reporting across all database operations.")]
        [Input("tableName", "The table name string to validate for null, empty, or whitespace values.")]
        [Input("operationName", "Descriptive name of the operation being attempted, used in error messages to provide clear context for debugging.")]
        [Output("isValid", "True if the table name is valid and can be used in SQL operations, false if the table name is invalid and an error has been recorded.")]
        private bool ValidateInputParameters(string tableName, string operationName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                BH.Engine.Base.Compute.RecordError($"Cannot {operationName}: table name is null, empty, or whitespace.");
                return false;
            }
            return true;
        }

        /***************************************************/

        [Description("Validates multiple common input parameters in a single call to reduce code duplication and provide consistent error handling. \n" +
            "This comprehensive validation method handles the most common combination of parameters used throughout the SQLite engine methods.")]
        [Input("connection", "The SQLite database connection to validate for null reference.")]
        [Input("objectType", "The .NET Type object to validate for null reference.")]
        [Input("operationName", "Descriptive name of the operation being attempted, used in error messages to provide clear context for debugging.")]
        [Output("isValid", "True if all parameters are valid and the operation can proceed, false if any parameter is invalid and appropriate errors have been recorded.")]
        private bool ValidateInputParameters(SqliteConnection connection, Type objectType, string operationName)
        {
            if (!ValidateInputParameters(connection, operationName))
                return false;
            
            if (!ValidateInputParameters(objectType, operationName))
                return false;
            
            return true;
        }

        /***************************************************/

        [Description("Validates database connection and table name parameters together, providing comprehensive validation for table-based operations. \n" +
            "This validation method handles the common scenario where both connection and table name validation are required simultaneously.")]
        [Input("connection", "The SQLite database connection to validate for null reference.")]
        [Input("tableName", "The table name string to validate for null, empty, or whitespace values.")]
        [Input("operationName", "Descriptive name of the operation being attempted, used in error messages to provide clear context for debugging.")]
        [Output("isValid", "True if both connection and table name are valid for database operations, false if either parameter is invalid and appropriate errors have been recorded.")]
        private bool ValidateInputParameters(SqliteConnection connection, string tableName, string operationName)
        {
            if (!ValidateInputParameters(connection, operationName))
                return false;
            
            if (!ValidateInputParameters(tableName, operationName))
                return false;
            
            return true;
        }

        /***************************************************/
    }
}
