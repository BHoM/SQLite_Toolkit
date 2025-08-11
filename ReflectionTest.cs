using System;
using System.Reflection;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.MaterialFragments;

class Program
{
    static void Main()
    {
        Type barType = typeof(Bar);
        Console.WriteLine($"Testing reflection for Bar type: {barType.Name}");
        
        // Test SectionProperty
        PropertyInfo sectionProp = barType.GetProperty("SectionProperty");
        Console.WriteLine($"SectionProperty: {sectionProp?.Name} of type {sectionProp?.PropertyType}");
        
        if (sectionProp != null)
        {
            // Test Material
            PropertyInfo materialProp = sectionProp.PropertyType.GetProperty("Material");
            Console.WriteLine($"Material: {materialProp?.Name} of type {materialProp?.PropertyType}");
            
            if (materialProp != null)
            {
                // Test Name
                PropertyInfo nameProp = materialProp.PropertyType.GetProperty("Name");
                Console.WriteLine($"Name: {nameProp?.Name} of type {nameProp?.PropertyType}");
                
                // Test Steel concrete implementation
                Type steelType = typeof(Steel);
                PropertyInfo steelNameProp = steelType.GetProperty("Name");
                Console.WriteLine($"Steel.Name: {steelNameProp?.Name} of type {steelNameProp?.PropertyType}");
            }
        }
        
        // Test the actual property path validation logic
        string propertyPath = "SectionProperty.Material.Name";
        string[] propertyParts = propertyPath.Split('.');
        Type currentType = barType;
        bool isValid = true;
        
        Console.WriteLine($"\nTesting property path: {propertyPath}");
        foreach (string propertyName in propertyParts)
        {
            PropertyInfo property = currentType.GetProperty(propertyName);
            Console.WriteLine($"  Looking for '{propertyName}' in {currentType.Name}: {(property != null ? "Found" : "NOT FOUND")}");
            if (property == null)
            {
                isValid = false;
                break;
            }
            currentType = property.PropertyType;
            Console.WriteLine($"    Next type: {currentType.Name}");
        }
        
        Console.WriteLine($"Property path valid: {isValid}");
    }
}
