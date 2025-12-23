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
using BH.oM.SQLite;
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

        [Description("Creates a SQL command to perform a WAL checkpoint operation.")]
        [Input("checkpointMode", "The checkpoint mode to use for the WAL checkpoint operation.")]
        [Output("command", "SQLCommand that can be executed to perform the WAL checkpoint.")]
        public static SQLCommand WalCheckpointCommand(WalCheckpointMode checkpointMode = WalCheckpointMode.Truncate)
        {
            string modeString = ConvertCheckpointModeToString(checkpointMode);

            SQLCommand command = new SQLCommand()
            {
                Command = $"PRAGMA wal_checkpoint({modeString});",
                Parameters = new Dictionary<string, object>()
            };

            return command;
        }

        /***************************************************/

        [Description("Converts WalCheckpointMode enum to the corresponding SQLite string value.")]
        [Input("mode", "The WAL checkpoint mode enum value.")]
        [Output("modeString", "The corresponding SQLite checkpoint mode string.")]
        private static string ConvertCheckpointModeToString(WalCheckpointMode mode)
        {
            switch (mode)
            {
                case WalCheckpointMode.Passive:
                    return "PASSIVE";
                case WalCheckpointMode.Full:
                    return "FULL";
                case WalCheckpointMode.Restart:
                    return "RESTART";
                case WalCheckpointMode.Truncate:
                default:
                    return "TRUNCATE";
            }
        }

        /***************************************************/
    }
}

