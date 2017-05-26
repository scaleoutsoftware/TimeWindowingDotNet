using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Soss.Client.Streaming;
using Xunit;

namespace Soss.Client.Streaming.Tests
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
    }
}
