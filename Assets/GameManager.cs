using UnityEngine;
using System.Collections;
using System.Net;

public class GameManager : MonoBehaviour {

	//enum
	private enum GameState{
		Main = 0,
		Create,
		Enter,
		Ready,
		Game,
		Result
	};

	private enum SceneState{
		Main = 0,
		Game,
	};

	private enum HostType{
		None = 0,
		Server,
		Client
	};

	//members
	private 	GameState 		state;	//게임 상태
	private		SceneState		scene;	//씬 상태
	private 	HostType		hostType;	//서버/클라이언트 구분
	private		NetworkManager	network;	//네트워크
	private		string			address;

	private		float 			remainTime;
	private		bool switchLeft;
	private		int[] count;

	//textures
	public		GUITexture		mainPage;
	public		GUITexture 		gamePage;
	public		GUITexture		logo;
	public 		GUITexture		button_left;
	public		GUITexture		button_right;
	public 		GUITexture 		score_board;

	//methods
	void Awake(){
		DontDestroyOnLoad (this.gameObject);

		state = GameState.Main;
		scene = SceneState.Main;
		hostType = HostType.None;
		network = GameObject.Find ("ServerManager").GetComponent<NetworkManager> ();
		address = Dns.GetHostAddresses (Dns.GetHostName())[0].ToString();

		Reset ();
	}

	void Reset(){
		remainTime = 3.0f;
		switchLeft = true;
		count = new int[2]{ 0, 0 };
	}
	
	// Update is called once per frame
	void Update () {
		switch (scene) {
		case SceneState.Main:
			{
				switch (state) {
				case GameState.Main:
					break;
				case GameState.Create:
					UpdateCreate();
					break;
				case GameState.Enter:
					UpdateEnter ();
					break;
				default:
					Debug.Log ("state error");
					break;
				}
			}
			break;
		case SceneState.Game:
			{
				switch (state) {
				case GameState.Ready:
					UpdateReady ();
					break;
				case GameState.Game:
					UpdateGame();
					break;
				case GameState.Result:
					UpdateResult ();
					break;
				default:
					Debug.Log ("state error");
					break;
				}
			}
			break;
		}
	}

	void OnGUI () {
		switch (scene) {
		case SceneState.Main:
			GUIMain ();
			{
				switch (state) {
				case GameState.Main:
					OnGUIMain ();
					break;
				case GameState.Create:
					OnGUICreate();
					break;
				case GameState.Enter:
					OnGUIEnter ();
					break;
				default:
					Debug.Log ("state error");
					break;
				}
			}
			break;
		case SceneState.Game:
			GUIGame ();
			{
				switch (state) {
				case GameState.Ready:
				case GameState.Game:
					OnGUIGame();
					break;
				case GameState.Result:
					OnGUIResult ();
					break;
				default:
					Debug.Log ("state error");
					break;
				}
			}
			break;
		}
	}

	void UpdateCreate(){
		if (!network.IsServer ()) {
			bool result = network.StartServer (NetworkManager.m_port, 1);

			if (!result) {
				Debug.LogError ("not connected");
			}
		}

		if (network.IsConnected ()) {
			scene = SceneState.Game;
			state = GameState.Ready;
		}
	}

	void UpdateEnter(){
		if (network.IsConnected ()) {
			state = GameState.Ready;
			scene = SceneState.Game;
		}	
	}

	void UpdateGame(){
		remainTime -= Time.deltaTime;

		if (remainTime <= 0.0f) {
			remainTime = 0.0f;
			state = GameState.Result;
		}

		if (Input.GetMouseButtonDown (0)) {
			Vector2 pos = (Vector2)Input.mousePosition;
			pos.x = Screen.width - pos.x;
			pos.y = Screen.height - pos.y;
			int tar = MPosToSwitch (pos); 

			if ((tar == 0 && !switchLeft) || (tar == 1 && switchLeft)) {
				count [0]++;
				switchLeft = !switchLeft;
			}
		}

		byte[] buf = IntToBArr (count [0]);
		network.Send (buf, buf.Length);

		byte[] ret = new byte[1];
		int recvSize = network.Recv(ref ret, ret.Length);

		if (recvSize > 0) {
			count[1] = (int)ret [0];
		}
	}

	byte[] IntToBArr(int index){
		byte[] buffer = new byte[1];
		buffer [0] = (byte)index;
		return buffer;
	}

	void UpdateReady(){
		remainTime -= Time.deltaTime;

		if (remainTime <= 0.0f) {
			remainTime = 10.0f;
			state = GameState.Game;
		}
	}

	

	int MPosToSwitch(Vector2 pos){

		if (pos.y > Screen.height * 0.9f || pos.y < Screen.height * 0.6f ||
		    pos.x < Screen.width * 0.1f || pos.x > Screen.width * 0.9f) {
			return -1;
		} else if (pos.x >= Screen.width * 0.5f) {
			return 1;
		} else {
			return 0;
		}
	}


