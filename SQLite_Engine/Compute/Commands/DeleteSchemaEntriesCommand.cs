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

        [Description("Creates a SQL command to delete existing schema entries for a table in the __Schema system table.")]
        [Input("tableName", "The table name to delete schema entries for.")]
        [Output("command", "SQLCommand that can be executed to delete existing schema entries.")]
        public static SQLCommand DeleteSchemaEntriesCommand(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                BH.Engine.Base.Compute.RecordError("Cannot create delete schema entries command: table name is null or empty.");
                return null;
            }

            SQLCommand command = new SQLCommand()
            {
                Command = "DELETE FROM __Schema WHERE TableName = @TableName",
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

