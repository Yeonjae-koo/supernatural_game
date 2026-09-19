using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace BeforeJericho.Editor
{
    // Temporary virtual input for real traversal. Never teleports or changes hunt progression.
    public static class ChapterPlayProbe
    {
        static Keyboard keyboard; static Mouse mouse;
        static bool background; static InputSettings.BackgroundBehavior backgroundMode;
        static InputSettings.EditorInputBehaviorInPlayMode editorMode;
        public static string Result = "Idle";
        public static void Begin()
        {
            if(!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            if(keyboard != null) return;
            background = Application.runInBackground; backgroundMode = InputSystem.settings.backgroundBehavior;
            editorMode = InputSystem.settings.editorInputBehaviorInPlayMode;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            EditorApplication.playModeStateChanged += OnMode;
        }
        static void OnMode(PlayModeStateChange state) { if(state == PlayModeStateChange.ExitingPlayMode) End(); }
        public static async void Input(string keys, float seconds = .1f, float lookX = 0, float lookY = 0, bool aim = false, bool fire = false)
        {
            if(Result == "Running") throw new InvalidOperationException("Wait for the active input step.");
            Begin(); Result = "Running";
            try
            {
                var state = new KeyboardState();
                foreach(string key in keys.Split(new[]{',',' '}, StringSplitOptions.RemoveEmptyEntries)) state.Set((Key)Enum.Parse(typeof(Key), key, true), true);
                InputSystem.QueueStateEvent(keyboard, state);
                InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(lookX,lookY) }.WithButton(MouseButton.Right,aim).WithButton(MouseButton.Left,fire));
                await Task.Delay(Mathf.CeilToInt(Mathf.Clamp(seconds,.03f,30)*1000));
                if(keyboard != null) InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                if(mouse != null) InputSystem.QueueStateEvent(mouse, new MouseState());
                await Task.Delay(100); Result = "Done";
            }
            catch(Exception e) { Result = e.ToString(); }
        }
        public static object Observe()
        {
            var s = UnityEngine.Object.FindAnyObjectByType<HuntSession>();
            if(!s) return new { error="No hunt session" };
            var c=s.player.GetComponent<HunterCombat>();
            return new { Result, position=s.player.transform.position, yaw=s.player.view.Yaw, s.HP, s.Evidence, s.Playing, s.Menu, s.Journal, s.Paused, s.Completed, s.Resolved, s.Objective, s.Message, s.CheckpointName, stance=s.player.Stance.ToString(), target=c.Target?c.Target.kind.ToString():"", c.Prompt, c.Ammo, c.Reserve, c.Weapon, c.LighterOn };
        }
        public static async void Walk(float x, float z, bool sprint = false, float tolerance = .3f)
        {
            if(Result == "Running") throw new InvalidOperationException("Wait for the active input step.");
            Begin(); Result = "Running";
            try
            {
                var session=UnityEngine.Object.FindAnyObjectByType<HuntSession>();
                float start=Time.realtimeSinceStartup; Vector3 previous=session.player.transform.position; float stuck=0;
                while(true)
                {
                    Vector3 d=new Vector3(x,session.player.transform.position.y,z)-session.player.transform.position;
                    if(d.magnitude<tolerance) break;
                    if(!session.Playing) throw new Exception("Gameplay stopped: "+session.Message);
                    if(Time.realtimeSinceStartup-start>60) throw new Exception("Walk timed out at "+session.player.transform.position);
                    float turn=Mathf.DeltaAngle(session.player.view.Yaw,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg);
                    InputSystem.QueueStateEvent(mouse,new MouseState {delta=new Vector2(turn/.11f,0)});
                    InputSystem.QueueStateEvent(keyboard,Mathf.Abs(turn)>20 ? new KeyboardState() : sprint&&d.magnitude>1.5f ? new KeyboardState(Key.W,Key.LeftShift) : new KeyboardState(Key.W));
                    await Task.Delay(80);
                    float distance=Vector3.Distance(previous,session.player.transform.position);
                    stuck=distance<.005f && Mathf.Abs(turn)<5 ? stuck+.08f:0;
                    previous=session.player.transform.position;
                    if(stuck>2) throw new Exception("Route blocked at "+previous+" toward "+x+","+z);
                }
                Result="Done";
            }
            catch(Exception e) { Result=e.Message; }
            finally { if(keyboard!=null) InputSystem.QueueStateEvent(keyboard,new KeyboardState()); if(mouse!=null) InputSystem.QueueStateEvent(mouse,new MouseState()); }
        }
        public static void End()
        {
            if(keyboard == null) return;
            InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse); keyboard=null; mouse=null;
            Application.runInBackground=background; InputSystem.settings.backgroundBehavior=backgroundMode;
            InputSystem.settings.editorInputBehaviorInPlayMode=editorMode;
            EditorApplication.playModeStateChanged-=OnMode; Result="Stopped";
        }
    }
}
