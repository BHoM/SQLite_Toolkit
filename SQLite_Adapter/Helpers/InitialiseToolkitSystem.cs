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

        [Description("Initialises the complete SQLite Toolkit system including all system tables.")]
        [Input("connection", "Active SQLite database connection.")]
        [Output("success", "True if the system was initialised successfully, false otherwise.")]
        private bool InitialiseToolkitSystem(SqliteConnection connection)
        {
            if (connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot initialise toolkit system: connection is null.");
                return false;
            }

            try
            {
                BH.Engine.Base.Compute.RecordNote("Initialising SQLite Toolkit...");

                // Create all system tables
                bool systemTablesCreated = AllSystemTables(connection);
                if (!systemTablesCreated)
                {
                    BH.Engine.Base.Compute.RecordError("Failed to create system tables.");
                    return false;
                }

                // Verify system integrity
                bool systemValid = VerifySystemIntegrity(connection);
                if (!systemValid)
                {
                    BH.Engine.Base.Compute.RecordError("System integrity check failed.");
                    return false;
                }

                BH.Engine.Base.Compute.RecordNote("SQLite Toolkit initialised successfully.");
                return true;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Error initialising toolkit : {ex.Message}");
                return false;
            }
        }

        /***************************************************/
    }
}

