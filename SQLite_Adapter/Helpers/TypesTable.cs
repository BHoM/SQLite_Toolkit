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
using BH.oM.SQLite.Commands;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Creates the __Types system table for storing type-to-table mappings.")]
        [Input("connection", "Active SQLite database connection.")]
        [Output("success", "True if the table was created successfully, false otherwise.")]
        private bool TypesTable(SqliteConnection connection)
        {
            if (connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create __Types table: connection is null.");
                return false;
            }

            try
            {
                // Use Engine method to generate the command
                SQLCommand command = BH.Engine.SQLite.Compute.CreateTypesTableCommand();
                if (command == null)
                    return false;

                // Execute the command using the existing ExecuteCommand method
                Output<List<object>, bool> result = ExecuteCommand(command);
                return result.Item2;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Error creating __Types table: {ex.Message}");
                return false;
            }
        }

        /***************************************************/
    }
}

