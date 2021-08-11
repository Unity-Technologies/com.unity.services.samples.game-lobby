using System;
using System.Collections;
using UnityEngine;

namespace Unity.Services.Relay.Helpers
{
    internal static class AsyncOpRetry
    {
        public static AsyncOpRetry<T> FromCreateAsync<T>(Func<int, T> op)
        {
            return AsyncOpRetry<T>.FromCreateAsync(op);
        }
    }

    internal class AsyncOpRetry<T>
    {
        private uint MaxRetries { get; set; } = 4;
        private float JitterMagnitude { get; set; } = 1.0f;
        private float DelayScale { get; set; } = 1.0f;
        private float MaxDelayTime { get; set; } = 8.0f;
        private Func<int, T> CreateOperation { get; set; }
        private Func<T, bool> RetryCondition { get; set; }
        private Action<T> OnComplete { get; set; }

        private AsyncOpRetry(Func<int, T> createAsyncOp)
        {
            CreateOperation = createAsyncOp;
        }

        private static float AddJitter(float number, float magnitude)
        {
            return number + (UnityEngine.Random.value * magnitude);
        }

        private static float Pow2(float exponent, float scale)
        {
            return (float) (Math.Pow(2.0f, exponent) * scale);
        }

        private static float CalculateDelay(int attemptNumber, float maxDelayTime, float delayScale,
            float jitterMagnitude)
        {
            float delayTime = Pow2(attemptNumber, delayScale);
            delayTime = AddJitter(delayTime, jitterMagnitude);
            delayTime = Math.Min(delayTime, maxDelayTime);
            return delayTime;
        }

        public AsyncOpRetry<T> WithJitterMagnitude(float magnitude)
        {
            JitterMagnitude = Mathf.Clamp(magnitude, 0.001f, 1.0f);
            return this;
        }

        public AsyncOpRetry<T> WithDelayScale(float scale)
        {
            DelayScale = Mathf.Clamp(scale, 0.05f, 1.0f);
            return this;
        }

        public AsyncOpRetry<T> WithMaxDelayTime(float time)
        {
            MaxDelayTime = Mathf.Clamp(time, 0.1f, 60.0f);
            return this;
        }

        public static AsyncOpRetry<T> FromCreateAsync(Func<int, T> op)
        {
            return new AsyncOpRetry<T>(op);
        }

        public AsyncOpRetry<T> WithRetryCondition(Func<T, bool> shouldRetry)
        {
            RetryCondition = shouldRetry;
            return this;
        }

        public AsyncOpRetry<T> WhenComplete(Action<T> onComplete)
        {
            OnComplete = onComplete;
            return this;
        }

        public AsyncOpRetry<T> UptoMaximumRetries(uint amount)
        {
            MaxRetries = amount;
            return this;
        }

        public IEnumerator Run()
        {
            T asyncOp = default;
            for (var attempt = 0; attempt <= MaxRetries; ++attempt)
            {
                asyncOp = CreateOperation(attempt + 1);
                yield return asyncOp;

                if ((!RetryCondition?.Invoke(asyncOp) ?? false))
                {
                    break;
                }

                var delayTime = CalculateDelay(attempt, MaxDelayTime, DelayScale, JitterMagnitude);
                yield return new WaitForSecondsRealtime(delayTime);
            }
            OnComplete?.Invoke(asyncOp);
        }
    }
}