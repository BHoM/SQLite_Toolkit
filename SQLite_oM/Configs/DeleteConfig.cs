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

using BH.oM.Adapter;
using BH.oM.Base.Attributes;
using System.ComponentModel;

namespace BH.oM.SQLite.Configs
{
    /***************************************************/
    /****               Public Classes               ****/
    /***************************************************/

    [Description("Configuration for deleting objects from SQLite database with intelligent filtering capabilities.")]
    public class DeleteConfig : ActionConfig
    {
        /***************************************************/
        /**** Properties                                ****/
        /***************************************************/



        [Description("Whether to require at least one filter condition. If true, prevents accidental deletion of entire table. Default is true.")]
        public virtual bool RequireFilterConditions { get; set; } = true;

        [Description("Maximum number of rows to delete in a single operation. If 0, no limit is applied. Use with caution.")]
        public virtual int MaxRowsToDelete { get; set; } = 0;

        [Description("Whether to perform a dry run that returns the count of rows that would be deleted without actually deleting them.")]
        public virtual bool DryRun { get; set; } = false;

        /***************************************************/
    }

    /***************************************************/
}

