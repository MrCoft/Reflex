using Reflex.Logging;
using System;
using UnityEngine;
using UnityEngine.Pool;

namespace Reflex.Core
{
    public sealed class GameObjectScope : MonoBehaviour
    {
        public static Action<GameObject, ContainerBuilder> OnGameObjectContainerBuilding;

        public Container Container;
        
        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            using var pooledObject = ListPool<IInstaller>.Get(out var installers);
            GetComponentsInChildren<IInstaller>(installers);

            for (var i = 0; i < installers.Count; i++)
            {
                installers[i].InstallBindings(containerBuilder);
            }

            ReflexLogger.Log($"GameObjectScope ({gameObject.name}) Bindings Installed", LogLevel.Info, gameObject);
        }
    }
}
