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
using BH.oM.Adapter;
using System.ComponentModel;
using System.Collections.Generic;

namespace BH.oM.SQLite.Configs
{
    /***************************************************/
    /****               Public Classes              ****/
    /***************************************************/

    [Description("Configuration settings for SQLite push operations, including object relationship handling and data insertion strategies.")]
    public class PushConfig : ActionConfig
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [Description("Strategy for handling nested BHoM objects within pushed objects.")]
        public virtual RelationshipStrategy RelationshipStrategy { get; set; } = RelationshipStrategy.AutoCreateTables;

        [Description("Whether to automatically create tables for nested BHoM objects that don't exist.")]
        public virtual bool AutoCreateRelatedTables { get; set; } = true;

        [Description("Whether to use foreign key constraints to maintain referential integrity between related objects.")]
        public virtual bool UseForeignKeyConstraints { get; set; } = true;

        [Description("Maximum depth to traverse when analysing nested BHoM object relationships to prevent infinite recursion.")]
        public virtual int MaxRelationshipDepth { get; set; } = 5;

        [Description("Conflict resolution strategy when inserting data that conflicts with existing records.")]
        public virtual ConflictResolution ConflictResolution { get; set; } = ConflictResolution.Ignore;

        [Description("Whether to use database transactions to ensure all related objects are inserted atomically.")]
        public virtual bool UseTransactions { get; set; } = true;

        [Description("Maximum number of records to insert in a single batch operation for performance optimisation.")]
        public virtual int BatchSize { get; set; } = 1000;

        [Description("Whether to validate object relationships before insertion to ensure data integrity.")]
        public virtual bool ValidateRelationships { get; set; } = true;

        [Description("Custom table naming strategy for automatically created related object tables.")]
        public virtual TableNamingStrategy TableNamingStrategy { get; set; } = TableNamingStrategy.TypeName;

        [Description("Properties to exclude from relationship analysis and table creation.")]
        public virtual List<string> ExcludedProperties { get; set; } = new List<string>();

        [Description("Custom foreign key naming pattern for relationship columns. Use {ParentTable} and {ChildTable} placeholders.")]
        public virtual string ForeignKeyNamingPattern { get; set; } = "FK_{ParentTable}_{ChildTable}";

        [Description("Whether to create indices automatically on foreign key columns for improved query performance.")]
        public virtual bool AutoCreateForeignKeyIndices { get; set; } = true;

        [Description("Whether to handle circular references by breaking them with nullable foreign keys.")]
        public virtual bool HandleCircularReferences { get; set; } = true;

        [Description("Strategy for handling collections of BHoM objects within a parent object.")]
        public virtual CollectionStrategy CollectionStrategy { get; set; } = CollectionStrategy.SeparateTable;

        [Description("Whether to preserve the original BHoM GUID when creating records in related tables.")]
        public virtual bool PreserveBHoMGuids { get; set; } = true;

        [Description("Whether to add timestamp columns (Created, Modified) to automatically created tables.")]
        public virtual bool AddTimestampColumns { get; set; } = true;

        /***************************************************/
    }

    /***************************************************/
} 