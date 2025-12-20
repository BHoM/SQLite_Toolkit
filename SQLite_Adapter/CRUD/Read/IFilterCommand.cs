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

using BH.oM.SQLite;
using BH.oM.SQLite.Objects;
using BH.oM.SQLite.Requests;
using BH.Engine.SQLite;
using System.Collections.Generic;
using BH.oM.Data.Requests;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private FilterCommand IFilterCommand(IRequest request)
        {
                return FilterCommand(request as dynamic);
        }

        private FilterCommand FilterCommand(EqualityFilterRequest request)
        {
                return Convert.EqualityFilter(request);
        }

        private FilterCommand FilterCommand(RangeFilterRequest request)
        {
                return Convert.RangeFilter(request);
        }

        /***************************************************/
        /**** Fallsback Methods                         ****/
        /***************************************************/

        private FilterCommand FilterCommand(ISqlRequest request)
        {
            Engine.Base.Compute.RecordError($"Request of type {request.GetType()} is not supported in this toolkit.");
            return null;
        }

        /***************************************************/
    }
}

