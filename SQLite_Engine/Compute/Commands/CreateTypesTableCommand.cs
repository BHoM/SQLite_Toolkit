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
using BH.oM.SQLite.Commands;
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Creates a SQL command to create the __Types system table for storing type-to-table mappings.")]
        [Output("command", "SQLCommand that can be executed to create the __Types system table.")]
        public static SQLCommand CreateTypesTableCommand()
        {
            SQLCommand command = new SQLCommand()
            {
                Command = @"
                    CREATE TABLE IF NOT EXISTS __Types (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FullTypeName TEXT NOT NULL UNIQUE,
                        TableName TEXT NOT NULL,
                        DateCreated DATETIME DEFAULT CURRENT_TIMESTAMP,
                        LastModified DATETIME DEFAULT CURRENT_TIMESTAMP
                    )",
                Parameters = new Dictionary<string, object>()
            };

            return command;
        }

        /***************************************************/
    }
}
