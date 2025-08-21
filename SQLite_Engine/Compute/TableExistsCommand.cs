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

        [Description("Creates a SQL command to check if a table exists in the database.")]
        [Input("tableName", "The table name to check for existence.")]
        [Output("command", "SQLCommand that can be executed to check table existence. Returns 1 if table exists, 0 if not.")]
        public static SQLCommand TableExistsCommand(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                BH.Engine.Base.Compute.RecordError("Cannot create table exists command: table name is null or empty.");
                return null;
            }

            SQLCommand command = new SQLCommand()
            {
                Command = @"
                    SELECT COUNT(*) 
                    FROM sqlite_master 
                    WHERE type='table' AND name=@TableName",
                Parameters = new Dictionary<string, object>
                {
                    { "@TableName", tableName }
                }
            };

            return command;
        }

        /***************************************************/
    }
}
