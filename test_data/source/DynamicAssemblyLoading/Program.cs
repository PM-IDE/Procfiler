// See https://aka.ms/new-console-template for more information

using System.Reflection;

Console.WriteLine("Hello, World!");
var directory = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.FullName!;
var path = Path.Combine(directory, "SomeAssembly", "SomeAssembly.dll");
Console.WriteLine(File.Exists(path));
var asm = Assembly.LoadFrom(path);
var classInstance = asm.CreateInstance("SomeAssembly.Class1");
Console.WriteLine(classInstance.GetType().Name);