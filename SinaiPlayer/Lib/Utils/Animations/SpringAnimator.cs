using System;

namespace SinaiPlayer.Lib.Utils.Animations
{
    public sealed class SpringAnimator
    {
        private const double Mass = 1.0;
        private const double ValueEpsilon = 0.05;
        private const double VelocityEpsilon = 0.05;

        private readonly double _stiffness;
        private readonly double _damping;
        private double _velocity;

        public double Value { get; private set; }
        public double Target { get; private set; }

        public bool IsSettled =>
            Math.Abs(Target - Value) < ValueEpsilon && Math.Abs(_velocity) < VelocityEpsilon;

        public SpringAnimator(double initialValue, double response = 0.42, double dampingFraction = 0.86)
        {
            Value = initialValue;
            Target = initialValue;

            double angularFrequency = 2 * Math.PI / response;
            _stiffness = angularFrequency * angularFrequency * Mass;
            _damping = 4 * Math.PI * dampingFraction * Mass / response;
        }

        public void SetTarget(double target) => Target = target;

        /// <summary>
        /// Advances the simulation by <paramref name="deltaSeconds"/> (semi-implicit Euler) and
        /// returns the new value. Snaps exactly to Target once the spring has settled so callers
        /// don't chase a fractional value forever.
        /// </summary>
        public double Step(double deltaSeconds)
        {
            // Clamp so a stalled UI thread (debugger break, GC pause) doesn't produce a huge,
            // visibly-wrong jump on the next frame.
            deltaSeconds = Math.Min(deltaSeconds, 1.0 / 30.0);

            double displacement = Value - Target;
            double springForce = -_stiffness * displacement;
            double dampingForce = -_damping * _velocity;
            double acceleration = (springForce + dampingForce) / Mass;

            _velocity += acceleration * deltaSeconds;
            Value += _velocity * deltaSeconds;

            if (IsSettled)
            {
                Value = Target;
                _velocity = 0;
            }

            return Value;
        }

        public void SnapTo(double value)
        {
            Value = value;
            Target = value;
            _velocity = 0;
        }
    }
}
