using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using CommonUsages=UnityEngine.XR.CommonUsages;

namespace PolygonRacing
{
    [DefaultExecutionOrder(100)]
    public sealed class PolygonRace : MonoBehaviour
    {
        enum Phase { Menu,Countdown,Driving,Finished }
        enum Mode { Race,TimeTrial }
        const int RaceLaps=3;
        // After the player finishes, rivals still running get this long to cross the line.
        const float ClassificationSeconds=30;
        static readonly PolygonCircuitLayout[] circuitOrder={PolygonCircuitLayout.Forest,PolygonCircuitLayout.CoastalExpressway,
            PolygonCircuitLayout.Downtown,PolygonCircuitLayout.Riviera,PolygonCircuitLayout.Sakura,PolygonCircuitLayout.Nordwald};
        static readonly string[] circuitLabels={"FOREST CIRCUIT","COASTAL EXPRESSWAY","DOWNTOWN STREETS","RIVIERA HARBOUR","SAKURA HILLS","NORDWALD RING"};
        readonly float[] menuRecords=new float[circuitOrder.Length];
        float playerFinish,classifyUntil,raceRecord,nextResults;
        bool classified,newRecord;
        PolygonDriver player;
        PolygonStartLights startLights;
        PolygonRival[] rivals;
        Phase phase;
        Mode mode;
        Mode menuMode;
        float countdown,elapsed,lapStarted,bestLap,lastLap,goUntil,trialBest;
        float[] finishTimes,previousRivalLaps;
        int resetSerial,place,finalPlace,selection;
        bool selectingCircuit,wasConfirm,wasMenu,navLatched;
        UnityEngine.XR.InputDevice left,right;
        readonly Progress progress=new Progress();
        readonly PolygonSlipstream.Charge playerSlipstream=new PolygonSlipstream.Charge();
        static bool pendingLaunch;
        static PolygonCircuitLayout pendingLayout;
        static Mode pendingMode;
        // Time Trial keys are unchanged from earlier versions; race best laps are kept separately
        // because drafting makes race laps faster than a solo lap.
        static string RecordKey(PolygonCircuitLayout layout,Mode recordMode)=>"PolygonRacing."+PolygonCircuit.LayoutIdFor(layout)+(recordMode==Mode.TimeTrial?".TimeTrialBest":".RaceBestLap");
        string BestKey=>RecordKey(PolygonCircuit.Layout,Mode.TimeTrial);
        string RaceBestKey=>RecordKey(PolygonCircuit.Layout,Mode.Race);
        int FieldSize=>rivals.Length+1;
        GameObject board;
        TextMesh boardText;
        Material boardMaterial;
        string boardValue;
        public bool SlipstreamAllowed=>phase==Phase.Driving&&mode==Mode.Race&&player&&!player.IsPaused;
        public float PlayerRaceDistance=>progress.LapsTravelled*PolygonCircuit.Length;
        // Same three-second countdown: reds at 0/.5/1/1.5/2 s, then out at 3 s.
        public int StartLightCount=>phase==Phase.Countdown?Mathf.Clamp(1+Mathf.FloorToInt((3-countdown)/.5f),1,5):0;

