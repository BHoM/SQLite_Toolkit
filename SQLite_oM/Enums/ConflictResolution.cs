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

using System.ComponentModel;

namespace BH.oM.SQLite
{
    /***************************************************/
    /****               Public Enums               ****/
    /***************************************************/

    [Description("Defines SQLite conflict resolution strategies for handling data conflicts during Insert or Update operations.")]
    public enum ConflictResolution
    {
        [Description("Undefined conflict resolution strategy.")]
        Undefined,

        [Description("Abort the operation when a conflict occurs (default SQLite behaviour).")]
        Abort,

        [Description("Fail the operation and return an error when a conflict occurs.")]
        Fail,

        [Description("Ignore the conflicting row and continue processing.")]
        Ignore,

        [Description("Replace the existing row with the new data when a conflict occurs.")]
        Replace,

        [Description("Rollback the entire transaction when a conflict occurs.")]
        Rollback
    }

    /***************************************************/
} 