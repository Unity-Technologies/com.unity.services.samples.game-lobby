using System;
using System.Collections;
using LobbyRelaySample;
using NUnit.Framework;
using Test.Tools;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace Test
{
	/// <summary>
	/// Accesses the Authentication and Relay services in order to ensure we can connect to Relay and retrieve a join code.
	/// RelayUtp* wraps the Relay API, so go through that in practice. This simply ensures the connection to the Lobby service is functional.
	///
	/// If the tests pass, you can assume you are connecting to the Relay service itself properly.
	/// </summary>
	public class RelayRoundTripTests
	{

		[OneTimeSetUp]
		public void Setup()
		{
			Auth.Authenticate("testProfile");
		}


		/// <summary>
		/// Create a Relay allocation, request a join code, and then join. Note that this is purely to ensure the service is functioning;
		/// in practice, the RelayUtpSetup does more work to bind to the allocation and has slightly different logic for hosts vs. clients.
		/// </summary>
		[UnityTest]
		public IEnumerator DoBaseRoundTrip()
		{
			yield return new WaitUntil(Auth.DoneAuthenticating);

			// Allocation
			Allocation allocation = null;
			yield return AsyncTestHelper.Await(async () => allocation = await Relay.Instance.CreateAllocationAsync(4));


			Guid allocationId = allocation.AllocationId;
			var allocationIP = allocation.RelayServer.IpV4;
			var allocationPort = allocation.RelayServer.Port;
			Assert.NotNull(allocationId);
			Assert.NotNull(allocationIP);
			Assert.NotNull(allocationPort);

			// Join code retrieval
			string joinCode = null;
			yield return AsyncTestHelper.Await(async () =>
				joinCode = await Relay.Instance.GetJoinCodeAsync(allocationId));


			Assert.False(string.IsNullOrEmpty(joinCode));

			// Joining with the join code
			JoinAllocation joinResponse = null;
			yield return AsyncTestHelper.Await(async () =>
				joinResponse = await Relay.Instance.JoinAllocationAsync(joinCode));


			var codeIp = joinResponse.RelayServer.IpV4;
			var codePort = joinResponse.RelayServer.Port;
			Assert.AreEqual(codeIp, allocationIP);
			Assert.AreEqual(codePort, allocationPort);
		}
	}
}
