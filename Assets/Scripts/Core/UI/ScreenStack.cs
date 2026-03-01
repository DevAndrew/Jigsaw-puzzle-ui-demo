using System.Collections.Generic;
using UnityEngine;

namespace JigsawPrototype.Core.UI
{
    public sealed class ScreenStack
    {
        private readonly IReadOnlyDictionary<ScreenId, GameObject> _screens;
        private readonly Stack<ScreenId> _stack = new Stack<ScreenId>();

        public ScreenStack(IReadOnlyDictionary<ScreenId, GameObject> screens)
        {
            _screens = screens;
        }

        public ScreenId Current => _stack.Count > 0 ? _stack.Peek() : ScreenId.Home;

        public void Replace(ScreenId id)
        {
            _stack.Clear();
            _stack.Push(id);
            Apply();
        }

        public void Push(ScreenId id)
        {
            if (_stack.Count > 0 && _stack.Peek() == id) return;
            _stack.Push(id);
            Apply();
        }

        public void Pop()
        {
            if (_stack.Count <= 1) return;
            _stack.Pop();
            Apply();
        }

        private void Apply()
        {
            var current = _stack.Peek();
            foreach (var kv in _screens)
            {
                if (kv.Value == null) continue;
                kv.Value.SetActive(kv.Key == current);
            }
        }
    }
}

