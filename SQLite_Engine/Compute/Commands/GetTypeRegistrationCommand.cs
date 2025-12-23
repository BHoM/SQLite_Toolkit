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
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Creates a SQL command to look up a type registration by full type name.")]
        [Input("fullTypeName", "The full type name including namespace.")]
        [Output("command", "SQLCommand that can be executed to retrieve the type registration.")]
        public static SQLCommand GetTypeRegistrationCommand(string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName))
            {
                BH.Engine.Base.Compute.RecordError("Cannot create type registration query: full type name is null or empty.");
                return null;
            }

            SQLCommand command = new SQLCommand()
            {
                Command = @"
                    SELECT Id, FullTypeName, TableName, DateCreated 
                    FROM __Types 
                    WHERE FullTypeName = @FullTypeName",
                Parameters = new Dictionary<string, object>
                {
                    { "@FullTypeName", fullTypeName }
                }
            };

            return command;
        }

        /***************************************************/

        [Description("Creates a SQL command to look up a type registration by .NET Type.")]
        [Input("type", "The .NET Type to look up.")]
        [Output("command", "SQLCommand that can be executed to retrieve the type registration.")]
        public static SQLCommand GetTypeRegistrationCommand(Type type)
        {
            if (type == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create type registration query: type is null.");
                return null;
            }

            string fullTypeName = type.FullName ?? type.Name;
            return GetTypeRegistrationCommand(fullTypeName);
        }

        /***************************************************/
    }
}

