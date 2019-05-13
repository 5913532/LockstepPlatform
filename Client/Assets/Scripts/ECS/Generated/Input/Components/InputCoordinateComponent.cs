//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Entitas.CodeGeneration.Plugins.ComponentEntityApiGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
public partial class InputEntity {

    public Lockstep.Core.State.Input.CoordinateComponent coordinate { get { return (Lockstep.Core.State.Input.CoordinateComponent)GetComponent(InputComponentsLookup.Coordinate); } }
    public bool hasCoordinate { get { return HasComponent(InputComponentsLookup.Coordinate); } }

    public void AddCoordinate(Lockstep.Math.LVector2 newValue) {
        var index = InputComponentsLookup.Coordinate;
        var component = (Lockstep.Core.State.Input.CoordinateComponent)CreateComponent(index, typeof(Lockstep.Core.State.Input.CoordinateComponent));
        component.value = newValue;
        AddComponent(index, component);
    }

    public void ReplaceCoordinate(Lockstep.Math.LVector2 newValue) {
        var index = InputComponentsLookup.Coordinate;
        var component = (Lockstep.Core.State.Input.CoordinateComponent)CreateComponent(index, typeof(Lockstep.Core.State.Input.CoordinateComponent));
        component.value = newValue;
        ReplaceComponent(index, component);
    }

    public void RemoveCoordinate() {
        RemoveComponent(InputComponentsLookup.Coordinate);
    }
}

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Entitas.CodeGeneration.Plugins.ComponentMatcherApiGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
public sealed partial class InputMatcher {

    static Entitas.IMatcher<InputEntity> _matcherCoordinate;

    public static Entitas.IMatcher<InputEntity> Coordinate {
        get {
            if (_matcherCoordinate == null) {
                var matcher = (Entitas.Matcher<InputEntity>)Entitas.Matcher<InputEntity>.AllOf(InputComponentsLookup.Coordinate);
                matcher.componentNames = InputComponentsLookup.componentNames;
                _matcherCoordinate = matcher;
            }

            return _matcherCoordinate;
        }
    }
}
