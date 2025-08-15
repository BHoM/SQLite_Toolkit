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
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** Create Methods                            ****/
        /***************************************************/

        // Create method for TableSchema objects - creates tables in the database
        protected bool Create(TableSchema tableSchema)
        {
            if (tableSchema == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create table: TableSchema is null.");
                return false;
            }

            if (m_Connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create table: no database connection.");
                return false;
            }

            try
            {
                // Generate CREATE TABLE SQL using the Engine method
                string createSql = BH.Engine.SQLite.Compute.Table(tableSchema, ifNotExists: true, dropIfExists: false);
                
                if (string.IsNullOrEmpty(createSql))
                {
                    BH.Engine.Base.Compute.RecordError($"Failed to generate CREATE TABLE SQL for {tableSchema.Name}");
                    return false;
                }

                // Execute the SQL using the shared method
                bool success = Command(m_Connection, createSql, (Dictionary<string, object>)null, $"CREATE TABLE {tableSchema.Name}");
                
                if (success)
                {
                    BH.Engine.Base.Compute.RecordNote($"Successfully created table: {tableSchema.Name}");
                }

                return success;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to create table {tableSchema.Name}: {ex.Message}");
                return false;
            }
        }

        // Create method for multiple TableSchema objects
        protected bool Create(IEnumerable<TableSchema> tableSchemas)
        {
            if (tableSchemas == null)
                return false;

            bool success = true;
            foreach (TableSchema schema in tableSchemas)
            {
                success &= Create(schema);
            }
            return success;
        }

        /***************************************************/
    }
} 