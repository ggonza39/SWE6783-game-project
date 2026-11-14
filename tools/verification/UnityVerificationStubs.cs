// Isolated verification host. These stand-ins do not simulate Unity physics or validate an actual Unity build.
using System;
using System.Collections.Generic;
namespace UnityEngine {
public class SerializeField:Attribute {} public class TooltipAttribute:Attribute {public TooltipAttribute(string s){}}
public class RangeAttribute:Attribute {public RangeAttribute(float a,float b){}}
public class DisallowMultipleComponent:Attribute {} public class DefaultExecutionOrder:Attribute {public DefaultExecutionOrder(int n){}}
public enum RuntimeInitializeLoadType {AfterSceneLoad} public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){}}
public class UnityException:Exception {}
public enum FindObjectsSortMode {None}
public class Object {
 public static List<Object> All=new List<Object>(); public string name; public bool destroyed;
 public Object(){All.Add(this);} public static T FindFirstObjectByType<T>() where T:Object {return All.Find(x=>x is T&&!x.destroyed) as T;}
 public static T[] FindObjectsByType<T>(FindObjectsSortMode s) where T:Object {return All.FindAll(x=>x is T&&!x.destroyed).ConvertAll(x=>(T)x).ToArray();}
 public static void DestroyImmediate(Object o)=>Destroy(o); public static void Destroy(Object o) {o.destroyed=true;if(o is GameObject g)foreach(var c in All)if(c is Component cc&&cc.gameObject==g)cc.destroyed=true;}
 public static bool operator ==(Object a,Object b){bool an=ReferenceEquals(a,null)||(!ReferenceEquals(a,null)&&a.destroyed);bool bn=ReferenceEquals(b,null)||(!ReferenceEquals(b,null)&&b.destroyed);return an&&bn||!an&&!bn&&ReferenceEquals(a,b);}public static bool operator !=(Object a,Object b)=>!(a==b);
 public override bool Equals(object o)=>ReferenceEquals(this,o); public override int GetHashCode()=>base.GetHashCode(); public static void DontDestroyOnLoad(Object o){}
}
public class Component:Object {
 public GameObject gameObject; public Transform transform=>gameObject.transform;
 public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();
 public T GetComponentInParent<T>() where T:Component {var c=GetComponent<T>();return c??transform.parent?.GetComponentInParent<T>();}
 public bool CompareTag(string s)=>gameObject.tag==s;
}
public class MonoBehaviour:Component {public bool enabled=true;}
public class GameObject:Object {
 public string tag;public bool activeSelf=true;public UnityEngine.SceneManagement.Scene scene=UnityEngine.SceneManagement.SceneManager.Active;
 public Transform transform;private Dictionary<Type,Component> components=new Dictionary<Type,Component>();
 public GameObject(string name){this.name=name;transform=new Transform{gameObject=this};components[typeof(Transform)]=transform;}
 public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};components[typeof(T)]=c;return c;}
 public T GetComponent<T>() where T:Component {foreach(var c in components.Values)if(c is T)return c as T;return null;}
 public T GetComponentInParent<T>() where T:Component=>transform.GetComponentInParent<T>();
 public static GameObject Find(string s)=>All.Find(x=>x is GameObject g&&g.name==s&&g.activeSelf) as GameObject;
 public static GameObject FindWithTag(string s)=>All.Find(x=>x is GameObject g&&g.tag==s&&g.activeSelf) as GameObject;
 public static GameObject[] FindGameObjectsWithTag(string s)=>All.FindAll(x=>x is GameObject g&&g.tag==s&&g.activeSelf).ConvertAll(x=>(GameObject)x).ToArray();
 public void SetActive(bool v){activeSelf=v;}
}
public class Transform:Component {
 public Transform parent;public Vector3 localPosition;public Vector3 localScale=Vector3.one;public Quaternion rotation=Quaternion.identity;
 public Vector3 position {get=>parent==null?localPosition:parent.position+localPosition;set=>localPosition=parent==null?value:value-parent.position;}
 public Vector3 lossyScale=>parent==null?localScale:new Vector3(parent.lossyScale.x*localScale.x,parent.lossyScale.y*localScale.y,1);
 public Vector3 eulerAngles=>new Vector3(0,0,rotation.angle); public Vector3 right=>rotation*Vector3.right;
 public void SetParent(Transform p,bool world=false){parent=p;}public Transform Find(string s)=>Object.All.Find(x=>x is Transform t&&t.parent==this&&t.gameObject.name==s) as Transform;
 public void Translate(Vector3 v){position+=v;} public bool IsChildOf(Transform t){return this==t||(parent!=null&&parent.IsChildOf(t));}
}
public struct Vector2 {
 public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2();public static Vector2 right=>new Vector2(1,0);
 public float sqrMagnitude=>x*x+y*y;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector2 normalized=>magnitude>0?this/magnitude:zero;
 public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);
 public static Vector2 operator *(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);public static Vector2 operator /(Vector2 a,float b)=>new Vector2(a.x/b,a.y/b);
 public static implicit operator Vector2(Vector3 a)=>new Vector2(a.x,a.y);public static implicit operator Vector3(Vector2 a)=>new Vector3(a.x,a.y,0);
 public static float Distance(Vector2 a,Vector2 b)=>(a-b).magnitude;public static float Angle(Vector2 a,Vector2 b)=>a.magnitude*b.magnitude==0?0:(float)(Math.Acos(Math.Clamp((a.x*b.x+a.y*b.y)/(a.magnitude*b.magnitude),-1,1))*180/Math.PI);
 public static Vector2 MoveTowards(Vector2 a,Vector2 b,float d)=>Distance(a,b)<=d?b:a+(b-a).normalized*d;
}
public struct Vector2Int {public int x,y;public Vector2Int(int x,int y){this.x=x;this.y=y;}}
public struct Vector3 {
 public float x,y,z;public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 right=>new Vector3(1,0,0);
 public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
 public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator *(Vector3 a,float f)=>new Vector3(a.x*f,a.y*f,a.z*f);
 public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;public static Vector3 MoveTowards(Vector3 a,Vector3 b,float d)=>Vector2.MoveTowards(a,b,d);
}
public struct Quaternion {
 public float angle;public static Quaternion identity=>new Quaternion();public static Quaternion Euler(float x,float y,float z)=>new Quaternion{angle=z};
 public static Vector3 operator *(Quaternion q,Vector3 p){double a=q.angle*Math.PI/180;return new Vector3((float)(p.x*Math.Cos(a)-p.y*Math.Sin(a)),(float)(p.x*Math.Sin(a)+p.y*Math.Cos(a)),p.z);}
}
public struct Rect {
 public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}
 public float xMin=>x;public float yMin=>y;public float xMax=>x+width;public float yMax=>y+height;public Vector2 center=>new Vector2(x+width/2,y+height/2);
 public bool Contains(Vector2 a)=>a.x>=x&&a.x<=xMax&&a.y>=y&&a.y<=yMax;
}
public struct Bounds {
 public Vector3 center,size;public Bounds(Vector3 p,Vector3 s){center=p;size=s;}public Vector3 min=>center-size*.5f;public Vector3 max=>center+size*.5f;
 public void Expand(Vector3 v){size+=v;}public void Encapsulate(Bounds b){var lo=new Vector3(Math.Min(min.x,b.min.x),Math.Min(min.y,b.min.y));var hi=new Vector3(Math.Max(max.x,b.max.x),Math.Max(max.y,b.max.y));center=(lo+hi)*.5f;size=hi-lo;}
}
public class Collider2D:Component {public bool enabled=true,isTrigger;}
public class BoxCollider2D:Collider2D {public Vector2 size=new Vector2(1,1),offset;public Bounds bounds=>new Bounds(transform.position,new Vector3(Math.Abs(size.x*transform.lossyScale.x),Math.Abs(size.y*transform.lossyScale.y)));}
public class Rigidbody2D:Component {public float gravityScale;public bool freezeRotation;public Vector2 linearVelocity;}
public struct RaycastHit2D {public Collider2D collider;public float distance;}
public static class Physics2D {
 public static HashSet<(Collider2D,Collider2D)> Ignored=new HashSet<(Collider2D,Collider2D)>();
 public static void SyncTransforms(){} public static void IgnoreCollision(Collider2D a,Collider2D b,bool ignore){if(ignore)Ignored.Add((a,b));else{Ignored.Remove((a,b));Ignored.Remove((b,a));}}
 public static bool GetIgnoreCollision(Collider2D a,Collider2D b)=>Ignored.Contains((a,b))||Ignored.Contains((b,a));
 public static int RaycastNonAlloc(Vector2 p,Vector2 d,RaycastHit2D[] b,float r)=>0;public static RaycastHit2D[] RaycastAll(Vector2 p,Vector2 d,float r)=>new RaycastHit2D[0];
}
public struct Color {
 public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}
 public static Color green=>new Color(0,1,0);public static Color blue=>new Color(0,0,1);public static Color white=>new Color(1,1,1);public static Color red=>new Color(1,0,0);public static Color yellow=>new Color(1,1,0);public static Color cyan=>new Color(0,1,1);public static Color gray=>new Color(.5f,.5f,.5f);
}
public struct Color32 {public byte r,g,b,a;public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}}
public enum TextureFormat {RGBA32} public enum FilterMode {Point} public enum TextureWrapMode {Clamp} public enum SpriteMeshType {FullRect}
public class Texture2D:Object {
 public int width=1,height=1;public FilterMode filterMode;public TextureWrapMode wrapMode;public static Texture2D whiteTexture=new Texture2D(1,1);
 public Texture2D(int w,int h){width=w;height=h;}public Texture2D(int w,int h,TextureFormat f,bool mip):this(w,h){}
 public void SetPixel(int x,int y,Color c){}public void SetPixels(Color[] c){}public void SetPixels32(Color32[] p){}public void Apply(bool a=false,bool b=false){}
}
public class Sprite:Object {public static Sprite Create(Texture2D t,Rect r,Vector2 p,float unit,uint e=0,SpriteMeshType m=SpriteMeshType.FullRect)=>new Sprite();}
public class SpriteRenderer:Component {public Sprite sprite;public Color color;public int sortingOrder,sortingLayerID;public bool enabled=true;}
public static class Resources {public static T Load<T>(string p) where T:class {if(typeof(T)==typeof(Texture2D))return new Texture2D(1254,1254){name=p} as T;if(typeof(T)==typeof(Sprite))return new Sprite() as T;return null;}}
public class Camera:MonoBehaviour {public static Camera main;public float orthographicSize=5.4f;public float aspect=>Screen.width*rect.width/(Screen.height*rect.height);public Rect rect=new Rect(0,0,1,1);public Rect pixelRect=>new Rect(0,0,Screen.width,Screen.height);public Vector3 WorldToScreenPoint(Vector3 p)=>new Vector3(Screen.width/2+p.x*40,Screen.height/2+p.y*40,1);}
public enum TextAnchor {UpperLeft,UpperCenter,UpperRight,MiddleLeft,MiddleCenter,MiddleRight,LowerLeft,LowerCenter,LowerRight}
public enum FontStyle {Normal,Bold} public class RectOffset {public RectOffset(int l,int r,int t,int b){}}
public class GUIStyleState {public Color textColor;public Texture2D background;}
public class GUIStyle {
 public int fontSize;public bool stretchWidth,stretchHeight,wordWrap;public RectOffset padding;public TextAnchor alignment;public FontStyle fontStyle;
 public GUIStyleState normal=new GUIStyleState(),hover=new GUIStyleState(),active=new GUIStyleState(),focused=new GUIStyleState();
 public GUIStyle(){}public GUIStyle(GUIStyle s){}public static GUIStyle none=>new GUIStyle();
}
public class GUISkin {public GUIStyle label=new GUIStyle(),button=new GUIStyle();}
public class DrawCall {public string Kind,Text,Texture;public float X,Y,W,H,R,G,B,A,U,V,UW,VH;public int Font,Anchor;public bool Bold,Wrap;}
public class GUIContent {public static GUIContent none=new GUIContent();}
public enum ScaleMode {StretchToFill}
public static class GUI {
 public static Color color=Color.white;public static GUISkin skin=new GUISkin();public static List<DrawCall> Calls=new List<DrawCall>();
 public static void DrawTexture(Rect p,Texture2D t,ScaleMode mode,bool alpha){DrawTexture(p,t);} public static void DrawTexture(Rect p,Texture2D t){Calls.Add(new DrawCall{Kind="rect",X=p.x,Y=p.y,W=p.width,H=p.height,R=color.r,G=color.g,B=color.b,A=color.a});}
 public static void DrawTextureWithTexCoords(Rect p,Texture2D t,Rect u){Calls.Add(new DrawCall{Kind="texture",Texture=t.name,X=p.x,Y=p.y,W=p.width,H=p.height,U=u.x,V=u.y,UW=u.width,VH=u.height});}
 public static void Label(Rect p,string text,GUIStyle s){Calls.Add(new DrawCall{Kind="label",X=p.x,Y=p.y,W=p.width,H=p.height,Text=text,Font=s.fontSize,Anchor=(int)s.alignment,Bold=s.fontStyle==FontStyle.Bold,Wrap=s.wordWrap,R=s.normal.textColor.r,G=s.normal.textColor.g,B=s.normal.textColor.b,A=s.normal.textColor.a});}
 public static bool Button(Rect p,GUIContent c,GUIStyle s)=>false; public static bool Button(Rect p,string t,GUIStyle s){Label(p,t,s);return false;}public static void SetNextControlName(string s){}
}
public class Event {public static Event current=new Event();public Vector2 mousePosition;}
public static class Screen {public static int width=1280,height=720;}
public static class Time {public static float time,deltaTime=.016f,timeScale=1,unscaledTime=1;}
public enum KeyCode {C,LeftControl,RightControl,LeftShift,RightShift,E,Tab,R,Escape,Return,Space,Y,N,KeypadEnter}
public static class Input {public static HashSet<KeyCode> Keys=new HashSet<KeyCode>();public static bool GetKey(KeyCode k)=>Keys.Contains(k);public static bool GetKeyDown(KeyCode k)=>false;public static bool GetMouseButton(int n)=>false;public static float GetAxisRaw(string s)=>0;}
public static class Application {public static bool isPlaying=true,isBatchMode=false;}
public static class Debug {public static void Log(string s){}public static void LogWarning(string s){}public static void LogError(string s,Object c=null){}}
public static class Gizmos {public static Color color;public static void DrawWireSphere(Vector3 p,float r){}public static void DrawRay(Vector3 p,Vector3 d){}}
public static class Random {public static int Range(int a,int b)=>a;public static float Range(float a,float b)=>a;public static Vector2 insideUnitCircle=>new Vector2(.5f,.5f);}
public static class Mathf {
 public const float Deg2Rad=(float)Math.PI/180,Rad2Deg=180/(float)Math.PI,Epsilon=float.Epsilon;
 public static float Abs(float f)=>Math.Abs(f);public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);
 public static float Sin(float a)=>(float)Math.Sin(a);public static float Exp(float a)=>(float)Math.Exp(a);public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x);public static int FloorToInt(float a)=>(int)Math.Floor(a);public static float Ceil(float a)=>(float)Math.Ceiling(a);public static int RoundToInt(float a)=>(int)Math.Round(a);
 public static float Clamp(float a,float b,float c)=>Math.Clamp(a,b,c);public static float Clamp01(float a)=>Clamp(a,0,1);public static float Repeat(float a,float b)=>a-(float)Math.Floor(a/b)*b;
 public static float DeltaAngle(float a,float b){float d=Repeat(b-a,360);return d>180?d-360:d;}public static float LerpAngle(float a,float b,float t)=>a+DeltaAngle(a,b)*t;public static float MoveTowardsAngle(float a,float b,float d)=>a+Math.Clamp(DeltaAngle(a,b),-d,d);
}
}
namespace UnityEngine.SceneManagement {
 public class Scene {public string name="UXFacilityExpanded",path="Assets/Scenes/UXFacilityExpanded.unity";public int buildIndex=0;}
 public enum LoadSceneMode {Single} public static class SceneManager {public static Scene Active=new Scene();public static event Action<Scene,LoadSceneMode> sceneLoaded {add{}remove{}}public static Scene GetActiveScene()=>Active;public static void LoadScene(int n){} }
}
public class GameplayMetrics:UnityEngine.MonoBehaviour {public static GameplayMetrics Instance;public string PlayerID="test";public void RecordHiding(){}public void RecordEvidence(int n){}public void RecordTerminalInteraction(){}public void RecordGuardDetection(){}public void RecordCameraDetection(){}public void RecordRestart(){}public void StartSession(){}public void EndSession(string s){} }
public class DetectionConeRenderer:UnityEngine.MonoBehaviour {public IConeDetector Detector;}
