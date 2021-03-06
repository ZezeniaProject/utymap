﻿using System;
using System.Collections.Generic;
using System.Linq;
using UtyMap.Unity.Core;
using UtyMap.Unity.Core.Models;
using UtyMap.Unity.Core.Tiling;
using UtyMap.Unity.Infrastructure.Primitives;
using UtyMap.Unity.Maps.Data;
using UtyMap.Unity.Maps.Elevation;
using NUnit.Framework;
using UtyDepend;
using UtyMap.Unity.Tests.Helpers;
using UtyRx;

namespace UtyMap.Unity.Tests.Integration
{
    [TestFixture(Category = TestHelper.IntegrationTestCategory)]
    public class ElementEditorTests
    {
        private CompositionRoot _compositionRoot;
        private IElementEditor _elementEditor;
        private IMapDataLoader _dataLoader;
        private Stylesheet _stylesheet;
        private IProjection _projection;

        [TestFixtureSetUp]
        public void Setup()
        {
            _compositionRoot = TestHelper.GetCompositionRoot(TestHelper.WorldZeroPoint,
                (container, _) => container.Register(Component.For<IElevationProvider>().Use<FlatElevationProvider>()));
            _elementEditor = _compositionRoot.GetService<IElementEditor>();
            _dataLoader = _compositionRoot.GetService<IMapDataLoader>();
            _stylesheet = _compositionRoot.GetService<Stylesheet>();
            _projection = _compositionRoot.GetService<IProjection>();           
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            _compositionRoot.Dispose();
        }

        [Test]
        public void CanAddNode()
        {
            var levelOfDetails = new Range<int>(1, 1);
            var node = new Element(7,
                new GeoCoordinate[] { new GeoCoordinate(5, 5) },
                new Dictionary<string, string>() { { "featurecla", "Populated place" } }, 
                new Dictionary<string, string>());

            _elementEditor.Add(MapStorageType.InMemory, node, levelOfDetails);

            Union<Element, Mesh> result = default(Union<Element, Mesh>);
            _dataLoader
                .Load(new Tile(new QuadKey(1, 0, 1), _stylesheet, _projection))
                .Do(u => result = u)
                .Wait();
            result.Match(
                e => CompareElements(node, e), 
                mesh =>
                {
                    throw new ArgumentException();
                });
        }

        private void CompareElements(Element expected, Element actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            // geometry
            Assert.AreEqual(expected.Geometry.Length, actual.Geometry.Length);
            for(int i = 0; i < expected.Geometry.Length; ++i)
                Assert.AreEqual(expected.Geometry[i], actual.Geometry[i]);
            // tags
            Assert.AreEqual(expected.Tags.Count, actual.Tags.Count);
            var keys = expected.Tags.Keys.ToArray();
            for (int i = 0; i < keys.Length; ++i)
                Assert.AreEqual(expected.Tags[keys[i]], actual.Tags[keys[i]]); 
        }
    }
}
