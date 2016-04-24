using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

public class PacketQueue{
	struct PacketInfo{
		public int size;
		public int offset;
	};

	private MemoryStream m_streamBuffer;

	private List<PacketInfo> m_offsetList;

	private int m_offset;

	public PacketQueue(){
		m_streamBuffer = new MemoryStream ();
		m_offsetList = new List<PacketInfo> ();
		m_offset = 0;
	}

	public int Enqueue(byte[] data, int size){
		PacketInfo info = new PacketInfo ();

		info.offset = m_offset;
		info.size = size;

		m_offsetList.Add (info);

		m_streamBuffer.Position = m_offset;
		m_streamBuffer.Write (data, 0, size);
		m_streamBuffer.Flush ();
		m_offset += size;

		return size;
	}

	public int Dequeue(ref byte[] buffer, int size){
		if (m_offsetList.Count <= 0) {
			return -1;
		}

		PacketInfo info = m_offsetList [0];

		int datasize = Mathf.Min (size, info.size);
		m_streamBuffer.Position = info.offset;
		int recvsize = m_streamBuffer.Read (buffer, 0, datasize);

		if (recvsize > 0) {
			m_offsetList.RemoveAt (0);
		}

		if (m_offsetList.Count == 0) {
			Clear ();
			m_offset = 0;
		}

		return recvsize;
	}

	public void Clear(){
		byte[] buffer = m_streamBuffer.GetBuffer ();
		Array.Clear (buffer, 0, buffer.Length);

		m_streamBuffer.Position = 0;
		m_streamBuffer.SetLength (0);
	}
}
