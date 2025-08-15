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
using BH.oM.SQLite;
using BH.oM.SQLite.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Creates a SQL CREATE TABLE statement from a table schema definition.")]
        [Input("tableSchema", "The table schema definition containing columns and constraints.")]
        [Input("ifNotExists", "Whether to add IF NOT EXISTS clause to avoid errors if table already exists.")]
        [Input("dropIfExists", "Whether to drop the table first if it already exists.")]
        [Output("sql", "The SQL CREATE TABLE statement, or null if the schema is invalid.")]
        public static string Table(TableSchema tableSchema, bool ifNotExists = true, bool dropIfExists = false)
        {
            if (tableSchema == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create table SQL: table schema is null.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(tableSchema.Name))
            {
                BH.Engine.Base.Compute.RecordError("Cannot create table SQL: table name is null or empty.");
                return null;
            }

            if (tableSchema.Columns == null || !tableSchema.Columns.Any())
            {
                BH.Engine.Base.Compute.RecordError("Cannot create table SQL: no columns defined.");
                return null;
            }

            try
            {
                StringBuilder sqlBuilder = new StringBuilder();

                // Drop table if requested
                if (dropIfExists)
                {
                    sqlBuilder.AppendLine($"DROP TABLE IF EXISTS \"{tableSchema.Name}\";");
                }

                // Generate CREATE TABLE statement
                string createSql = GenerateCreateTableSql(tableSchema, ifNotExists);
                sqlBuilder.AppendLine(createSql);

                // Create indexes if defined
                if (tableSchema.Indexes != null && tableSchema.Indexes.Any())
                {
                    foreach (Index index in tableSchema.Indexes)
                    {
                        string indexSql = GenerateCreateIndexSql(index);
                        sqlBuilder.AppendLine(indexSql);
                    }
                }

                return sqlBuilder.ToString().Trim();
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to generate table SQL for {tableSchema.Name}: {ex.Message}");
                return null;
            }
        }

        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private static string GenerateCreateTableSql(TableSchema tableSchema, bool ifNotExists)
        {
            StringBuilder sql = new StringBuilder();
            
            sql.Append("CREATE TABLE ");
            if (ifNotExists)
                sql.Append("IF NOT EXISTS ");
            
            sql.Append($"\"{tableSchema.Name}\" (");

            // Add columns
            IEnumerable<string> columnDefinitions = tableSchema.Columns.OrderBy(c => c.Position).Select(GenerateColumnDefinition);
            sql.Append(string.Join(", ", columnDefinitions));

            // Add foreign key constraints
            if (tableSchema.ForeignKeys != null && tableSchema.ForeignKeys.Any())
            {
                sql.Append(", ");
                sql.Append(string.Join(", ", tableSchema.ForeignKeys));
            }

            sql.Append(")");

            // Add WITHOUT ROWID if specified
            if (tableSchema.WithoutRowId)
                sql.Append(" WITHOUT ROWID");

            sql.Append(";");

            return sql.ToString();
        }

        private static string GenerateColumnDefinition(Column column)
        {
            StringBuilder def = new StringBuilder();
            
            def.Append($"\"{column.Name}\" {GetSqliteTypeName(column.DataType)}");

            // Add length constraint for text columns
            if (column.DataType == SqliteDataType.TEXT && column.MaxLength > 0)
            {
                def.Append($"({column.MaxLength})");
            }

            // Add constraints
            if (column.IsPrimaryKey)
            {
                def.Append(" PRIMARY KEY");
                
                if (column.IsAutoIncrement && column.DataType == SqliteDataType.INTEGER)
                {
                    def.Append(" AUTOINCREMENT");
                }
            }

            if (!column.AllowNull)
            {
                def.Append(" NOT NULL");
            }

            if (column.IsUnique && !column.IsPrimaryKey)
            {
                def.Append(" UNIQUE");
            }

            if (!string.IsNullOrWhiteSpace(column.DefaultValue))
            {
                def.Append($" DEFAULT {column.DefaultValue}");
            }

            if (!string.IsNullOrWhiteSpace(column.AdditionalConstraints))
            {
                def.Append($" {column.AdditionalConstraints}");
            }

            return def.ToString();
        }

        private static string GetSqliteTypeName(SqliteDataType dataType)
        {
            switch (dataType)
            {
                case SqliteDataType.INTEGER:
                    return "INTEGER";
                case SqliteDataType.REAL:
                    return "REAL";
                case SqliteDataType.TEXT:
                    return "TEXT";
                case SqliteDataType.BLOB:
                    return "BLOB";
                case SqliteDataType.NUMERIC:
                    return "NUMERIC";
                default:
                    return "TEXT"; // Default fallback
            }
        }

        private static string GenerateCreateIndexSql(Index index)
        {
            if (index == null || !index.Columns.Any())
                return string.Empty;

            // Check for index name from both IndexName property and inherited Name property
            string indexName = index.Name;
            if (string.IsNullOrWhiteSpace(indexName))
                return string.Empty;

            StringBuilder sql = new StringBuilder();
            sql.Append("CREATE ");
            
            if (index.IsUnique)
                sql.Append("UNIQUE ");
                
            sql.Append($"INDEX IF NOT EXISTS \"{indexName}\" ON \"{index.Name}\" (");
            
            List<string> quotedColumns = index.Columns.Select(col => $"\"{col}\"").ToList();
            sql.Append(string.Join(", ", quotedColumns));
            
            sql.Append(");");
            
            return sql.ToString();
        }

        /***************************************************/
    }
} 