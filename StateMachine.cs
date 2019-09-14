using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Syy.Logics
{
    public abstract class StateMachine
    {
        State _active;
        TransitionMap _transitionMap = new TransitionMap();

        public bool EnableLog { get; set; }

        public abstract void SetupState();

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
            if (EnableLog)
            {
                Debug.Log($"【{GetType().Name}】{_active?.GetType().Name} -> {active.GetType().Name}");
            }

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

        public State GetActiveState()
        {
            return _active;
        }

        public virtual string GetActiveStateInfo()
        {
            if (_active == null)
            {
                return "No Active State";
            }

            var sb = new StringBuilder();
            sb.Append(_active.GetType().Name);
            var activeChild = _active.GetActiveState();
            while (activeChild != null)
            {
                sb.AppendLine();
                sb.Append(activeChild.GetType().Name);
                activeChild = activeChild.GetActiveState();
            }
            return sb.ToString();
        }
    }

    public abstract class StateMachine<TManager, TOwner> : StateMachine where TManager : StateMachine<TManager, TOwner>
    {
        TOwner _owner;

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
    }

    public abstract class State
    {
        public object Owner;
        public StateMachine Manager;

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

                if (Manager.EnableLog)
                {
                    Debug.Log($"【{Manager.GetType().Name}】Activate : {_active?.GetType().Name}");
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
            if (Manager.EnableLog)
            {
                Debug.Log($"【{Manager.GetType().Name}】{_active?.GetType().Name} -> {active.GetType().Name}");
            }
            _active?.Finish();
            _active = active;
            _active.Start();
        }

        public State GetActiveState()
        {
            return _active;
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
