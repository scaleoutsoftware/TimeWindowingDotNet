using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scaleout.Client.Streaming;
using Xunit;

namespace Scaleout.Client.Streaming.Tests
{
    
    public class UtilityTests
    {
        [Fact]
        public void RemoveFirst0()
        {
            List<int> foo = new List<int>();
            foo.RemoveFirstItems(0);
            Assert.Equal(0, foo.Count);
        }
        
        [Fact]
        public void RemoveFirst1()
        {
            List<int> foo = new List<int> { 42 };
            Assert.Equal(1, foo.Count);
            foo.RemoveFirstItems(1);
            Assert.Equal(0, foo.Count);
        }

        [Fact]
        public void RemoveFirst2()
        {
            List<int> foo = new List<int> { 42, 43 };
            Assert.Equal(2, foo.Count);

            foo.RemoveFirstItems(1);
            Assert.Equal(1, foo.Count);
            Assert.Equal(43, foo[0]);
        }

        [Fact]
        public void RemoveFirst3()
        {
            LinkedList<int> foo = new LinkedList<int>();
            foo.RemoveFirstItems(0);
            Assert.Equal(0, foo.Count);
        }

        [Fact]
        public void RemoveFirst4()
        {
            LinkedList<int> foo = new LinkedList<int>();
            foo.AddFirst(42);
            Assert.Equal(1, foo.Count);
            foo.RemoveFirstItems(1);
            Assert.Equal(0, foo.Count);
        }

        [Fact]
        public void RemoveFirst5()
        {
            LinkedList<int> foo = new LinkedList<int>();
            foo.AddFirst(42);
            foo.AddLast(43);
            Assert.Equal(2, foo.Count);

            foo.RemoveFirstItems(1);
            Assert.Equal(1, foo.Count);
            Assert.Equal(43, foo.First.Value);
        }
    }
}
