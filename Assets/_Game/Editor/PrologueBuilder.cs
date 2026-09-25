using System.IO;
using BeforeJericho;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
namespace BeforeJericho.Editor
{
    public static class PrologueBuilder
    {
        const string Root = "Assets/_Game";
        static Material _stone, _brick, _wood, _metal, _cream, _gold, _blue, _skin, _jacket;
        static TMP_FontAsset _font;
        static HuntSession _session;
        static readonly System.Collections.Generic.List<HuntInteractable> Items = new();
        [MenuItem("Before Jericho/Create Prologue Slice")]
        public static void Create()
        {
            if(File.Exists(Root + "/Scenes/SCN_NewOrleans_Slice.unity")) throw new System.InvalidOperationException("Slice already exists. Edit it incrementally rather than rebuilding.");
            if(EditorSceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Save the current scene before generating the slice.");
            foreach(string folder in new[]{"Scenes","Prefabs/Actors","Data","Art/Materials"}) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if(!_font) throw new System.InvalidOperationException("Import TMP essential resources first.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Items.Clear();
            _stone = Material("Wet limestone", new Color(.3f,.34f,.35f), .5f);
            _brick = Material("Aged stucco", new Color(.28f,.2f,.16f));
            _wood = Material("Walnut", new Color(.12f,.075f,.04f));
            _metal = Material("Wrought iron", new Color(.055f,.07f,.075f), .6f);
            _cream = Material("Tomb marble", new Color(.58f,.6f,.53f));
            _gold = Material("Brass", new Color(.75f,.47f,.17f), .7f);
            _blue = Material("Spirit", new Color(.32f,.64f,.68f), 0, new Color(.12f,.5f,.55f));
            _skin = Material("Skin", new Color(.58f,.38f,.25f)); _jacket = Material("Leather",new Color(.12f,.065f,.035f), .2f);
            var road = Material("Rain dark asphalt",new Color(.055f,.07f,.08f), .75f);
            Box("Street", new Vector3(0,-.25f,34), new Vector3(16,.5f,78), road);
            Box("District floor",new Vector3(0,-.45f,34), new Vector3(50,.5f,78),_stone);
            Box("West boundary",new Vector3(-24,3,34),new Vector3(.6f,6,78),_brick);
            Box("East boundary",new Vector3(24,3,34),new Vector3(.6f,6,78),_brick);
            Box("South boundary",new Vector3(0,3,-5),new Vector3(48,6,.5f),_brick);
            Box("North boundary",new Vector3(0,3,73),new Vector3(48,6,.5f),_brick);
            for(int z=0;z<70;z+=3)
            { Box("Curb L",new Vector3(-7.7f,.05f,z),new Vector3(.3f,.2f,2.9f),_cream); Box("Curb R",new Vector3(7.7f,.05f,z),new Vector3(.3f,.2f,2.9f),_cream); }
            for(int z=4;z<69;z+=12) { Lamp(new Vector3(6.8f,0,z)); Lamp(new Vector3(-6.8f,0,z+3)); }
            Facade(-13,3,9); Facade(13,3,14); Facade(13,22,10); Facade(-14,41,12);
            // Open-front antique shop: directly navigable, no hidden door blockers.
            Box("Shop floor",new Vector3(-13,.02f,22),new Vector3(12,.15f,16),_wood);
            Box("Shop rear wall",new Vector3(-19,2.3f,22),new Vector3(.4f,4.6f,16),_brick);
            Box("Shop south wall",new Vector3(-13,2.3f,14),new Vector3(12,4.6f,.4f),_brick);
            Box("Shop north wall",new Vector3(-13,2.3f,30),new Vector3(12,4.6f,.4f),_brick);
            Box("Shop header",new Vector3(-7,3.8f,22),new Vector3(.4f,1.2f,16),_wood);
            Sign("MERCER  /  ANTIQUES",new Vector3(-6.72f,3.6f,22),new Vector3(0,90,0),5,.45f);
            for(int z=17;z<29;z+=5) { Box("Antique shelf",new Vector3(-18,1.1f,z),new Vector3(1,2.2f,3),_wood); for(int j=0;j<3;j++) Box("Estate box",new Vector3(-17.8f,2.4f,z-1+j),new Vector3(.6f,.4f,.7f),_gold); }
            Table(new Vector3(-11,0,18)); Table(new Vector3(-15,0,24));
            Item(HuntItem.Mirror,"Examine broken mirror","MIRROR: Glass scattered outward. No footprints.",new Vector3(-11,1.15f,18),_blue);
            Item(HuntItem.Photos,"Inspect photographs","PHOTOS: The same brass necklace appears in three rooms.",new Vector3(-15,1.15f,24),_cream);
            Item(HuntItem.Ledger,"Read estate ledger","LEDGER: Estate lot / St. Marais / necklace, no provenance.",new Vector3(-12,1.1f,28),_wood);
            Item(HuntItem.Scratches,"Inspect soot residue","RESIDUE: Soot in claw marks. A spirit bound to an object.",new Vector3(-18.4f,1.3f,27),_metal,true);
            Item(HuntItem.Iron,"Take iron poker","IRON: A fireplace poker disperses spirits. Equip with [3].",new Vector3(-10,1,27),_metal);
            LightAt("Shop amber",new Vector3(-12,3.5f,23),new Color(1,.55f,.22f),2.3f,12);
            // Cemetery on east side, accessible from the central street.
            Box("Cemetery court",new Vector3(14,.02f,44),new Vector3(15,.15f,18),_stone);
            for(int x=11;x<=19;x+=4) for(int z=39;z<=49;z+=5)
            {
                Box("Raised tomb",new Vector3(x,.8f,z),new Vector3(2,1.6f,2.8f),_cream);
                Box("Tomb cornice",new Vector3(x,1.65f,z),new Vector3(2.2f,.2f,3),_cream);
                Box("Cross",new Vector3(x,2.2f,z),new Vector3(.14f,.9f,.15f),_metal);
                Box("Cross arms",new Vector3(x,2.35f,z),new Vector3(.6f,.13f,.15f),_metal);
            }
            Sign("ST. MARAIS\nCEMETERY",new Vector3(8,2.5f,35),new Vector3(0,180,0),4,.55f);
            Table(new Vector3(10,0,34));
            Item(HuntItem.Record,"Read burial record","RECORD: The grave is empty. Her necklace was never recovered. It is the anchor.",new Vector3(10,1.15f,34),_cream);
            Item(HuntItem.Salt,"Take rock salt","SALT: Contain the necklace, then burn it. Return to the cellar.",new Vector3(12,1,34),_cream);
            // Final courtyard/cellar compression keeps the first slice traversable.
            Box("Cellar floor",new Vector3(0,.02f,64),new Vector3(13,.15f,16),_stone);
            Box("Cellar left",new Vector3(-6.5f,2,64),new Vector3(.5f,4,16),_brick);
            Box("Cellar right",new Vector3(6.5f,2,64),new Vector3(.5f,4,16),_brick);
            Box("Cellar back",new Vector3(0,2,72),new Vector3(13,4,.4f),_brick);
            Sign("MERCER ESTATE\nCELLAR",new Vector3(0,3,56),new Vector3(0,180,0),5,.5f);
            Table(new Vector3(0,0,65));
            Item(HuntItem.Anchor,"Salt and burn necklace","The necklace is the anchor.",new Vector3(0,1.15f,65),_gold);
            var circle = new GameObject("Salt circle");
            for(int i=0;i<32;i++) { float a=i*Mathf.PI/16; var p=Box("Salt",new Vector3(Mathf.Cos(a)*1.5f,.13f,65+Mathf.Sin(a)*1.5f),new Vector3(.16f,.025f,.16f),_cream); p.transform.SetParent(circle.transform); Object.DestroyImmediate(p.GetComponent<Collider>()); }
            var flame = LightAt("Anchor flame",new Vector3(0,1.7f,65),new Color(1,.3f,.04f),5,7).gameObject;
            // Impala silhouette and exit at starting point.
            var car = new GameObject("1967 Impala - blockout");
            Part(car.transform,"Body",new Vector3(3,.65f,2),new Vector3(2.1f,.65f,4.8f),_metal);
            Part(car.transform,"Cabin",new Vector3(3,1.25f,1.7f),new Vector3(1.8f,.65f,2.3f),road);
            for(int x=0;x<2;x++) for(int z=0;z<2;z++) Part(car.transform,"Wheel",new Vector3(2+x*2,.4f,.4f+z*3),new Vector3(.3f,.65f,.65f),_metal);
            Item(HuntItem.Exit,"Return to the Impala","Call John.",new Vector3(1.6f,1,2),_metal);
            var definition=ScriptableObject.CreateInstance<HuntDefinition>(); AssetDatabase.CreateAsset(definition,Root+"/Data/SO_PrologueHunt.asset");
            _session = new GameObject("Hunt Session").AddComponent<HuntSession>(); _session.definition=definition; _session.saltCircle=circle; _session.fire=flame;
            _session.sound=_session.gameObject.AddComponent<HuntAudio>(); circle.SetActive(false); flame.SetActive(false);
            Player(); Ghost(); UI(); _session.items=Items.ToArray();
            var moon=new GameObject("Moon").AddComponent<Light>(); moon.type=LightType.Directional; moon.color=new Color(.48f,.65f,.85f); moon.intensity=.75f; moon.transform.rotation=Quaternion.Euler(45,-25,0); moon.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.13f,.17f,.22f);
            RenderSettings.fog=true; RenderSettings.fogColor=new Color(.06f,.09f,.12f); RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=.018f;
            var volume=new GameObject("Atmosphere").AddComponent<Volume>(); volume.isGlobal=true;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile,Root+"/Data/Atmosphere.asset"); volume.sharedProfile=profile;
            HdrpAuthoring.Atmosphere(profile);
            HdrpAuthoring.FinishScene("SCN_NewOrleans_Slice", true);
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/SCN_NewOrleans_Slice.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(scene.path,true)};
            AssetDatabase.SaveAssets();
            Debug.Log("BEFORE JERICHO: Prologue scene created with " + Items.Count + " interactions.");
        }
        static Material Material(string name,Color color,float smooth=0,Color? emission=null)
        {
            var m=HdrpAuthoring.Lit(name, color, smooth, emission);
            AssetDatabase.CreateAsset(m,Root+"/Art/Materials/"+name+".mat");return m;
        }
        static GameObject Box(string name,Vector3 position,Vector3 scale,Material m)
        { var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.position=position;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;return g; }
        static GameObject Part(Transform parent,string name,Vector3 p,Vector3 s,Material m)
        { var g=Box(name,p,s,m);g.transform.SetParent(parent,false); Object.DestroyImmediate(g.GetComponent<Collider>());g.layer=2;return g; }
        static Light LightAt(string name,Vector3 p,Color color,float intensity,float range)
        { var l=new GameObject(name).AddComponent<Light>();l.transform.position=p;l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;return l; }
        static void Lamp(Vector3 p)
        { Box("Lamp post",p+Vector3.up*1.8f,new Vector3(.1f,3.6f,.1f),_metal); Box("Lantern",p+Vector3.up*3.7f,new Vector3(.3f,.5f,.3f),_gold);LightAt("Gas lamp",p+Vector3.up*3.5f,new Color(1,.57f,.24f),3,9); }
        static void Facade(float x,float z,float depth)
        {
            Box("Townhouse",new Vector3(x,4,z),new Vector3(10,8,depth),_brick);
            float side=x>0 ? x-5.1f : x+5.1f;
            for(int i=0;i<3;i++) { Box("Shutter window",new Vector3(side,4.8f,z-depth*.3f+i*depth*.3f),new Vector3(.12f,1.8f,1),_wood); }
            Box("Balcony",new Vector3(side,3.4f,z),new Vector3(1.8f,.2f,depth),_metal);
            for(float zz=z-depth/2;zz<z+depth/2;zz+=.5f) Box("Iron railing",new Vector3(side+(x>0?-1:1),4,zz),new Vector3(.06f,1.1f,.06f),_metal);
        }
        static void Table(Vector3 p) { Box("Worktable",p+Vector3.up*.95f,new Vector3(2.2f,.2f,1.2f),_wood); for(int x=-1;x<=1;x+=2) Box("Table leg",p+new Vector3(x*.8f,.45f,0),new Vector3(.15f,.9f,.8f),_wood); }
        static void Sign(string text,Vector3 p,Vector3 rotation,float width,float size)
        {
            var g=new GameObject("Sign - "+text);g.transform.position=p;g.transform.eulerAngles=rotation;
            var t=g.AddComponent<TextMeshPro>(); t.font=_font;t.text=text;t.fontSize=size*10;t.alignment=TextAlignmentOptions.Center;t.color=new Color(.9f,.77f,.5f);t.rectTransform.sizeDelta=new Vector2(width,2);
        }
        static void Item(HuntItem type,string label,string description,Vector3 p,Material m,bool light=false)
        {
            var g=Box("Interact - "+type,p,new Vector3(.5f,.2f,.4f),m);var item=g.AddComponent<HuntInteractable>();item.kind=type;item.label=label;item.description=description;item.needsLight=light;
            var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);marker.name="Evidence marker";marker.transform.SetParent(g.transform);marker.transform.position=p+Vector3.up*.65f;marker.transform.localScale=new Vector3(.16f,.4f,.2f);marker.GetComponent<Renderer>().sharedMaterial=_gold;Object.DestroyImmediate(marker.GetComponent<Collider>());item.marker=marker.transform;Items.Add(item);
        }
        static void Player()
        {
            var g=new GameObject("Dean Winchester");g.layer=2;g.transform.position=new Vector3(0,.1f,3);
            var cc=g.AddComponent<CharacterController>();cc.height=1.8f;cc.center=Vector3.up*.9f;cc.radius=.32f;cc.stepOffset=.25f;
            g.AddComponent<HunterInput>();var motor=g.AddComponent<HunterMotor>();
            var body=new GameObject("Dean placeholder body").transform;body.SetParent(g.transform,false);motor.body=body;
            Part(body,"Leather jacket",new Vector3(0,1.15f,0),new Vector3(.58f,.65f,.3f),_jacket);
            Part(body,"Head",new Vector3(0,1.7f,0),new Vector3(.27f,.32f,.27f),_skin);
            Part(body,"Hair",new Vector3(0,1.88f,0),new Vector3(.29f,.08f,.29f),_wood);
            for(int i=-1;i<=1;i+=2) { Part(body,"Jeans",new Vector3(i*.16f,.5f,0),new Vector3(.22f,.75f,.25f),_metal);Part(body,"Boot",new Vector3(i*.16f,.1f,.06f),new Vector3(.25f,.2f,.4f),_wood);Part(body,"Sleeve",new Vector3(i*.38f,1.1f,.07f),new Vector3(.2f,.55f,.22f),_jacket); }
            var combat=g.AddComponent<HunterCombat>();
            combat.handgun=Part(body,"1911 placeholder",new Vector3(.4f,1.03f,.4f),new Vector3(.1f,.15f,.38f),_metal);
            combat.knife=Part(body,"Knife placeholder",new Vector3(.4f,1.03f,.4f),new Vector3(.07f,.07f,.5f),_cream);
            combat.iron=Part(body,"Iron poker",new Vector3(.4f,1.03f,.6f),new Vector3(.05f,.05f,1),_metal); combat.knife.SetActive(false);combat.iron.SetActive(false);
            var muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(g.transform);muzzle.localPosition=new Vector3(.4f,1.05f,.65f);combat.muzzle=muzzle;
            var light=LightAt("Lighter",new Vector3(0,0,0),new Color(1,.48f,.13f),4,3.5f);light.transform.SetParent(g.transform);light.transform.localPosition=new Vector3(-.4f,1.2f,.3f);light.enabled=false;combat.lighter=light;
            var flash=LightAt("Muzzle flash",Vector3.zero,new Color(1,.7f,.3f),5,4);flash.transform.SetParent(muzzle,false);flash.enabled=false;combat.muzzleFlash=flash;
            // Save actor prefab before scene references are assigned.
            PrefabUtility.SaveAsPrefabAsset(g,Root+"/Prefabs/Actors/PF_Dean.prefab");
            var cameraGO=new GameObject("OTS Camera");var camera=cameraGO.AddComponent<Camera>();cameraGO.tag="MainCamera";cameraGO.AddComponent<AudioListener>();camera.nearClipPlane=.05f;camera.farClipPlane=150;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.05f,.07f,.1f);
            var view=cameraGO.AddComponent<ShoulderCamera>();view.player=motor;view.session=_session;view.lens=camera;
            motor.view=view;motor.session=_session;combat.motor=motor;combat.view=view;combat.session=_session;_session.player=motor;
        }
        static void Ghost()
        {
            var g=new GameObject("Vengeful Spirit");g.transform.position=new Vector3(0,1.3f,64);g.layer=8;
            var collider=g.AddComponent<CapsuleCollider>();collider.height=2;collider.radius=.5f;
            var ghost=g.AddComponent<GhostHunt>();var body=new GameObject("Apparition").transform;body.SetParent(g.transform,false);
            Part(body,"Dress",new Vector3(0,-.3f,0),new Vector3(.7f,1.5f,.45f),_blue);Part(body,"Pale face",new Vector3(0,.65f,0),new Vector3(.33f,.4f,.3f),_cream);
            Part(body,"Dark hair",new Vector3(0,.68f,-.13f),new Vector3(.45f,.7f,.2f),_metal);
            var glow=LightAt("Ghost glow",Vector3.zero,new Color(.2f,.8f,.85f),2,4);glow.transform.SetParent(g.transform,false);glow.enabled=false;
            ghost.apparition=body;ghost.glow=glow;PrefabUtility.SaveAsPrefabAsset(g,Root+"/Prefabs/Actors/PF_Ghost.prefab");ghost.session=_session;_session.ghost=ghost;body.gameObject.SetActive(false);
        }
        static void UI()
        { UIPrefabBuilder.PlaceInScene(_session); } // PF_HuntUI: the HTML-prototype UI (screens, HUD, options); keeps one EventSystem
    }
}
