﻿//
//  Presentation.cs
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
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NitroDebugger.RSP
{
	/// <summary>
	/// Represents the Presentation layer.
	/// </summary>
	public class Presentation
	{
		private const int MaxWriteAttemps = 10;
		private const byte PacketSeparator = 0x24;	// '$'

		private Session session;
		private CancellationTokenSource cts;
		private CommandPacket lastCommandSent;

		public Presentation(string hostname, int port)
		{
			this.session = new Session(hostname, port);
			this.ResetCancellationToken();
		}

		public void Close()
		{
			this.session.Close();
		}

		public void SendCommand(CommandPacket command)
		{
			this.lastCommandSent = command;
			this.SendData(PacketBinConverter.ToBinary(command));
		}

		private void SendData(byte[] data)
		{
			int count = 0;
			int response;

			do {
				if (count == MaxWriteAttemps)
					throw new ProtocolViolationException("[PRES] Can not send correctly");
				count++;

				this.session.Write(data);
				response = this.session.ReadByte();
			} while (response != RawPacket.Ack);
		}

		public ReplyPacket SendInterrupt()
		{
			this.SendData(new byte[] { RawPacket.Interrupt });
			return this.ReceiveReply();
		}

		public void SendNack()
		{
			this.session.Write(RawPacket.Nack);
		}

		public ReplyPacket ReceiveReply()
		{
			ReplyPacket response = null;
			int count = 0;

			do {
				if (count == MaxWriteAttemps)
					throw new ProtocolViolationException("[PRES] Can not receive correctly");
				count++;

				response = this.NextReply();
			} while (response == null);

			return response;
		}

		private ReplyPacket NextReply()
		{
			ReplyPacket response = null;
			try {
				// Get data
				byte[] packet = this.session.ReadPacket(PacketSeparator);
				response = PacketBinConverter.FromBinary(packet, lastCommandSent);

				// Send ACK
				this.session.Write(RawPacket.Ack);
			} catch (FormatException) {
				// Error... send NACK
				this.session.Write(RawPacket.Nack);
			}

			return response;
		}

		public void CancelRead(Task taskToCancell)
		{
			try {
				this.cts.Cancel();
				taskToCancell.Wait(this.cts.Token);
			} catch (OperationCanceledException) {
			} catch (AggregateException) {
			}

			this.ResetCancellationToken();
		}

		private void ResetCancellationToken()
		{
			this.cts = new CancellationTokenSource();
			this.session.Cancellation = this.cts.Token;
		}
	}
}

