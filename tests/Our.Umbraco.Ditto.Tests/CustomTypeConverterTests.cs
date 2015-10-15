﻿namespace Our.Umbraco.Ditto.Tests
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using NUnit.Framework;
    using Our.Umbraco.Ditto.Tests.Mocks;
    using global::Umbraco.Core.Models;

    using Newtonsoft.Json;

    [TestFixture]
    public class CustomTypeConverterTests
    {
        public class MyModel
        {
            [UmbracoProperty("Id")]
            [DittoTypeConverter(typeof(MyCustomConverter))]
            public virtual IPublishedContent MyProperty { get; set; }
        }

        public class MyCustomConverter : DittoConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(int);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                var id = (int)value;

                return new PublishedContentMock
                {
                    Id = id
                };
            }
        }

        [Test]
        public void Custom_TypeConverter_Converts()
        {
            var id = 1234;

            var content = new PublishedContentMock()
            {
                Id = id
            };

            var model = content.As<MyModel>();

            Assert.That(model.MyProperty, Is.InstanceOf<IPublishedContent>());
            Assert.That(model.MyProperty.Id, Is.EqualTo(id));
        }

        [Test]
        public void CanSerializeModelWithTypeAttribute()
        {
            var id = 1234;

            var content = new PublishedContentMock()
            {
                Id = id
            };

            var model = content.As<MyModel>();

            Assert.That(model.MyProperty, Is.InstanceOf<IPublishedContent>());
            Assert.That(model.MyProperty.Id, Is.EqualTo(id));

            var serialized = JsonConvert.SerializeObject(model);
            Assert.NotNull(serialized);
            Assert.True(serialized.IndexOf("\"Id\":1234", StringComparison.Ordinal) > -1);
        }
    }
}