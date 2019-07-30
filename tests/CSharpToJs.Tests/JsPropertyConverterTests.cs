﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSharpToJs.Core.Models;
using CSharpToJs.Core.Services;
using CSharpToJs.Tests.Mocks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace CSharpToJs.Tests
{
    public class JsPropertyConverterTests
    {
        [Fact]
        public void DefaultJsPropertyConverterSimpleConversion()
        {
            var dummy = new DummyClass
            {
                Field = "IsField",
                IAmAProperty = "IsProp",
                BoolProp = true,
                IntegerProp = 10
            };
            

            var values = new Dictionary<string, string>
            {
                { nameof(dummy.IAmAProperty), $"\"{dummy.IAmAProperty}\""},
                { nameof(dummy.IntegerProp), dummy.IntegerProp.ToString() },
                { nameof(dummy.BoolProp), dummy.BoolProp.ToString().ToLower() }
            };

            var converter = new JsPropertyConverter();

            foreach (var (key, value) in values)
            {
                var propInfo = typeof(DummyClass).GetProperty(key);
                var orgValue = propInfo.GetValue(dummy);
                var jsProp = converter.Convert(new PropertyConverterContext
                {
                    PropertyInfo = propInfo,
                    OriginalValue = orgValue
                });

                Assert.Same(orgValue, jsProp.OriginalValue);
                Assert.Same(propInfo, jsProp.PropertyInfo);
                Assert.Equal(key[0].ToString().ToLower()+key.Substring(1), jsProp.Name);
                Assert.Equal(JsPropertyType.Plain, jsProp.PropertyType);
                Assert.Equal(value, jsProp.Value);
            }
        }

        [Fact]
        public void DefaultJsPropertyConverterComplexConversion()
        {
            var dummy = new DummyClass
            {
                IAmAProperty = "IsProp",
                AComplexType = new ComplexType
                {
                    IsComplex = "very"
                }
            };
            var propInfo = typeof(DummyClass).GetProperty(nameof(DummyClass.AComplexType));
            var orgValue = propInfo.GetValue(dummy);
            var expectedPropName = "aComplexType";
            var expectedValue = "new ComplexType()";

            var converter = new JsPropertyConverter();

            var jsProp = converter.Convert(new PropertyConverterContext
            {
                PropertyInfo = propInfo,
                OriginalValue =  orgValue,
                IncludedNamespaces = new List<string> { "CSharpToJs.Tests.Mocks" }
            });

            Assert.Same(orgValue, jsProp.OriginalValue);
            Assert.Equal(expectedPropName, jsProp.Name);
            Assert.Equal(JsPropertyType.Instance, jsProp.PropertyType);
            Assert.Equal(expectedValue, jsProp.Value);
        }

        [Fact]
        public void DefaultJsPropertyConverterComplexConversionWhenExcluded()
        {
            var dummy = new DummyClass
            {
                IAmAProperty = "IsProp",
                AComplexType = new ComplexType()
                {
                    IsComplex = "very"
                }
            };
            var propInfo = typeof(DummyClass).GetProperty(nameof(DummyClass.AComplexType));
            var orgValue = propInfo.GetValue(dummy);
            var expectedPropName = "aComplexType";
            var expectedValue = JsonConvert.SerializeObject(dummy.AComplexType, Formatting.None, Settings.SerializerSettings);

            var converter = new JsPropertyConverter();

            var jsProp = converter.Convert(new PropertyConverterContext
            {
                PropertyInfo = propInfo,
                OriginalValue = orgValue,
                IncludedNamespaces = new List<string> { "CSharpToJs.Tests.Mocks" },
                ExcludedNamespaces = new List<string> { "CSharpToJs.Tests.Mocks" }
            });

            Assert.Same(orgValue, jsProp.OriginalValue);
            Assert.Equal(expectedPropName, jsProp.Name);
            Assert.Equal(JsPropertyType.Plain, jsProp.PropertyType);
            Assert.Equal(expectedValue, jsProp.Value);
        }

        [Fact]
        public void SetCustomNameConverter()
        {
            var converter = new JsPropertyConverter();
            var nameConverter = new PropertyNameConverterMock();

            converter.PropertyNameConverter = nameConverter;

            Assert.Equal(nameConverter, converter.PropertyNameConverter);
        }

        [Fact]
        public void EnsureDependenciesAreSerializedForJsIgnore()
        {
            var propResolver = new PropertyResolver();
            var instance = new DummyClass();
            var propValue = instance.IgnoredClass;

            var props = propResolver.GetProperties(typeof(DummyClass));
            var prop = props.Single(a => a.Name == nameof(DummyClass.IgnoredClass));
            var propConverter = new JsPropertyConverter();
            var jsProp = propConverter.Convert(new PropertyConverterContext
            {
                PropertyInfo = prop,
                OriginalValue = propValue,
            });

            Assert.Equal(JsPropertyType.Plain, jsProp.PropertyType);
            Assert.Equal(jsProp.Value,
                JsonConvert.SerializeObject(new IgnoredClass(), Formatting.None, Settings.SerializerSettings));
        }
    }
}
