#if !NOT_UNITY3D

using System;
using System.Collections.Generic;
using System.Linq;
using ModestTree;
using UnityEngine;
using Zenject;

namespace Zenject
{
    public class AddToCurrentGameObjectComponentProvider : IProvider
    {
        readonly string _concreteIdentifier;
        readonly Type _componentType;
        readonly DiContainer _container;
        readonly List<TypeValuePair> _extraArguments;

        public AddToCurrentGameObjectComponentProvider(
            DiContainer container, Type componentType,
            string concreteIdentifier, List<TypeValuePair> extraArguments)
        {
            Assert.That(componentType.DerivesFrom<Component>());

            _concreteIdentifier = concreteIdentifier;
            _extraArguments = extraArguments;
            _componentType = componentType;
            _container = container;
        }

        protected DiContainer Container
        {
            get
            {
                return _container;
            }
        }

        protected Type ComponentType
        {
            get
            {
                return _componentType;
            }
        }

        protected string ConcreteIdentifier
        {
            get
            {
                return _concreteIdentifier;
            }
        }

        public Type GetInstanceType(InjectContext context)
        {
            return _componentType;
        }

        GameObject GetGameObject(InjectContext context)
        {
            var component = context.ObjectInstance as Component;

            Assert.IsNotNull(component,
                "Object '{0}' can only be injected into MonoBehaviour's since it was bound with 'FromSiblingComponent'. Attempted to inject into non-MonoBehaviour '{1}'",
                context.MemberType, context.ObjectType);

            return component.gameObject;
        }

        public IEnumerator<List<object>> GetAllInstancesWithInjectSplit(InjectContext context, List<TypeValuePair> args)
        {
            Assert.IsNotNull(context);

            object instance;

            if (!_container.IsValidating || DiContainer.CanCreateOrInjectDuringValidation(_componentType))
            {
                var gameObj = GetGameObject(context);

                instance = gameObj.GetComponent(_componentType);

                if (instance != null)
                {
                    yield return new List<object>() { instance };
                    yield break;
                }

                instance = gameObj.AddComponent(_componentType);
            }
            else
            {
                instance = new ValidationMarker(_componentType);
            }

            // Note that we don't just use InstantiateComponentOnNewGameObjectExplicit here
            // because then circular references don't work
            yield return new List<object>() { instance };

            var injectArgs = new InjectArgs()
            {
                ExtraArgs = _extraArguments.Concat(args).ToList(),
                UseAllArgs = true,
                Context = context,
                ConcreteIdentifier = _concreteIdentifier,
            };

            _container.InjectExplicit(instance, _componentType, injectArgs);

            Assert.That(injectArgs.ExtraArgs.IsEmpty());
        }
    }
}

#endif