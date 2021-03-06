﻿using NUnit.Framework;
using Our.Umbraco.Ditto.Tests.Mocks;
using Umbraco.Core.Dictionary;
using Moq;
using Umbraco.Core.ObjectResolution;

namespace Our.Umbraco.Ditto.Tests
{
    [TestFixture]
    [Category("Processors")]
    public class DictionaryValueTests
    {
        public class MyModel
        {
            [UmbracoDictionary("hello")]
            public string MyProperty { get; set; }

            [UmbracoDictionary]
            public string Foo { get; set; }
        }

        [TestFixtureSetUp]
        public void Init()
        {
            if (!CultureDictionaryFactoryResolver.HasCurrent)
            {
                var mockDictionary = new Mock<ICultureDictionary>();
                mockDictionary.SetupGet(p => p["hello"]).Returns("world");
                mockDictionary.SetupGet(p => p["Foo"]).Returns("bar");

                var mockDictionaryFactory = new Mock<ICultureDictionaryFactory>();
                mockDictionaryFactory.Setup(x => x.CreateDictionary()).Returns(mockDictionary.Object);


                CultureDictionaryFactoryResolver.Current = new CultureDictionaryFactoryResolver(mockDictionaryFactory.Object);

                Resolution.Freeze();
            }
        }
        
        [TestFixtureTearDown]
        public void Teardown()
        {
            if (Resolution.IsFrozen)
            {
                Resolution.Reset();
            }
        }

        [Test]
        public void DictionaryValue_Returned()
        {
            // Create mock content
            var content = new MockPublishedContent();

            // Run conversion
            var model = content.As<MyModel>();

            // Assert checks
            Assert.That(model.MyProperty, Is.EqualTo("world"));
            Assert.That(model.Foo, Is.EqualTo("bar"));
        }
    }
}