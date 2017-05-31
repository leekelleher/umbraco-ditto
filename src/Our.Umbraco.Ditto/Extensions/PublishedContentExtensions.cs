﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Umbraco.Core.Models;
using Umbraco.Web;

namespace Our.Umbraco.Ditto
{
    using global::Umbraco.Core;
    using System.Collections;

    /// <summary>
    /// Encapsulates extension methods for <see cref="IPublishedContent"/>.
    /// </summary>
    public static class PublishedContentExtensions
    {
        /// <summary>
        /// The cache for storing type property information.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache
            = new ConcurrentDictionary<Type, PropertyInfo[]>();

        /// <summary>
        /// Returns the given instance of <see cref="IPublishedContent"/> as the specified type.
        /// </summary>
        /// <param name="content">
        /// The <see cref="IPublishedContent"/> to convert.
        /// </param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/>
        /// </param>
        /// <param name="instance">
        /// An existing instance of T to populate
        /// </param>
        /// <param name="processorContexts">
        /// A collection of <see cref="DittoProcessorContext"/> entities to use whilst processing values.
        /// </param>
        /// <param name="onConverting">
        /// The <see cref="Action{ConversionHandlerContext}"/> to fire when converting.
        /// </param>
        /// <param name="onConverted">
        /// The <see cref="Action{ConversionHandlerContext}"/> to fire when converted.
        /// </param>
        /// <param name="chainContext">
        /// The <see cref="DittoChainContext"/> for the current processor chain.
        /// </param>
        /// <typeparam name="T">
        /// The <see cref="Type"/> of items to return.
        /// </typeparam>
        /// <returns>
        /// The converted generic <see cref="Type"/>.
        /// </returns>
        public static T As<T>(
            this IPublishedContent content,
            CultureInfo culture = null,
            T instance = null,
            IEnumerable<DittoProcessorContext> processorContexts = null,
            Action<DittoConversionHandlerContext> onConverting = null,
            Action<DittoConversionHandlerContext> onConverted = null,
            DittoChainContext chainContext = null)
            where T : class
        {
            return content.As(typeof(T), culture, instance, processorContexts, onConverting, onConverted, chainContext) as T;
        }

        /// <summary>
        /// Gets a collection of the given type from the given <see cref="IEnumerable{IPublishedContent}"/>.
        /// </summary>
        /// <param name="items">
        /// The <see cref="IEnumerable{IPublishedContent}"/> to convert.
        /// </param>
        /// <param name="culture">The <see cref="CultureInfo"/></param>
        /// <param name="processorContexts">
        /// A collection of <see cref="DittoProcessorContext"/> entities to use whilst processing values.
        /// </param>
        /// <param name="onConverting">
        /// The <see cref="Action{ConversionHandlerContext}"/> to fire when converting.
        /// </param>
        /// <param name="onConverted">
        /// The <see cref="Action{ConversionHandlerContext}"/> to fire when converted.
        /// </param>
        /// <param name="chainContext">
        /// The <see cref="DittoChainContext"/> for the current processor chain.
        /// </param>
        /// <typeparam name="T">
        /// The <see cref="Type"/> of items to return.
        /// </typeparam>
        /// <returns>
        /// The converted <see cref="IEnumerable{T}"/>.
        /// </returns>
        public static IEnumerable<T> As<T>(
            this IEnumerable<IPublishedContent> items,
            CultureInfo culture = null,
            IEnumerable<DittoProcessorContext> processorContexts = null,
            Action<DittoConversionHandlerContext> onConverting = null,
            Action<DittoConversionHandlerContext> onConverted = null,
            DittoChainContext chainContext = null)
            where T : class
        {
            return items.As(typeof(T), culture, processorContexts, onConverting, onConverted, chainContext).Select(x => x as T);
        }

