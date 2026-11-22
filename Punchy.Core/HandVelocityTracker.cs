using System;

namespace Punchy.Core
{
    public readonly struct HandMotionSample
    {
        public bool HasPrevious { get; }
        public Vec3 PreviousPosition { get; }
        public Vec3 CurrentPosition { get; }
        public Vec3 StepDelta { get; }
        public float StepDistance { get; }
        public Vec3 Velocity { get; }
        public float Speed { get; }
        public Vec3 Direction { get; }
        public bool WasTeleportReset { get; }

        public HandMotionSample(
            bool hasPrevious,
            Vec3 previousPosition,
            Vec3 currentPosition,
            Vec3 stepDelta,
            float stepDistance,
            Vec3 velocity,
            float speed,
            Vec3 direction,
            bool wasTeleportReset)
        {
            HasPrevious = hasPrevious;
            PreviousPosition = previousPosition;
            CurrentPosition = currentPosition;
            StepDelta = stepDelta;
            StepDistance = stepDistance;
            Velocity = velocity;
            Speed = speed;
            Direction = direction;
            WasTeleportReset = wasTeleportReset;
        }
    }

    /// <summary>
    /// Zero-allocation sliding-window hand velocity tracker immune to 20Hz NetworkTransform packet clumping and teleport spikes.
    /// </summary>
    public sealed class HandVelocityTracker
    {
        private struct TimedPosition
        {
            public double Time;
            public Vec3 Position;
        }

        private const int BufferCapacity = 16;
        private readonly TimedPosition[] _buffer = new TimedPosition[BufferCapacity];
        private readonly double _windowSeconds;

        private int _head;
        private int _count;

        public HandVelocityTracker(double windowSeconds = 0.06d)
        {
            _windowSeconds = Math.Max(0.01d, windowSeconds);
        }

        public void Reset()
        {
            _head = 0;
            _count = 0;
        }

        public HandMotionSample Sample(
            double currentTime,
            Vec3 currentPosition,
            float maxTeleportStepMeters = 1.5f,
            float maxValidSpeed = 25.0f)
        {
            if (double.IsNaN(currentTime) || double.IsInfinity(currentTime) || IsInvalidVec(currentPosition))
            {
                return new HandMotionSample(false, currentPosition, currentPosition, Vec3.Zero, 0f, Vec3.Zero, 0f, Vec3.Zero, false);
            }

            if (_count == 0)
            {
                Push(currentTime, currentPosition);
                return new HandMotionSample(false, currentPosition, currentPosition, Vec3.Zero, 0f, Vec3.Zero, 0f, Vec3.Zero, false);
            }

            TimedPosition latest = GetAt(_count - 1);
            if (currentTime <= latest.Time)
            {
                return new HandMotionSample(true, latest.Position, latest.Position, Vec3.Zero, 0f, Vec3.Zero, 0f, Vec3.Zero, false);
            }

            Vec3 stepDelta = currentPosition - latest.Position;
            float stepDistance = stepDelta.Magnitude;

            if (stepDistance > maxTeleportStepMeters)
            {
                Reset();
                Push(currentTime, currentPosition);
                return new HandMotionSample(false, currentPosition, currentPosition, Vec3.Zero, 0f, Vec3.Zero, 0f, Vec3.Zero, true);
            }

            Push(currentTime, currentPosition);

            // Keep at least two samples and retain the oldest sample at/just prior to the window start
            // so 20Hz replicated poses sampled at 60Hz FixedUpdate still reflect true packet velocity.
            double windowCutoff = currentTime - _windowSeconds;
            while (_count > 2)
            {
                TimedPosition second = GetAt(1);
                if (second.Time <= windowCutoff)
                    PopOldest();
                else
                    break;
            }

            TimedPosition oldest = GetAt(0);
            double elapsed = currentTime - oldest.Time;
            if (elapsed <= 1e-5d)
            {
                return new HandMotionSample(true, latest.Position, currentPosition, stepDelta, stepDistance, Vec3.Zero, 0f, Vec3.Zero, false);
            }

            Vec3 windowDisplacement = currentPosition - oldest.Position;
            Vec3 velocity = windowDisplacement / (float)elapsed;
            float speed = velocity.Magnitude;

            if (speed > maxValidSpeed)
            {
                Reset();
                Push(currentTime, currentPosition);
                return new HandMotionSample(false, currentPosition, currentPosition, Vec3.Zero, 0f, Vec3.Zero, 0f, Vec3.Zero, true);
            }

            Vec3 direction = speed > 1e-4f
                ? velocity.Normalized
                : (stepDistance > 1e-4f ? stepDelta.Normalized : Vec3.Zero);

            return new HandMotionSample(
                hasPrevious: true,
                previousPosition: latest.Position,
                currentPosition: currentPosition,
                stepDelta: stepDelta,
                stepDistance: stepDistance,
                velocity: velocity,
                speed: speed,
                direction: direction,
                wasTeleportReset: false);
        }

        private void Push(double time, Vec3 position)
        {
            if (_count == BufferCapacity)
                PopOldest();

            int tail = (_head + _count) % BufferCapacity;
            _buffer[tail] = new TimedPosition { Time = time, Position = position };
            _count++;
        }

        private void PopOldest()
        {
            if (_count == 0)
                return;
            _head = (_head + 1) % BufferCapacity;
            _count--;
        }

        private TimedPosition GetAt(int logicalIndex)
        {
            int idx = (_head + logicalIndex) % BufferCapacity;
            return _buffer[idx];
        }

        private static bool IsInvalidVec(Vec3 v) =>
            float.IsNaN(v.X) || float.IsNaN(v.Y) || float.IsNaN(v.Z) ||
            float.IsInfinity(v.X) || float.IsInfinity(v.Y) || float.IsInfinity(v.Z);
    }
}