        // Unwrapped player route distance, independent of steering/heading.
        sealed class Progress
        {
            float previous;
            public float LapsTravelled {get;private set;}
            public int Laps=>Mathf.Max(0,Mathf.FloorToInt(LapsTravelled));
            public void Reset(Vector3 p){previous=PolygonCircuit.ProjectLap(p,out _);LapsTravelled=0;}
            public void Tick(Vector3 p)
            {
                float current=PolygonMarinaPit.ProjectRaceLap(p,out float offset);
                float delta=Mathf.Repeat(current-previous+.5f,1)-.5f;
                if(Mathf.Abs(delta)<.04f&&offset<PolygonWorld.RoadHalfWidth+8)LapsTravelled+=delta;
                previous=current;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register(){SceneManager.sceneLoaded-=SceneLoaded;SceneManager.sceneLoaded+=SceneLoaded;}
        static void SceneLoaded(Scene scene,LoadSceneMode loadMode)
        {
            var driver=Object.FindFirstObjectByType<PolygonDriver>();
            if(!driver||Object.FindFirstObjectByType<PolygonRace>())return;
            var go=new GameObject("Race menu and timing");SceneManager.MoveGameObjectToScene(go,driver.gameObject.scene);go.AddComponent<PolygonRace>();
        }
        void Start()
        {
            player=Object.FindFirstObjectByType<PolygonDriver>();
            startLights=Object.FindFirstObjectByType<PolygonStartLights>();
            rivals=Object.FindObjectsByType<PolygonRival>(FindObjectsSortMode.None);
            System.Array.Sort(rivals,(a,b)=>a.GridSlot.CompareTo(b.GridSlot));
            if(!player){enabled=false;return;}
            finishTimes=new float[rivals.Length];previousRivalLaps=new float[rivals.Length];
            CreateBoard();ShowMenu();
            if(pendingLaunch)
            {
                pendingLaunch=false;
                if(PolygonCircuit.Layout==pendingLayout)Begin(pendingMode);
                else Debug.LogError("The requested circuit scene loaded with the wrong circuit layout.");
            }
        }
        void CreateBoard()
        {
            board=new GameObject("Countdown and results - cockpit panel");board.transform.SetParent(player.transform,false);board.transform.localPosition=new Vector3(0,1.32f,1.6f);
            var background=GameObject.CreatePrimitive(PrimitiveType.Cube);background.name="Panel background";background.transform.SetParent(board.transform,false);background.transform.localScale=new Vector3(1.5f,.84f,.018f);
            var collider=background.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            var shader=PolygonLook.LitShader();
            boardMaterial=new Material(shader){name="Race panel charcoal",color=new Color(.025f,.045f,.065f)};
            background.GetComponent<Renderer>().sharedMaterial=boardMaterial;
            boardText=PolygonWorld.Label("Race display",board.transform,"",new Vector3(0,0,-.025f),.009f,new Color(.5f,1,.78f));
        }
        void ShowMenu()
        {
            ClearSlipstream();
            phase=Phase.Menu;player.RaceManaged=true;player.RestartRace();player.SetRaceLock(true);resetSerial=player.ResetSerial;
            foreach(var car in rivals){car.RaceManaged=true;car.SetRaceLock(true);}
            selectingCircuit=false;selection=0;navLatched=false;DrawMenu();
        }
        void DrawMenu()
        {
            string restart=mode==Mode.TimeTrial?"RESTART TIME TRIAL":"RESTART RACE";
            if(selectingCircuit)
            {
                var text=new System.Text.StringBuilder(menuMode==Mode.Race?"SELECT CIRCUIT  BEST RACE LAP\n":"SELECT CIRCUIT  BEST TIME TRIAL\n");
                for(int i=0;i<circuitOrder.Length;i++)
                    text.Append(selection==i?"> ":"  ").Append(circuitLabels[i]).Append("  ").Append(RecordText(menuRecords[i])).Append('\n');
                text.Append(selection==circuitOrder.Length?"> ":"  ").Append("BACK\nSTICK: MOVE  A: SELECT  Y: MENU");
                SetBoard(text.ToString(),true);
                return;
            }
            SetBoard("POLYGON RACING VR\n"+(selection==0?"> ":"  ")+"START RACE\n"+(selection==1?"> ":"  ")+"TIME TRIAL\n"+(selection==2?"> ":"  ")+restart+"\nSTICK UP/DOWN   A: SELECT\nY / M: MENU",true);
        }
        static string RecordText(float seconds)=>seconds>0?PolygonDriver.FormatTime(seconds):"--:--.-";
        // Read once when the circuit list opens (not every frame).
        void OpenCircuitList(Mode selected)
        {
            menuMode=selected;selectingCircuit=true;selection=0;
            for(int i=0;i<circuitOrder.Length;i++)menuRecords[i]=PlayerPrefs.GetFloat(RecordKey(circuitOrder[i],menuMode),0);
            DrawMenu();
        }
        void Begin(Mode selected)
        {
            ClearSlipstream();
            mode=selected;player.RestartRace();player.RaceManaged=true;player.SetRaceLock(true);resetSerial=player.ResetSerial;
            trialBest=PlayerPrefs.GetFloat(BestKey,0);raceRecord=PlayerPrefs.GetFloat(RaceBestKey,0);
            classified=newRecord=false;playerFinish=0;
            foreach(var car in rivals){car.RestartRace();car.RaceManaged=true;car.SetParticipating(mode==Mode.Race);car.SetRaceLock(true);}
            for(int i=0;i<finishTimes.Length;i++){finishTimes[i]=-1;previousRivalLaps[i]=rivals[i].RaceDistance/PolygonCircuit.Length;}
            progress.Reset(player.transform.position);
            countdown=3;elapsed=lapStarted=bestLap=lastLap=goUntil=0;place=FieldSize;finalPlace=0;
            phase=Phase.Countdown;SetBoard("3\nGET READY",!startLights);
        }
        static bool Button(UnityEngine.XR.InputDevice d,InputFeatureUsage<bool> key)=>d.TryGetFeatureValue(key,out var value)&&value;
        void Update()
        {
            if(!player||rivals==null)return;
            if(!left.isValid)left=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if(!right.isValid)right=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            var keyboard=Keyboard.current;
            bool confirm=Button(right,CommonUsages.primaryButton)||(keyboard!=null&&keyboard.enterKey.isPressed);
            bool menu=Button(left,CommonUsages.secondaryButton)||(keyboard!=null&&keyboard.mKey.isPressed);
            bool confirmPressed=confirm&&!wasConfirm,menuPressed=menu&&!wasMenu;
            wasConfirm=confirm;wasMenu=menu;
            if(menuPressed){ShowMenu();return;}
            if(phase==Phase.Menu)
            {
                // The driver's A/R reset is harmless in the menu; absorb its serial change.
                resetSerial=player.ResetSerial;player.SetRaceLock(true);
                float y=0;if(left.TryGetFeatureValue(CommonUsages.primary2DAxis,out var stick))y=stick.y;
                if(keyboard!=null){if(keyboard.upArrowKey.isPressed)y=1;if(keyboard.downArrowKey.isPressed)y=-1;}
                bool held=Mathf.Abs(y)>.6f;
                int options=selectingCircuit?circuitOrder.Length+1:3;
                if(held&&!navLatched){selection=(selection+(y>0?options-1:1))%options;DrawMenu();}
                navLatched=held;
                if(confirmPressed)
                {
                    if(selectingCircuit)
                    {
                        if(selection==circuitOrder.Length){selectingCircuit=false;selection=menuMode==Mode.Race?0:1;DrawMenu();}
                        else LoadCircuit(circuitOrder[selection]);
                    }
                    else if(selection<2)OpenCircuitList(selection==0?Mode.Race:Mode.TimeTrial);
                    else Begin(mode);
                }
                return;
            }
            if(player.ResetSerial!=resetSerial){Begin(mode);return;}
            if(player.IsPaused)
            {
                player.SetSlipstreamStrength(0);
                foreach(var car in rivals)car.SetSlipstreamStrength(0);
                return;
            }
            if(phase==Phase.Countdown)
            {
                countdown-=Mathf.Min(Time.deltaTime,.05f);
                if(countdown<=0)
                {
                    phase=Phase.Driving;player.SetRaceLock(false);
                    if(mode==Mode.Race)foreach(var car in rivals)car.SetRaceLock(false);
                    goUntil=1;SetBoard("GO!",!startLights);
                }
                return;
            }
            if(phase!=Phase.Driving&&phase!=Phase.Finished)return;
            float dt=Mathf.Min(Time.deltaTime,.05f);elapsed+=dt;
            TrackRivalFinishes(dt);
            if(phase==Phase.Finished){UpdateClassification();return;}
            float previousPlayerLaps=progress.LapsTravelled;
            int before=progress.Laps;progress.Tick(player.transform.position);
            UpdatePlayerSlipstream(dt);
            place=1;
            if(mode==Mode.Race)for(int i=0;i<rivals.Length;i++)
                if(finishTimes[i]>=0||rivals[i].RaceDistance/PolygonCircuit.Length>progress.LapsTravelled+.0001f)place++;
            if(progress.Laps>before)
            {
                lastLap=elapsed-lapStarted;lapStarted=elapsed;
                if(bestLap==0||lastLap<bestLap)bestLap=lastLap;
                if(mode==Mode.TimeTrial&&(trialBest==0||lastLap<trialBest))
                {trialBest=lastLap;PlayerPrefs.SetFloat(BestKey,trialBest);PlayerPrefs.Save();}
                if(mode==Mode.Race&&(raceRecord==0||lastLap<raceRecord))
                {raceRecord=lastLap;newRecord=true;PlayerPrefs.SetFloat(RaceBestKey,raceRecord);PlayerPrefs.Save();}
            }
            if(mode==Mode.Race&&progress.Laps>=RaceLaps)
            {
                playerFinish=elapsed-dt+dt*Mathf.InverseLerp(previousPlayerLaps,progress.LapsTravelled,RaceLaps);
                finalPlace=1;foreach(float time in finishTimes)if(time>=0&&time<=playerFinish)finalPlace++;
                // Only the player stops; rivals still racing run on to the line (they ignore
                // stopped cars in their traffic checks) so the results show a real order.
                place=finalPlace;phase=Phase.Finished;player.SetRaceLock(true);
                ClearSlipstream();classifyUntil=elapsed+ClassificationSeconds;nextResults=0;
                UpdateClassification();
            }
            else if(elapsed>=goUntil)SetBoard("",false);
        }
        // Rivals report their actual accumulated route distance, rather than estimating
        // their lap count from a sideways offset on the visible road.
        void TrackRivalFinishes(float dt)
        {
            if(mode!=Mode.Race)return;
            for(int i=0;i<rivals.Length;i++)
            {
                float laps=rivals[i].RaceDistance/PolygonCircuit.Length;
                if(laps>=RaceLaps&&finishTimes[i]<0)
                {finishTimes[i]=elapsed-dt+dt*Mathf.InverseLerp(previousRivalLaps[i],laps,RaceLaps);rivals[i].SetRaceLock(true);}
                previousRivalLaps[i]=laps;
            }
        }
        void UpdateClassification()
        {
            if(!classified)
            {
                bool all=true;foreach(float time in finishTimes)if(time<0)all=false;
                if(all||elapsed>=classifyUntil){classified=true;foreach(var car in rivals)car.SetRaceLock(true);}
            }
            // The order only changes as rivals cross the line; refresh a few times a second.
            if(elapsed<nextResults&&!classified)return;
            nextResults=elapsed+.25f;
            SetBoard(ResultsText(),true);
        }
        string ResultsText()
        {
            int n=FieldSize;var names=new string[n];var times=new float[n];var laps=new float[n];
            names[0]="YOU";times[0]=playerFinish;laps[0]=RaceLaps;
            for(int i=0;i<rivals.Length;i++){names[i+1]=rivals[i].DisplayName;times[i+1]=finishTimes[i];laps[i+1]=Mathf.Min(RaceLaps,rivals[i].RaceDistance/PolygonCircuit.Length);}
            var order=new int[n];for(int i=0;i<n;i++)order[i]=i;
            // Finishers by time, then cars still running by distance covered.
            System.Array.Sort(order,(a,b)=>times[a]>=0&&times[b]>=0?times[a].CompareTo(times[b]):times[a]>=0?-1:times[b]>=0?1:laps[b].CompareTo(laps[a]));
            var text=new System.Text.StringBuilder($"FINISH - {Ordinal(finalPlace)} PLACE\n");
            float winner=times[order[0]];
            for(int r=0;r<n;r++)
            {
                int i=order[r];string value;
                if(r==0)value=PolygonDriver.FormatTime(times[i]);
                else if(times[i]>=0){float gap=times[i]-winner;value=gap<60?$"+{gap:0.0}s":"+"+PolygonDriver.FormatTime(gap);}
                else if(!classified)value="RUNNING";
                else{int down=Mathf.FloorToInt(RaceLaps-laps[i]);value=down>0?$"+{down} LAP":"NO TIME";}
                text.Append(r+1).Append(i==0?" > ":"   ").Append(names[i]).Append("   ").Append(value).Append('\n');
            }
            text.Append("BEST LAP ").Append(PolygonDriver.FormatTime(bestLap)).Append(newRecord?"  NEW RECORD":"").Append("\nA: RACE AGAIN   Y: MENU");
            return text.ToString();
        }
        void LateUpdate()
        {
            if(!player||!player.dashboard||rivals==null)return;
            if(player.IsPaused){player.dashboard.text="PAUSED\nX / P TO RESUME";return;}
            if(phase==Phase.Menu){DrawMenu();player.dashboard.text="CHOOSE A MODE\nA: SELECT   Y: MENU";return;}
            if(phase==Phase.Countdown){SetBoard(Mathf.Max(1,Mathf.CeilToInt(countdown))+"\nGET READY",!startLights);player.dashboard.text=mode==Mode.Race?"GET READY\n3 LAP RACE":"GET READY\nTIME TRIAL";return;}
            if(phase==Phase.Finished){player.dashboard.text=$"FINISH {finalPlace}/{FieldSize}\nA: RESTART  Y: MENU";return;}
            if(mode==Mode.TimeTrial)
                player.dashboard.text=$"{player.SpeedKmh:000} KM/H  TT\n{PolygonDriver.FormatTime(elapsed-lapStarted)} BEST {(trialBest>0?PolygonDriver.FormatTime(trialBest):"--:--.-")}"+(player.IsOnPitLane?"\nMARINA PIT 80 KM/H":"");
            else
            {
                float leader=progress.LapsTravelled;int finished=0;
                for(int i=0;i<rivals.Length;i++){leader=Mathf.Max(leader,rivals[i].RaceDistance/PolygonCircuit.Length);if(finishTimes[i]>=0)finished++;}
                int lapGap=Mathf.FloorToInt(Mathf.Max(0,leader-progress.LapsTravelled));
                string gap=finished>0?" "+finished+" FIN":lapGap>0?" +"+lapGap+"L":"";
                player.dashboard.text=$"{player.SpeedKmh:000} KM/H {place}/{FieldSize}{gap}\nLAP {Mathf.Min(RaceLaps,progress.Laps+1)}/{RaceLaps} {PolygonDriver.FormatTime(elapsed-lapStarted)}\n{DraftIndicator()}";
            }
        }
        void UpdatePlayerSlipstream(float deltaTime)
        {
            float alignment=0;
            if(mode==Mode.Race)
            {
                foreach(var car in rivals)
                {
                    if(!car||!car.Participating||car.RaceLocked)continue;
                    alignment=Mathf.Max(alignment,PolygonSlipstream.Measure(player.transform.position,
                        player.transform.forward,PlayerRaceDistance,car.transform.position,car.transform.forward,car.RaceDistance));
                }
            }
            player.SetSlipstreamStrength(playerSlipstream.Advance(alignment,deltaTime));
        }
        string DraftIndicator()
        {
            if(player.IsOnPitLane)return "MARINA PIT  80 KM/H";
            int bars=Mathf.Clamp(Mathf.CeilToInt(playerSlipstream.Buildup*6),0,6);
            string gauge=new string('|',bars)+new string('.',6-bars);
            return playerSlipstream.Strength>=.65f?$"DRAFT BOOST [{gauge}]":$"DRAFT [{gauge}]";
        }
        void ClearSlipstream()
        {
            playerSlipstream.Reset();
            if(player)player.SetSlipstreamStrength(0);
            if(rivals!=null)foreach(var car in rivals)if(car)car.ResetSlipstream();
        }
        static string Ordinal(int n)=>n==1?"1ST":n==2?"2ND":n==3?"3RD":"4TH";
        void SetBoard(string value,bool visible)
        {
            if(!board)return;if(board.activeSelf!=visible)board.SetActive(visible);
            if(boardValue!=value){boardValue=value;boardText.text=value;}
        }
        void LoadCircuit(PolygonCircuitLayout layout)
        {
            string sceneName=layout==PolygonCircuitLayout.Forest?"ForestCircuit":layout==PolygonCircuitLayout.Downtown?"DowntownCircuit":layout==PolygonCircuitLayout.Riviera?"RivieraHarbour":layout==PolygonCircuitLayout.Sakura?"SakuraHills":layout==PolygonCircuitLayout.Nordwald?"NordwaldRing":"CoastalExpressway";
            if(PolygonCircuit.Layout==layout){Begin(menuMode);return;}
            if(!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Cannot load {sceneName}. Add its scene to the active build profile before selecting this circuit.");
                return;
            }
            pendingLaunch=true;pendingLayout=layout;pendingMode=menuMode;
            SceneManager.LoadScene(sceneName);
        }
        void OnGUI()
        {
            if(!player||rivals==null||!player.view||player.view.stereoEnabled)return;
            float scale=Mathf.Clamp(Screen.width/1200f,.7f,1.6f);var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            var style=new GUIStyle(GUI.skin.box){fontSize=17,alignment=TextAnchor.MiddleLeft};
            string status=phase==Phase.Menu?"CHOOSE A MODE":mode==Mode.TimeTrial?"TIME TRIAL":"POSITION "+place+" / "+FieldSize;
            GUI.Box(new Rect(20,20,530,100),"  POLYGON RACING VR\n  "+status+"\n  Y / M: menu    A / R: restart    X / P: pause",style);
            if(phase==Phase.Menu)
            {
                if(selectingCircuit)
                {
                    for(int i=0;i<circuitOrder.Length;i++)
                        if(GUI.Button(new Rect(20,130+50*i,340,42),circuitLabels[i]+"   "+RecordText(menuRecords[i])))LoadCircuit(circuitOrder[i]);
                    if(GUI.Button(new Rect(20,130+50*circuitOrder.Length,340,42),"BACK")){selectingCircuit=false;selection=menuMode==Mode.Race?0:1;DrawMenu();}
                }
                else
                {
                    if(GUI.Button(new Rect(20,130,280,42),"START RACE"))OpenCircuitList(Mode.Race);
                    if(GUI.Button(new Rect(20,180,280,42),"TIME TRIAL"))OpenCircuitList(Mode.TimeTrial);
                    if(GUI.Button(new Rect(20,230,280,42),"RESTART CURRENT MODE"))Begin(mode);
                }
            }
            GUI.matrix=old;
        }
        void OnDestroy()
        {
            ClearSlipstream();
            if(player){player.RaceManaged=false;player.SetRaceLock(false);}
            if(rivals!=null)foreach(var car in rivals)if(car){car.RaceManaged=false;car.SetParticipating(true);car.SetRaceLock(false);}
            if(board)Destroy(board);if(boardMaterial)Destroy(boardMaterial);
        }
    }
}
