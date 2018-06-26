using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityCommon;

namespace UnityScene
{
    public class SceneConst
    {
        public const float LOADSCENEPROCESS = 0.5f;
        public const float UNSCENEPROCESS = 0.6f;
        public const float INITNETPROCESS = 0.7f;
        public const float INITCONFIGPROCESS = 0.8f;
        public const float INITSCENEPROCESS = 0.9f;
        public const string SceneName = "";
    }

    public class SceneMgr : MonoSingleton<SceneMgr>
    {
        /*条件：保证场景中有个不销毁的场景,激活场景非不销毁的场景
        Loading both Synchronous and Asynchronous happens on a thread. The thread loads all the required assets and 
        then it loads all the objects from the scene, when it is done progress is 0.9 or 90%, the last 10% is the
        awake on the main thread. So when you set allowSceneActivation to false, the last10% are never done before
        you set it back to true. This is why scene.isLoaded never becomes true.  Another thing, in your script you
        have to set nextScene active before you unload the previous scene otherwise Unload will set the next scene
        in the SceneManager as active which might not be the scene you want.
        同时加载和异步加载发生在线程上。 线程加载所有必需的资产和
         那么它会加载场景中的所有对象，当它完成时，进度是0.9或90％，最后10％是
         在主线上醒来。 因此，当您将allowSceneActivation设置为false时，最后的10％永远不会完成
         你将其设置回true。 这就是为什么scene.isLoaded永远不会成为现实。 另一件事，在你的脚本你
         必须在卸载前一个场景之前设置nextScene激活，否则Unload将设置下一个场景
         在SceneManager中处于活动状态，这可能不是您想要的场景。
        //  UnityEngine.SceneManagement.SceneManager.activeSceneChanged   Subscribe to this event to get notified when the active Scene has changed.
        //  UnityEngine.SceneManagement.SceneManager.sceneLoaded    Add a delegate to this to get notifications when a Scene has loaded.
        //  UnityEngine.SceneManagement.SceneManager.sceneUnloaded   Add a delegate to this to get notifications when a Scene has unloaded

        加载进度条暂时分五个阶段：
        场景加载，卸载上个场景，请求网络数据，请求或者读取配置文件，实例化
        流程：
        1.加载场景
        2.初始化时加载配置文件，根据配置文件加载动态资源
        3.请求初始化数据
        第2和3由每个场景初始化类来完成

        细节：如果处理分阶段的进度显示,配置文件本地和远程如何处理
         */


        private bool isLoading;
        AsyncOperation unloadAsync;
        AsyncOperation loadAsync;
        public System.Action<float> FreshProcessEvent;
        public event Action<string> SceneUnLoadEvent;
        public event Action<string> SceneLoadedEvent;

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnSceneChanged;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnLoaded;
        }


        private void OnSceneUnLoaded(Scene arg0)
        {
            Debug.Assert(true, "场景卸载成功" + arg0.name);
            // SceneUnLoadEvent?.Invoke(arg0.name);
        }


