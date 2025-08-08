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

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Creates an optimised table for IRecord objects with all primitive properties as columns.")]
        [Input("connection", "Active SQLite database connection.")]
        [Input("recordType", "The .NET type that implements IRecord interface.")]
        [Input("dropIfExists", "Whether to drop the table first if it already exists. Default is false.")]
        [Output("success", "True if table was created successfully, false otherwise.")]
        public static bool CreateIRecordTable(SqliteConnection connection, Type recordType, bool dropIfExists = false)
        {
            if (!BH.Engine.SQLite.Query.IsIRecord(recordType))
            {
                BH.Engine.Base.Compute.RecordError($"Type {recordType.FullName} does not implement IRecord interface.");
                return false;
            }

            // Use the smart table creation with no config (will use all primitive properties)
            return CreateTableForObjectType(connection, recordType, null, dropIfExists);
        }

        /***************************************************/
    }
}
