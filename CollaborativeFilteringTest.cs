// Author: Ivan Grgurina

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using CollaborativeFiltering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CollaborativeFilteringTest
{
    [TestClass]
    public class FormatUnitTest
    {
        [TestMethod]
        public void Should_Round_Up_Decimals()
        {
            double toBeTruncated = 3.5789055;

            var CF = new CF();
            string actualTruncatedFormat = CF.ToString(toBeTruncated);
            string expected = "3.579";

            Assert.AreEqual(expected, actualTruncatedFormat);
        }

        [TestMethod]
        public void Should_Correctly_Load_Input()
        {
            var input =
                "5 5\r\n1 2 X 2 4\r\n2 X 3 X 5\r\n3 1 X 4 X\r\nX 2 4 X 4\r\n1 X 3 4 X\r\n3\r\n1 3 0 1\r\n4 1 0 2\r\n5 5 1 3\r\n";
            var sr = new StringReader(input);
            Console.SetIn(sr);
            
            var cf = new CF();
            cf.ProcessInput(Console.ReadLine);

            Assert.AreEqual(5, cf.NumberOfItems);
            Assert.AreEqual(5, cf.NumberOfUsers);
            Assert.AreEqual(3, cf.Queries.Count);
            Assert.AreEqual(5, cf.ItemUserMatrix.Count);
            Assert.AreEqual(5, cf.UserItemMatrix.Count);
        }

        [TestMethod]
        public void Should_Give_Correct_Result()
        {
            var input =
                "5 5\r\n1 2 X 2 4\r\n2 X 3 X 5\r\n3 1 X 4 X\r\nX 2 4 X 4\r\n1 X 3 4 X\r\n3\r\n1 3 0 1\r\n4 1 0 2\r\n5 5 1 3\r\n";
            var sr = new StringReader(input);
            Console.SetIn(sr);

            var cf = new CF();
            cf.ProcessInput(Console.ReadLine);

            string[] expected = new[]
            {
                "3.000",
                "2.198",
                "2.560"
            };
            var i = 0;
            foreach (var q in cf.ExecuteAll<Query>())
            {
                Assert.AreEqual(expected[i], cf.ToString(q));
                i++;
            }


        }
    }
}