        /// <summary>
        /// Gets a collection of the given type from the given <see cref="IEnumerable{IPublishedContent}"/>.
        /// </summary>
        /// <param name="items">
        /// The <see cref="IEnumerable{IPublishedContent}"/> to convert.
        /// </param>
        /// <param name="type">
        /// The <see cref="Type"/> of items to return.
        /// </param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/>.
        /// </param>
        /// <param name="processorContexts">
        /// A collection of <see cref="DittoProcessorContext"/> entities to use whilst processing values.
        /// </param>
        /// <param name="onConverting">
        /// The <see cref="Action{ConversionHandlerContext}"/> to fire when converting.
        /// </param>
        /// <param name="onConverted">
        /// The <see cref="Action{ConversionHandlerContext}"/> to fire when converted.
        /// </param>
        /// <param name="chainContext">
        /// The <see cref="DittoChainContext"/> for the current processor chain.
        /// </param>
        /// <returns>
        /// The converted <see cref="IEnumerable{T}"/>.
        /// </returns>
        public static IEnumerable<object> As(
            this IEnumerable<IPublishedContent> items,
            Type type,
            CultureInfo culture = null,
            IEnumerable<DittoProcessorContext> processorContexts = null,
            Action<DittoConversionHandlerContext> onConverting = null,
            Action<DittoConversionHandlerContext> onConverted = null,
            DittoChainContext chainContext = null)
        {
            using (DittoDisposableTimer.DebugDuration<IEnumerable<object>>("IEnumerable As"))
            {
                var typedItems = items.Select(x => x.As(type, culture, null, processorContexts, onConverting, onConverted, chainContext));

                // We need to cast back here as nothing is strong typed anymore.
                return (IEnumerable<object>)EnumerableInvocations.Cast(type, typedItems);
            }
        }

        /// <summary>
        /// Returns an object representing the given <see cref="Type"/>.
        /// </summary>
        /// <param name="content">
        /// The <see cref="IPublishedContent"/> to convert.
        /// </param>
        /// <param name="type">
        /// The <see cref="Type"/> of items to return.
        /// </param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/>
        /// </param>
        /// <param name="instance">
        /// An existing instance of T to populate
        /// </param>
        /// <param name="processorContexts">
        /// A collection of <see cref="DittoProcessorContext"/> entities to use whilst processing values.
        /// </param>
        /// <param name="onConverting">
        /// The <see cref="Action{ConversionHandlerContext}"/> to fire when converting.
        /// </param>
        /// <param name="onConverted">
        /// The <see cref="Action{ConversionHandlerContext}"/> to fire when converted.
        /// </param>
        /// <param name="chainContext">
        /// The <see cref="DittoChainContext"/> for the current processor chain.
        /// </param>
        /// <returns>
        /// The converted <see cref="Object"/> as the given type.
        /// </returns>
        public static object As(
            this IPublishedContent content,
            Type type,
            CultureInfo culture = null,
            object instance = null,
            IEnumerable<DittoProcessorContext> processorContexts = null,
            Action<DittoConversionHandlerContext> onConverting = null,
            Action<DittoConversionHandlerContext> onConverted = null,
            DittoChainContext chainContext = null)
        {
            // Ensure content
            if (content == null)
            {
                return null;
            }

            // Ensure instance is of target type
            if (instance != null && !type.IsInstanceOfType(instance))
            {
                throw new ArgumentException(string.Format("The instance parameter does not implement Type '{0}'", type.Name), "instance");
            }

            // Get the accessor for UmbracoContext & ApplicationContext
            var umbracoApplicationContextAccessor = (IDittoContextAccessor)Ditto.GetContextAccessorType().GetInstance();

            // Check if the culture has been set, otherwise use from Umbraco, or fallback to a default
            if (culture == null)
            {
                if (umbracoApplicationContextAccessor.UmbracoContext != null && umbracoApplicationContextAccessor.UmbracoContext.PublishedContentRequest != null)
                {
                    culture = umbracoApplicationContextAccessor.UmbracoContext.PublishedContentRequest.Culture;
                }
                else
                {
                    // Fallback
                    culture = CultureInfo.CurrentCulture;
                }
            }

            // Ensure a chain context
            if (chainContext == null)
            {
                chainContext = new DittoChainContext();
            }

            // Populate prcessor contexts collection with any passed in contexts
            chainContext.ProcessorContexts.AddRange(processorContexts);

            // Convert
            using (DittoDisposableTimer.DebugDuration<object>(string.Format("IPublishedContent As ({0})", content.DocumentTypeAlias)))
            {
                var cacheAttr = type.GetCustomAttribute<DittoCacheAttribute>(true);
                if (cacheAttr != null)
                {
                    var ctx = new DittoCacheContext(cacheAttr, content, type, culture);
                    return cacheAttr.GetCacheItem(ctx, () => ConvertContent(content, type, umbracoApplicationContextAccessor, culture, instance, onConverting, onConverted, chainContext));
                }
                else
                {
                    return ConvertContent(content, type, umbracoApplicationContextAccessor, culture, instance, onConverting, onConverted, chainContext);
                }
            }
        }

