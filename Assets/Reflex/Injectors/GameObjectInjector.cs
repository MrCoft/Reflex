using System.Collections.Generic;
using Reflex.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace Reflex.Injectors
{
    public static class GameObjectInjector
    {
        public static void InjectSingle(GameObject gameObject, Container container)
        {
            if (gameObject.TryGetComponent<MonoBehaviour>(out var monoBehaviour))
            {
                AttributeInjector.Inject(monoBehaviour, container);
            }
        }

        public static void InjectObject(GameObject gameObject, Container container)
        {
            using var pooledObject = ListPool<MonoBehaviour>.Get(out var monoBehaviours);
            gameObject.GetComponents<MonoBehaviour>(monoBehaviours);

            for (var i = 0; i < monoBehaviours.Count; i++)
            {
                var monoBehaviour = monoBehaviours[i];

                if (monoBehaviour != null)
                {
                    AttributeInjector.Inject(monoBehaviour, container);
                }
            }
        }

        public static void InjectRecursive(GameObject gameObject, Container container)
        {
            using var pooledObject = ListPool<MonoBehaviour>.Get(out var monoBehaviours);
            gameObject.GetComponentsInChildren<MonoBehaviour>(true, monoBehaviours);

            for (var i = 0; i < monoBehaviours.Count; i++)
            {
                var monoBehaviour = monoBehaviours[i];

                if (monoBehaviour != null)
                {
                    AttributeInjector.Inject(monoBehaviour, container);
                }
            }
        }

        public static void InjectRecursiveMany(List<GameObject> gameObjects, Container container)
        {
            using var pooledObject = ListPool<MonoBehaviour>.Get(out var monoBehaviours);
            using var pooledGameObjectScope = ListPool<GameObjectScope>.Get(out var gameObjectScopes);
            using var pooledGameObjectScopeTree = HashSetPool<Transform>.Get(out var gameObjectScopeTree);

            for (var i = 0; i < gameObjects.Count; i++)
            {
                var gameObject = gameObjects[i];
                
                gameObject.GetComponentsInChildren<GameObjectScope>(true, gameObjectScopes);
                
                gameObjectScopeTree.Clear();
                for (var j = 0; j < gameObjectScopes.Count; j++)
                {
                    var treeNode = gameObjectScopes[j].transform;

                    while (treeNode != null)
                    {
                        gameObjectScopeTree.Add(treeNode);
                        treeNode = treeNode.parent;
                    }
                }
                
                InjectRecursiveWithPossibleGameObjectScopes(
                    gameObject,
                    container,
                    monoBehaviours,
                    gameObjectScopeTree);
            }
        }

        public static void InjectRecursiveWithPossibleGameObjectScopes(
            GameObject gameObject,
            Container container,
            List<MonoBehaviour> monoBehaviours,
            HashSet<Transform> gameObjectScopeTree)
        {
            if (gameObjectScopeTree.Contains(gameObject.transform))
            {
                var gameObjectScope = gameObject.GetComponent<GameObjectScope>();
                
                if (gameObjectScope != null)
                {
                    InjectGameObjectScope(gameObjectScope, container);
                    return;
                }
                
                // todo - include inactive?
                gameObject.GetComponents<MonoBehaviour>(monoBehaviours);

                for (var i = 0; i < monoBehaviours.Count; i++)
                {
                    var monoBehaviour = monoBehaviours[i];

                    if (monoBehaviour != null)
                    {
                        AttributeInjector.Inject(monoBehaviour, container);
                    }
                }
                
                var childrenCount = gameObject.transform.childCount;
                for (var i = 0; i < childrenCount; i++)
                {
                    InjectRecursiveWithPossibleGameObjectScopes(
                        gameObject.transform.GetChild(i).gameObject,
                        container,
                        monoBehaviours,
                        gameObjectScopeTree);
                }
            }
            else
            {
                gameObject.GetComponentsInChildren<MonoBehaviour>(monoBehaviours);

                for (var i = 0; i < monoBehaviours.Count; i++)
                {
                    var monoBehaviour = monoBehaviours[i];

                    if (monoBehaviour != null)
                    {
                        AttributeInjector.Inject(monoBehaviour, container);
                    }
                }
            }
        }
        
        private static void InjectGameObjectScope(GameObjectScope gameObjectScope, Container container)
        {
            using var pooledMonoBehaviours = ListPool<MonoBehaviour>.Get(out var monoBehaviours);
            gameObjectScope.GetComponents<MonoBehaviour>(monoBehaviours);
            
            for (var i = 0; i < monoBehaviours.Count; i++)
            {
                var monoBehaviour = monoBehaviours[i];

                if (monoBehaviour != null && monoBehaviour is IInstaller)
                {
                    AttributeInjector.Inject(monoBehaviour, container);
                }
            }
            
            var childContainer = container.Scope(builder =>
            {
                builder.SetName($"{gameObjectScope.name} ({gameObjectScope.GetHashCode()})");
                gameObjectScope.InstallBindings(builder);
                GameObjectScope.OnGameObjectContainerBuilding?.Invoke(gameObjectScope.gameObject, builder);
            });
            gameObjectScope.Container = childContainer;
            
            for (var i = 0; i < monoBehaviours.Count; i++)
            {
                var monoBehaviour = monoBehaviours[i];

                if (monoBehaviour != null && !(monoBehaviour is IInstaller))
                {
                    AttributeInjector.Inject(monoBehaviour, childContainer);
                }
            }
            
            using var pooledObject = ListPool<GameObject>.Get(out var children);
            var childrenCount = gameObjectScope.transform.childCount;
            for (var i = 0; i < childrenCount; i++)
            {
                children.Add(gameObjectScope.transform.GetChild(i).gameObject);
            }
            
            InjectRecursiveMany(children, childContainer);
        }
    }
}
