using System;
using System.Reflection;
using NUnit.Framework;

namespace MahjongPrototype.Tests.TestSupport.Core
{
    internal sealed class CollectionTestAccess
    {
        private readonly ReflectionTestAccess reflection;

        public CollectionTestAccess(ReflectionTestAccess reflection)
        {
            this.reflection = reflection;
        }

        public int Count(object collection)
        {
            if (collection is Array array)
                return array.Length;

            return (int)reflection.GetProperty(collection, "Count");
        }

        public object Item(object collection, int index)
        {
            Assert.That(collection, Is.Not.Null, "Cannot read an item from a null collection.");

            if (collection is Array array)
                return array.GetValue(index);

            PropertyInfo itemProperty = collection.GetType().GetProperty("Item");
            Assert.That(
                itemProperty,
                Is.Not.Null,
                $"Indexer property not found: {collection.GetType().FullName}.Item");
            return itemProperty.GetValue(collection, new object[] { index });
        }

        public object Last(object collection)
        {
            return Item(collection, Count(collection) - 1);
        }
    }
}