        /// <summary>
        /// Returns an object representing the given <see cref="Type"/>.
        /// </summary>
        /// <param name="content">
        /// The <see cref="IPublishedContent"/> to convert.
        /// </param>
        /// <param name="type">
        /// The <see cref="Type"/> of items to return.
        /// </param>
        /// <param name="contextAccessor">
        /// The context accessor.
        /// </param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/>
        /// </param>
        /// <param name="instance">
        /// An existing instance of T to populate
        /// </param>
        /// <param name="onConverting">
        /// The <see cref="Action{ConversionHandlerContext}"/> to fire when converting.
        /// </param>
        /// <param name="onConverted">
        /// The <see cref="Action{ConversionHandlerContext}"/> to fire when converted.
        /// </param>
        /// <param name="chainContext">
        /// The <see cref="DittoChainContext"/> for the current processor chain.
        /// </param>
        /// <returns>
        /// The converted <see cref="Object"/> as the given type.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the given type has invalid constructors.
        /// </exception>
        private static object ConvertContent(
            IPublishedContent content,
            Type type,
            IDittoContextAccessor contextAccessor,
            CultureInfo culture,
            object instance,
            Action<DittoConversionHandlerContext> onConverting,
            Action<DittoConversionHandlerContext> onConverted,
            DittoChainContext chainContext)
        {
            // Get the default constructor, parameters and create an instance of the type.
            ParameterInfo[] constructorParams = type.GetConstructorParameters();
            bool hasParameter = false;

            // If not already an instance, create an instance of the object
            if (instance == null)
            {
                if (constructorParams != null && constructorParams.Length == 0)
                {
                    // Internally this uses Activator.CreateInstance which is heavily optimized.
                    instance = type.GetInstance();
                }
                else if (constructorParams != null && constructorParams.Length == 1 && constructorParams[0].ParameterType == typeof(IPublishedContent))
                {
                    // This extension method is about 7x faster than the native implementation.
                    instance = type.GetInstance(content);
                    hasParameter = true;
                }
                else
                {
                    // No valid constructor, but see if the value can be cast to the type
                    if (type.IsInstanceOfType(content))
                    {
                        instance = content;
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format("Can't convert IPublishedContent to {0} as it has no valid constructor. A valid constructor is either an empty one, or one accepting a single IPublishedContent parameter.", type));
                    }
                }
            }

            // Collect all the properties of the given type and loop through writable ones.
            PropertyInfo[] properties;
            PropertyCache.TryGetValue(type, out properties);

            if (properties == null)
            {
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(x => x.CanWrite && x.GetSetMethod() != null).ToArray();

                PropertyCache.TryAdd(type, properties);
            }

            // Gets the default processor for this conversion.
            var defaultProcessorType = DittoProcessorRegistry.Instance.GetDefaultProcessorType(type);

            // A dictionary to store lazily invoked values.
            var lazyProperties = new Dictionary<string, Lazy<object>>();

