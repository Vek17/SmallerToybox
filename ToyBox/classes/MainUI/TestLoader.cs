using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Cutscenes.Commands;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.JsonSystem.BinaryFormat;
using Kingmaker.Blueprints.JsonSystem.Converters;
using Kingmaker.BundlesLoading;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Modding;
using Kingmaker.Utility;
using ModKit;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static AkMIDIEvent;
using static Kingmaker.Blueprints.JsonSystem.BlueprintsCache;
using static Kingmaker.Kingdom.Settlements.SettlementGridTopology;

namespace ToyBox.classes.MainUI {
    internal class TestLoader {
        public class ThreadedJob {
            private bool m_IsDone = false;
            private bool m_IsRunning = false;
            private object m_Handle = new object();
            private Thread m_Thread = null;
            public bool IsDone {
                get {
                    bool tmp;
                    lock (m_Handle) {
                        tmp = m_IsDone;
                    }
                    return tmp;
                }
                set {
                    lock (m_Handle) {
                        m_IsDone = value;
                    }
                }
            }
            public bool IsRunning {
                get {
                    bool tmp;
                    lock (m_Handle) {
                        tmp = m_IsRunning;
                    }
                    return tmp;
                }
                set {
                    lock (m_Handle) {
                        m_IsRunning = value;
                    }
                }
            }

            public virtual void Start() {
                m_Thread = new System.Threading.Thread(Run);
                m_Thread.Start();
            }
            public virtual void Abort() {
                m_Thread.Abort();
            }

            protected virtual void ThreadFunction() { }

            protected virtual void OnFinished() { }

            public virtual bool Update() {
                if (IsDone) {
                    OnFinished();
                    return true;
                }
                return false;
            }
            public IEnumerator WaitFor() {
                while (!Update()) {
                    yield return null;
                }
            }
            private void Run() {
                IsRunning = true;
                ThreadFunction();
                IsDone = true;
                IsRunning = false;
            }
        }
        public class BlueprintCacheLoader : ThreadedJob {
            public readonly List<SimpleBlueprint> Blueprints = new List<SimpleBlueprint>(); // arbitary job data
            private object m_Handle = new object();
            private int Total = 1;
            private int Loaded = 0;
            private  float m_progress = 0;

            public float Progress {
                get {
                    float tmp;
                    lock (m_Handle) {
                        tmp = m_progress;
                    }
                    return tmp;
                }
                set {
                    lock (m_Handle) {
                        m_progress = value;
                    }
                }
            }

