using CourierB;
using Xunit;

namespace CourierB.Tests
{
    public class MediatorFactoryTests
    {

        [Fact]
        public void SingeltonMediator()
        {
            var singelton = MediatorFactory.GetMediator();
            int singletonHashCode = singelton.GetHashCode();

            var secondTime = MediatorFactory.GetMediator();
            int secondTimehasCode = secondTime.GetHashCode();

            Assert.Equal(singletonHashCode,secondTimehasCode);
        }

        [Fact]
        public void InstanceMediator()
        {

            var singelton = MediatorFactory.GetMediator();
            int singletonHashCode = singelton.GetHashCode();

            var secondTime = MediatorFactory.GetMediator();
            int secondTimehasCode = secondTime.GetHashCode();

            Assert.NotEqual(singletonHashCode, secondTimehasCode);
        }

    }
}
