using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
namespace BeforeJericho
{
    // Opt-in development-player input recorder. Never changes transforms or progression.
    public sealed class ChapterValidationInput : MonoBehaviour
    {
        [Serializable] public class Step { public string[] keys; public float seconds = .1f, lookX, lookY; public bool left, right; public float mouseX, mouseY; }
        [Serializable] public class Command { public int id; public Step[] steps; }
        [Serializable] class Observation { public int command, step, hp, evidence, power, frames, width, height, ghoulsActive, bossHp, bossMaxHp; public int[] ghoulHp; public string quality; public string scene, chapterId, saveSlot, phase, stance, target, prompt, objective, checkpoint, message; public bool menu, paused, completed, resolved, water, rawheadActive, lighter, sprint, bossActive, bossRevealed, bossStaggered, normalsCleared, johnMap; public Vector3 player, enemy, camera; public Vector3[] ghoulPositions; public float yaw, meanFps, worstFrameMs; }
        string _dir; string _runId = Guid.NewGuid().ToString("N"); Keyboard _keyboard; Mouse _mouse; Command _command; int _last, _step; float _remaining, _sample, _seconds, _worst; int _frames; HuntSession _session;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-chapterValidation");
            if(!Debug.isDebugBuild || i < 0 || i + 1 >= args.Length) return;
            var go = new GameObject("Development input validation"); DontDestroyOnLoad(go);
            var driver = go.AddComponent<ChapterValidationInput>(); driver._dir = args[i + 1]; Application.runInBackground = true; Directory.CreateDirectory(driver._dir);
            driver._keyboard = InputSystem.AddDevice<Keyboard>(); driver._mouse = InputSystem.AddDevice<Mouse>();
            SceneManager.sceneLoaded += driver.SceneLoaded;
            InputSystem.onBeforeUpdate += driver.InputTick;
        }
        void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _session = FindAnyObjectByType<HuntSession>();
            if(_session) _session.saveSlot = "qa-standalone-" + _runId + "-" + scene.name;
        }
        void InputTick()
        {
            if(InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            if(_command == null)
            {
                string path = Path.Combine(_dir, "input.json");
                if(File.Exists(path)) try { var c = JsonUtility.FromJson<Command>(File.ReadAllText(path)); if(c != null && c.steps != null && c.steps.Length > 0 && c.id > _last) { _last = c.id; _command = c; _step = 0; _remaining = c.steps[0].seconds; } } catch(IOException) { }
            }
            if(_command == null) return;
            Step s = _command.steps[_step];
            var ks = new KeyboardState();
            if(s?.keys != null) foreach(string key in s.keys) if(Enum.TryParse<Key>(key, true, out var k)) ks.Set(k, true);
            InputSystem.QueueStateEvent(_keyboard, ks);
            var ms = new MouseState();
            if(s != null) { ms.delta = new Vector2(s.lookX, s.lookY); ms.position = new Vector2(s.mouseX, s.mouseY); ms = ms.WithButton(MouseButton.Left, s.left).WithButton(MouseButton.Right, s.right); s.lookX = s.lookY = 0; }
            InputSystem.QueueStateEvent(_mouse, ms);
        }
        void Update()
        {
            if(_command != null) { _remaining -= Time.unscaledDeltaTime; if(_remaining <= 0) { if(++_step >= _command.steps.Length) _command = null; else _remaining = _command.steps[_step].seconds; } }
            if(_session && _session.Playing) { _frames++; _seconds += Time.unscaledDeltaTime; _worst = Mathf.Max(_worst, Time.unscaledDeltaTime); }
            _sample -= Time.unscaledDeltaTime; if(_sample > 0 || !_session) return; _sample = .25f;
            var p = _session.player; var c = p.GetComponent<HunterCombat>(); var ch = _session.chapter;
            var tx = _session.eastTexas;
            var enemies = tx && tx.Director ? tx.Director.Enemies : null;
            var boss = enemies != null && enemies.Length > 4 ? enemies[4] : null;
            string phase = ch ? ch.Phase.ToString() : tx ? tx.TransitionReady ? "Transition" : tx.BreadcrumbFound ? "Breadcrumb" : _session.Resolved ? "Resolved" : tx.Revealed ? "Revealed" : tx.Identified ? "Identified" : "Investigation" : "";
            var o = new Observation {
                command = _last, step = _command == null ? -1 : _step,
                hp = _session.HP, evidence = _session.Evidence, power = ch ? ch.PowerStep : 0,
                scene = SceneManager.GetActiveScene().name,
                chapterId = _session.stanford ? StanfordChapter.ChapterId : _session.winslow ? WinslowChapter.ChapterId : _session.grants ? GrantsChapter.ChapterId : tx ? EastTexasChapter.ChapterId : ch ? AtchafalayaChapter.ChapterId : "PROLOGUE",
                saveSlot = _session.saveSlot, phase = phase, stance = p.Stance.ToString(),
                target = c.Target ? c.Target.kind.ToString() : "", prompt = c.Prompt,
                objective = _session.Objective, checkpoint = _session.CheckpointName, message = _session.Message,
                menu = _session.Menu, paused = _session.Paused, completed = _session.Completed, resolved = _session.Resolved,
                water = ch && ch.RawheadInWater, rawheadActive = ch && ch.Rawhead.Active,
                lighter = c.LighterOn, sprint = p.Sprinting,
                bossActive = boss && boss.Active, bossRevealed = tx && tx.Revealed,
                bossStaggered = boss && boss.Staggered, normalsCleared = tx && tx.Director.NormalsCleared,
                johnMap = tx && tx.BreadcrumbFound, ghoulsActive = tx ? tx.Director.ActiveCount : 0,
                bossHp = boss ? boss.HP : 0, bossMaxHp = boss ? boss.MaxHP : 0,
                player = p.transform.position, enemy = ch ? ch.Rawhead.transform.position : boss ? boss.transform.position : Vector3.zero,
                camera = p.view.transform.position, yaw = p.view.Yaw,
                meanFps = _seconds > 0 ? _frames / _seconds : 0, worstFrameMs = _worst * 1000,
                frames = _frames, width = Screen.width, height = Screen.height,
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()]
            };
            if(enemies != null) {
                o.ghoulHp = new int[enemies.Length]; o.ghoulPositions = new Vector3[enemies.Length];
                for(int i = 0; i < enemies.Length; i++) { o.ghoulHp[i] = enemies[i].HP; o.ghoulPositions[i] = enemies[i].transform.position; }
            };
            string json = JsonUtility.ToJson(o, true); File.WriteAllText(Path.Combine(_dir, "state.json"), json);
        }
        void OnDestroy() { SceneManager.sceneLoaded -= SceneLoaded; InputSystem.onBeforeUpdate -= InputTick; if(_keyboard != null) InputSystem.RemoveDevice(_keyboard); if(_mouse != null) InputSystem.RemoveDevice(_mouse); }
    }
}