            protected void ThreadFunction2() {
                var bpCache = ResourcesLibrary.BlueprintsCache;
                if (bpCache == null) { return; }
                lock (bpCache) {
                    lock (Blueprints) {
                        var toc = bpCache.m_LoadedBlueprints;
                        Total = toc.Count();
                        var keys = toc.Keys.ToArray();
                        foreach (var guid in keys) {
                            Loaded++;
                            try {
                                Blueprints.Add(bpCache.Load(guid));
                            }
                            catch {
                                continue;
                            }
                            UpdateProgress(Loaded, Total);
                        }
                    }
                }
            }
            protected void ThreadFunctionOld() {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var bpCache = ResourcesLibrary.BlueprintsCache;
                if (bpCache == null) { return; }
                lock (bpCache) {
                    lock (Blueprints) {
                        var toc = bpCache.m_LoadedBlueprints;
                        Total = toc.Count();
                        var entries = toc.Values.OrderBy(v => v.Offset).ToArray();
                        var memStream = new MemoryStream();
                        bpCache.m_PackFile.Seek(0U, SeekOrigin.Begin);
                        bpCache.m_PackFile.CopyTo(memStream);
                        var Seralizer = new ReflectionBasedSerializer(new PrimitiveSerializer(new BinaryReader(memStream), UnityObjectConverter.AssetList));
                        foreach (var entry in entries) {
                            if (entry.Blueprint == null) {
                                if (entry.Offset == 0U) {
                                    continue;
                                }
                                using (ProfileScope.New("LoadBlueprint", ctx: null)) {
                                    memStream.Seek((long)((ulong)entry.Offset), SeekOrigin.Begin);
                                    SimpleBlueprint simpleBlueprint = null;
                                    Seralizer.Blueprint(ref simpleBlueprint);
                                    if (simpleBlueprint == null) {
                                        continue;
                                    }
                                    var guid = simpleBlueprint.AssetGuid;
                                    object obj;
                                    OwlcatModificationsManager.Instance.OnResourceLoaded(simpleBlueprint, guid.ToString(), out obj);
                                    simpleBlueprint = ((obj as SimpleBlueprint) ?? simpleBlueprint);
                                    var lookupEntry = toc[guid];
                                    lookupEntry.Blueprint = simpleBlueprint;
                                    lookupEntry.Blueprint.OnEnable();
                                    Blueprints.Add(lookupEntry.Blueprint);
                                }
                            }
                            else {
                                Blueprints.Add(entry.Blueprint);
                            }
                            Loaded++;
                            UpdateProgress(Loaded, Total);
                        }
                    }
                }
                watch.Stop();
                Mod.Log($"loaded {Blueprints.Count} blueprints in {watch.ElapsedMilliseconds} milliseconds");
            }
            protected override void ThreadFunction() {
                var watch = Stopwatch.StartNew();
                var bpCache = ResourcesLibrary.BlueprintsCache;
                if (bpCache == null) { return; }
                lock (bpCache) {
                    lock (Blueprints) {
                        var toc = bpCache.m_LoadedBlueprints;
                        //Total = toc.Count();
                        var entries = toc.Values.OrderBy(v => v.Offset).ToList();
                        var memStream = new MemoryStream();
                        bpCache.m_PackFile.Seek(0U, SeekOrigin.Begin);
                        bpCache.m_PackFile.CopyTo(memStream);
                        var byteBuffer = memStream.GetBuffer();
                        var chunks = SplitList(entries, 500);
                        var tasks = new List<Task<IEnumerable<SimpleBlueprint>>>();
                        Total = chunks.Count();
                        foreach (var chunk in chunks) {
                            tasks.Add(StartTask(chunk, byteBuffer));
                        }
                        //Task.WaitAll(tasks.ToArray());
                        do {
                            UpdateProgress(Loaded, Total);
                            //Mod.Log($"Count: {tasks.Count(t => t.IsCompleted)} of {Total}");
                            Thread.Sleep(50);
                        } while (tasks.Any(t => !t.IsCompleted));
                        tasks.ForEach(t => Blueprints.AddRange(t.Result));
                        /*
                        Parallel.ForEach(Blueprints, bp => {
                            var entry = bpCache.m_LoadedBlueprints[bp.AssetGuid];
                            entry.Blueprint = bp;
                            System.Object obj;
                            //bpCache.AddCachedBlueprint(bp.AssetGuid, bp);
                            OwlcatModificationsManager.Instance.OnResourceLoaded(bp, bp.AssetGuid.ToString(), out obj);
                        });
                        */
                    }
                }
                watch.Stop();
                Mod.Log($"loaded {Blueprints.Count} blueprints in {watch.ElapsedMilliseconds} milliseconds");
            }
            protected Task<IEnumerable<SimpleBlueprint>> StartTask(List<BlueprintCacheEntry> entries, byte[] byteBuff) {
                var DialogNamespace = typeof(BlueprintCue).Namespace;
                var CutsceneNamespace = typeof(CommandAction).Namespace;
                var CutsceneCommandsNamespace = typeof(CommandAddFact).Namespace;
                return Task<IEnumerable<SimpleBlueprint>>.Factory.StartNew(() => {
                    List<SimpleBlueprint> Blueprints = new List<SimpleBlueprint>(entries.Count());
                    //lock (Blueprints) {
                    var stream = new MemoryStream(byteBuff);
                    var Seralizer = new ReflectionBasedSerializer(new PrimitiveSerializer(new BinaryReader(stream), UnityObjectConverter.AssetList));
                    foreach (var entry in entries) {
                        if (entry.Blueprint == null) {
                            if (entry.Offset == 0U) {
                                continue;
                            }
                            //using (ProfileScope.New("LoadBlueprint", ctx: null)) {
                            stream.Seek(entry.Offset, SeekOrigin.Begin);
                            Type type = Seralizer.m_Primitive.ReadType();
                            if (type.Namespace == DialogNamespace) { continue; }
                            if (type.Namespace == CutsceneNamespace) { continue; }
                            if (type.Namespace == CutsceneCommandsNamespace) { continue; }
                            stream.Seek(entry.Offset, SeekOrigin.Begin);
                            SimpleBlueprint simpleBlueprint = null;
                            Seralizer.Blueprint(ref simpleBlueprint);
                            if (simpleBlueprint == null) {
                                continue;
                            }
                            simpleBlueprint.OnEnable();
                            Blueprints.Add(simpleBlueprint);
                            //}
                        }
                        else {
                            Blueprints.Add(entry.Blueprint);
                        } 
                    }
                    Interlocked.Increment(ref Loaded);
                    //}
                    return Blueprints;
                });
            }
            private  static IEnumerable<List<T>> SplitList<T>(List<T> bigList, int nSize = 1000) {
                int i = 0;
                for (; i < bigList.Count; i += nSize) {
                    yield return bigList.GetRange(i, Math.Min(nSize, bigList.Count - i));
                }
                if (i <= bigList.Count - i) {
                    yield return bigList.GetRange(i, bigList.Count - i);
                }
            }

