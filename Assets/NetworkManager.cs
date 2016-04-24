using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class NetworkManager : MonoBehaviour {
	public const int 	m_port = 50700;
	private Socket m_listener = null;
	private Socket m_socket = null;
	private PacketQueue	m_sendQueue;
	private PacketQueue m_recvQueue;
	private bool m_isServer = false;
	private bool m_isConnected = false;
	private bool m_isThreadRun = false;
	private Thread m_thread;
	private EventHandler m_handler;
	private static int buffer_size = 1400;

	//delegate
	public delegate void	EventHandler(NetEventState a);

	void Awake(){
		DontDestroyOnLoad (this.gameObject);

		m_sendQueue = new PacketQueue ();
		m_recvQueue = new PacketQueue ();
	}

	public bool StartServer(int port, int connectNum){
		try{
			m_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
				ProtocolType.Tcp);
			m_listener.Bind(new IPEndPoint(IPAddress.Any, port));
			m_listener.Listen(connectNum);
		}catch{
			Debug.LogError ("start server fail");
			return false;
		}

		m_isServer = true;

		return LaunchThread ();
	}

	public bool Connect(string address, int port){
		if (m_listener != null) {
			return false;

		}

		bool ret = false;
		try{
			m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			m_socket.NoDelay = true;
			m_socket.Connect (address, port);
			ret = LaunchThread();
		}
		catch{
			m_socket = null;
		}

		if (ret) {
			m_isConnected = true;
			Debug.Log ("success connect");
		} else {
			m_isConnected = false;
		}

		if (m_handler != null) {
			NetEventState state = new NetEventState ();
			state.type = NetEventType.Connect;
			state.result = m_isConnected == true ? NetEventResult.Success : NetEventResult.Fail;
			m_handler (state);
		}
	
		return m_isConnected;
	}

	public bool IsServer(){
		return m_isServer;
	}

	public bool IsConnected(){
		return m_isConnected;
	}

	public int Send(byte[] data, int size){
		if (m_sendQueue == null)
			return 0;

		return m_sendQueue.Enqueue (data, size);
	}

	public int Recv(ref byte[] buffer, int size){
		if (m_recvQueue == null) {
			return 0;
		}

		return m_recvQueue.Dequeue (ref buffer, size);
	}

	public void RegisterEveneHandler(EventHandler handler){
		m_handler += handler;
	}

	public void UnregisterEventHandler(EventHandler handler){
		m_handler -= handler;
	}

	bool LaunchThread(){
		try{
			m_isThreadRun = true;
			m_thread = new Thread(new ThreadStart(ThreadLoop));
			m_thread.Start();
		}
		catch{
			Debug.LogError ("launch thread fail");
			return false;
		}

		return true;
	}

	public void Disconnect(){
		m_isConnected = false;

		if (m_socket != null) {
			m_socket.Shutdown (SocketShutdown.Both);
			m_socket.Close ();
			m_socket = null;

			if (m_handler != null) {
				NetEventState state = new NetEventState ();
				state.type = NetEventType.Disconnect;
				state.result = NetEventResult.Success;
				m_handler (state);
			}
		}
	}

	void ThreadLoop(){
		Debug.Log ("thread start");

		while (m_isThreadRun) {
			AcceptClient ();

			if (m_socket != null && m_isConnected == true) {
				DispatchSend ();

				DispatchResv ();
			}

			Thread.Sleep (5);
		}

		Debug.Log ("Dispatch thread ended");
	}

	void DispatchSend(){
		try{
			if(m_socket.Poll(0, SelectMode.SelectWrite)){
				byte[] buffer = new byte[buffer_size];

				int sendsize = m_sendQueue.Dequeue(ref buffer, buffer.Length);

				while(sendsize > 0){
					m_socket.Send(buffer, sendsize, SocketFlags.None);
					sendsize = m_sendQueue.Dequeue(ref buffer, buffer.Length);
				}
			}
		}
		catch{
			return;
		}
	}

	void DispatchResv(){
		try{
			while(m_socket.Poll(0, SelectMode.SelectRead)){
				byte[] buffer = new byte[buffer_size];

				int recvSize = m_socket.Receive(buffer, buffer.Length, SocketFlags.None);

				if(recvSize == 0){
					Disconnect();
				}

				else if(recvSize > 0){
					m_recvQueue.Enqueue(buffer, recvSize);
				}
			}
		}
		catch{
			return;
		}
	}

	void AcceptClient(){
		if (m_listener != null && m_listener.Poll (0, SelectMode.SelectRead)) {
			m_socket = m_listener.Accept ();
			m_isConnected = true;

			if (m_handler != null) {
				NetEventState state = new NetEventState ();
				state.type = NetEventType.Connect;
				state.result = NetEventResult.Success;
				m_handler (state);
			}

			Debug.Log ("connected");
		}
	}
}

public enum NetEventType{
	Connect = 0,
	Disconnect,
	SendError,
	ReceiveError
};

public enum NetEventResult{
	Fail = -1,
	Success = 0
};

public class NetEventState{
	public NetEventType type;
	public NetEventResult result;

}