	void UpdateResult(){

	}

	//GUI
	void GUIMain(){
		Rect bg = new Rect (0, 0, Screen.width, Screen.height);

		GUI.DrawTexture (bg, mainPage.texture);

		Rect rlogo = new Rect (Screen.width / 2 - 200, Screen.height / 2 - 300, 
			400, 150);

		GUI.DrawTexture (rlogo, logo.texture);
	}

	void GUIGame(){
		Rect bg = new Rect (0, 0, Screen.width, Screen.height);

		GUI.DrawTexture (bg, gamePage.texture);

		GUI.DrawTexture(new Rect(Screen.width * 0.1f, Screen.height * 0.1f,
			Screen.width * 0.25f, Screen.height * 0.1f), score_board.texture);

		GUI.DrawTexture (new Rect (Screen.width * 0.65f, Screen.height * 0.1f,
			Screen.width * 0.25f, Screen.height * 0.1f), score_board.texture);

		GUI.DrawTexture(new Rect(Screen.width * 0.4f, Screen.height * 0.1f,
			Screen.width * 0.2f, Screen.height * 0.1f), score_board.texture);
	}

	void OnGUIMain(){
		if(GUI.Button(new Rect(Screen.width * 0.3f, Screen.height * 0.5f,
			Screen.width * 0.4f, Screen.height * 0.2f), "방 만들기")){
			state = GameState.Create;
		}

		if (GUI.Button (new Rect (Screen.width * 0.3f, Screen.height * 0.75f,
			Screen.width * 0.4f, Screen.height * 0.2f), "방 들어가기")) {
			state = GameState.Enter;
		}
	}

	void OnGUICreate(){

		GUI.DrawTexture (new Rect(Screen.width * 0.3f, Screen.height * 0.45f,
			Screen.width * 0.4f, Screen.height * 0.1f), score_board.texture);
		
		GUI.Label (new Rect(Screen.width * 0.3f, Screen.height * 0.45f,
			Screen.width * 0.4f, Screen.height * 0.1f), "기다리는 중입니다");

		GUI.DrawTexture (new Rect(Screen.width * 0.3f, Screen.height * 0.6f,
			Screen.width * 0.4f, Screen.height * 0.1f), score_board.texture);
		
		GUI. Label (new Rect(Screen.width * 0.3f, Screen.height * 0.6f,
			Screen.width * 0.4f, Screen.height * 0.1f), address);

		if(GUI.Button(new Rect (Screen.width * 0.3f, Screen.height * 0.75f,
			Screen.width * 0.4f, Screen.height * 0.2f), "돌아가기")){
			state = GameState.Main;
		}
	}

	void OnGUIEnter(){
		address = GUI.TextField (new Rect(Screen.width * 0.3f, Screen.height * 0.45f,
			Screen.width * 0.4f, Screen.height * 0.1f), address);

		if(GUI.Button(new Rect(Screen.width * 0.3f, Screen.height * 0.6f,
			Screen.width * 0.4f, Screen.height * 0.1f), "입장하기")){
			bool ret = network.Connect (address, NetworkManager.m_port);
		}

		if(GUI.Button (new Rect (Screen.width * 0.3f, Screen.height * 0.75f,
			Screen.width * 0.4f, Screen.height * 0.1f), "돌아가기")){
			state = GameState.Main;
		}


	}

	void OnGUIGame(){
		GUI.DrawTexture (new Rect (Screen.width * 0.1f, Screen.height * 0.6f,
			Screen.width * 0.8f, Screen.height * 0.3f), switchLeft == true ? button_left.texture : button_right.texture);

		GUI.Label(new Rect(Screen.width * 0.1f, Screen.height * 0.1f,
			Screen.width * 0.25f, Screen.height * 0.1f), "" + count[0]);

		GUI.Label (new Rect (Screen.width * 0.65f, Screen.height * 0.1f,
			Screen.width * 0.25f, Screen.height * 0.1f), "" + count [1]);

		GUI.Label(new Rect(Screen.width * 0.4f, Screen.height * 0.1f,
			Screen.width * 0.2f, Screen.height * 0.1f), "" + remainTime);
	}

	void OnGUIResult(){
		GUI.Label(new Rect(Screen.width * 0.1f, Screen.height * 0.1f,
			Screen.width * 0.25f, Screen.height * 0.1f), "" + count[0]);

		GUI.Label (new Rect (Screen.width * 0.65f, Screen.height * 0.1f,
			Screen.width * 0.25f, Screen.height * 0.1f), "" + count [1]);
		
		if(GUI.Button(new Rect(Screen.width/2 - 100, Screen.height / 2 + 200,
			200, 80), "돌아가기")){
			state = GameState.Main;
			scene = SceneState.Main;

			network.Disconnect ();
		}
	}
}
