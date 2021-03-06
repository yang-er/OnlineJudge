﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Routing
{
    internal sealed class TemplateBinderMini
    {
        public static TemplateValuesResult GetValues(
            KeyValuePair<string, object>[] _slots,
            KeyValuePair<string, object>[] _filters,
            string[] _requiredKeys,
            RoutePattern _pattern,
            RouteValueDictionary _defaults,
            RouteValueDictionary ambientValues,
            RouteValueDictionary values)
        {
            // Make a new copy of the slots array, we'll use this as 'scratch' space
            // and then the RVD will take ownership of it.
            var slots = new KeyValuePair<string, object>[_slots.Length];
            Array.Copy(_slots, 0, slots, 0, slots.Length);

            // Keeping track of the number of 'values' we've processed can be used to avoid doing
            // some expensive 'merge' operations later.
            var valueProcessedCount = 0;

            // Start by copying all of the values out of the 'values' and into the slots. There's no success
            // case where we *don't* use all of the 'values' so there's no reason not to do this up front
            // to avoid visiting the values dictionary again and again.
            for (var i = 0; i < slots.Length; i++)
            {
                var key = slots[i].Key;
                if (values.TryGetValue(key, out var value))
                {
                    // We will need to know later if the value in the 'values' was an null value.
                    // This affects how we process ambient values. Since the 'slots' are initialized
                    // with null values, we use the null-object-pattern to track 'explicit null', which means that
                    // null means omitted.
                    value = IsRoutePartNonEmpty(value) ? value : SentinullValue.Instance;
                    slots[i] = new KeyValuePair<string, object>(key, value);

                    // Track the count of processed values - this allows a fast path later.
                    valueProcessedCount++;
                }
            }

            // In Endpoint Routing, patterns can have logical parameters that appear 'to the left' of
            // the route template. This governs whether or not the template can be selected (they act like
            // filters), and whether the remaining ambient values should be used.
            // should be used.
            // For example, in case of MVC it flattens out a route template like below
            //  {controller}/{action}/{id?}
            // to
            //  Products/Index/{id?},
            //  defaults: new { controller = "Products", action = "Index" },
            //  requiredValues: new { controller = "Products", action = "Index" }
            // In the above example, "controller" and "action" are no longer parameters.
            var copyAmbientValues = ambientValues != null;
            if (copyAmbientValues)
            {
                var requiredKeys = _requiredKeys;
                for (var i = 0; i < requiredKeys.Length; i++)
                {
                    // For each required key, the values and ambient values need to have the same value.
                    var key = requiredKeys[i];
                    var hasExplicitValue = values.TryGetValue(key, out var value);

                    if (ambientValues == null || !ambientValues.TryGetValue(key, out var ambientValue))
                    {
                        ambientValue = null;
                    }

                    // For now, only check ambient values with required values that don't have a parameter
                    // Ambient values for parameters are processed below
                    var hasParameter = _pattern.GetParameter(key) != null;
                    if (!hasParameter)
                    {
                        if (!_pattern.RequiredValues.TryGetValue(key, out var requiredValue))
                        {
                            throw new InvalidOperationException($"Unable to find required value '{key}' on route pattern.");
                        }

                        if (!RoutePartsEqual(ambientValue, _pattern.RequiredValues[key]) &&
                            !ReferenceEquals(RoutePattern.RequiredValueAny, _pattern.RequiredValues[key]))
                        {
                            copyAmbientValues = false;
                            break;
                        }

                        if (hasExplicitValue && !RoutePartsEqual(value, ambientValue))
                        {
                            copyAmbientValues = false;
                            break;
                        }
                    }
                }
            }

            // We can now process the rest of the parameters (from left to right) and copy the ambient
            // values as long as the conditions are met.
            //
            // Find out which entries in the URI are valid for the URI we want to generate.
            // If the URI had ordered parameters a="1", b="2", c="3" and the new values
            // specified that b="9", then we need to invalidate everything after it. The new
            // values should then be a="1", b="9", c=<no value>.
            //
            // We also handle the case where a parameter is optional but has no value - we shouldn't
            // accept additional parameters that appear *after* that parameter.
            var parameters = _pattern.Parameters;
            var parameterCount = _pattern.Parameters.Count;
            for (var i = 0; i < parameterCount; i++)
            {
                var key = slots[i].Key;
                var value = slots[i].Value;

                // Whether or not the value was explicitly provided is signficant when comparing
                // ambient values. Remember that we're using a special sentinel value so that we
                // can tell the difference between an omitted value and an explicitly specified null.
                var hasExplicitValue = value != null;

                var hasAmbientValue = false;
                var ambientValue = (object)null;

                var parameter = parameters[i];

                // We are copying **all** ambient values
                if (copyAmbientValues)
                {
                    hasAmbientValue = ambientValues != null && ambientValues.TryGetValue(key, out ambientValue);
                    if (hasExplicitValue && hasAmbientValue && !RoutePartsEqual(ambientValue, value))
                    {
                        // Stop copying current values when we find one that doesn't match
                        copyAmbientValues = false;
                    }

                    if (!hasExplicitValue &&
                        !hasAmbientValue &&
                        _defaults?.ContainsKey(parameter.Name) != true)
                    {
                        // This is an unsatisfied parameter value and there are no defaults. We might still
                        // be able to generate a URL but we should stop 'accepting' ambient values.
                        //
                        // This might be a case like:
                        //  template: a/{b?}/{c?}
                        //  ambient: { c = 17 }
                        //  values: { }
                        //
                        // We can still generate a URL from this ("/a") but we shouldn't accept 'c' because
                        // we can't use it.
                        //
                        // In the example above we should fall into this block for 'b'.
                        copyAmbientValues = false;
                    }
                }

                // This might be an ambient value that matches a required value. We want to use these even if we're
                // not bulk-copying ambient values.
                //
                // This comes up in a case like the following:
                //  ambient-values: { page = "/DeleteUser", area = "Admin", }
                //  values: { controller = "Home", action = "Index", }
                //  pattern: {area}/{controller}/{action}/{id?}
                //  required-values: { area = "Admin", controller = "Home", action = "Index", page = (string)null, }
                //
                // OR in plain English... when linking from a page in an area to an action in the same area, it should
                // be possible to use the area as an ambient value.
                if (!copyAmbientValues && !hasExplicitValue && _pattern.RequiredValues.TryGetValue(key, out var requiredValue))
                {
                    hasAmbientValue = ambientValues != null && ambientValues.TryGetValue(key, out ambientValue);
                    if (hasAmbientValue &&
                        (RoutePartsEqual(requiredValue, ambientValue) || ReferenceEquals(RoutePattern.RequiredValueAny, requiredValue)))
                    {
                        // Treat this an an explicit value to *force it*.
                        slots[i] = new KeyValuePair<string, object>(key, ambientValue);
                        hasExplicitValue = true;
                        value = ambientValue;
                    }
                }

                // If the parameter is a match, add it to the list of values we will use for URI generation
                if (hasExplicitValue && !ReferenceEquals(value, SentinullValue.Instance))
                {
                    // Already has a value in the list, do nothing
                }
                else if (copyAmbientValues && hasAmbientValue)
                {
                    slots[i] = new KeyValuePair<string, object>(key, ambientValue);
                }
                else if (parameter.IsOptional || parameter.IsCatchAll)
                {
                    // Value isn't needed for optional or catchall parameters - wipe out the key, so it
                    // will be omitted from the RVD.
                    slots[i] = default;
                }
                else if (_defaults != null && _defaults.TryGetValue(parameter.Name, out var defaultValue))
                {

                    // Add the default value only if there isn't already a new value for it and
                    // only if it actually has a default value.
                    slots[i] = new KeyValuePair<string, object>(key, defaultValue);
                }
                else
                {
                    // If we get here, this parameter needs a value, but doesn't have one. This is a
                    // failure case.
                    return null;
                }
            }

            // Any default values that don't appear as parameters are treated like filters. Any new values
            // provided must match these defaults.
            var filters = _filters;
            for (var i = 0; i < filters.Length; i++)
            {
                var key = filters[i].Key;
                var value = slots[i + parameterCount].Value;

                // We use a sentinel value here so we can track the different between omission and explicit null.
                // 'real null' means that the value was omitted.
                var hasExplictValue = value != null;
                if (hasExplictValue)
                {
                    // If there is a non-parameterized value in the route and there is a
                    // new value for it and it doesn't match, this route won't match.
                    if (!RoutePartsEqual(value, filters[i].Value))
                    {
                        return null;
                    }
                }
                else
                {
                    // If no value was provided, then blank out this slot so that it doesn't show up in accepted values.
                    slots[i + parameterCount] = default;
                }
            }

            // At this point we've captured all of the 'known' route values, but we have't
            // handled an extra route values that were provided in 'values'. These all
            // need to be included in the accepted values.
            var acceptedValues = RouteValueDictionary.FromArray(slots);

            if (valueProcessedCount < values.Count)
            {
                // There are some values in 'value' that are unaccounted for, merge them into
                // the dictionary.
                foreach (var kvp in values)
                {
                    if (!_defaults.ContainsKey(kvp.Key))
                    {
#if RVD_TryAdd
                        acceptedValues.TryAdd(kvp.Key, kvp.Value);
#else
                        if (!acceptedValues.ContainsKey(kvp.Key))
                        {
                            acceptedValues.Add(kvp.Key, kvp.Value);
                        }
#endif
                    }
                }
            }

            // Currently this copy is required because BindValues will mutate the accepted values :(
            var combinedValues = new RouteValueDictionary(acceptedValues);

            // Add any ambient values that don't match parameters - they need to be visible to constraints
            // but they will ignored by link generation.
            CopyNonParameterAmbientValues(
                ambientValues: ambientValues,
                acceptedValues: acceptedValues,
                combinedValues: combinedValues,_pattern);

            return new TemplateValuesResult()
            {
                AcceptedValues = acceptedValues,
                CombinedValues = combinedValues,
            };
        }

        [DebuggerDisplay("explicit null")]
        private class SentinullValue
        {
            public static object Instance = new SentinullValue();

            [DebuggerStepThrough]
            private SentinullValue()
            {
            }

            public override string ToString() => string.Empty;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsRoutePartNonEmpty(object part)
        {
            if (part == null)
            {
                return false;
            }

            if (ReferenceEquals(SentinullValue.Instance, part))
            {
                return false;
            }

            if (part is string stringPart && stringPart.Length == 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Compares two objects for equality as parts of a case-insensitive path.
        /// </summary>
        /// <param name="a">An object to compare.</param>
        /// <param name="b">An object to compare.</param>
        /// <returns>True if the object are equal, otherwise false.</returns>
        [DebuggerStepThrough]
        public static bool RoutePartsEqual(object a, object b)
        {
            var sa = a as string ?? (ReferenceEquals(SentinullValue.Instance, a) ? string.Empty : null);
            var sb = b as string ?? (ReferenceEquals(SentinullValue.Instance, b) ? string.Empty : null);

            // In case of strings, consider empty and null the same.
            // Since null cannot tell us the type, consider it to be a string if the other value is a string.
            if ((sa == string.Empty && sb == null) || (sb == string.Empty && sa == null))
            {
                return true;
            }
            else if (sa != null && sb != null)
            {
                // For strings do a case-insensitive comparison
                return string.Equals(sa, sb, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                if (a != null && b != null)
                {
                    // Explicitly call .Equals() in case it is overridden in the type
                    return a.Equals(b);
                }
                else
                {
                    // At least one of them is null. Return true if they both are
                    return a == b;
                }
            }
        }

        [DebuggerStepThrough]
        private static void CopyNonParameterAmbientValues(
            RouteValueDictionary ambientValues,
            RouteValueDictionary acceptedValues,
            RouteValueDictionary combinedValues,
            RoutePattern _pattern)
        {
            if (ambientValues == null)
            {
                return;
            }

            foreach (var kvp in ambientValues)
            {
                if (IsRoutePartNonEmpty(kvp.Value))
                {
                    var parameter = _pattern.GetParameter(kvp.Key);
                    if (parameter == null && !acceptedValues.ContainsKey(kvp.Key))
                    {
                        combinedValues.Add(kvp.Key, kvp.Value);
                    }
                }
            }
        }

        [DebuggerStepThrough]
        public void FakeCall(
            TemplateBinder binder,
            RouteValueDictionary ambientValues,
            RouteValueDictionary values)
        {
            var par = GetType().GetMethod(nameof(GetValues)).GetParameters();
            var ot = new object[par.Length];
            for (int i = 0; i < par.Length - 2; i++)
                ot[i] = typeof(TemplateBinder).GetField(par[i].Name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(binder);
            ot[par.Length - 2] = ambientValues;
            ot[par.Length - 1] = values;
            GetType().GetMethod(nameof(GetValues)).Invoke(null, ot);
        }
    }
}