/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2025, the respective contributors. All rights reserved.
 */

using System;
using System.Collections.Generic;
using NUnit.Framework;
using FluentAssertions;
using BH.Engine.SQLite;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.MaterialFragments;
using System.Reflection;
using System.Linq;

namespace SQLite_Toolkit.Tests.Debug
{
    /// <summary>
    /// Debug test to understand the interface property path issue
    /// </summary>
    [TestFixture]
    public class DebugPropertyTest
    {
        [Test]
        public void Debug_PropertyPath_ReflectionIssue()
        {
            // Let's trace exactly what happens
            Type barType = typeof(Bar);
            
            // Step 1: Get SectionProperty
            PropertyInfo sectionProp = barType.GetProperty("SectionProperty");
            Assert.That(sectionProp, Is.Not.Null, "SectionProperty should exist");
            Assert.That(sectionProp.PropertyType.Name, Is.EqualTo("ISectionProperty"), "Should be ISectionProperty interface");
            
            // Step 2: Get Material from ISectionProperty interface
            PropertyInfo materialProp = sectionProp.PropertyType.GetProperty("Material");
            Assert.That(materialProp, Is.Not.Null, "Material property should exist on ISectionProperty");
            Assert.That(materialProp.PropertyType.Name, Is.EqualTo("IMaterialFragment"), "Should be IMaterialFragment interface");
            
            // Step 3: Get Name from IMaterialFragment interface
            PropertyInfo nameProp = materialProp.PropertyType.GetProperty("Name");
            
            // This is likely where it fails - IMaterialFragment might not define Name
            Console.WriteLine($"Material property type: {materialProp.PropertyType}");
            Console.WriteLine($"Material properties available:");
            foreach (var prop in materialProp.PropertyType.GetProperties())
            {
                Console.WriteLine($"  - {prop.Name} : {prop.PropertyType}");
            }
            
            // Let's also check what Steel has
            Type steelType = typeof(Steel);
            Console.WriteLine($"\nSteel properties available:");
            foreach (var prop in steelType.GetProperties())
            {
                Console.WriteLine($"  - {prop.Name} : {prop.PropertyType}");
            }
            
            // The issue is likely that Name is defined in BHoMObject base class
            // but ISectionProperty -> IMaterialFragment doesn't inherit from IBHoMObject properly
            
            // Let's test the actual path resolution
            string propertyPath = "SectionProperty.Material.Name";
            bool isValid = barType.IsValidPropertyPath(propertyPath);
            Console.WriteLine($"\nProperty path '{propertyPath}' is valid: {isValid}");
            
            // Let's try a different approach - get the actual type that would be used
            Type materialFragmentType = typeof(IMaterialFragment);
            Console.WriteLine($"\nIMaterialFragment properties:");
            foreach (var prop in materialFragmentType.GetProperties())
            {
                Console.WriteLine($"  - {prop.Name} : {prop.PropertyType}");
            }
            
            // Check the inheritance hierarchy
            Console.WriteLine($"\nSteel inheritance:");
            Type current = steelType;
            while (current != null)
            {
                Console.WriteLine($"  - {current.Name}");
                Console.WriteLine($"    Interfaces: {string.Join(", ", current.GetInterfaces().Select(i => i.Name))}");
                current = current.BaseType;
            }
        }
    }
}
