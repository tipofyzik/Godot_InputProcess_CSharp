# Implementation of the processing input via C# events in Godot Engine
The [Godot](https://godotengine.org/) is an open-source, free game engine. It's popular among the indie-gamedevelopers. I write my code in C# since every popular engine requires this programming language.

## Introduction to the problem
Recently, I started doing my project, however, I forced with the fact that Godot can't handle input via signals. The developer, usually, handles input from the keyboard and mouse via if-statements in a _Process function that checks statements every engine tick (typically it is 60 physics ticks per second). 

**But what's the problem?** Imagine that you have your player and let's say it has 4 special buttons: space - jump, shift - dash, mouse leftclick - fire, E-button - interact with object. Right now, you are adding this action to the input map and checking them to see if anything was pressed (code example below)  
```
	public override void _Process(double delta) {
		get_input();  //This function gets basic control (left, right, up, down) and sets the player velocity
		if (Input.IsActionJustPressed("jump")) {
			jump();
		}
		else if (Input.IsActionJustPressed("dash")) {
			dash();
		}
		else if (Input.IsActionPressed("fire")) {
			fire();
		}
		else if (Input.IsActionJustPressed("interact_with_object")) {
			interact_with_object();
		}
		MoveAndSlide();  //This function is responsible for character moving (see Godot documentation)
}

```
So, every tick the engine check 4 conditions in the worst case. Now, imagine that you have 3 entities and they also have for 2 conditions each. So, every tick Godot checks 4+3*2=10 conditions in the worst case. In short, the larger your game, the more conditions the game engine checks. I want to optimize this process by implementing the InputHandler class that would take an input and then send the corresponding signal to the game entities (for example, player). It will reduce engine load since this special class will have no more than a small constant number of if-statements

## Implementation
**Code is written in C#, you can transfer it in gd-script (Godot built-in language).**

**The idea**: The best thing is to read all input that the engine gets from the user and then filter it according to the keybinds, i.e., process only keys that has are connetcted to their own actions.  
**The result**: In the inplementation, I don't use input map. The main drawback here is that you need to set all your actions manually (I'll explain how to do so a little bit later). 
**Note**: I wrote this code for 2D game and I believe, everithing you need to do to make it work for 3D game is to replace Node2D : Godot.Node2D by Node3D : Godot.Node3D in the _Input method (but it should be checked, since I didn't test this). See explanation below


**How it works**:  
1. First you need to do is to create InputHandler class (how to write logic we'll discuss later)  
'''
public partial class InputHandler {
}

'''
Then write a global class that contains the InputHandler instance and the constructor of this global class. It essential to use the only ine InputHandler object since we want to precess input only once  
'''
public partial class Global : Node {

	public static Global data { get; set; } = new Global() { };
	public InputHandler input_handler = new InputHandler() { };
 }
'''

2. Then it's necessary to override an _Inpit method in the built-in Node2D class (represents a 2D object).  
'''
public partial class Node2D : Godot.Node2D {
    public override void _Input(InputEvent @event) {
        if (@event is InputEventKey || @event is InputEventMouseButton) {
            Global.data.input_handler.process_key_state(@event.AsText(), @event.IsPressed());
        }
    }
}
'''
Here, the first filtering of buttons occurs. The _Input function, basically, calls when an input is detected (button/mouse click, mouse motion, gamepad stick moved,etc.). I require only button and mouse cliks, so here I just check whether the input suitable for me. Take into account that here we use the global instance of the InputHandler class.

Now we can write the logic for our InputHandler class.
We have 2 types of actions: "just_pressed" and "pressed". How it works in godot read [here](https://forum.godotengine.org/t/how-to-differntiate-between-is-action-just-pressed-and-is-action-pressed/8671). I explain my implementations for the "just_pressed" actions because it's a little bit tricky to implement.

**What we need to do?**  
Open your InputHandler class.  
**The first step** is to write a [delegate](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/delegates/) for this class. In our case this delegate should take only one argument - the string key that corresponds to the pressed button. Then we need to write an event for our delegate, to which other classes will subscribe. Once this event is triggered, the subscribed classes will perform the necessary actions.  
'''
public partial class InputHandler {

    // Handle once pressed actions
    public delegate void ActionPressedEventHandler(string key);
    public event ActionPressedEventHandler my_action_just_pressed;
    private readonly Dictionary<string, bool> once_pressed_key_states = new Dictionary<string, bool>() {
        {"your_keybind", false},
    };
}
'''
You can also note a dictionary structure. This type is chosen for its  data [retrieval speed](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=net-8.0&redirectedfrom=MSDN#remarks:~:text=is%20not%20found.%0A*/-,Remarks,-The%20Dictionary%3CTKey) and its convenience. In this dictionary, we write keys that we want to process and their state ("true" represents pressed button and "false" represent that button is released). For example, let's say that you want to process 3 buttons: Q, E, Space, so the dicitonary will be:  
'''
    private readonly Dictionary<string, bool> once_pressed_key_states = new Dictionary<string, bool>() {
        {"Q", false},
        {"E", false},    
        {"Space", false},
    };
'''

**The second step** is to write logic. We need to invoke our event if the corresponding is "just" pressed. We can inplement it via boolean flag: if our key-state in the dictionary is false (the key wasn't pressed before) and we get from the _Input method that our key is_pressed we change the state of the key in the dictionary to "true" and Invoke our event. Once key was released, we change its state back to "false".  
'''
    private void process_once_pressed_key(string key, bool key_pressed) {
        if (once_pressed_key_states[key] == false && key_pressed == true) {
            once_pressed_key_states[key] = true;
            my_action_just_pressed?.Invoke(key);
        }
        else if (once_pressed_key_states[key] == true && key_pressed == false) {
            once_pressed_key_states[key] = false;
        }
    }
'''

**The third step**: Our fucntion is written, now we need to call it when it requires. For this purpose, we write one more, general, function that will be called on out overridden _Input method (form the point 2). As I mentioned before, on the _Input method the first filtering of buttons occurs. Now, we filter it one more time  but more precisely. Our dictionary contains only the limited amount of key, so we need to chech whether the pressed key is in there. If so, we proceed and call the function written above and, consequeintly, invoke the event. Otherwise, nothing happens.  
'''
    public void process_key_state(string key, bool key_pressed) {
        if (once_pressed_key_states.ContainsKey(key)) {
            process_once_pressed_key(key, key_pressed);
        }
}
'''

**The forth step**: Now we have everything ti handle our input. The last thing is to work with the classes that are subcsribed to events.  
In my project for all entities I write an abstract class that contains the basic functionality for my object. Then I inherit from it and add a unique functionality for my object. For example, I want to create a player (the user will control it). First, I write an AbstractPlayer class that contains all general methods and then I write inherited class Player : AbstractPlayer. 

AbstractPlayer class  
'''
public abstract partial class AbstractPlayer : CharacterBody2D {

    public string current_player_name;


    
    // Here, I handle my animations
    protected const float speed = 500.0f;
    private AnimationPlayer animation;
    private AnimatedSprite2D sprite;

    protected void play_idle(AnimationPlayer _animation) {
        animation = _animation;
        animation.Play("Idle");
    }

    protected void set_sprite(AnimatedSprite2D _sprite) {
        sprite = _sprite;
    }

    // Here, I handle my input
    protected InputHandler input_hadler = Global.data.input_handler;
    protected readonly Dictionary<string, Action> my_custom_actions = new Dictionary<string, Action>() { };

    protected AbstractPlayer() {
        my_custom_actions["Q"] = swap_player;
    }

    protected void my_action_is_pressed(string key) {
        my_custom_actions[key]();
    }

    protected void swap_player() {
        if (current_player_name == "player" && Global.data.mirrored_player_exists) {
            GlobalPosition = Global.data.mirrored_player_position;
        }
        else if (current_player_name != "player" && Global.data.mirrored_player_exists) {
            GlobalPosition = Global.data.player_position;
        }
    }

    public override void _ExitTree() {
        input_hadler.my_action_just_pressed -= my_action_is_pressed;
        QueueFree();
    }

}
'''

Player class  
'''
public partial class Player : AbstractPlayer {

	public override void _Ready() {
		current_player_name = "player";

		play_idle(GetNode<AnimationPlayer>("AnimationPlayer"));
		set_sprite(GetNode<AnimatedSprite2D>("AnimatedSprite2D"));

		input_hadler.my_action_just_pressed += my_action_is_pressed;
	}

	private void get_input() {
		Vector2 input_direction = Input.GetVector("left", "right", "up", "down");
		flip_x(input_direction.X);
		flip_y(input_direction.Y);
		Velocity = input_direction * speed;
	}

	public override void _Process(double delta) {
		get_input();
		Global.data.player_position = GlobalPosition;
		MoveAndSlide();
	}

}
'''
