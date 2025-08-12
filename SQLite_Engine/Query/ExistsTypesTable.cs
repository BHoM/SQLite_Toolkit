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
using System.ComponentModel;

namespace BH.Engine.SQLite
{
    public static partial class Query
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Verifies that the __Types system table exists within the database and creates it automatically if absent. \n" +
            "This method ensures the type management infrastructure is properly initialised before performing type registration operations.")]
        [Input("connection", "Active SQLite database connection with appropriate write permissions. The connection should be in an open state and support table creation operations.")]
        [Output("exists", "True if the __Types table already existed or was successfully created during this operation, false if table creation failed due to database errors or permission restrictions.")]
        public static bool ExistsTypesTable(this SqliteConnection connection)
        {
            if (connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot ensure __Types table: connection is null.");
                return false;
            }

            // Check if table already exists to avoid duplicate messages
            bool tableExists = connection.TableExists("__Types");
            bool success = Create.TypesTable(connection);
            
            // Only show success message if table was actually created (didn't exist before)
            if (success && !tableExists)
                BH.Engine.Base.Compute.RecordNote("Successfully created __Types system table.");
            
            return success;
        }

        /***************************************************/
    }
}
