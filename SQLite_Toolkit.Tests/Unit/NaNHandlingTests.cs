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
 * The Free Software Foundation, either version 3.0 of the License, or          
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

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using FluentAssertions;
using BH.Adapter.SQLite;
using BH.oM.SQLite;
using BH.oM.SQLite.Configs;
using BH.oM.Adapter.Commands;
using BH.Tests.SQLite.Base;
using BH.oM.Base;
using BH.oM.Base.Attributes;
using BH.oM.SQLite.Requests;
using BH.oM.SQLite.Objects;

namespace BH.Tests.SQLite.Unit
{
    /// <summary>
    /// Unit tests for NaN (Not a Number) and Infinity value handling in SQLite conversion operations.
    /// Tests both ConvertToNull and ConvertToZero strategies for round-trip data integrity.
    /// </summary>
    [TestFixture]
    public class NaNHandlingTests : SQLiteTestBase
    {
        private SQLiteAdapter adapter;

        [SetUp]
        public void SetUp()
        {
            // Create a fresh adapter for each test
            adapter = CreateInMemoryTestAdapter();
            adapter.Execute(new Open() { FileName = ":memory:" });
        }

        [TearDown]
        public new void TearDown()
        {
            adapter?.Execute(new Close());
        }

        [Test]
        public void NaNHandling_ConvertToNull_RoundTripTest()
        {
            // Arrange: Create settings with ConvertToNull strategy
            SQLiteSettings settings = new SQLiteSettings()
            {
                NaNHandling = NaNHandling.ConvertToNull
            };

            // Create adapter with NaN handling settings
            adapter = new SQLiteAdapter(":memory:", settings, true);

            // Create test object with NaN values
            NaNTestObject testObject = new NaNTestObject()
            {
                Name = "NaN Test Object",
                DoubleValue = double.NaN,
                FloatValue = float.NaN,
                NormalValue = 42.5,
                PositiveInfinity = double.PositiveInfinity,
                NegativeInfinity = double.NegativeInfinity
            };

            List<NaNTestObject> testObjects = new List<NaNTestObject> { testObject };

            // Act: Push the object with NaN values
            List<object> pushResult = adapter.Push(testObjects);
            pushResult.Should().NotBeNull();
            pushResult.Count.Should().Be(1, "One object should be pushed successfully");

            // Pull the object back using EqualityFilterRequest (no filters = get all)
            List<object> pullResult = adapter.Pull(new EqualityFilterRequest() { TableName = typeof(NaNTestObject).Name }).ToList();
            pullResult.Should().NotBeNull();
            pullResult.Count.Should().Be(1, "One query result should be retrieved");

            // Extract data from QueryResult
            QueryResult queryResult = pullResult.First() as QueryResult;
            queryResult.Should().NotBeNull("Result should be a QueryResult object");
            queryResult.Data.Should().NotBeNull();
            queryResult.Data.Count.Should().Be(1, "One row should be returned");

            Dictionary<string, object> retrievedRow = queryResult.Data.First();

            // Normal values should be preserved
            retrievedRow["Name"].Should().Be("NaN Test Object");
            System.Convert.ToDouble(retrievedRow["NormalValue"]).Should().Be(42.5);

            // NaN and Infinity values should be converted back to NaN
            // (NULL in database converted back to NaN when ConvertToNull strategy is used)
            retrievedRow["DoubleValue"].Should().Be(double.NaN);
            retrievedRow["FloatValue"].Should().Be(float.NaN);
            retrievedRow["PositiveInfinity"].Should().Be(double.NaN);
            retrievedRow["NegativeInfinity"].Should().Be(double.NaN);
        }

        [Test]
        public void NaNHandling_ConvertToZero_RoundTripTest()
        {
            // Arrange: Create settings with ConvertToZero strategy
            SQLiteSettings settings = new SQLiteSettings()
            {
                NaNHandling = NaNHandling.ConvertToZero
            };

            // Create adapter with NaN handling settings
            adapter = new SQLiteAdapter(":memory:", settings, true);

            // Create test object with NaN values
            NaNTestObject testObject = new NaNTestObject()
            {
                Name = "Zero Test Object",
                DoubleValue = double.NaN,
                FloatValue = float.NaN,
                NormalValue = 42.5,
                PositiveInfinity = double.PositiveInfinity,
                NegativeInfinity = double.NegativeInfinity
            };

            List<NaNTestObject> testObjects = new List<NaNTestObject> { testObject };

            // Act: Push the object with NaN values
            List<object> pushResult = adapter.Push(testObjects);
            pushResult.Should().NotBeNull();
            pushResult.Count.Should().Be(1, "One object should be pushed successfully");

            // Pull the object back using EqualityFilterRequest (no filters = get all)
            List<object> pullResult = adapter.Pull(new EqualityFilterRequest() { TableName = typeof(NaNTestObject).Name }).ToList();
            pullResult.Should().NotBeNull();
            pullResult.Count.Should().Be(1, "One query result should be retrieved");

            // Extract data from QueryResult
            QueryResult queryResult = pullResult.First() as QueryResult;
            queryResult.Should().NotBeNull("Result should be a QueryResult object");
            queryResult.Data.Should().NotBeNull();
            queryResult.Data.Count.Should().Be(1, "One row should be returned");

            Dictionary<string, object> retrievedRow = queryResult.Data.First();

            // Normal values should be preserved
            retrievedRow["Name"].Should().Be("Zero Test Object");
            System.Convert.ToDouble(retrievedRow["NormalValue"]).Should().Be(42.5);

            // NaN and Infinity values should be converted to zero
            System.Convert.ToDouble(retrievedRow["DoubleValue"]).Should().Be(0.0, "DoubleValue should be 0.0 when ConvertToZero strategy is used");
            System.Convert.ToSingle(retrievedRow["FloatValue"]).Should().Be(0.0f, "FloatValue should be 0.0 when ConvertToZero strategy is used");
            System.Convert.ToDouble(retrievedRow["PositiveInfinity"]).Should().Be(0.0, "PositiveInfinity should be 0.0 when ConvertToZero strategy is used");
            System.Convert.ToDouble(retrievedRow["NegativeInfinity"]).Should().Be(0.0, "NegativeInfinity should be 0.0 when ConvertToZero strategy is used");
        }
    }

    /***************************************************/
    /****               Test Objects                  ****/
    /***************************************************/

    public class NaNTestObject : BHoMObject
    {
        /***************************************************/
        /**** Properties                              ****/
        /***************************************************/

        [System.ComponentModel.Description("Name identifier for the test object.")]
        public override string Name { get; set; } = "";

        [System.ComponentModel.Description("Double value that may contain NaN.")]
        public virtual double DoubleValue { get; set; } = 0.0;

        [System.ComponentModel.Description("Float value that may contain NaN.")]
        public virtual float FloatValue { get; set; } = 0.0f;

        [System.ComponentModel.Description("Normal numeric value for comparison.")]
        public virtual double NormalValue { get; set; } = 0.0;

        [System.ComponentModel.Description("Positive infinity value for testing.")]
        public virtual double PositiveInfinity { get; set; } = 0.0;

        [System.ComponentModel.Description("Negative infinity value for testing.")]
        public virtual double NegativeInfinity { get; set; } = 0.0;

        /***************************************************/
    }

    /***************************************************/
}
