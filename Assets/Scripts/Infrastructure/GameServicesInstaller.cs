using System;
using System.Collections.Generic;
using FallowEarth.ResourcesSystem;
using UnityEngine;

namespace FallowEarth.Infrastructure
{
    public interface IGameServiceContainer
    {
        void Register<TService>(TService instance) where TService : class;
        bool TryResolve<TService>(out TService service) where TService : class;
        TService Resolve<TService>() where TService : class;
    }

    public class GameServiceContainer : IGameServiceContainer
    {
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        public void Register<TService>(TService instance) where TService : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            services[typeof(TService)] = instance;
        }

        public bool TryResolve<TService>(out TService service) where TService : class
        {
            if (services.TryGetValue(typeof(TService), out var stored) && stored is TService typed)
            {
                service = typed;
                return true;
            }

            service = null;
            return false;
        }

        public TService Resolve<TService>() where TService : class
        {
            if (TryResolve<TService>(out var service))
                return service;
            throw new InvalidOperationException($"Service of type {typeof(TService).Name} has not been registered.");
        }
    }

    public static class GameServices
    {
        public static IGameServiceContainer Container { get; private set; }

        public static void SetContainer(IGameServiceContainer container)
        {
            Container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public static void Clear()
        {
            Container = null;
        }

        public static bool TryResolve<TService>(out TService service) where TService : class
        {
            if (Container == null)
            {
                service = null;
                return false;
            }

            return Container.TryResolve(out service);
        }

        public static TService Resolve<TService>() where TService : class
        {
            if (Container == null)
                throw new InvalidOperationException("Game services have not been initialised.");

            return Container.Resolve<TService>();
        }
    }

    public class GameServicesInstaller : MonoBehaviour
    {
        [Header("Service Prefabs or Scene References")]
        [SerializeField]
        private ResourceManager resourceManagerPrefab;

        [SerializeField]
        private ResourceLogisticsManager resourceLogisticsManagerPrefab;

        private GameServiceContainer container;

        public IGameServiceContainer Container => container;

        void Awake()
        {
            container = new GameServiceContainer();
            GameServices.SetContainer(container);

            var resourceManager = EnsureInstance(resourceManagerPrefab, "ResourceManager");
            container.Register<IResourceManager>(resourceManager);

            var logisticsManager = EnsureInstance(resourceLogisticsManagerPrefab, "ResourceLogisticsManager");
            container.Register<IResourceLogisticsService>(logisticsManager);
        }

        void OnDestroy()
        {
            if (container != null && GameServices.Container == container)
            {
                GameServices.Clear();
            }
        }

        static T EnsureInstance<T>(T prefabOrInstance, string defaultName) where T : MonoBehaviour
        {
            if (prefabOrInstance == null)
            {
                var go = new GameObject(defaultName);
                return go.AddComponent<T>();
            }

            if (prefabOrInstance.gameObject.scene.IsValid())
            {
                return prefabOrInstance;
            }

            return Instantiate(prefabOrInstance);
        }
    }
}
