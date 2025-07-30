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

using BH.oM.Adapter;
using BH.oM.Base;
using BH.oM.SQLite;
using BH.oM.SQLite.Objects;
using BH.oM.SQLite.Configs;
using BH.Engine.SQLite;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter
    {
        /***************************************************/
        /**** BHoM Object Creation Methods             ****/
        /***************************************************/

        // Enhanced Create method for BHoM objects with relationship handling
        protected bool Create(IEnumerable<IBHoMObject> bhomObjects, ActionConfig actionConfig = null)
        {
            if (bhomObjects == null || !bhomObjects.Any())
                return true;

            if (m_Connection == null)
            {
                BH.Engine.Base.Compute.RecordError("Cannot create BHoM objects: no database connection.");
                return false;
            }

            PushConfig pushConfig = actionConfig as PushConfig ?? new PushConfig();

            try
            {
                // Step 1: Analyze object relationships
                BH.Engine.Base.Compute.RecordNote("Analyzing BHoM object relationships...");
                var existingTables = GetExistingTableNames();
                var analysis = BH.Engine.SQLite.Compute.AnalyseObjectRelationships(bhomObjects, pushConfig, existingTables);

                if (analysis.TableSchemas.Any())
                {
                    // Step 2: Create tables for object types that don't exist
                    if (pushConfig.AutoCreateRelatedTables)
                    {
                        BH.Engine.Base.Compute.RecordNote($"Creating {analysis.TableSchemas.Count} tables for BHoM object types...");
                        foreach (TableSchema schema in analysis.TableSchemas)
                        {
                            if (!existingTables.Contains(schema.Name))
                            {
                                bool created = Create(schema);
                                if (!created)
                                {
                                    BH.Engine.Base.Compute.RecordError($"Failed to create table for type: {schema.Name}");
                                    return false;
                                }
                            }
                        }
                    }
                }

                // Step 3: Insert objects with relationship handling
                return InsertBHoMObjectsWithRelationships(bhomObjects, analysis, pushConfig);
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to create BHoM objects: {ex.Message}");
                return false;
            }
        }

        /***************************************************/
        /**** Private Helper Methods                   ****/
        /***************************************************/

        private HashSet<string> GetExistingTableNames()
        {
            var tableNames = new HashSet<string>();

            try
            {
                string sql = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
                using (SqliteCommand command = new SqliteCommand(sql, m_Connection))
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tableNames.Add(reader.GetString(0));
                    }
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to get existing table names: {ex.Message}");
            }

            return tableNames;
        }

        private bool InsertBHoMObjectsWithRelationships(IEnumerable<IBHoMObject> bhomObjects, ObjectRelationshipAnalysis analysis, PushConfig pushConfig)
        {
            try
            {
                using (SqliteTransaction transaction = pushConfig.UseTransactions ? m_Connection.BeginTransaction() : null)
                {
                    try
                    {
                        // Group objects by type for efficient processing
                        var objectsByType = bhomObjects.GroupBy(obj => obj.GetType());

                        // Step 1: Insert all main objects first (to establish primary keys)
                        var insertedObjects = new Dictionary<Guid, IBHoMObject>();
                        
                        foreach (var group in objectsByType)
                        {
                            Type objectType = group.Key;
                            var objectsOfType = group.ToList();
                            
                            string tableName = GetTableNameForType(objectType, pushConfig.TableNamingStrategy);
                            var typeAnalysis = analysis.TypeAnalyses.FirstOrDefault(ta => ta.ObjectType == objectType);
                            
                            if (typeAnalysis != null)
                            {
                                bool success = InsertObjectsOfType(objectsOfType, tableName, typeAnalysis, pushConfig, insertedObjects);
                                if (!success)
                                {
                                    transaction?.Rollback();
                                    return false;
                                }
                            }
                        }

                        // Step 2: Handle collections and related objects
                        foreach (var group in objectsByType)
                        {
                            Type objectType = group.Key;
                            var objectsOfType = group.ToList();
                            var typeAnalysis = analysis.TypeAnalyses.FirstOrDefault(ta => ta.ObjectType == objectType);
                            
                            if (typeAnalysis != null)
                            {
                                bool success = InsertRelatedObjects(objectsOfType, typeAnalysis, pushConfig, insertedObjects);
                                if (!success)
                                {
                                    transaction?.Rollback();
                                    return false;
                                }
                            }
                        }

                        transaction?.Commit();
                        
                        BH.Engine.Base.Compute.RecordNote($"Successfully inserted {bhomObjects.Count()} BHoM objects with relationships.");
                        return true;
                    }
                    catch
                    {
                        transaction?.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to insert BHoM objects with relationships: {ex.Message}");
                return false;
            }
        }

        private bool InsertObjectsOfType(List<IBHoMObject> objects, string tableName, ObjectTypeAnalysis typeAnalysis, 
            PushConfig pushConfig, Dictionary<Guid, IBHoMObject> insertedObjects)
        {
            try
            {
                // Build INSERT statement for main object properties (excluding relationships)
                var simpleProperties = typeAnalysis.Properties
                    .Where(p => p.RelationshipType == RelationshipType.None)
                    .ToList();

                if (!simpleProperties.Any() && !pushConfig.PreserveBHoMGuids)
                    return true; // Nothing to insert

                // Build column list
                var columns = new List<string> { "BHoM_Guid" };
                columns.AddRange(simpleProperties.Select(p => p.PropertyName));

                // Add foreign key columns for one-to-one relationships
                var oneToOneProperties = typeAnalysis.Properties
                    .Where(p => p.RelationshipType == RelationshipType.OneToOne && p.IsBHoMObject)
                    .ToList();

                columns.AddRange(oneToOneProperties.Select(p => $"{p.PropertyName}_Id"));

                if (pushConfig.AddTimestampColumns)
                {
                    columns.AddRange(new[] { "Created", "Modified" });
                }

                string columnList = string.Join(", ", columns.Select(col => $"\"{col}\""));
                string placeholderList = string.Join(", ", columns.Select(col => $"@{col}"));
                
                string conflictClause = GetConflictClause(pushConfig.ConflictResolution);
                string insertSql = $"INSERT {conflictClause} INTO \"{tableName}\" ({columnList}) VALUES ({placeholderList})";

                using (SqliteCommand command = new SqliteCommand(insertSql, m_Connection))
                {
                    // Add parameters
                    foreach (string column in columns)
                    {
                        command.Parameters.Add($"@{column}", SqliteType.Text);
                    }

                    // Insert each object
                    foreach (IBHoMObject obj in objects)
                    {
                        try
                        {
                            // Set BHoM_Guid
                            command.Parameters["@BHoM_Guid"].Value = obj.BHoM_Guid.ToString();

                            // Set simple property values
                            foreach (PropertyAnalysis property in simpleProperties)
                            {
                                object value = property.PropertyInfo.GetValue(obj);
                                command.Parameters[$"@{property.PropertyName}"].Value = ConvertValueForSQLite(value, property.SqliteDataType);
                            }

                            // Set foreign key values for one-to-one relationships
                            foreach (PropertyAnalysis property in oneToOneProperties)
                            {
                                object relatedObj = property.PropertyInfo.GetValue(obj);
                                if (relatedObj is IBHoMObject bhomRelatedObj)
                                {
                                    command.Parameters[$"@{property.PropertyName}_Id"].Value = bhomRelatedObj.BHoM_Guid.ToString();
                                }
                                else
                                {
                                    command.Parameters[$"@{property.PropertyName}_Id"].Value = DBNull.Value;
                                }
                            }

                            // Set timestamp values
                            if (pushConfig.AddTimestampColumns)
                            {
                                DateTime now = DateTime.UtcNow;
                                command.Parameters["@Created"].Value = now.ToString("yyyy-MM-dd HH:mm:ss");
                                command.Parameters["@Modified"].Value = now.ToString("yyyy-MM-dd HH:mm:ss");
                            }

                            command.ExecuteNonQuery();
                            
                            // Track inserted object
                            insertedObjects[obj.BHoM_Guid] = obj;
                        }
                        catch (Exception ex)
                        {
                            BH.Engine.Base.Compute.RecordError($"Failed to insert object {obj.BHoM_Guid}: {ex.Message}");
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to insert objects of type {tableName}: {ex.Message}");
                return false;
            }
        }

        private bool InsertRelatedObjects(List<IBHoMObject> objects, ObjectTypeAnalysis typeAnalysis, 
            PushConfig pushConfig, Dictionary<Guid, IBHoMObject> insertedObjects)
        {
            // Handle one-to-many relationships (collections)
            var collectionProperties = typeAnalysis.Properties
                .Where(p => p.RelationshipType == RelationshipType.OneToMany && p.IsBHoMCollection)
                .ToList();

            foreach (PropertyAnalysis property in collectionProperties)
            {
                if (pushConfig.CollectionStrategy == CollectionStrategy.SeparateTable)
                {
                    bool success = InsertCollectionAsSeparateTable(objects, property, pushConfig, insertedObjects);
                    if (!success)
                        return false;
                }
                // Handle other collection strategies here
            }

            return true;
        }

        private bool InsertCollectionAsSeparateTable(List<IBHoMObject> parentObjects, PropertyAnalysis collectionProperty, 
            PushConfig pushConfig, Dictionary<Guid, IBHoMObject> insertedObjects)
        {
            try
            {
                // This would create separate tables for collection items
                // For now, this is a simplified implementation
                BH.Engine.Base.Compute.RecordNote($"Collection handling for {collectionProperty.PropertyName} - separate table strategy not fully implemented yet.");
                return true;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to insert collection {collectionProperty.PropertyName}: {ex.Message}");
                return false;
            }
        }

        private object ConvertValueForSQLite(object value, SqliteDataType dataType)
        {
            if (value == null)
                return DBNull.Value;

            switch (dataType)
            {
                case SqliteDataType.INTEGER:
                    return System.Convert.ToInt64(value);
                case SqliteDataType.REAL:
                    return System.Convert.ToDouble(value);
                case SqliteDataType.TEXT:
                    return value.ToString();
                case SqliteDataType.BLOB:
                    return value; // Assume it's already byte[]
                case SqliteDataType.NUMERIC:
                    // For NUMERIC type, preserve the original value type to avoid date conversion
                    if (value is int || value is long || value is short || value is byte)
                        return System.Convert.ToInt64(value);
                    else if (value is double || value is float || value is decimal)
                        return System.Convert.ToDouble(value);
                    else
                        return value.ToString(); // Fallback to text for other types
                default:
                    return value.ToString();
            }
        }

        private string GetTableNameForType(Type type, TableNamingStrategy strategy)
        {
            switch(strategy)
            {
                case TableNamingStrategy.TypeName:
                    return type.Name;
                case TableNamingStrategy.TypeNameWithPrefix:
                    return $"BHoM_{type.Name}";
                default:
                    return type.Name;
            };
        }

        /***************************************************/
    }
} 