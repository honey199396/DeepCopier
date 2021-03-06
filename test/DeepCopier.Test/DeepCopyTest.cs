using System.Linq;
using Xunit;
using DeepCopier.Test.TestClasses;
using System.Collections.Generic;

namespace DeepCopier.Test
{
    public class DeepCopierTest
    {
        /// <summary>
        /// 测试遍历对象
        /// </summary>
        [Fact]
        public void TestEnumable()
        {
            int[] a = new[] { 1, 2, 3 };

            int[] b = Copier.Copy(a);

            Assert.Equal(a, b);

            List<string> c = new List<string> { "1", "2", "3" };

            List<string> d = Copier.Copy(c);

            Assert.Equal(c, d);

            int[][] e = new[] { new[] { 1, 2, 3 }, new[] { 1, 2, 3 }, new[] { 1, 2, 3 } };
            int[][] f = Copier.Copy(e);
            Assert.NotSame(e, f);
            Assert.Equal(e.SelectMany(nums => nums), e.SelectMany(nums => nums));
        }

        /// <summary>
        /// 测试拷贝一个对象本身
        /// </summary>
        [Fact]
        public void TestSelfCopy()
        {
            ClassA a = new ClassA
            {
                ValueTypeProp = 123,
                StringProp = "test"
            };

            ClassA a2 = Copier.Copy(a);

            Assert.Equal(a.ValueTypeProp, a2.ValueTypeProp);
            Assert.Equal(a.StringProp, a2.StringProp);
        }

        /// <summary>
        /// 测试拷贝简单的属性
        /// </summary>
        [Fact]
        public void TestSimpleProperties()
        {
            ClassA a = new ClassA
            {
                ValueTypeProp = 123,
                StringProp = "test"
            };

            ClassB b = Copier.Copy<ClassA, ClassB>(a);

            Assert.Equal(a.ValueTypeProp, b.ValueTypeProp);
            Assert.Equal(a.StringProp, b.StringProp);
        }

        /// <summary>
        /// 测试拷贝引用类型的属性
        /// </summary>
        [Fact]
        public void TestRefTypeProperties()
        {
            ClassB b = new ClassB
            {
                ValueTypeProp = 1,
                StringProp = "string1",
                ClassATypeProp = new ClassA
                {
                    ValueTypeProp = 2,
                    StringProp = "string2",
                }
            };

            ClassC c = Copier.Copy<ClassB, ClassC>(b);

            Assert.Equal(b.ValueTypeProp, c.ValueTypeProp);
            Assert.Equal(b.StringProp, c.StringProp);
            Assert.Equal(b.ClassATypeProp.ValueTypeProp, c.ClassATypeProp.ValueTypeProp);
            Assert.Equal(b.ClassATypeProp.StringProp, c.ClassATypeProp.StringProp);
            Assert.NotSame(b.ClassATypeProp, c.ClassATypeProp);
        }

        /// <summary>
        /// 测试可遍历的属性
        /// </summary>
        [Fact]
        public void TestEnumableProperties()
        {
            // 测试数组的拷贝
            ClassD d = new ClassD
            {
                VuleTypeArray = new [] { 1, 2, 3 },
                ClassATypeArray = new []
                {
                    new ClassA
                    {
                        ValueTypeProp = 1,
                        StringProp = "string1"
                    },
                    new ClassA
                    {
                        ValueTypeProp = 2,
                        StringProp = "string2"
                    }
                }
            };

            ClassE e = Copier.Copy<ClassD, ClassE>(d);
            Assert.Equal(d.VuleTypeArray, e.VuleTypeArray);
            Assert.NotSame(d.VuleTypeArray, e.VuleTypeArray);

            Assert.Equal(d.ClassATypeArray.Select(x => x.ValueTypeProp),
                e.ClassATypeArray.Select(x => x.ValueTypeProp));

            Assert.Equal(d.ClassATypeArray.Select(x => x.StringProp),
                e.ClassATypeArray.Select(x => x.StringProp));

            Assert.NotEqual(d.ClassATypeArray.AsEnumerable(), e.ClassATypeArray.AsEnumerable());

            Assert.NotSame(d.ClassATypeArray, e.ClassATypeArray);

            // 测试List的拷贝
            ClassD d2 = new ClassD
            {
                VuleTypeList = new List<int> { 1, 2, 3 },
                ClassATypeList = new List<ClassA>
                {
                    new ClassA
                    {
                        ValueTypeProp = 1,
                        StringProp = "string1"
                    },
                    new ClassA
                    {
                        ValueTypeProp = 2,
                        StringProp = "string2"
                    }
                }
            };

            ClassE e2 = Copier.Copy<ClassD, ClassE>(d2);
            Assert.Equal(d2.VuleTypeList, e2.VuleTypeList);
            Assert.NotSame(d2.VuleTypeList, e2.VuleTypeList);

            Assert.Equal(d2.ClassATypeList.Select(x => x.ValueTypeProp),
                e2.ClassATypeList.Select(x => x.ValueTypeProp));

            Assert.Equal(d2.ClassATypeList.Select(x => x.StringProp),
                e2.ClassATypeList.Select(x => x.StringProp));

            Assert.NotEqual(d2.ClassATypeList.AsEnumerable(), e2.ClassATypeList.AsEnumerable());

            Assert.NotSame(d2.ClassATypeList, e2.ClassATypeList);
        }

        /// <summary>
        /// 测试HashSet的拷贝
        /// </summary>
        [Fact]
        public void TestHashSet()
        {
            ClassF f = new ClassF
            {
                ClassACollection = new HashSet<ClassA>
                {
                    new ClassA
                    {
                        ValueTypeProp = 123,
                        StringProp = "test"
                    }
                }
            };

            var f2 = Copier.Copy(f);

            Assert.NotSame(f.ClassACollection, f2.ClassACollection);

            Assert.Equal(f.ClassACollection.Select(a => a.StringProp), f2.ClassACollection.Select(a => a.StringProp));

        }


        /// <summary>
        /// 测试向已存在的对象拷贝属性值
        /// </summary>
        [Fact]
        public void TestCopyToExistingObject()
        {
            ClassA a = new ClassA
            {
                ValueTypeProp = 123,
                StringProp = "test"
            };

            ClassA a2 = new ClassA();
            Copier.Copy(a, a2);

            Assert.Equal(a.ValueTypeProp, a2.ValueTypeProp);
            Assert.Equal(a.StringProp, a2.StringProp);

            ClassB b = new ClassB
            {
                ValueTypeProp = 1,
                StringProp = "string1",
                ClassATypeProp = new ClassA
                {
                    ValueTypeProp = 2,
                    StringProp = "string2",
                }
            };

            ClassC c = new ClassC();
            Copier.Copy(b, c);

            Assert.Equal(b.ValueTypeProp, c.ValueTypeProp);
            Assert.Equal(b.StringProp, c.StringProp);
            Assert.Equal(b.ClassATypeProp.ValueTypeProp, c.ClassATypeProp.ValueTypeProp);
            Assert.Equal(b.ClassATypeProp.StringProp, c.ClassATypeProp.StringProp);
            Assert.NotSame(b.ClassATypeProp, c.ClassATypeProp);
        }
    }
}
