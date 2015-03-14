﻿/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2014 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
#if KeePass2PCL
using PCLCrypto;
#else
using System.Security.Cryptography;
#endif
using System.Diagnostics;

using KeePass2PCL.Utility;

namespace KeePass2PCL.Cryptography
{
	public sealed class HashingStreamEx : Stream
	{
		private Stream m_sBaseStream;
		private bool m_bWriting;
#if KeePass2PCL
		private ICryptoTransform m_hash;
#else
		private HashAlgorithm m_hash;
#endif

		private byte[] m_pbFinalHash = null;

		public byte[] Hash
		{
			get { return m_pbFinalHash; }
		}

		public override bool CanRead
		{
			get { return !m_bWriting; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return m_bWriting; }
		}

		public override long Length
		{
			get { return m_sBaseStream.Length; }
		}

		public override long Position
		{
			get { return m_sBaseStream.Position; }
			set { throw new NotSupportedException(); }
		}

#if KeePass2PCL
		public HashingStreamEx(Stream sBaseStream, bool bWriting, HashAlgorithm? hashAlgorithm)
#else
		public HashingStreamEx(Stream sBaseStream, bool bWriting, HashAlgorithm hashAlgorithm)
#endif
		{
			if(sBaseStream == null) throw new ArgumentNullException("sBaseStream");

			m_sBaseStream = sBaseStream;
			m_bWriting = bWriting;
#if KeePass2PCL
			m_hash = WinRTCrypto.HashAlgorithmProvider.OpenAlgorithm(hashAlgorithm ?? HashAlgorithm.Sha256).CreateHash();
#elif !KeePassLibSD
			m_hash = (hashAlgorithm ?? new SHA256Managed());
#else // KeePassLibSD
			m_hash = null;

			try { m_hash = HashAlgorithm.Create("SHA256"); }
			catch(Exception) { }
			try { if(m_hash == null) m_hash = HashAlgorithm.Create(); }
			catch(Exception) { }
#endif
			if(m_hash == null) { Debug.Assert(false); return; }

			// Validate hash algorithm
			if((!m_hash.CanReuseTransform) || (!m_hash.CanTransformMultipleBlocks) ||
				(m_hash.InputBlockSize != 1) || (m_hash.OutputBlockSize != 1))
			{
#if false && DEBUG
				MessageService.ShowWarning("Broken HashAlgorithm object in HashingStreamEx.");
#endif
				m_hash = null;
			}
		}

		public override void Flush()
		{
			m_sBaseStream.Flush();
		}

#if KeePass2PCL || KeePassRT
		protected override void Dispose(bool disposing)
		{
			if(!disposing) return;
#else
		public override void Close()
		{
#endif
			if(m_hash != null)
			{
				try
				{
#if KeePass2PCL
					m_pbFinalHash = m_hash.TransformFinalBlock(new byte[0], 0, 0);
#else
					m_hash.TransformFinalBlock(new byte[0], 0, 0);

					m_pbFinalHash = m_hash.Hash;
#endif
				}
				catch(Exception) { Debug.Assert(false); }

				m_hash = null;
			}

			m_sBaseStream.Dispose();
		}

		public override long Seek(long lOffset, SeekOrigin soOrigin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long lValue)
		{
			throw new NotSupportedException();
		}

		public override int Read(byte[] pbBuffer, int nOffset, int nCount)
		{
			if(m_bWriting) throw new InvalidOperationException();

			int nRead = m_sBaseStream.Read(pbBuffer, nOffset, nCount);
			int nPartialRead = nRead;
			while((nRead < nCount) && (nPartialRead != 0))
			{
				nPartialRead = m_sBaseStream.Read(pbBuffer, nOffset + nRead,
					nCount - nRead);
				nRead += nPartialRead;
			}

#if DEBUG
			byte[] pbOrg = new byte[pbBuffer.Length];
			Array.Copy(pbBuffer, pbOrg, pbBuffer.Length);
#endif

			if((m_hash != null) && (nRead > 0))
				m_hash.TransformBlock(pbBuffer, nOffset, nRead, pbBuffer, nOffset);

#if DEBUG
			Debug.Assert(MemUtil.ArraysEqual(pbBuffer, pbOrg));
#endif

			return nRead;
		}

		public override void Write(byte[] pbBuffer, int nOffset, int nCount)
		{
			if(!m_bWriting) throw new InvalidOperationException();

#if DEBUG
			byte[] pbOrg = new byte[pbBuffer.Length];
			Array.Copy(pbBuffer, pbOrg, pbBuffer.Length);
#endif

			if((m_hash != null) && (nCount > 0))
				m_hash.TransformBlock(pbBuffer, nOffset, nCount, pbBuffer, nOffset);

#if DEBUG
			Debug.Assert(MemUtil.ArraysEqual(pbBuffer, pbOrg));
#endif

			m_sBaseStream.Write(pbBuffer, nOffset, nCount);
		}
	}
}