            // process lazy load properties first
            foreach (var propertyInfo in properties.Where(x => x.ShouldAttemptLazyLoad()))
            {
                // Configure lazy properties
                using (DittoDisposableTimer.DebugDuration<object>(string.Format("ForEach Lazy Property ({1} {0})", propertyInfo.Name, content.Id)))
                {
                    // Ensure it's a virtual property (Only relevant to property level lazy loads)
                    if (!propertyInfo.IsVirtualAndOverridable())
                    {
                        throw new InvalidOperationException("Lazy property '" + propertyInfo.Name + "' of type '" + type.AssemblyQualifiedName + "' must be declared virtual in order to be lazy loadable.");
                    }

                    // Check for the ignore attribute (Only relevant to class level lazy loads).
                    if (propertyInfo.HasCustomAttribute<DittoIgnoreAttribute>())
                    {
                        continue;
                    }

                    // Create a Lazy<object> to deferr returning our value.
                    var deferredPropertyInfo = propertyInfo;
                    var localInstance = instance;

                    // ReSharper disable once PossibleMultipleEnumeration
                    lazyProperties.Add(propertyInfo.Name, new Lazy<object>(() => GetProcessedValue(content, culture, type, deferredPropertyInfo, localInstance, defaultProcessorType, contextAccessor, chainContext)));
                }
            }

            if (lazyProperties.Any())
            {
                // Create a proxy instance to replace our object.
                var interceptor = new LazyInterceptor(instance, lazyProperties);
                var factory = new ProxyFactory();

                instance = hasParameter
                    ? factory.CreateProxy(type, interceptor, content)
                    : factory.CreateProxy(type, interceptor);
            }

            // We have the instance object but haven't yet populated properties
            // so fire the on converting event handlers
            OnConverting(ref content, type, instance, onConverting);

            // Process any non lazy properties next
            foreach (var propertyInfo in properties.Where(x => !x.ShouldAttemptLazyLoad()))
            {
                // Configure non lazy properties
                using (DittoDisposableTimer.DebugDuration<object>(string.Format("ForEach Property ({1} {0})", propertyInfo.Name, content.Id)))
                {
                    // Check for the ignore attribute.
                    if (propertyInfo.HasCustomAttribute<DittoIgnoreAttribute>())
                    {
                        continue;
                    }

                    // Set the value normally.
                    // ReSharper disable once PossibleMultipleEnumeration
                    var value = GetProcessedValue(content, culture, type, propertyInfo, instance, defaultProcessorType, contextAccessor, chainContext);

                    // This is 2x as fast as propertyInfo.SetValue(instance, value, null);
                    PropertyInfoInvocations.SetValue(propertyInfo, instance, value);
                }
            }

            // We have now finished populating the instance object so go ahead
            // and fire the on converted event handlers
            OnConverted(ref content, type, instance, onConverted);

            return instance;
        }

        /// <summary>
        /// Returns the processed value for the given type and property.
        /// </summary>
        /// <param name="content">The <see cref="IPublishedContent" /> to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo" /></param>
        /// <param name="targetType">The target type.</param>
        /// <param name="propertyInfo">The <see cref="PropertyInfo" /> property info associated with the type.</param>
        /// <param name="instance">The instance to assign the value to.</param>
        /// <param name="defaultProcessorType">The default processor type.</param>
        /// <param name="contextAccessor">The context accessor.</param>
        /// <param name="chainContext">The <see cref="DittoChainContext"/> for the current processor chain.</param>
        /// <returns>
        /// The <see cref="object" /> representing the Umbraco value.
        /// </returns>
        private static object GetProcessedValue(
            IPublishedContent content,
            CultureInfo culture,
            Type targetType,
            PropertyInfo propertyInfo,
            object instance,
            Type defaultProcessorType,
            IDittoContextAccessor contextAccessor,
            DittoChainContext chainContext)
        {
            // Time custom value-processor.
            using (DittoDisposableTimer.DebugDuration<object>(string.Format("Custom ValueProcessor ({0}, {1})", content.Id, propertyInfo.Name)))
            {
                // Get the target property description
                var propertyDescriptor = TypeDescriptor.GetProperties(instance)[propertyInfo.Name];

                // Create a base processor context for this current chain level
                var baseProcessorContext = new DittoProcessorContext
                {
                    Content = content,
                    TargetType = targetType,
                    PropertyDescriptor = propertyDescriptor,
                    Culture = culture
                };

                // Check for cache attribute
                var cacheAttr = propertyInfo.GetCustomAttribute<DittoCacheAttribute>(true);
                if (cacheAttr != null)
                {
                    var ctx = new DittoCacheContext(cacheAttr, content, targetType, propertyDescriptor, culture);
                    return cacheAttr.GetCacheItem(ctx, () => DoGetProcessedValue(content, propertyInfo, defaultProcessorType, contextAccessor, baseProcessorContext, chainContext));
                }
                else
                {
                    return DoGetProcessedValue(content, propertyInfo, defaultProcessorType, contextAccessor, baseProcessorContext, chainContext);
                }
            }
        }

