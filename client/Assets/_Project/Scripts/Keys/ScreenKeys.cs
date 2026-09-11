#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Chiki.Client.Keys
{
    /// <summary>
    /// The keys a menu screen listens to, bound by physical key through the Input System and
    /// delivered to one callback on key-down. Screens call the same callback from their public
    /// key entry point, so tests and replays drive them without a device.
    /// </summary>
    public sealed class ScreenKeys : IDisposable
    {
        private readonly List<InputAction> _actions = new List<InputAction>();

        public ScreenKeys(IEnumerable<Key> keys, Action<Key> onKeyDown)
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            if (onKeyDown is null)
            {
                throw new ArgumentNullException(nameof(onKeyDown));
            }

            foreach (var key in keys)
            {
                var captured = key;
                var action = new InputAction("screen-" + key, InputActionType.Button, InputMap.ControlPath(key));
                action.performed += _ => onKeyDown(captured);
                action.Enable();
                _actions.Add(action);
            }
        }

        public void Dispose()
        {
            foreach (var action in _actions)
            {
                action.Disable();
                action.Dispose();
            }

            _actions.Clear();
        }
    }
}
