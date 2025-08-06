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
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Applies parameters from a FilterResult to a SqliteCommand for safe query execution.")]
        [Input("command", "The SqliteCommand to add parameters to.")]
        [Input("filterResult", "The FilterResult containing the parameters to apply.")]
        [Output("success", "True if parameters were applied successfully, false otherwise.")]
        public static bool ApplyFilterParameters(SqliteCommand command, FilterResult filterResult)
        {
            if (command == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot apply filter parameters: command is null.");
                return false;
            }

            if (filterResult == null || filterResult.Parameters == null)
            {
                // No parameters to apply - this is valid for queries without filters
                return true;
            }

            foreach (KeyValuePair<string, object> parameter in filterResult.Parameters)
            {
                string paramName = parameter.Key;
                object paramValue = parameter.Value;

                // Ensure parameter name starts with @
                if (!paramName.StartsWith("@"))
                {
                    paramName = "@" + paramName;
                }

                // Convert value to appropriate SQLite type
                object sqliteValue = ConvertToSqliteValue(paramValue);
                
                command.Parameters.AddWithValue(paramName, sqliteValue);
            }

            BH.Engine.Base.Compute.RecordNote($"Applied {filterResult.Parameters.Count} parameters to SQL command.");
            return true;
        }

        /***************************************************/
    }
}
