using NUnit.Framework;
using Rootbound.Player;

namespace Rootbound.Tests
{
    public sealed class MovementMathTests
    {
        [Test]
        public void JumpBufferAllowsCoyoteJumpButNotAfterLeavingTheWindow()
        {
            Assert.IsTrue(MovementMath.CanGroundJump(false, 0.08f, 0.1f));
            Assert.IsFalse(MovementMath.CanGroundJump(false, 0f, 0.1f));
            Assert.IsFalse(MovementMath.CanGroundJump(true, 0.1f, 0f));
        }

        [Test]
        public void AirJumpRequiresRemainingJumpAndExpiredCoyoteTime()
        {
            Assert.IsTrue(MovementMath.CanAirJump(false, 0f, 1));
            Assert.IsFalse(MovementMath.CanAirJump(false, 0f, 0));
            Assert.IsFalse(MovementMath.CanAirJump(false, 0.08f, 1));
            Assert.IsFalse(MovementMath.CanAirJump(true, 0f, 1));
            Assert.AreEqual(0f, MovementMath.AdvanceTimer(0.1f, 0.15f));
        }
    }
}
