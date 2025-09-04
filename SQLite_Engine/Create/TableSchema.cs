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

using BH.oM.Base;
using BH.oM.Base.Attributes;
using BH.oM.SQLite;
using BH.oM.SQLite.Configs;
using BH.oM.SQLite.Objects;
using System;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.SQLite
{
    public static partial class Create
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Creates an optimised table schema using unified mapping strategy to determine the most appropriate column mapping approach. \n" +
            "Uses PushConfig mappings (property and fragment mappings) combined with primitive property detection for comprehensive schema generation.")]
        [Input("objectType", "The .NET Type representing objects to be stored in the table. This type determines which mapping strategy is most appropriate for schema generation.")]
        [Input("tableName", "The target database table name for the schema. If empty or null, will be automatically derived from the object type name using standard naming conventions.")]
        [Input("config", "Optional PushConfig containing custom property-to-column mappings, property exclusion lists, and validation preferences. If null, uses automatic primitive property detection.")]
        [Output("tableSchema", "A complete TableSchema object containing all column definitions, data types, and constraints required for table creation, or null if schema generation fails due to type analysis errors.")]
        public static TableSchema TableSchema(Type objectType, string tableName = "", PushConfig config = null)
        {
            if (objectType == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create table schema: object type is null.");
                return null;
            }

            try
            {
                // Determine table name if not provided
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    tableName = objectType.Name;
                }

                // Create the table schema object
                TableSchema tableSchema = new TableSchema()
                {
                    Name = tableName
                };

                // Resolve property mappings using three-tier strategy
                System.Collections.Generic.Dictionary<string, PropertyColumnInfo> columnSchema = objectType.ColumnSchema(config);
                System.Collections.Generic.List<PropertyColumnInfo> columnMappings = columnSchema.Values.ToList();

                if (columnMappings == null || !columnMappings.Any())
                {
                    BH.Engine.Base.Compute.RecordWarning($"No valid property mappings found for type {objectType.FullName}. Table will have no data columns.");
                    return null;
                }

                // Convert property mappings to column definitions
                int position = 0;
                foreach (PropertyColumnInfo mapping in columnMappings)
                {
                    Column column = CreateColumnFromPropertyMapping(mapping, position);
                    if (column != null)
                    {
                        tableSchema.Columns.Add(column);
                        position++;
                    }
                }

                // Set BHoM_Guid as primary key if no existing primary key and BHoM_Guid column exists
                if (!tableSchema.Columns.Any(c => c.IsPrimaryKey))
                {
                    Column guidColumn = tableSchema.Columns.FirstOrDefault(c => string.Equals(c.Name, "BHoM_Guid", StringComparison.OrdinalIgnoreCase));
                    if (guidColumn != null)
                    {
                        guidColumn.IsPrimaryKey = true;
                        guidColumn.IsUnique = true;
                        guidColumn.AllowNull = false;
                    }
                }

                return tableSchema;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to create smart table schema for type {objectType.FullName}: {ex.Message}");
                return null;
            }
        }

        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private static Column CreateColumnFromPropertyMapping(PropertyColumnInfo mapping, int position)
        {
            if (mapping == null || string.IsNullOrWhiteSpace(mapping.ColumnName))
            {
                BH.Engine.Base.Compute.RecordWarning("Cannot create column: mapping information is incomplete.");
                return null;
            }

            try
            {
                Column column = new Column()
                {
                    Name = mapping.ColumnName,
                    DataType = GetSqliteDataTypeFromNetType(mapping.PropertyType),
                    NetTypeName = mapping.PropertyType.FullName,
                    AllowNull = true, // Default to allow nulls for flexibility
                    Position = position
                };

                // Set specific constraints based on .NET type
                if (mapping.PropertyType == typeof(string))
                {
                    column.DataType = SqliteDataType.TEXT;
                    // Could add MaxLength if we detect string length attributes
                }
                else if (mapping.PropertyType == typeof(Guid) || mapping.PropertyType == typeof(Guid?))
                {
                    column.DataType = SqliteDataType.TEXT;
                    column.MaxLength = 36; // Standard GUID string length
                }
                else if (mapping.PropertyType == typeof(DateTime) || mapping.PropertyType == typeof(DateTime?))
                {
                    column.DataType = SqliteDataType.TEXT; // Store as ISO format
                }
                else if (mapping.PropertyType == typeof(bool) || mapping.PropertyType == typeof(bool?))
                {
                    column.DataType = SqliteDataType.INTEGER; // 0/1 for boolean
                }

                // Handle nullable types
                if (mapping.PropertyType.IsGenericType && mapping.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    column.AllowNull = true;
                }
                else if (mapping.PropertyType.IsValueType)
                {
                    column.AllowNull = false; // Non-nullable value types
                }

                return column;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to create column for property mapping {mapping.ColumnName}: {ex.Message}");
                return null;
            }
        }

        /***************************************************/

        private static SqliteDataType GetSqliteDataTypeFromNetType(Type netType)
        {
            // Handle nullable types
            if (netType.IsGenericType && netType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                netType = Nullable.GetUnderlyingType(netType);
            }

            // Map .NET types to SQLite data types
            if (netType == typeof(string) || netType == typeof(Guid) || netType == typeof(DateTime))
            {
                return SqliteDataType.TEXT;
            }
            else if (netType == typeof(int) || netType == typeof(long) || netType == typeof(short) || 
                     netType == typeof(byte) || netType == typeof(bool) || netType.IsEnum)
            {
                return SqliteDataType.INTEGER;
            }
            else if (netType == typeof(double) || netType == typeof(float))
            {
                return SqliteDataType.REAL;
            }
            else if (netType == typeof(decimal))
            {
                return SqliteDataType.NUMERIC;
            }
            else if (netType == typeof(byte[]))
            {
                return SqliteDataType.BLOB;
            }
            else
            {
                // Default fallback for unknown types
                return SqliteDataType.TEXT;
            }
        }

        /***************************************************/
    }
}
