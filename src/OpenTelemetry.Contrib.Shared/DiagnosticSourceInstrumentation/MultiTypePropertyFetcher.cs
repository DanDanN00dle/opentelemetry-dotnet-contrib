// <copyright file="MultiTypePropertyFetcher.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace OpenTelemetry.Instrumentation
{
    /// <summary>
    /// PropertyFetcher fetches a property from an object.
    /// </summary>
    /// <typeparam name="T">The type of the property being fetched.</typeparam>
    internal class MultiTypePropertyFetcher<T>
    {
        private readonly string propertyName;
        private readonly ConcurrentDictionary<Type, PropertyFetch> innerFetcher = new ConcurrentDictionary<Type, PropertyFetch>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTypePropertyFetcher{T}"/> class.
        /// </summary>
        /// <param name="propertyName">Property name to fetch.</param>
        public MultiTypePropertyFetcher(string propertyName)
        {
            this.propertyName = propertyName;
        }

        /// <summary>
        /// Fetch the property from the object.
        /// </summary>
        /// <param name="obj">Object to be fetched.</param>
        /// <returns>Property fetched.</returns>
        public T Fetch(object obj)
        {
            if (obj == null)
            {
                return default;
            }

            var type = obj.GetType().GetTypeInfo();
            PropertyFetch fetcher = null;
            if (!this.innerFetcher.TryGetValue(type, out fetcher))
            {
                var property = type.DeclaredProperties.FirstOrDefault(p => string.Equals(p.Name, this.propertyName, StringComparison.InvariantCultureIgnoreCase));
                if (property == null)
                {
                    property = type.GetProperty(this.propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                }

                fetcher = PropertyFetch.FetcherForProperty(property);

                this.innerFetcher.TryAdd(type, fetcher);
            }

            if (fetcher == null)
            {
                return default;
            }

            return fetcher.Fetch(obj);
        }

        // see https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/DiagnosticSourceEventSource.cs
        private class PropertyFetch
        {
            /// <summary>
            /// Create a property fetcher from a .NET Reflection PropertyInfo class that
            /// represents a property of a particular type.
            /// </summary>
            public static PropertyFetch FetcherForProperty(PropertyInfo propertyInfo)
            {
                if (propertyInfo == null || !typeof(T).IsAssignableFrom(propertyInfo.PropertyType))
                {
                    // returns null on any fetch.
                    return new PropertyFetch();
                }

                var typedPropertyFetcher = typeof(TypedPropertyFetch<,>);
                var instantiatedTypedPropertyFetcher = typedPropertyFetcher.MakeGenericType(
                    typeof(T), propertyInfo.DeclaringType, propertyInfo.PropertyType);
                return (PropertyFetch)Activator.CreateInstance(instantiatedTypedPropertyFetcher, propertyInfo);
            }

            public virtual T Fetch(object obj)
            {
                return default;
            }

            private class TypedPropertyFetch<TDeclaredObject, TDeclaredProperty> : PropertyFetch
                where TDeclaredProperty : T
            {
                private readonly Func<TDeclaredObject, TDeclaredProperty> propertyFetch;

                public TypedPropertyFetch(PropertyInfo property)
                {
                    this.propertyFetch = (Func<TDeclaredObject, TDeclaredProperty>)property.GetMethod.CreateDelegate(typeof(Func<TDeclaredObject, TDeclaredProperty>));
                }

                public override T Fetch(object obj)
                {
                    if (obj is TDeclaredObject o)
                    {
                        return this.propertyFetch(o);
                    }

                    return default;
                }
            }
        }
    }
}
