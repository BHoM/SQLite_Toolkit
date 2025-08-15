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

using System;
using System.Collections.Generic;
using System.Linq;
using BH.oM.Data.Requests;
using BH.oM.Adapter;
using BH.oM.SQLite.Requests;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        public override IEnumerable<object> Pull(IRequest query, PullType pullType = PullType.AdapterDefault, ActionConfig actionConfig = null)
        {
            List<object> result = new List<object>();

            if (query == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot pull data: query is null.");
                return result;
            }

            if (m_Connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot pull data: no database connection. Please open a connection first.");
                return result;
            }

            m_LastUsed = DateTime.Now;

            // Handle different request types
            if (query is EqualityFilterRequest equalityRequest)
            {
                return EqualityFilterRequest(equalityRequest);
            }
            else if (query is RangeFilterRequest rangeRequest)
            {
                return RangeFilterRequest(rangeRequest);
            }
            else
            {
                BH.Engine.Base.Compute.RecordWarning($"Request type {query.GetType().Name} is not supported by this adapter.");
                return result;
            }
        }

        /***************************************************/
    }
} 