﻿//
//  GdbClientTests.cs
//
//  Author:
//       Benito Palacios Sánchez <benito356@gmail.com>
//
//  Copyright (c) 2014 Benito Palacios Sánchez
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using NUnit.Framework;
using NitroDebugger.RSP;
using System.Net.Sockets;
using System.Net;

namespace UnitTests
{
	[TestFixture]
	public class GdbClientTests
	{
		private const string Host = "localhost";
		private const int Port = 10104;
		private GdbClient client;

		private TcpListener server;
		private TcpClient serverClient;
		private NetworkStream serverStream;

		[TestFixtureSetUp]
		public void Setup()
		{
			this.server = new TcpListener(IPAddress.Loopback, Port);
			this.server.Start();

			this.client = new GdbClient(Host, Port);
			this.client.Connect();

			this.serverClient = this.server.AcceptTcpClient();
			this.serverStream = this.serverClient.GetStream();
		}

		[TestFixtureTearDown]
		public void Dispose()
		{
			this.client.Disconnect();
			this.serverClient.Close();
			this.server.Stop();
		}

		[TearDown]
		public void ResetServer()
		{
			this.client.Disconnect();
			this.serverClient.Close();

			while (this.server.Pending())
				this.server.AcceptTcpClient().Close();

			this.client.Connect();
			this.serverClient = this.server.AcceptTcpClient();
			this.serverStream = this.serverClient.GetStream();
		}

		[Test]
		public void StoreHostAndPort()
		{
			Assert.AreEqual(Host, this.client.Host);
			Assert.AreEqual(Port, this.client.Port);
		}

		[Test]
		public void Connect()
		{
			Assert.IsTrue(this.client.IsConnected);
		}

		[Test]
		public void NotConnectedOnBadPort()
		{
			GdbClient clientError = new GdbClient("localhost", 10102);
			clientError.Connect();
			Assert.IsFalse(clientError.IsConnected);
		}

		[Test]
		public void Disconnect()
		{
			this.client.Disconnect();
			Assert.IsFalse(this.client.IsConnected);
		}

		[Test]
		public void CannotConnectIfConnected()
		{
			Assert.DoesNotThrow(() => this.client.Connect());
		}

		[Test]
		public void CannnotDisconnectIfNotConnected()
		{
			this.client.Disconnect();
			Assert.DoesNotThrow(() => this.client.Disconnect());
		}
	}
}
