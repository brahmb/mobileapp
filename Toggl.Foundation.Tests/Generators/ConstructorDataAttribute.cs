using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using FluentAssertions;
using FsCheck.Xunit;
using Toggl.Multivac.Extensions;
using Xunit.Sdk;

namespace Toggl.Foundation.Tests.Generators
{
    public sealed class ConstructorDataAttribute : DataAttribute
    {
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            var parameterCount = testMethod.GetParameters().Length;
            return Enumerable.Range(0, parameterCount)
                .Select(i => Enumerable.Range(0, parameterCount)
                    .Select(j => (object)(i != j))
                    .ToArray());
        }
    }

    public sealed class ConstructorTestDataTests
    {
        public sealed class TheGeneratedSequence
        {
            private static readonly ConstructorDataAttribute constructorAttribute = new ConstructorDataAttribute();

            [Property]
            public void ReturnsSequenceWithExactlyOneFalseItem(byte n)
            {
                if (n == 0) return;
                var testMethod = createMethodWithNParameters(n);

                var testData = constructorAttribute.GetData(testMethod);

                testData.All(sequence => sequence.Count(x => (bool)x == false) == 1).Should().BeTrue();
            }

            [Property]
            public void ReturnsSequenceWithExactlyOneFalseItemForEachParameter(byte n)
            {
                if (n == 0) return;
                var testMethod = createMethodWithNParameters(n);

                var testData = constructorAttribute.GetData(testMethod).ToList();

                testData.Count.Should().Be(n);
                Enumerable.Range(0, n).ForEach(i =>
                {
                    var valueForElement = (bool)testData.ElementAt(i).ElementAt(i);
                    valueForElement.Should().BeFalse();
                });
            }

            private static MethodInfo createMethodWithNParameters(int n)
            {
                var methodArgs = Enumerable.Repeat(typeof(int), n).ToArray();
                return new DynamicMethod("TestMethod", typeof(void), methodArgs, typeof(ConstructorTestDataTests).Module);
            }
        }
    }
}
