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
using BH.oM.SQLite.Commands;
using BH.oM.SQLite.Objects;
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Creates a SQL command to insert schema information for a column into the __Schema system table.")]
        [Input("tableName", "The table name.")]
        [Input("column", "The column schema information to insert.")]
        [Output("command", "SQLCommand that can be executed to insert the column schema.")]
        public static SQLCommand InsertColumnSchemaCommand(string tableName, Column column)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                BH.Engine.Base.Compute.RecordError("Cannot create insert column schema command: table name is null or empty.");
                return null;
            }

            if (column == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create insert column schema command: column is null.");
                return null;
            }

            SQLCommand command = new SQLCommand()
            {
                Command = @"
                    INSERT INTO __Schema (TableName, ColumnName, DataType, NetTypeName, IsNullable, IsPrimaryKey, DefaultValue) 
                    VALUES (@TableName, @ColumnName, @DataType, @NetTypeName, @IsNullable, @IsPrimaryKey, @DefaultValue)",
                Parameters = new Dictionary<string, object>
                {
                    { "@TableName", tableName },
                    { "@ColumnName", column.Name },
                    { "@DataType", column.DataType.ToString() },
                    { "@NetTypeName", column.NetTypeName },
                    { "@IsNullable", column.AllowNull },
                    { "@IsPrimaryKey", column.IsPrimaryKey },
                    { "@DefaultValue", column.DefaultValue }
                }
            };

            return command;
        }

        /***************************************************/
    }
}