        /// <summary>
        /// Returns the processed value for the given type and property.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="defaultProcessorType">The default processor type.</param>
        /// <param name="contextAccessor">The context accessor.</param>
        /// <param name="baseProcessorContext">The base processor context.</param>
        /// <param name="chainContext">The <see cref="DittoChainContext"/> for the current processor chain.</param>
        /// <returns>Returns the processed value.</returns>
        private static object DoGetProcessedValue(
            IPublishedContent content,
            PropertyInfo propertyInfo,
            Type defaultProcessorType,
            IDittoContextAccessor contextAccessor,
            DittoProcessorContext baseProcessorContext,
            DittoChainContext chainContext)
        {
            // Check the property for any explicit processor attributes
            var processorAttrs = propertyInfo.GetCustomAttributes<DittoProcessorAttribute>(true)
                .OrderBy(x => x.Order)
                .ToList();

            if (!processorAttrs.Any())
            {
                // Adds the default processor attribute
                processorAttrs.Add((DittoProcessorAttribute)defaultProcessorType.GetInstance());
            }

            var propertyType = propertyInfo.PropertyType;

            // Check for type registered processors
            processorAttrs.AddRange(propertyType.GetCustomAttributes<DittoProcessorAttribute>(true)
                    .OrderBy(x => x.Order));

            // Check any type arguments in generic enumerable types.
            // This should return false against typeof(string) etc also.
            var typeInfo = propertyType.GetTypeInfo();
            bool isEnumerable = false;
            Type typeArg = null;
            if (propertyType.IsCastableEnumerableType())
            {
                typeArg = typeInfo.GenericTypeArguments.First();
                processorAttrs.AddRange(typeInfo.GenericTypeArguments.First().GetCustomAttributes<DittoProcessorAttribute>(true)
                    .OrderBy(x => x.Order)
                    .ToList());

                isEnumerable = true;
            }

            // Check for globally registered processors
            processorAttrs.AddRange(DittoProcessorRegistry.Instance.GetRegisteredProcessorAttributesFor(propertyInfo.PropertyType));

            // Add any core processors onto the end
            processorAttrs.AddRange(DittoProcessorRegistry.Instance.GetPostProcessorAttributes());

            // Create holder for value as it's processed
            object currentValue = content;

            // Process attributes
            foreach (var processorAttr in processorAttrs)
            {
                // Get the right context type
                var ctx = chainContext.ProcessorContexts.GetOrCreate(baseProcessorContext, processorAttr.ContextType);

                // Populate UmbracoContext & ApplicationContext 
                processorAttr.UmbracoContext = contextAccessor.UmbracoContext;
                processorAttr.ApplicationContext = contextAccessor.ApplicationContext;

                // Process value
                currentValue = processorAttr.ProcessValue(currentValue, ctx, chainContext);
            }

            // The following has to happen after all the processors.
            if (isEnumerable && currentValue != null && currentValue.Equals(Enumerable.Empty<object>()))
            {
                if (propertyType.IsInterface)
                {
                    // You cannot set an enumerable of type from an empty object array.
                    currentValue = EnumerableInvocations.Cast(typeArg, (IEnumerable)currentValue);
                }
                else
                {
                    // This should allow the casting back of IEnumerable<T> to an empty List<T> Collection<T> etc.
                    // I cant think of any that don't have an empty constructor
                    currentValue = propertyType.GetInstance();
                }
            }

            return (currentValue == null && propertyType.IsValueType)
                ? propertyInfo.PropertyType.GetInstance() // Set to default instance of value type
                : currentValue;
        }