        /// <summary>
        /// 在场景所有awake执行后
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            Debug.Log("awake场景加载成功" + arg0.name);
            //  SceneLoadedEvent?.Invoke(arg0.name);
        }

        void OnSceneChanged(Scene s1, Scene s2)
        {
            Debug.Log(s1.name + "切换到" + s2.name);
        }

        public IEnumerator LoadScene(string nextScene)
        {
            Scene curScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            if (string.IsNullOrEmpty(nextScene) || isLoading || curScene.name.Equals(nextScene)) yield break;

            isLoading = true;

            yield return StartCoroutine(AsyncLoadScene(nextScene));
            Debug.Log(curScene.name);
            unloadAsync = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(curScene);

            FreshProcessEvent.Invoke(SceneConst.UNSCENEPROCESS);
            //  System.GC.Collect();
             Resources.UnloadUnusedAssets();
            //2017版本，如果切换场景不进行Resources.UnloadUnusedAssets()，尽快回收资源，但是材质所引用的贴图可能无法切换一次场景
            //回收，它仍然被ugui的事件UnityEngine.EventSystems.StandaloneInputModule和UnityEngine.EventSystems引用
            //也可能只是被材质引用
            //   UnityEngine.SceneManagement.SceneManager.LoadScene("xxx")直接切换场景时，上面的问题不存在
            yield return unloadAsync;

            SceneBase sceneLoader = UnityTool.GetTopObject("SceneLoader").GetComponent<SceneBase>();

            FreshProcessEvent.Invoke(SceneConst.INITCONFIGPROCESS);

            yield return new WaitUntil(sceneLoader.FinishConfigRes);

            FreshProcessEvent.Invoke(SceneConst.INITSCENEPROCESS);

            yield return new WaitUntil(sceneLoader.FinishGetData);

            FreshProcessEvent.Invoke(1f);

            yield return new WaitUntil(sceneLoader.FinishInit);

            FreshProcessEvent.Invoke(1f);

            isLoading = false;

        }

        IEnumerator AsyncLoadScene(string sceneName)
        {
            loadAsync = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!loadAsync.isDone)
            {
                //更新进度

                FreshProcessEvent?.Invoke(loadAsync.progress * SceneConst.LOADSCENEPROCESS);
                yield return null;
            }
            Debug.Log("==============");
            yield return loadAsync;

            UnityEngine.SceneManagement.SceneManager.SetActiveScene(UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName));

        }

        /// <summary>
        /// 场景要用buildplayer
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerator LoadWWWScene(string sceneName, string path)
        {
            WWW www = new WWW(path);
            yield return www;
            var asset = www.assetBundle;
            loadAsync = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            yield return loadAsync;
        }


        /*
         // called zero
    void Awake()
    {
        Debug.Log("Awake");
    }

    // called first
    void OnEnable()
    {
        Debug.Log("OnEnable called");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // called second
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("OnSceneLoaded: " + scene.name);
        Debug.Log(mode);
    }

    // called third
    void Start()
    {
        Debug.Log("Start");
    }

    // called when the game is terminated
    void OnDisable()
    {
        Debug.Log("OnDisable");
        SceneManager.sceneLoaded -= OnSceneLoaded;
    } 
        */

    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace UnityScene
{
    public class LoadingView : MonoBehaviour
    {
        [SerializeField]
        private Slider slider;
        [SerializeField]
        private Text tips;
        [SerializeField]
        private Text process;
        [SerializeField]
        private AnimationCurve curve;
        private float time;
        private bool isLoading;
        private float processValue;
        private float lastProcessValue;
        private float dValue;
        private float difference = 0.0001f;
        private void Awake()
        {
            UnityScene.SceneMgr.Instance.FreshProcessEvent += Refresh;
            Close();
        }

        // Update is called once per frame
        void Update()
        {
            /*
             * 进度条显示策略：前30%在1秒内定值dValue曲线变化
             * 后面的进度，收到新的进度重置曲线时间轴，设置进度条初值为上次的进度值，变化值为（目标值-初值）*曲线值
             */

            if (isLoading)
            {
                time += Time.deltaTime;
                float cur = curve.Evaluate(time) * dValue;
                slider.value = lastProcessValue + cur;
                float sliderValue = slider.value * 100;
                process.text = (int)sliderValue + "%";
                if (Mathf.Abs(1 - slider.value) <= difference)
                {
                    float delayTime = time < curve.keys[1].time ? curve.keys[1].time - time + 0.2f : 0.2f;
                    Invoke("Close", delayTime);
                }
            }
        }

        private void Refresh(float value)
        {
            if (value <= 0 || value > 1) return;
            //test
            gameObject.SetActive(true);
            isLoading = true;

            if (value <= SceneConst.LOADSCENEPROCESS)
            {
                lastProcessValue = 0;
                processValue = value;
                dValue = SceneConst.LOADSCENEPROCESS;
            }
            else
            {
                time = 0;
                lastProcessValue = slider.value;
                processValue = value;
                dValue = processValue - lastProcessValue;
            }
        }

        void Close()
        {
            // Destroy(this);
            Debug.Log("------------");
            time = 0;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            UnityScene.SceneMgr.Instance.FreshProcessEvent -= Refresh;
        }

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace UnityScene
{
    public abstract class SceneBase : MonoBehaviour
    {
        //gameobj name需要用SceneLoader
        public System.Action<float> FreshDataProcessEvent;
        public System.Action<float> FreshConfigProcessEvent;

        private void Awake()
        {
            GameAwake();
        }


        protected virtual void GameAwake()
        {

        }

        public virtual bool FinishConfigRes()
        {
            return true;
        }

        public virtual bool FinishGetData()
        {
            return true;
        }

        public virtual bool FinishInit()
        {
            return true;
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityScene;
public class SceneTest : MonoBehaviour {
    AsyncOperation ap;
    // Use this for initialization
    void Start () {
       
    }

    // Update is called once per frame
    void Update () {
        if (Input.GetKeyDown(KeyCode.C))
        {
            // UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
           // UnityEngine.SceneManagement.SceneManager.LoadScene("Main",UnityEngine.SceneManagement.LoadSceneMode.Additive);
            ap= UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Main", UnityEngine.SceneManagement.LoadSceneMode.Additive);
            ap.allowSceneActivation = false;//相当于场景只是隐藏状态，等待激活（显示）
            //
            // LoadingManager.Instance.Load("Main");
            //  StartCoroutine(SceneMgr.Instance.LoadScene("Main"));
            //不能直接这边调用，否则会导致场景无法卸载完成。因为这个协程属于当前场景，所以当前场景要切换到下
            //一个场景时，这个场景的协程还在调用导致场景无法卸载
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            ap.allowSceneActivation = true;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityScene
{
    public class TestScene : SceneBase
    {
        private bool para1 = true;
        private bool para2 = true;
        protected override void GameAwake()
        {
            base.GameAwake();
        }
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                para1 = true;
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                para2 = true;
            }
        }

        public override bool FinishConfigRes()
        {
            return para1;
        }

        public override bool FinishGetData()
        {
            return para2;
        }
    }

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.C))
        {
            //LoadingManager.Instance.Load("Map");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Map");

            //  StartCoroutine(SceneMgr.Instance.LoadScene("Main"));
            //不能直接这边调用，否则会导致场景无法卸载完成。因为这个协程属于当前场景，所以当前场景要切换到下
            //一个场景时，这个场景的协程还在调用导致场景无法卸载
        }
    }
}


