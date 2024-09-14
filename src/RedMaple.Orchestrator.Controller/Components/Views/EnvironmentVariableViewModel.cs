using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Components.Views
{
    public class EnvironmentVariableViewModel
    {
        private Dictionary<string, string?> _variables;
        private List<EnvironmentVariable> _list;

        public void Remove(string key)
        {
            _variables?.Remove(key);
            _list.RemoveAll(x => x.Key == key);
        }

        public void Add(string key, string value)
        {
            _variables.Add(key, value);
            _list.Add(new EnvironmentVariable(this, key, value));
        }

        public EnvironmentVariableViewModel(Dictionary<string, string?> variables)
        {
            _variables = variables;
            _list = new List<EnvironmentVariable>(GetVariables());
        }

        public IReadOnlyList<EnvironmentVariable> Variables => _list;

        private IEnumerable<EnvironmentVariable> GetVariables()
        {
            foreach (var pair in _variables)
            {
                yield return new EnvironmentVariable(this, pair);
            }
        }

        public class EnvironmentVariable
        {
            private string _key;
            private string? _value;
            private EnvironmentVariableViewModel _owner;

            public string? Value
            {
                get => _value;
                set
                {
                    _value = value;
                    _owner._variables[Key] = value;
                }
            }
            public string Key
            {
                get => _key;
                set
                {
                    if (_owner._variables.ContainsKey(value))
                    {
                        return;
                    }

                    _owner._variables.Remove(_key);
                    _key = value;
                    _owner._variables.Add(_key, _value);
                }
            }

            public EnvironmentVariable(EnvironmentVariableViewModel owner, string key)
            {
                _key = key;
                _owner = owner;
            }
            public EnvironmentVariable(EnvironmentVariableViewModel owner, string key, string value)
            {
                _key = key;
                _value = value;
                _owner = owner;
            }
            public EnvironmentVariable(EnvironmentVariableViewModel owner, KeyValuePair<string, string?> pair)
            {
                _key = pair.Key;
                _value = pair.Value;
                _owner = owner;
            }
        }
    }
}