        /// <summary>
        /// Fires off the various on converting events.
        /// </summary>
        /// <param name="content">The <see cref="IPublishedContent"/> to convert.</param>
        /// <param name="type">The instance type.</param>
        /// <param name="instance">The instance to assign the value to.</param>
        /// <param name="callback">
        /// The <see cref="Action{ConversionHandlerContext}"/> to fire when converting.
        /// </param>
        private static void OnConverting(
            ref IPublishedContent content,
            Type type,
            object instance,
            Action<DittoConversionHandlerContext> callback)
        {
            OnConvert<DittoOnConvertingAttribute>(
                DittoConversionHandlerType.OnConverting,
                ref content,
                type,
                instance,
                callback);
        }

        /// <summary>
        /// Fires off the various on converted events.
        /// </summary>
        /// <param name="content">The <see cref="IPublishedContent"/> to convert.</param>
        /// <param name="type">The instance type.</param>
        /// <param name="instance">The instance to assign the value to.</param>
        /// <param name="callback">
        /// The <see cref="Action{ConversionHandlerContext}"/> to fire when converted.
        /// </param>
        private static void OnConverted(
            ref IPublishedContent content,
            Type type,
            object instance,
            Action<DittoConversionHandlerContext> callback)
        {
            OnConvert<DittoOnConvertedAttribute>(
                DittoConversionHandlerType.OnConverted,
                ref content,
                type,
                instance,
                callback);
        }

        /// <summary>
        /// Convenience method for calling converting/converter handlers.
        /// </summary>
        /// <typeparam name="TAttributeType">The type of the attribute type.</typeparam>
        /// <param name="conversionType">Type of the conversion.</param>
        /// <param name="content">The content.</param>
        /// <param name="type">The type.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="callback">The callback.</param>
        private static void OnConvert<TAttributeType>(
            DittoConversionHandlerType conversionType,
            ref IPublishedContent content,
            Type type,
            object instance,
            Action<DittoConversionHandlerContext> callback)
            where TAttributeType : Attribute
        {
            // Trigger conversion handlers
            var conversionCtx = new DittoConversionHandlerContext
            {
                Content = content,
                ModelType = type,
                Model = instance
            };

            // Check for class level DittoOnConvertedAttribute
            foreach (var attr in type.GetCustomAttributes<DittoConversionHandlerAttribute>())
            {
                ((DittoConversionHandler)attr.HandlerType.GetInstance())
                    .Run(conversionCtx, conversionType);
            }

            // Check for globaly registered handlers
            foreach (var handlerType in DittoConversionHandlerRegistry.Instance.GetRegisteredHandlerTypesFor(type))
            {
                ((DittoConversionHandler)handlerType.GetInstance())
                    .Run(conversionCtx, conversionType);
            }

            // Check for method level DittoOnConvertedAttribute
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.GetCustomAttribute<TAttributeType>() != null))
            {
                var p = method.GetParameters();
                if (p.Length == 1 && p[0].ParameterType == typeof(DittoConversionHandlerContext))
                {
                    method.Invoke(instance, new object[] { conversionCtx });
                }
            }

            // Check for a callback function
            if (callback != null)
            {
                callback(conversionCtx);
            }

            // When converting, if the IPublishedContent has changed, pass back the reference
            if (conversionType == DittoConversionHandlerType.OnConverting && conversionCtx.Content != content)
                content = conversionCtx.Content;
        }
    }
}