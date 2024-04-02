# Implementation of the processing input via C# events in Godot Engine
The [Godot](https://godotengine.org/) is an open-source, free game engine. It's popular among the indie-gamedevelopers. I write my code in C# since every popular engine requires this programming language.

## Introduction to the problem
Recently, I started doing my project, however, I forced with the fact that Godot can't handle input via signals. The developer, usually, handles input from the keyboard and mouse via if-statements in a _Process function that checks statements every engine tick (typically it is 60 physics ticks per second). 

**But what's the problem?** Imagine that you have your player and let's say it has 4 special buttons: space - jump, shift - dash, mouse leftclick - fire, E-button - interact with object. Right now, you are adding this action to the input map and checking them to see if anything was pressed (code example below)  
<pre><code class='language-cs'>
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
</code></pre>

So, every tick the engine check 4 conditions in the worst case. Now, imagine that you have 3 entities and they also have for 2 conditions each. So, every tick Godot checks 4+3*2=10 conditions in the worst case. In short, the larger your game, the more conditions the game engine checks. I want to optimize this process by implementing the InputHandler class that would take an input and then send the corresponding signal to the game entities (for example, player). It will reduce engine load since this special class will have no more than a small constant number of if-statements

## Implementation
**Code is written in C#, you can transfer it in gd-script (Godot built-in language). The solution works for Godot version: 4.2.1**

**The idea**: The best thing is to read all input that the engine gets from the user and then filter it according to the keybinds, i.e., process only keys that has are connetcted to their own actions.  
**The result**: In the inplementation, I don't use input map. The main drawback here is that you need to set all your actions manually (I'll explain how to do so a little bit later). 
**Note**: I wrote this code for 2D game and I believe, everithing you need to do to make it work for 3D game is to replace Node2D : Godot.Node2D by Node3D : Godot.Node3D in the _Input method (but it should be checked, since I didn't test this). See explanation below


**How it works**:  
1. First you need to do is to create InputHandler class (how to write logic we'll discuss later)
<pre><code class='language-cs'>
public partial class InputHandler {
}  
</code></pre>
Then write a global class that contains the InputHandler instance and the constructor of this global class. It essential to use the only ine InputHandler object since we want to precess input only once  

<pre><code class='language-cs'>
public partial class Global : Node {

	public static Global data { get; set; } = new Global() { };
	public InputHandler input_handler = new InputHandler() { };
}  
</code></pre>

2. Then it's necessary to override an _Inpit method in the built-in Node2D class (represents a 2D object).  
<pre><code class='language-cs'>
public partial class Node2D : Godot.Node2D {
    public override void _Input(InputEvent @event) {
        if (@event is InputEventKey || @event is InputEventMouseButton) {
            Global.data.input_handler.process_key_state(@event.AsText(), @event.IsPressed());
        }
    }
}
</code></pre>
Here, the first filtering of buttons occurs. The _Input function, basically, calls when an input is detected (button/mouse click, mouse motion, gamepad stick moved,etc.). I require only button and mouse cliks, so here I just check whether the input suitable for me. Take into account that here we use the global instance of the InputHandler class.

Now we can write the logic for our InputHandler class.
We have 2 types of actions: "just_pressed" and "pressed". How it works in godot read [here](https://forum.godotengine.org/t/how-to-differntiate-between-is-action-just-pressed-and-is-action-pressed/8671). I explain my implementations for the "just_pressed" actions because it's a little bit tricky to implement.

**What we need to do?**  
Open your InputHandler class.  
**The first step** is to write a [delegate](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/delegates/) for this class. In our case this delegate should take only one argument - the string key that corresponds to the pressed button. Then we need to write an event for our delegate, to which other classes will subscribe. Once this event is triggered, the subscribed classes will perform the necessary actions.  
<pre><code class='language-cs'>
public partial class InputHandler {

    // Handle once pressed actions
    public delegate void ActionPressedEventHandler(string key);
    public event ActionPressedEventHandler my_action_just_pressed;
    private readonly Dictionary<string, bool> once_pressed_key_states = new Dictionary<string, bool>() {
        {"your_keybind", false},
    };
}
</code></pre>
You can also note a dictionary structure. This type is chosen for its  data [retrieval speed](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=net-8.0&redirectedfrom=MSDN#remarks:~:text=is%20not%20found.%0A*/-,Remarks,-The%20Dictionary%3CTKey) and its convenience. In this dictionary, we write keys that we want to process and their state ("true" represents pressed button and "false" represent that button is released). For example, let's say that you want to process 3 buttons: Q, E, Space, so the dicitonary will be:  
<pre><code class='language-cs'>
    private readonly Dictionary<string, bool> once_pressed_key_states = new Dictionary<string, bool>() {
        {"Q", false},
        {"E", false},    
        {"Space", false},
    };
</code></pre>

**The second step** is to write logic. We need to invoke our event if the corresponding is "just" pressed. We can inplement it via boolean flag: if our key-state in the dictionary is false (the key wasn't pressed before) and we get from the _Input method that our key is_pressed we change the state of the key in the dictionary to "true" and Invoke our event. Once key was released, we change its state back to "false".  
<pre><code class='language-cs'>
private void process_once_pressed_key(string key, bool key_pressed) {
	if (once_pressed_key_states[key] == false && key_pressed == true) {
	    once_pressed_key_states[key] = true;
	    my_action_just_pressed?.Invoke(key);
	}
	else if (once_pressed_key_states[key] == true && key_pressed == false) {
	    once_pressed_key_states[key] = false;
	}
}
</code></pre>

**The third step**: Our fucntion is written, now we need to call it when it requires. For this purpose, we write one more, general, function that will be called on out overridden _Input method (form the point 2). As I mentioned before, on the _Input method the first filtering of buttons occurs. Now, we filter it one more time  but more precisely. Our dictionary contains only the limited amount of key, so we need to chech whether the pressed key is in there. If so, we proceed and call the function written above and, consequeintly, invoke the event. Otherwise, nothing happens.  
<pre><code class='language-cs'>
public void process_key_state(string key, bool key_pressed) {
	if (once_pressed_key_states.ContainsKey(key)) {
	    process_once_pressed_key(key, key_pressed);
	}
}
</code></pre>

**The forth step**: Now we have everything ti handle our input. The last thing is to work with the classes that are subcsribed to events.  
In my project for all entities I write an abstract class that contains the basic functionality for my object. Then I inherit from it and add a unique functionality for my object. For example, I want to create a player (the user will control it). First, I write an AbstractPlayer class that contains all general methods and then I write inherited class Player : AbstractPlayer. 

AbstractPlayer class

<pre><code class='language-cs'>
public abstract partial class AbstractPlayer : CharacterBody2D {

    public string current_player_name;


    
    // Here, I handle my animations
    //Some code

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
</code></pre>

Player class  

<pre><code class='language-cs'>
public partial class Player : AbstractPlayer {

	public override void _Ready() {
		current_player_name = "player";

		play_idle(GetNode<AnimationPlayer>("AnimationPlayer"));
		set_sprite(GetNode<AnimatedSprite2D>("AnimatedSprite2D"));

		input_hadler.my_action_just_pressed += my_action_is_pressed;
	}
	// Other code

}
</code></pre>

Let's take a look at "// Here, I handle my input" section in the AbstractPlayer class.  
For our player to do the necessary actions we, first, define an InputHandler object (that is our global instance) and also define a dictionary where we store that actions we want player to do. Then we write a cinstructor of the class that contains the relationship between the button and the function we want to call. In my case this is Q-button with the function "swap_payer" that exactly swap two players (I have the original and the reflected one).  
Then we write a protected function my_action_is_pressed() (name it whatever you want) that we will call each time our event has been invoked. In this function, the necessary actions will be called. And finally we write the action functions itself (swap_player in my case). 

In the Godot each object has function [_Ready](https://docs.godotengine.org/en/stable/classes/class_node.html#class-node-private-method-ready:~:text=void%20_ready%20(%20)%20virtual) that calls once the object is intered the scene tree. Here we **must** subcsribe on the event. We can make it by writing 
<pre><code class='language-cs'>
input_hadler.my_action_just_pressed += my_action_is_pressed;
</code></pre>
where input_handler is the global InputHandler instance that we defined before and my_action_just_pressed is the event. When we write "+= my_action_is_pressed" we say that this function will be called once the event my_action_just_pressed is invoked. And in this function we call the required action by accesing it through our custom action dictionary (I defined it in the AbstractPlayer class).

**Note**: dont forget to unsubscribe from the actions if your object is exited from the scene tree (deleted). If you don't do so, the code will try to perform a fucntion for a  disposed object that will call the corresponding exception.


That's it. Now, every time the button you need is pressed, it will emit the corresponding signal, which will then trigger the necessary functions. Once again, I have depicted schematically in the picture below how it works

![Scheme](https://github.com/tipofyzik/Godot_InputProcess_CSharp/assets/84290230/5f5067b0-f3fa-48dd-9f83-0ec4fa2027c9)


## Advanced input handling
In the InputHandler file you might have noticed that I have 2 delegates and 3 events (and 3 corresponding dictionaries). This is because I handle not only "just_pressed" keys. Namely, I handle 3 types of actions: "just_pressed", "pressed" or held event and "scene" action. We considered the first one. The second event works almost in the same way but it will be called all the time when the button is held. Once you released it, the action will stop. Such event can be useful to perform such actions as assault rifle fire.  
The last event I wrote for the global buttons. Let's say, the key "Escape" is responsible for opening the pause menu. This event will come in handy here. 

**In case you decide to write your own events, don't forget about 2 thigs**:
1. Each event should be responsible for its own purpose. Don't use only one event instance to handle input, it will cause a lot of problems. Imagine that you try to handle "just_pressed" and "held" actions using only on event instance. And let's say you have 1 button in each corresponding dictionary. For example, a leftclick mouse button that corresponds to "fire" aciton. For the assault rifle you need to hold this button but for the pistol you're required to press it only once for a single shot. So, the class that corresponds for your pistol should be subcribed to "my_action_just_pressed" event and the assault riffle class shoud be subscribed to "my_action_held" event (see event in the InputHandler.cs file).
2. Always unsubscribe from the events once your objects is deleted, dropped, etc. (depends on your game logic).


## Results an further development
My input handler implementation has a big advantage comparing with the classic input handling in Godot:  
It check only a limited amount of if-statement. In the worst case, we have 6 if-statements (1 in the first filter, 3 in the second filter and 2 in the calling event). On average (if the input key is in our dictionary) we have from 3 to 5 checks. If we handle this as usual, i.e., using if/else-if statements for each action in _Process funstion for each object, it will cost computing resources. Thus is because the Godot will check this statements each engine tick for each object. In my implementation, the Godot check only the _Input function and then calls the events if required.

It has the only one (I hope so) drawback:  
You need to set up all the input manually in your classes and keep in mind which actions you want for which objects. Also, you have to remember to subscribe and unsubscribe from the event. However, once you get used to it, it will become pretty easy to you


**Further development**:
I'd glad to hear your feedback. Maybe, there is something that I didn't take into account. It works awesome for 2D games (according to my tests). You can share your experience in the comments under the post on [reddit](https://www.reddit.com/r/godot/comments/1btv1pm/implementation_of_the_processing_input_via_c/) 
