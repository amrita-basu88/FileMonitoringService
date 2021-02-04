using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using FileMonitorService.Models;
using LoggerSingleton;

namespace FileMonitorService.Core
{
    public class InvokeMethodHelper
    {
        private static readonly ConcurrentDictionary<String, Type> TypeCache = new ConcurrentDictionary<String, Type>();

        public static void InvokeMethodByReflection(NotificationModel notificationModel, InvokeMethodData invokeMethodData)
        {
            Type methodClassType = ResolveType(invokeMethodData.AssemblyName, invokeMethodData.ClassName);
            if (methodClassType == null)
            {
                SingletonLogger.Instance.Error(String.Format("Could not resolve method AssemblyName='{0}' MethodClass='{1}'", 
                    invokeMethodData.AssemblyName, invokeMethodData.ClassName));
                return;
            }

            List<object> parameters = new List<object> {notificationModel};

            foreach (var methodParameter in invokeMethodData.MethodParameters)
            {
                Type parameterClassType = ResolveType(methodParameter.AssemblyName, methodParameter.ClassName);
                if (parameterClassType == null)
                {
                    SingletonLogger.Instance.Error(String.Format("Could not resolve method AssemblyName='{0}' ParameterClass='{1}'",
                        methodParameter.AssemblyName, methodParameter.ClassName));
                    return;
                }
                object parameter = methodParameter.DeserializeXmlData(parameterClassType);

                parameters.Add( parameter );
            }

            MethodInfo methodInfo = methodClassType.GetMethod(invokeMethodData.MethodName);
            methodInfo.Invoke(Activator.CreateInstance(methodClassType), parameters.ToArray());
        }

        private static Type ResolveType(String assemblyName, String className)
        {
            String keyTypeCache = assemblyName + className;
            Type type;
            if (TypeCache.ContainsKey(keyTypeCache))
            {
                type = TypeCache[keyTypeCache];
            }
            else
            {
                type = Type.GetType(className);
                if (type == null)
                {
                    foreach (Assembly loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = loadedAssembly.GetType(className);
                        if (type != null)
                        {
                            break;
                        }
                    }

                    type = Assembly.Load(assemblyName).GetType(className);
                }

                if (type == null)
                {
                    return null;
                }

                TypeCache.GetOrAdd(keyTypeCache, type);
            }
            return type;
        }
    }
}
