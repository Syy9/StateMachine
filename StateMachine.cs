using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Syy.Logics
{
    public abstract class StateMachine<TManager, TOwner> where TManager : StateMachine<TManager, TOwner>
    {
        TOwner _owner;
        State _active;
        TransitionMap _transitionMap = new TransitionMap();

        public abstract void SetupState();

        public void SetOwner(TOwner owner)
        {
            _owner = owner;
        }

        public State CreateState<TState>() where TState : State, new()
        {
            var state = new TState();
            state.Manager = this;
            state.Owner = _owner;
            return state;
        }

        public void DefineTransition(State from, State to, Enum trigger)
        {
            _transitionMap.Define(from, to, trigger);
        }

        public void SetTrigger(Enum trigger, object arg = null)
        {
            if (_active != null)
            {
                _active.SetTrigger(trigger, arg);
                var to = _transitionMap.Transition(_active, trigger);
                if (to != null)
                {
                    if (arg != null && to is IRequireParameter)
                    {
                        ((IRequireParameter)to).Parameter = arg;
                    }
                    ChangeActive(to);
                }
            }
        }

        public void ChangeActive(State active)
        {
            _active?.Finish();
            _active = active;
            _active.Start();
        }

        public void Start()
        {
            _active = _transitionMap.First();
            _active.Start();
        }

        public void Update()
        {
            _active?.Update();
        }
    }

    public abstract class State
    {
        public object Owner;
        public object Manager;

        State _parent;
        State _active;
        TransitionMap _transitionMap = new TransitionMap();

        public void Start()
        {
            OnStart();
            _active = _transitionMap.First();
            if (_active != null)
            {
                if (this is IRequireParameter && _active is IRequireParameter)
                {
                    ((IRequireParameter)_active).Parameter = ((IRequireParameter)this).Parameter;
                }
                _active.Start();
            }
        }

        public void Update()
        {
            OnUpdate();
            _active?.Update();
        }

        public void Finish()
        {
            OnFinish();
            _active?.Finish();
            _active = null;
        }

        protected virtual void OnStart() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnFinish() { }

        public void SetParent(State parent)
        {
            _parent = parent;
        }

        public void DefineTransition(State from, State to, Enum trigger)
        {
            _transitionMap.Define(from, to, trigger);
        }

        internal void SetTrigger(Enum trigger, object arg = null)
        {
            if (_active != null)
            {
                _active.SetTrigger(trigger, arg);
                var to = _transitionMap.Transition(_active, trigger);
                if (to != null)
                {
                    if (arg != null && to is IRequireParameter)
                    {
                        ((IRequireParameter)to).Parameter = arg;
                    }
                    ChangeActive(to);
                }
            }
        }

        void ChangeActive(State active)
        {
            _active?.Finish();
            _active = active;
            _active.Start();
        }
    }

    public abstract class State<TManager, TOwner> : State where TManager : StateMachine<TManager, TOwner>
    {
        public new TManager Manager { get { return (TManager)base.Manager; } }
        public new TOwner Owner { get { return (TOwner)base.Owner; } }
    }

    public class TransitionMap
    {
        Dictionary<State, Dictionary<Enum, State>> _transitions = new Dictionary<State, Dictionary<Enum, State>>();
        public void Define(State from, State to, Enum trigger)
        {
            if (!_transitions.ContainsKey(from))
            {
                _transitions[from] = new Dictionary<Enum, State>();
            }

            _transitions[from][trigger] = to;
        }

        public State Transition(State from, Enum trigger)
        {
            if (!_transitions.ContainsKey(from) || !_transitions[from].ContainsKey(trigger))
            {
                return null;
            }

            return _transitions[from][trigger];
        }

        public State First()
        {
            return _transitions.FirstOrDefault().Key;
        }
    }

    public interface IRequireParameter
    {
        object Parameter { get; set; }
    }

    public interface IRequireParameter<T> : IRequireParameter
    {
        T CastParameter { get; }
    }
}
