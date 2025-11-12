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

        [Description("Creates all system tables required for the SQLite Toolkit.")]
        [Input("connection", "Active SQLite database connection.")]
        [Output("success", "True if all system tables were created successfully, false otherwise.")]
        private bool AllSystemTables(SqliteConnection connection)
        {
            try
            {
                // Check if __Types table exists before creating
                bool typesExists = TableExists(connection, "__Types");
                bool typesSuccess;
                
                if (typesExists)
                {
                    BH.Engine.Base.Compute.RecordNote("System table '__Types' already exists.");
                    typesSuccess = true;
                }
                else
                {
                    typesSuccess = TypesTable(connection);
                    if (typesSuccess)
                    {
                        BH.Engine.Base.Compute.RecordNote("Successfully created system table '__Types'.");
                    }
                }

                // Check if __Schema table exists before creating
                bool schemaExists = TableExists(connection, "__Schema");
                bool schemaSuccess;
                
                if (schemaExists)
                {
                    BH.Engine.Base.Compute.RecordNote("System table '__Schema' already exists.");
                    schemaSuccess = true;
                }
                else
                {
                    schemaSuccess = SchemaTable(connection);
                    if (schemaSuccess)
                    {
                        BH.Engine.Base.Compute.RecordNote("Successfully created system table '__Schema'.");
                    }
                }

                if (typesSuccess && schemaSuccess)
                {
                    if (typesExists && schemaExists)
                    {
                        BH.Engine.Base.Compute.RecordNote("All system tables were already present and verified.");
                    }
                    else if (!typesExists || !schemaExists)
                    {
                        BH.Engine.Base.Compute.RecordNote("System tables initialised successfully - some tables were created.");
                    }
                    return true;
                }
                else
                {
                    BH.Engine.Base.Compute.RecordWarning("Failed to create one or more system tables.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Error creating system tables: {ex.Message}");
                return false;
            }
        }

        /***************************************************/
    }
}
