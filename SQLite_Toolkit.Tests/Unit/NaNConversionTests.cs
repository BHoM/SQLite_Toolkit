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

using System;
using NUnit.Framework;
using FluentAssertions;
using BH.oM.SQLite;
using Convert = BH.Adapter.SQLite.Convert;

namespace BH.Tests.SQLite.Unit
{
    /// <summary>
    /// Unit tests for NaN (Not a Number) and Infinity value conversion methods.
    /// Tests the ToSQLite and FromSQLite conversion logic directly.
    /// </summary>
    [TestFixture]
    public class NaNConversionTests
    {
        [Test]
        public void ToSQLite_ConvertToNull_NaNValues()
        {
            // Arrange & Act & Assert
            var result1 = Convert.Value(double.NaN, NaNHandling.ConvertToNull);
            result1.Should().Be(DBNull.Value, "double.NaN should convert to DBNull.Value with ConvertToNull strategy");

            var result2 = Convert.Value(float.NaN, NaNHandling.ConvertToNull);
            result2.Should().Be(DBNull.Value, "float.NaN should convert to DBNull.Value with ConvertToNull strategy");

            var result3 = Convert.Value(double.PositiveInfinity, NaNHandling.ConvertToNull);
            result3.Should().Be(DBNull.Value, "double.PositiveInfinity should convert to DBNull.Value with ConvertToNull strategy");

            var result4 = Convert.Value(double.NegativeInfinity, NaNHandling.ConvertToNull);
            result4.Should().Be(DBNull.Value, "double.NegativeInfinity should convert to DBNull.Value with ConvertToNull strategy");
        }

        [Test]
        public void ToSQLite_ConvertToZero_NaNValues()
        {
            // Arrange & Act & Assert
            var result1 = Convert.Value(double.NaN, NaNHandling.ConvertToZero);
            result1.Should().Be(0.0, "double.NaN should convert to 0.0 with ConvertToZero strategy");

            var result2 = Convert.Value(float.NaN, NaNHandling.ConvertToZero);
            result2.Should().Be(0.0f, "float.NaN should convert to 0.0f with ConvertToZero strategy");

            var result3 = Convert.Value(double.PositiveInfinity, NaNHandling.ConvertToZero);
            result3.Should().Be(0.0, "double.PositiveInfinity should convert to 0.0 with ConvertToZero strategy");

            var result4 = Convert.Value(double.NegativeInfinity, NaNHandling.ConvertToZero);
            result4.Should().Be(0.0, "double.NegativeInfinity should convert to 0.0 with ConvertToZero strategy");
        }

        [Test]
        public void ToSQLite_PreservesNormalValues()
        {
            // Arrange & Act & Assert
            var result1 = Convert.Value(42.5, NaNHandling.ConvertToNull);
            result1.Should().Be(42.5, "Normal double values should be preserved");

            var result2 = Convert.Value(3.14f, NaNHandling.ConvertToZero);
            result2.Should().Be(3.14f, "Normal float values should be preserved");

            var result3 = Convert.Value("test string", NaNHandling.ConvertToNull);
            result3.Should().Be("test string", "String values should be preserved");

            var result4 = Convert.Value(123, NaNHandling.ConvertToNull);
            result4.Should().Be(123, "Integer values should be preserved");
        }

        [Test]
        public void FromSQLite_ConvertToNull_NullToNaN()
        {
            // Arrange & Act & Assert
            var result1 = Convert.Value(DBNull.Value, typeof(double), NaNHandling.ConvertToNull);
            double.IsNaN((double)result1).Should().BeTrue("DBNull.Value should convert back to double.NaN with ConvertToNull strategy");

            var result2 = Convert.Value(null, typeof(float), NaNHandling.ConvertToNull);
            float.IsNaN((float)result2).Should().BeTrue("null should convert back to float.NaN with ConvertToNull strategy");

            var result3 = Convert.Value(DBNull.Value, typeof(double?), NaNHandling.ConvertToNull);
            double.IsNaN(((double?)result3).Value).Should().BeTrue("DBNull.Value should convert back to nullable double.NaN with ConvertToNull strategy");
        }

        [Test]
        public void FromSQLite_ConvertToZero_NullToZero()
        {
            // Arrange & Act & Assert
            var result1 = Convert.Value(DBNull.Value, typeof(double), NaNHandling.ConvertToZero);
            result1.Should().Be(0.0, "DBNull.Value should convert to 0.0 with ConvertToZero strategy");

            var result2 = Convert.Value(null, typeof(float), NaNHandling.ConvertToZero);
            result2.Should().Be(0.0f, "null should convert to 0.0f with ConvertToZero strategy");
        }

        [Test]
        public void FromSQLite_PreservesNormalValues()
        {
            // Arrange & Act & Assert
            var result1 = Convert.Value(42.5, typeof(double), NaNHandling.ConvertToNull);
            result1.Should().Be(42.5, "Normal double values should be preserved");

            var result2 = Convert.Value(3.14f, typeof(float), NaNHandling.ConvertToZero);
            result2.Should().Be(3.14f, "Normal float values should be preserved");

            var result3 = Convert.Value("test string", typeof(string), NaNHandling.ConvertToNull);
            result3.Should().Be("test string", "String values should be preserved");
        }
    }
}
