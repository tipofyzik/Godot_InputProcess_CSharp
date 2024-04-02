using System.Collections.Generic;
using Godot;



public partial class InputHandler {

    // Handle once pressed actions
    public delegate void ActionPressedEventHandler(string key);
    public event ActionPressedEventHandler my_action_just_pressed;
    private readonly Dictionary<string, bool> once_pressed_key_states = new Dictionary<string, bool>() {
        {"your_keybind", false},
    };
    private void process_once_pressed_key(string key, bool key_pressed) {
        if (once_pressed_key_states[key] == false && key_pressed == true) {
            once_pressed_key_states[key] = true;
            my_action_just_pressed?.Invoke(key);
        }
        else if (once_pressed_key_states[key] == true && key_pressed == false) {
            once_pressed_key_states[key] = false;
        }
    }

    // Handle held actions
    public event ActionPressedEventHandler my_action_held;
    private readonly Dictionary<string, bool> held_key_states = new Dictionary<string, bool>() {
        {"your_keybind", false},
    };
    private void process_held_key(string key, bool key_pressed) {
        if (key_pressed == true) {
            held_key_states[key] = true;
            my_action_held?.Invoke(key);
        }
        else {
            held_key_states[key] = false;
        }
    }

    // Handle scene control
    public delegate void SceneActionPressedEventHandler();
    public event SceneActionPressedEventHandler scene_action_pressed;
    private readonly Dictionary<string, bool> scene_key_states = new Dictionary<string, bool>() {
        {"your_keybind", false},
    };
    private void process_scene_pressed_key(string key, bool key_pressed) {
        if (scene_key_states[key] == false && key_pressed == true) {
            scene_key_states[key] = true;
            scene_action_pressed?.Invoke();
        }
        else if (scene_key_states[key] == true && key_pressed == false) {
            scene_key_states[key] = false;
        }
    }



    // Process pressed key
    public void process_key_state(string key, bool key_pressed) {
        if (once_pressed_key_states.ContainsKey(key)) {
            process_once_pressed_key(key, key_pressed);
        }
        /* If you have the buttom that contains in both the once_pressed_key_states and held_key_states 
        dictionaries, you can replace the second else-if-condition by if-condition  */
        else if (held_key_states.ContainsKey(key)) {
            process_held_key(key, key_pressed);
        }
        else if (scene_key_states.ContainsKey(key)) {
            process_scene_pressed_key(key, key_pressed);
        }
    }

}



public partial class Node2D : Godot.Node2D {
    public override void _Input(InputEvent @event) {
        if (@event is InputEventKey || @event is InputEventMouseButton) {
            Global.data.input_handler.process_key_state(@event.AsText(), @event.IsPressed());
        }
    }
}