            protected override void OnFinished() {
            }
            public IEnumerator WaitFor(Action<int, int> UpdateProgress) {
                UpdateProgress.Invoke(Total, Loaded);
                while (!Update()) {
                    yield return null;
                }
            }
            private void UpdateProgress(int loaded, int total) {
                if (total <= 0) {
                    Progress = 0.0f;
                    return;
                }
                Progress = loaded / (float)total;
            }
        }
        public class BlueprintCacheLoader2 : ThreadedJob {
            public ConcurrentBag<SimpleBlueprint> Blueprints = new ConcurrentBag<SimpleBlueprint>();
            private object m_Handle = new object();
            private int Total = 0;
            private int Loaded = 0;
            private float m_progress = 0;

            public float Progress {
                get {
                    float tmp;
                    lock (m_Handle) {
                        tmp = m_progress;
                    }
                    return tmp;
                }
                set {
                    lock (m_Handle) {
                        m_progress = value;
                    }
                }
            }
            
            protected override void ThreadFunction() {
                var bpCache = ResourcesLibrary.BlueprintsCache;
                if (bpCache == null) { return; }
                lock (bpCache) {
                    lock (Blueprints) {
                        var toc = bpCache.m_LoadedBlueprints;
                        Total = toc.Count();
                        var keys = toc.Keys.ToArray();
                        Parallel.ForEach(keys, guid => {
                            Interlocked.Increment(ref Loaded);
                            try {
                                Blueprints.Add(bpCache.Load(guid));
                            }
                            catch {
                                //continue;
                            }
                            UpdateProgress(Loaded, Total);
                        });
                        foreach (var guid in keys) {
                            
                        }
                    }
                }
            }
            protected override void OnFinished() {
                Mod.Log($"loaded {Loaded} blueprints");
            }
            public IEnumerator WaitFor(Action<int, int> UpdateProgress) {
                UpdateProgress.Invoke(Total, Loaded);
                while (!Update()) {
                    yield return null;
                }
            }
            private void UpdateProgress(int loaded, int total) {
                if (total <= 0) {
                    Progress = 0.0f;
                    return;
                }
                Progress = loaded / (float)total;
            }
        }
    }
}
