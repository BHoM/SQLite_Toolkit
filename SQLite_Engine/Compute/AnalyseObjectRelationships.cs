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
using BH.oM.Base;
using BH.oM.SQLite;
using BH.oM.SQLite.Objects;
using BH.oM.SQLite.Configs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Collections;

namespace BH.Engine.SQLite
{
    public static partial class Compute
    {
        /***************************************************/
        /**** Public Methods                            ****/
        /***************************************************/

        [Description("Analyzes BHoM objects to identify nested relationships and generates table schemas with appropriate foreign key constraints.")]
        [Input("objects", "The BHoM objects to analyze for relationships.")]
        [Input("pushConfig", "Configuration settings for how to handle object relationships.")]
        [Input("existingTables", "Names of tables that already exist in the database to avoid recreating them.")]
        [Output("analysisResult", "Analysis result containing table schemas and relationship information.")]
        public static ObjectRelationshipAnalysis AnalyseObjectRelationships(IEnumerable<IBHoMObject> objects, PushConfig pushConfig = null, HashSet<string> existingTables = null)
        {
            if (objects == null || !objects.Any())
            {
                BH.Engine.Base.Compute.RecordWarning("No objects provided for relationship analysis.");
                return new ObjectRelationshipAnalysis();
            }

            pushConfig = pushConfig ?? new PushConfig();
            existingTables = existingTables ?? new HashSet<string>();

            var result = new ObjectRelationshipAnalysis();
            var processedTypes = new HashSet<Type>();
            var analysisContext = new AnalysisContext(pushConfig, existingTables, processedTypes);

            try
            {
                // Group objects by type for analysis
                var objectsByType = objects.GroupBy(obj => obj.GetType());

                foreach (var group in objectsByType)
                {
                    Type objectType = group.Key;
                    var objectsOfType = group.ToList();

                    BH.Engine.Base.Compute.RecordNote($"Analyzing {objectsOfType.Count} objects of type {objectType.Name}");

                    // Analyze the object type structure
                    ObjectTypeAnalysis typeAnalysis = AnalyzeObjectType(objectType, objectsOfType, analysisContext, 0);
                    result.TypeAnalyses.Add(typeAnalysis);

                    // Generate table schema for this type
                    TableSchema mainTableSchema = GenerateTableSchemaForType(objectType, typeAnalysis, analysisContext);
                    if (mainTableSchema != null)
                    {
                        result.TableSchemas.Add(mainTableSchema);
                    }
                }

                // Generate schemas for related types that were discovered
                GenerateRelatedTableSchemas(result, analysisContext);

                BH.Engine.Base.Compute.RecordNote($"Analysis complete. Generated {result.TableSchemas.Count} table schemas with {result.Relationships.Count} relationships.");
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to analyze object relationships: {ex.Message}");
            }

            return result;
        }

        /***************************************************/
        /**** Private Methods                           ****/
        /***************************************************/

        private static ObjectTypeAnalysis AnalyzeObjectType(Type objectType, List<IBHoMObject> objects, AnalysisContext context, int depth)
        {
            if (depth > context.PushConfig.MaxRelationshipDepth)
            {
                BH.Engine.Base.Compute.RecordWarning($"Maximum relationship depth reached for type {objectType.Name}");
                return new ObjectTypeAnalysis { ObjectType = objectType };
            }

            if (context.ProcessedTypes.Contains(objectType))
            {
                // Circular reference detected - return minimal analysis
                return new ObjectTypeAnalysis { ObjectType = objectType, HasCircularReference = true };
            }

            context.ProcessedTypes.Add(objectType);

            var analysis = new ObjectTypeAnalysis { ObjectType = objectType };

            try
            {
                // Get all properties of the object type
                PropertyInfo[] properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo property in properties)
                {
                    // Skip excluded properties
                    if (context.PushConfig.ExcludedProperties.Contains(property.Name))
                        continue;

                    PropertyAnalysis propAnalysis = AnalyzeProperty(property, objects, context, depth + 1);
                    analysis.Properties.Add(propAnalysis);
                }

                // Detect relationships
                analysis.Relationships = DetectRelationships(analysis.Properties);
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to analyze object type {objectType.Name}: {ex.Message}");
            }
            finally
            {
                context.ProcessedTypes.Remove(objectType);
            }

