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

using BH.oM.SQLite.Objects;
using BH.oM.SQLite.Requests;
using BH.Engine.SQLite;
using System.Collections.Generic;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private IEnumerable<object> RangeFilterRequest(RangeFilterRequest rangeRequest)
        {
            List<object> result = new List<object>();

            // Convert filter to SQL
            FilterCommand filterResult = Convert.RangeFilter(rangeRequest);
            if (filterResult == null)
            {
                BH.Engine.Base.Compute.RecordWarning("Failed to process range filter request.");
                return result;
            }

            // Set limit if specified
            if (rangeRequest.MaxResults > 0)
                filterResult.Limit = rangeRequest.MaxResults;

            // Execute filtered query
            QueryResult queryResult = ExecuteQuery(rangeRequest.TableName, filterResult);
            result.Add(queryResult);
            
            return result;
        }

        /***************************************************/
    }
}
