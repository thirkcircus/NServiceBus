using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NServiceBus.Utils.Reflection;

namespace NServiceBus.Hosting.Helpers
{
    /// <summary>
    /// Helpers for assembly scanning operations
    /// </summary>
    public class AssemblyScanner
    {
        /// <summary>
        /// Gets a list of assemblies that can be scanned and a list of errors that occurred while scanning.
        /// 
        /// </summary>
        /// <returns></returns>
        public static AssemblyScannerResults GetScannableAssemblies()
        {
            var assemblyFiles = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetFiles("*.dll", SearchOption.AllDirectories)
                .Union(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetFiles("*.exe", SearchOption.AllDirectories));
            var results = new AssemblyScannerResults();
            foreach (var assemblyFile in assemblyFiles)
            {
                try
                {
                    Assembly assembly = Assembly.LoadFrom(assemblyFile.FullName);

                    //will throw if assembly cannot be loaded
                    assembly.GetTypes();
                    results.Assemblies.Add(assembly);
                }
                catch (BadImageFormatException bif)
                {
                    var error = new ErrorWhileScanningAssemblies(bif, "Could not load " + assemblyFile.FullName +
                        ". Consider using 'Configure.With(AllAssemblies.Except(\"" + assemblyFile.Name + "\"))' to tell NServiceBus not to load this file.");
                    results.Errors.Add(error);
                }
                catch (ReflectionTypeLoadException e)
                {
                    var sb = new StringBuilder();
                    sb.Append(string.Format("Could not scan assembly: {0}. Exception message {1}.", assemblyFile.FullName, e));
                    if (e.LoaderExceptions.Any())
                    {
                        sb.Append(Environment.NewLine + "Scanned type errors: ");
                        foreach (var ex in e.LoaderExceptions)
                            sb.Append(Environment.NewLine + ex.Message);
                    }
                    var error = new ErrorWhileScanningAssemblies(e, sb.ToString());
                    results.Errors.Add(error);
                }
            }
            return results;
        }
    }

    /// <summary>
    /// Helpers for filtering scanned assemblies
    /// </summary>
    public static class AssemblyListExtensions
    {
        /// <summary>
        /// Gets all types in a collection of assemblies
        /// </summary>
        /// <param name="assemblies">A enumerable of assemblies</param>
        /// <returns>All the types in the assemblies</returns>
        [DebuggerNonUserCode]
        public static IEnumerable<Type> AllTypes(this IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
                foreach (var type in assembly.GetTypes())
                {
                    yield return type;
                }
        }

        /// <summary>
        /// Gets all types in a collection of assemblies that is assignable to the provided type
        /// </summary>
        /// <param name="assemblies">An enumerable of assemblies</param>
        /// <returns>All the types in the assemblies that is assignable to the provided type</returns>
        public static IEnumerable<Type> AllTypesAssignableTo<T>(this IEnumerable<Assembly> assemblies)
        {
            return assemblies.AllTypesAssignableTo(typeof(T));
        }

        /// <summary>
        /// Gets all types in a collection of assemblies that is assignable to the provided type
        /// </summary>
        /// <param name="assemblies">An enumerable of assemblies</param>
        /// <param name="type">The type to filter by</param>
        /// <returns>All the types in the assemblies that is assignable to the provided type</returns>
        public static IEnumerable<Type> AllTypesAssignableTo(this IEnumerable<Assembly> assemblies, Type type)
        {
            return assemblies.AllTypes().Where(type.IsAssignableFrom);
        }

        /// <summary>
        /// Gets all types in a collection of assemblies that is assignable to the provided type
        /// </summary>
        /// <param name="assemblies">An enumerable of assemblies</param>
        /// <param name="openGenericType">The open generic type</param>
        /// <param name="genericArg">The generic argument</param>
        /// <returns>All the types that close the given generic type</returns>
        public static IEnumerable<Type> AllTypesClosing(this IEnumerable<Assembly> assemblies, Type openGenericType, Type genericArg)
        {
            return assemblies.AllTypes().Where(type => type.GetGenericallyContainedType(openGenericType, genericArg) != null);
        }

        /// <summary>
        /// Instantiates all types in a collection of assemblies that is assignable to the provided type
        /// </summary>
        /// <param name="assemblies">An enumerable of assemblies</param>
        /// <returns>
        /// Instantiated instances of all the types in the assemblies that is assignable to the 
        /// provided type
        /// </returns>
        public static IEnumerable<T> AllInstancesAssignableTo<T>(this IEnumerable<Assembly> assemblies)
        {
            var types = assemblies.AllTypesAssignableTo(typeof(T))
                .Where(t => !t.IsAbstract && !t.IsInterface);

            var instances = types.Select(t =>
            {
                var constructor = t.GetConstructor(Type.EmptyTypes);

                if (constructor == null)
                    throw new InvalidOperationException(string.Format("Cannot instantiate type {0} implementing {1}. The type needs to have a default constructor.", t.AssemblyQualifiedName, typeof(T).AssemblyQualifiedName));

                return (T)Activator.CreateInstance(t);
            }).ToArray();

            return instances;
        }
    }
}