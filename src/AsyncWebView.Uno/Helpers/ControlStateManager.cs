using System;
using System.Collections.Generic;
using Trigger = System.Tuple<chinook.AsyncWebView.ControlState, chinook.AsyncWebView.ControlState, System.Action>;

namespace chinook.AsyncWebView
{
	internal partial class ControlStateManager
	{
		private ControlState _state;
		private List<Trigger> _triggers = new List<Trigger>();

		public ControlStateManager()
		{
		}

		public void AddClearTrigger(ControlState inactiveStates, Action action)
		{
			AddTrigger(ControlState.None, inactiveStates, action);
		}

		public void AddSetTrigger(ControlState activeStates, Action action)
		{
			AddTrigger(activeStates, ControlState.None, action);
		}

		public void AddTrigger(ControlState activeStates, ControlState inactiveStates, Action action)
		{
			_triggers.Add(new Trigger(activeStates, inactiveStates, action));
		}

		public bool HasState(ControlState state)
		{
			return _state.HasFlag(state);
		}

		public bool SetState(ControlState state)
		{
			var previousState = _state;

			_state |= state;

			var changedStates = _state ^ previousState;

			if (changedStates != ControlState.None)
			{
				ExecuteTriggers(changedStates);

				return true;
			}

			return false;
		}

		public bool ClearState(ControlState state)
		{
			var previousState = _state;

			_state &= ControlState.All ^ state;

			var changedStates = previousState ^ _state;

			if (changedStates != ControlState.None)
			{
				ExecuteTriggers(changedStates);

				return true;
			}

			return false;
		}

		public void AddSubscription(ControlState activatedStates, ControlState deactivatedStates, Func<IDisposable> subscriptionSelector)
		{
			var subscription = default(IDisposable);

			AddSetTrigger(
				activatedStates,
				() => subscription = subscriptionSelector());

			AddClearTrigger(deactivatedStates, () => subscription?.Dispose());
		}

		public void AddSubscription(ControlState states, Func<IDisposable> subscriptionSelector)
		{
			AddSubscription(states, states, subscriptionSelector);
		}

		private void ExecuteTriggers(ControlState changedStates)
		{
			foreach (var trigger in _triggers)
			{
				// Are we concerned?
				if (((trigger.Item1 & changedStates) != ControlState.None)
					|| ((trigger.Item2 & changedStates) != ControlState.None))
				{
					// Do we match required and excluded states?
					if (_state.HasFlag(trigger.Item1)
						&& ((_state & trigger.Item2) == ControlState.None))
					{
						trigger.Item3();
					}
				}
			}
		}
	}
}
