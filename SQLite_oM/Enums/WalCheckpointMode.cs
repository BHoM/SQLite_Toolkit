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

using System.ComponentModel;

namespace BH.oM.SQLite
{
    /***************************************************/
    /**** Public Enums                              ****/
    /***************************************************/

    [Description("Defines the mode for WAL checkpoint operations in SQLite databases.")]
    public enum WalCheckpointMode
    {
        [Description("Passive checkpoint - checkpoint as many frames as possible without waiting for any database readers or writers to finish.")]
        Passive,

        [Description("Full checkpoint - blocks until there are no database writers and all readers are reading from the most recent database snapshot.")]
        Full,

        [Description("Restart checkpoint - like FULL but also truncates the WAL file to zero bytes upon successful completion.")]
        Restart,

        [Description("Truncate checkpoint - like RESTART but also resets the checkpoint pointer to the beginning of the WAL file.")]
        Truncate
    }

    /***************************************************/
}

