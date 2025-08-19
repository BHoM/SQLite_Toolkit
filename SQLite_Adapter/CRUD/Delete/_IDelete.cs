/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2024, the respective contributors. All rights reserved.
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
using BH.oM.Data.Requests;
using BH.oM.SQLite;
using BH.oM.SQLite.Configs;
using BH.oM.SQLite.Objects;
using BH.oM.SQLite.Commands;
using BH.oM.SQLite.Requests;
using BH.Engine.SQLite;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BH.Adapter.SQLite
{
    public partial class SQLiteAdapter : BHoMAdapter
    {
        /***************************************************/
        /**** Protected Methods                         ****/
        /***************************************************/

        /***************************************************/

        // Main Delete method that implements IRequest-based deletion
        // This is the primary deletion implementation following BHoM patterns
        protected override int Delete(IRequest request, ActionConfig actionConfig = null)
        {
            // Ensure connection is available
            if (m_Connection == null || m_Connection.State != System.Data.ConnectionState.Open)
            {
                BH.Engine.Base.Compute.RecordError("No active database connection. Use Open command first.");
                return 0;
            }

            // Get delete configuration
            DeleteConfig deleteConfig = actionConfig as DeleteConfig;
            if (deleteConfig == null)
                deleteConfig = new DeleteConfig();
            
            try
            {
                return DeleteInternal(request, deleteConfig);
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Error during delete operation for request type {request?.GetType().Name}: {ex.Message}");
                return 0;
            }
        }

        /***************************************************/
        /**** Private Helper Methods                    ****/
        /***************************************************/

        private int DeleteInternal(IRequest request, DeleteConfig deleteConfig)
        {
            // Treat the request as ISqlRequest first to get table name
            if (!(request is ISqlRequest sqlRequest))
            {
                BH.Engine.Base.Compute.RecordError($"Delete operation requires an ISqlRequest. Request type {request.GetType().Name} is not supported.");
                return 0;
            }

            // Get table name from the request
            string tableName = sqlRequest.TableName;
            if (string.IsNullOrWhiteSpace(tableName))
            {
                BH.Engine.Base.Compute.RecordError("Cannot delete: table name not specified in request.");
                return 0;
            }

            // Process the request and build filter command
            FilterCommand filterCommand = null;
            if (request is EqualityFilterRequest equalityRequest)
            {
                filterCommand = Convert.EqualityFilter(equalityRequest, "eq");
            }
            else if (request is RangeFilterRequest rangeRequest)
            {
                filterCommand = Convert.RangeFilter(rangeRequest, "rng");
            }
            else
            {
                BH.Engine.Base.Compute.RecordError($"Delete operation not supported for request type: {request.GetType().Name}");
                return 0;
            }

            if (filterCommand == null)
            {
                BH.Engine.Base.Compute.RecordError("Failed to build filter command from request.");
                return 0;
            }

            // Validate that we have filter conditions if required
            if (deleteConfig.RequireFilterConditions && string.IsNullOrWhiteSpace(filterCommand.WhereClause))
            {
                BH.Engine.Base.Compute.RecordError("No filter conditions specified for delete operation. " +
                    "To prevent accidental deletion of entire table, provide filter conditions in the request. " +
                    "Set RequireFilterConditions to false in DeleteConfig to override this safety check.");
                return 0;
            }

            // Handle dry run
            if (deleteConfig.DryRun)
            {
                return PerformDeleteDryRun(tableName, filterCommand);
            }

            // Apply row limit if specified
            if (deleteConfig.MaxRowsToDelete > 0)
            {
                int affectedRowCount = GetAffectedRowCount(tableName, filterCommand);
                if (affectedRowCount > deleteConfig.MaxRowsToDelete)
                {
                    BH.Engine.Base.Compute.RecordError($"Delete operation would affect {affectedRowCount} rows, " +
                        $"which exceeds the configured maximum of {deleteConfig.MaxRowsToDelete}. " +
                        "Adjust your filter conditions or increase MaxRowsToDelete in DeleteConfig.");
                    return 0;
                }
            }

            // Execute delete operation
            QueryResult deleteResult = ExecuteQuery(SqlOperation.Delete, tableName, filterCommand);
            
            if (deleteResult.IsSuccess)
            {
                BH.Engine.Base.Compute.RecordNote($"Successfully deleted {deleteResult.RowCount} rows from table '{tableName}'.");
                return deleteResult.RowCount;
            }
            else
            {
                BH.Engine.Base.Compute.RecordError($"Failed to execute delete operation on table '{tableName}': {deleteResult.ErrorMessage}");
                return 0;
            }
        }



        private int PerformDeleteDryRun(string tableName, FilterCommand filter)
        {
            try
            {
                int affectedRows = GetAffectedRowCount(tableName, filter);
                BH.Engine.Base.Compute.RecordNote($"Dry run: {affectedRows} rows would be deleted from table '{tableName}' with the specified filter conditions.");
                return affectedRows;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"Failed to perform dry run: {ex.Message}");
                return 0;
            }
        }

        private int GetAffectedRowCount(string tableName, FilterCommand filter)
        {
            try
            {
                // Use CountQuery engine method to build count query
                QueryResult countResult = ExecuteQuery(SqlOperation.Count, tableName, filter);

                if (countResult.IsSuccess && countResult.Data.Count > 0)
                {
                    var firstRow = countResult.Data[0];
                    if (firstRow.Values.Count > 0)
                    {
                        object countValue = firstRow.Values.First();
                        if (int.TryParse(countValue?.ToString(), out int count))
                            return count;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordWarning($"Failed to get affected row count: {ex.Message}");
                return 0;
            }
        }

    }
}


