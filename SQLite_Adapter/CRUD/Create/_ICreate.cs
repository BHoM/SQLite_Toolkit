/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2024, the respective contributors. All rights reserved.
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
using BH.oM.Base;
using BH.oM.SQLite.Objects;
using BH.oM.SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter : BHoMAdapter
    {
        // NOTE: CRUD folder methods
        // All methods in the CRUD folder are used as "back-end" methods by the Adapter itself.
        // They are automatically invoked by the Adapter Actions (Push, Pull, etc.).
        // Specifically, the Create is primarily called by the Push (in the context of the CRUD method, and also by other methods that require it: Update, UpdateProperty).

        // The Create should only contain the logic that generates the objects in the external software.
        // Note: With simplified scope, users manage their own table creation and data insertion.
        // This adapter focuses on connection management and query execution.
        protected override bool ICreate<T>(IEnumerable<T> objects, ActionConfig actionConfig = null)
        {
            if (m_Connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create objects: no database connection. Please open a connection first.");
                return false;
            }

            // Update last used timestamp
            m_LastUsed = DateTime.Now;

            bool success = true;

            foreach (T obj in objects)
            {
                success &= Create(obj as dynamic);
            }

            // Perform WAL checkpoint after push operation if WAL mode is enabled
            if (m_WalModeEnabled && success)
            {
                BH.Engine.SQLite.Compute.WalCheckpoint(m_Connection, "TRUNCATE");
            }

            return success;
        }

        /***************************************************/

        // Fallback case. If no specific Create is found, here we should handle what happens then.
        protected bool Create(IBHoMObject obj)
        {
            BH.Engine.Base.Compute.RecordError($"No specific Create method found for {obj.GetType().Name}.");
            return false;
        }

        /***************************************************/
        /**** Private Helper Methods                   ****/
        /***************************************************/

        private string GetConflictClause(ConflictResolution conflictResolution)
        {
            switch(conflictResolution)
            {
                case ConflictResolution.Replace:
                    return "OR REPLACE";
                case ConflictResolution.Ignore:
                    return "OR IGNORE";
                case ConflictResolution.Fail:
                    return "OR FAIL";
                case ConflictResolution.Abort:
                    return "OR FAIL"; // OR ABORT is not valid for INSERT statements, use OR FAIL instead
                case ConflictResolution.Rollback:
                    return "OR ROLLBACK";
                default:
                    return "";
            };
        }
    }
}


