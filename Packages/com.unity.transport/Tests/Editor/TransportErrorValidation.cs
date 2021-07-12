using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using Unity.Networking.Transport.Protocols;
using Unity.Networking.Transport.Utilities;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;

namespace Unity.Networking.Transport.ErrorValidation
{
    public class TransportErrorValidation
    {
        private const string backend = "baselib";

        // -- NetworkDriver ----------------------------------------------------

        // - NullReferenceException : If the NetworkInterface is invalid for some reason
        [Test]
        public void Given_InvalidNetworkInterface_SystemThrows_NullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => { var driver = new NetworkDriver(default(INetworkInterface)); });
        }

        // - ArgumentException : If the NetworkParameters are outside their given range.
        [Test]
        public void Given_ParametersOutsideSpecifiedRange_Throws_ArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var param = new NetworkDataStreamParameter() { size = -1 };
                var driver = new NetworkDriver(new BaselibNetworkInterface(), param);
            });
        }

        // -- NetworkPipeline --------------------------------------------------

        // - ArgumentException : If the NetworkParameters are outside their given range.
        [Test]
        public void Given_PiplineParametersOutsideSpecifiedRange_Throws_ArgumentException()
        {
            var param = new NetworkPipelineParams() { initialCapacity = -1 };
            Assert.Throws<ArgumentException>(() =>
            {
                var driver = new NetworkDriver(new BaselibNetworkInterface(), param);
            });
        }

        // -- BaselibNetworkInterface ------------------------------------------

        [Test]
        public void Given_BaselibReceiveParametersOutsideSpecifiedRange_LogsWarning()
        {
            var param = new BaselibNetworkParameter() {receiveQueueCapacity = -1,sendQueueCapacity = 1};
            using (var driver = new NetworkDriver(new BaselibNetworkInterface(), param))
            {
                LogAssert.Expect(LogType.Warning, "Value for receiveQueueCapacity must be larger then zero.");
            }
        }
        [Test]
        public void Given_BaselibSendParametersOutsideSpecifiedRange_LogsWarning()
        {
            var param = new BaselibNetworkParameter() { sendQueueCapacity = -1 , receiveQueueCapacity = 1};
            using (var driver = new NetworkDriver(new BaselibNetworkInterface(), param))
            {
                LogAssert.Expect(LogType.Warning, "Value for sendQueueCapacity must be larger then zero.");
            }
        }
    }
}