            return analysis;
        }

        private static PropertyAnalysis AnalyzeProperty(PropertyInfo property, List<IBHoMObject> objects, AnalysisContext context, int depth)
        {
            var analysis = new PropertyAnalysis
            {
                PropertyInfo = property,
                PropertyName = property.Name,
                PropertyType = property.PropertyType
            };

            try
            {
                // Determine if this is a BHoM object property
                if (typeof(IBHoMObject).IsAssignableFrom(property.PropertyType))
                {
                    analysis.IsBHoMObject = true;
                    analysis.RelationshipType = RelationshipType.OneToOne;
                }
                // Check for collections of BHoM objects
                else if (IsCollectionOfBHoMObjects(property.PropertyType, out Type elementType))
                {
                    analysis.IsBHoMCollection = true;
                    analysis.CollectionElementType = elementType;
                    analysis.RelationshipType = RelationshipType.OneToMany;
                }
                // Simple property
                else
                {
                    analysis.SqliteDataType = InferSqliteDataType(property.PropertyType);
                    analysis.RelationshipType = RelationshipType.None;
                }

                // Sample property values from actual objects to understand data patterns
                SamplePropertyValues(analysis, property, objects);
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to analyze property {property.Name}: {ex.Message}");
            }

            return analysis;
        }

        private static bool IsCollectionOfBHoMObjects(Type type, out Type elementType)
        {
            elementType = null;

            // Check for IEnumerable<T> where T : IBHoMObject
            if (type.IsGenericType)
            {
                Type[] genericArgs = type.GetGenericArguments();
                if (genericArgs.Length == 1 && typeof(IBHoMObject).IsAssignableFrom(genericArgs[0]))
                {
                    elementType = genericArgs[0];
                    return true;
                }
            }

            // Check for arrays of BHoM objects
            if (type.IsArray && typeof(IBHoMObject).IsAssignableFrom(type.GetElementType()))
            {
                elementType = type.GetElementType();
                return true;
            }

            // Check for IList, ICollection etc.
            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    Type[] args = interfaceType.GetGenericArguments();
                    if (args.Length == 1 && typeof(IBHoMObject).IsAssignableFrom(args[0]))
                    {
                        elementType = args[0];
                        return true;
                    }
                }
            }

            return false;
        }

        private static SqliteDataType InferSqliteDataType(Type propertyType)
        {
            // Handle nullable types
            Type actualType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            // Integer types
            if (actualType == typeof(int) || actualType == typeof(long) || actualType == typeof(short) ||
                actualType == typeof(byte) || actualType == typeof(uint) || actualType == typeof(ulong) ||
                actualType == typeof(ushort) || actualType == typeof(sbyte) || actualType == typeof(bool))
            {
                return SqliteDataType.INTEGER;
            }

            // Real types
            if (actualType == typeof(float) || actualType == typeof(double))
            {
                return SqliteDataType.REAL;
            }

            // Numeric types (for decimal, etc.)
            if (actualType == typeof(decimal))
            {
                return SqliteDataType.NUMERIC;
            }

            // Blob types
            if (actualType == typeof(byte[]) || actualType == typeof(System.IO.Stream))
            {
                return SqliteDataType.BLOB;
            }

            // Everything else as TEXT (including strings, DateTime, Guid, etc.)
            return SqliteDataType.TEXT;
        }

        private static void SamplePropertyValues(PropertyAnalysis analysis, PropertyInfo property, List<IBHoMObject> objects)
        {
            try
            {
                int sampleSize = Math.Min(10, objects.Count);
                for (int i = 0; i < sampleSize; i++)
                {
                    object value = property.GetValue(objects[i]);
                    if (value != null)
                    {
                        analysis.SampleValues.Add(value);
                        
                        // Track max length for text properties
                        if (analysis.SqliteDataType == SqliteDataType.TEXT)
                        {
                            string stringValue = value.ToString();
                            analysis.MaxTextLength = Math.Max(analysis.MaxTextLength, stringValue.Length);
                        }
                    }
                    else
                    {
                        analysis.HasNullValues = true;
                    }
                }
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to sample values for property {property.Name}: {ex.Message}");
            }
        }

        private static List<ObjectRelationship> DetectRelationships(List<PropertyAnalysis> properties)
        {
            var relationships = new List<ObjectRelationship>();

            foreach (PropertyAnalysis property in properties)
            {
                if (property.RelationshipType != RelationshipType.None)
                {
                    relationships.Add(new ObjectRelationship
                    {
                        PropertyName = property.PropertyName,
                        RelationshipType = property.RelationshipType,
                        RelatedType = property.IsBHoMObject ? property.PropertyType : property.CollectionElementType,
                        IsCollection = property.IsBHoMCollection
                    });
                }
            }

            return relationships;
        }

        private static TableSchema GenerateTableSchemaForType(Type objectType, ObjectTypeAnalysis typeAnalysis, AnalysisContext context)
        {
            try
            {
                string tableName = GetTableNameForType(objectType, context.PushConfig.TableNamingStrategy);
                
                var schema = new TableSchema
                {
                    Name = tableName,
                    Columns = new List<Column>(),
                    Indexes = new List<Index>()
                };

                // Add BHoM_Guid column (primary key)
                schema.Columns.Add(new Column
                {
                    Name = "BHoM_Guid",
                    DataType = SqliteDataType.TEXT,
                    IsPrimaryKey = true,
                    AllowNull = false,
                    Position = 0
                });

                int columnPosition = 1;

                // Add columns for simple properties
                foreach (PropertyAnalysis property in typeAnalysis.Properties)
                {
                    if (property.RelationshipType == RelationshipType.None)
                    {
                        var column = new Column
                        {
                            Name = property.PropertyName,
                            DataType = property.SqliteDataType,
                            AllowNull = property.HasNullValues,
                            Position = columnPosition++
                        };

                        // Set max length for text columns
                        if (property.SqliteDataType == SqliteDataType.TEXT && property.MaxTextLength > 0)
                        {
                            column.MaxLength = Math.Max(property.MaxTextLength * 2, 255); // Allow some growth
                        }

                        schema.Columns.Add(column);
                    }
                    else if (property.RelationshipType == RelationshipType.OneToOne && property.IsBHoMObject)
                    {
                        // Add foreign key column for one-to-one relationships
                        schema.Columns.Add(new Column
                        {
                            Name = $"{property.PropertyName}_Id",
                            DataType = SqliteDataType.TEXT,
                            AllowNull = true, // Related objects might be null
                            Position = columnPosition++,
                            AdditionalConstraints = $"REFERENCES \"{GetTableNameForType(property.PropertyType, context.PushConfig.TableNamingStrategy)}\"(BHoM_Guid)"
                        });
                    }
                }

                // Add timestamp columns if requested
                if (context.PushConfig.AddTimestampColumns)
                {
                    schema.Columns.Add(new Column
                    {
                        Name = "Created",
                        DataType = SqliteDataType.TEXT,
                        AllowNull = false,
                        DefaultValue = "CURRENT_TIMESTAMP",
                        Position = columnPosition++
                    });

                    schema.Columns.Add(new Column
                    {
                        Name = "Modified",
                        DataType = SqliteDataType.TEXT,
                        AllowNull = false,
                        DefaultValue = "CURRENT_TIMESTAMP",
                        Position = columnPosition++
                    });
                }

                return schema;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to generate table schema for type {objectType.Name}: {ex.Message}");
                return null;
            }
        }

        private static void GenerateRelatedTableSchemas(ObjectRelationshipAnalysis result, AnalysisContext context)
        {
            // This method would generate schemas for related types discovered during analysis
            // Implementation would iterate through relationships and create additional schemas
            // For now, this is a placeholder for the more complex relationship handling
        }

        private static string GetTableNameForType(Type type, TableNamingStrategy strategy)
        {
            switch (strategy)
            {
                case TableNamingStrategy.TypeName:
                    return type.Name;
                case TableNamingStrategy.TypeNameWithPrefix:
                    return $"BHoM_{type.Name}";
                default:
                    return type.Name;
            }
        }

        /***************************************************/
    }

    /***************************************************/
} 